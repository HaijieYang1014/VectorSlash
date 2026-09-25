using UnityEngine;

namespace VectorSlash
{
    [System.Serializable]
    public class MeteorSpawnOption
    {
        public Meteor prefab;
        [Tooltip("Relative chance of picking this meteor.")]
        public float weight = 1f;
        [Tooltip("This meteor only appears once the level is this far along (0 = from the start, 0.5 = halfway).")]
        [Range(0f, 1f)] public float appearsAfterProgress;
    }

    /// <summary>Spawns meteors above the screen aimed at the ship. They get faster and more frequent as the level goes on.</summary>
    public class MeteorSpawner : MonoBehaviour
    {
        public MeteorSpawnOption[] meteors;
        [Tooltip("Meteors aim roughly at this (drag the Ship here).")]
        public Transform target;
        [Tooltip("Meteors aim up to this far left or right of the target.")]
        public float aimSpread = 2.5f;

        [Header("Difficulty over the level")]
        public float firstSpawnDelay = 1.5f;
        [Tooltip("Seconds between meteors at the start / end of the level.")]
        public float startInterval = 2.4f;
        public float endInterval = 1f;
        [Tooltip("Meteor speed at the start / end of the level.")]
        public float startSpeed = 2f;
        public float endSpeed = 3.4f;
        [Tooltip("Stop spawning near the end so the arrival is clean.")]
        [Range(0f, 1f)] public float stopSpawningAt = 0.95f;

        Camera cam;
        float timer;

        void Awake()
        {
            cam = Camera.main;
            ResetSpawner();
        }

        public void ResetSpawner() => timer = firstSpawnDelay;

        void Update()
        {
            GameManager game = GameManager.Instance;
            if (game == null || game.State != GameState.Playing) return;

            float progress = game.Progress;
            if (progress >= stopSpawningAt) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = Mathf.Lerp(startInterval, endInterval, progress) * Random.Range(0.8f, 1.2f);

            Meteor prefab = Pick(progress);
            if (prefab != null) Spawn(prefab, Mathf.Lerp(startSpeed, endSpeed, progress));
        }

        public Meteor Spawn(Meteor prefab, float speed)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            Vector2 centre = cam.transform.position;
            var position = new Vector2(centre.x + Random.Range(-halfWidth + 1f, halfWidth - 1f), centre.y + halfHeight + 1f);

            Vector2 aim = target != null ? (Vector2)target.position : centre + Vector2.down * halfHeight;
            aim.x += Random.Range(-aimSpread, aimSpread);

            Meteor meteor = Instantiate(prefab, position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            var family = new MeteorFamily { multiCut = prefab.NeedsAnotherCut };
            meteor.Launch((aim - position).normalized * (speed * prefab.speedMultiplier), family, -1);
            return meteor;
        }

        Meteor Pick(float progress)
        {
            float total = 0f;
            foreach (MeteorSpawnOption option in meteors)
                if (Available(option, progress)) total += option.weight;
            if (total <= 0f) return null;

            float roll = Random.Range(0f, total);
            foreach (MeteorSpawnOption option in meteors)
            {
                if (!Available(option, progress)) continue;
                roll -= option.weight;
                if (roll <= 0f) return option.prefab;
            }
            return null;
        }

        static bool Available(MeteorSpawnOption option, float progress) =>
            option.prefab != null && option.weight > 0f && progress >= option.appearsAfterProgress;
    }
}
