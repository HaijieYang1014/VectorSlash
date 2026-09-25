using UnityEngine;
using UnityEngine.InputSystem;

namespace VectorSlash
{
    public enum GameState { Title, Playing, Won, Lost }

    /// <summary>
    /// Runs one flight: ship hull and fuel, level progress, score and combos, and the
    /// title / win / lose flow. Put exactly one in the scene.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        const string HighScoreKey = "VectorSlash.HighScore";

        public static GameManager Instance { get; private set; }

        [Header("References (found automatically if left empty)")]
        public Ship ship;
        public MeteorSpawner spawner;
        public SlashController slasher;
        public GameUI ui;

        [Header("Level")]
        [Tooltip("Seconds until the ship reaches its destination.")]
        public float levelDuration = 75f;

        [Header("Ship")]
        public float maxHealth = 100f;
        public float maxFuel = 100f;
        public float startFuel = 65f;
        public float fuelDrainPerSecond = 1.4f;

        [Header("Scoring")]
        public int slicePoints = 10;
        [Tooltip("Extra points per tile bounce when a fragment is collected.")]
        public int ricochetPoints = 15;
        [Tooltip("Bonus for cutting two or more meteors with one slash.")]
        public int multiSlicePoints = 30;
        [Tooltip("Bonus for collecting every fragment of a meteor that needed two cuts.")]
        public int perfectSplitPoints = 150;
        public float perfectSplitFuel = 10f;
        [Tooltip("Slices and collections within this many seconds of each other build the combo.")]
        public float comboWindow = 2f;

        public GameState State { get; private set; } = GameState.Title;
        public float StateTime { get; private set; }
        public float Health { get; set; }
        public float Fuel { get; set; }
        public float Elapsed { get; set; }
        public float Progress => Mathf.Clamp01(Elapsed / levelDuration);
        public int Score { get; private set; }
        public int HighScore { get; private set; }
        public bool NewHighScore { get; private set; }
        public int ComboCount { get; private set; }
        public int ComboMultiplier => Mathf.Min(1 + ComboCount / 4, 4);
        public string EndReason { get; private set; } = "";

        public int MeteorsSliced { get; private set; }
        public int FragmentsCollected { get; private set; }
        public int PerfectSplits { get; private set; }
        public int TilesShattered { get; private set; }

        Camera cam;
        float comboTimer;
        int lastCutSlashId = -1;
        int cutsInSlash;

        void Awake()
        {
            Instance = this;
            cam = Camera.main;
            if (ship == null) ship = FindAnyObjectByType<Ship>();
            if (spawner == null) spawner = FindAnyObjectByType<MeteorSpawner>();
            if (slasher == null) slasher = FindAnyObjectByType<SlashController>();
            if (ui == null) ui = FindAnyObjectByType<GameUI>();
            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            ResetRun();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            StateTime += Time.deltaTime;
            Keyboard keyboard = Keyboard.current;

            if (State == GameState.Playing)
            {
                Elapsed += Time.deltaTime;
                Fuel = Mathf.Max(0f, Fuel - fuelDrainPerSecond * Time.deltaTime);
                if (ComboCount > 0)
                {
                    comboTimer -= Time.deltaTime;
                    if (comboTimer <= 0f) ComboCount = 0;
                }
                CheckEndConditions();
                if (keyboard != null && keyboard.rKey.wasPressedThisFrame) StartGame();
            }
            else if (StartPressed(keyboard) && (State == GameState.Title || StateTime > 1f))
            {
                StartGame();
            }
        }

        static bool StartPressed(Keyboard keyboard)
        {
            Pointer pointer = Pointer.current;
            return (pointer != null && pointer.press.wasPressedThisFrame)
                || (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame
                                         || keyboard.rKey.wasPressedThisFrame));
        }

        public void StartGame()
        {
            ResetRun();
            SetState(GameState.Playing);
            if (slasher != null) slasher.IgnoreUntilRelease();
        }

        void SetState(GameState state)
        {
            State = state;
            StateTime = 0f;
        }

        void ResetRun()
        {
            foreach (Meteor meteor in Meteor.Active.ToArray()) meteor.Despawn();
            foreach (Fragment fragment in Fragment.Active.ToArray()) fragment.Despawn();
            if (slasher != null) slasher.ClearTiles();
            if (spawner != null) spawner.ResetSpawner();

            Health = maxHealth;
            Fuel = startFuel;
            Elapsed = 0f;
            Score = 0;
            ComboCount = 0;
            comboTimer = 0f;
            NewHighScore = false;
            EndReason = "";
            MeteorsSliced = FragmentsCollected = PerfectSplits = TilesShattered = 0;
            lastCutSlashId = -1;
            cutsInSlash = 0;
        }

        void CheckEndConditions()
        {
            if (Health <= 0f) EndRun(false, "SHIP DESTROYED");
            else if (Fuel <= 0f) EndRun(false, "OUT OF FUEL");
            else if (Elapsed >= levelDuration) EndRun(true, "DESTINATION REACHED");
        }

        void EndRun(bool won, string reason)
        {
            EndReason = reason;
            if (Score > HighScore)
            {
                HighScore = Score;
                NewHighScore = true;
                PlayerPrefs.SetInt(HighScoreKey, HighScore);
                PlayerPrefs.Save();
            }
            SetState(won ? GameState.Won : GameState.Lost);
        }

        /// <summary>True when a position is far enough outside the camera view to be gone for good.</summary>
        public bool IsOffscreen(Vector2 position)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            Vector2 c = cam.transform.position;
            return position.x < c.x - halfWidth - 2f || position.x > c.x + halfWidth + 2f
                || position.y < c.y - halfHeight - 2f || position.y > c.y + halfHeight + 4f;
        }

        // ----- Called by meteors, fragments, tiles and collectors -----

        public void AddPiece(MeteorFamily family)
        {
            if (family != null) family.alive++;
        }

        /// <summary>A meteor or fragment left play. <paramref name="failed"/> means it hit the ship or flew away.</summary>
        public void RemovePiece(MeteorFamily family, bool failed)
        {
            if (family == null) return;
            family.alive--;
            if (failed) family.failed = true;

            bool perfect = family.alive == 0 && family.multiCut && !family.failed
                && family.fragments > 0 && family.collected == family.fragments;
            if (perfect && State == GameState.Playing)
            {
                PerfectSplits++;
                Fuel = Mathf.Min(maxFuel, Fuel + perfectSplitFuel);
                ShowEvent($"PERFECT SPLIT!  +{AddScore(perfectSplitPoints)}");
            }
        }

        public void MeteorSliced(int slashId, bool needsAnotherCut)
        {
            MeteorsSliced++;
            RegisterCombo();
            int points = AddScore(slicePoints);

            if (slashId != lastCutSlashId)
            {
                lastCutSlashId = slashId;
                cutsInSlash = 0;
            }
            cutsInSlash++;

            if (cutsInSlash >= 2) ShowEvent($"{cutsInSlash}x SLICE!  +{AddScore(multiSlicePoints * (cutsInSlash - 1))}");
            else if (needsAnotherCut) ShowEvent("Cracked! Cut the pieces again");
            else ShowEvent($"Slice  +{points}");
        }

        public void CollectFragment(Fragment fragment, FuelCollector collector)
        {
            if (State != GameState.Playing) return;

            Fuel = Mathf.Min(maxFuel, Fuel + fragment.fuel);
            RegisterCombo();
            int points = AddScore(fragment.points + ricochetPoints * fragment.Bounces);
            FragmentsCollected++;
            if (fragment.Family != null) fragment.Family.collected++;
            collector.Pulse();

            ShowEvent(fragment.Bounces >= 2 ? $"RICOCHET x{fragment.Bounces}!  +{points}" : $"Fuel +{fragment.fuel:0}  +{points}");
            fragment.Collect();
        }

        public void ShipHit(float damage)
        {
            if (State != GameState.Playing) return;
            Health = Mathf.Max(0f, Health - damage);
            ComboCount = 0;
            if (ship != null) ship.Flash();
            ShowEvent($"Hull hit  -{damage:0}");
        }

        public void TileShattered()
        {
            TilesShattered++;
            ShowEvent("Tile shattered by a meteor!");
        }

        void RegisterCombo()
        {
            ComboCount++;
            comboTimer = comboWindow;
        }

        int AddScore(int basePoints)
        {
            int points = basePoints * ComboMultiplier;
            Score += points;
            return points;
        }

        void ShowEvent(string text)
        {
            if (ui != null) ui.ShowEvent(text);
        }
    }
}
