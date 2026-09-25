using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VectorSlash
{
    /// <summary>
    /// Shows the game state on a uGUI Canvas. Every field is optional: hook up whatever you've made.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("Bars (UI > Slider)")]
        public Slider healthBar;
        public Slider fuelBar;
        public Slider progressBar;

        [Header("Text (UI > Text - TextMeshPro)")]
        public TMP_Text scoreText;
        public TMP_Text comboText;
        public TMP_Text tilesText;
        [Tooltip("Shows short messages like \"PERFECT SPLIT!\" for a moment.")]
        public TMP_Text eventText;

        [Header("Title / results screen")]
        [Tooltip("A Panel shown when the game isn't running.")]
        public GameObject messagePanel;
        public TMP_Text messageText;

        const string TitleMessage =
            "VECTOR SLASH\n\n" +
            "Drag across meteors to slice them.\n" +
            "Release to leave the slash behind as an energy tile (3 s, max 3).\n" +
            "Bounce the fragments into the fuel collectors.\n" +
            "Intact meteors break tiles. Big meteors need two cuts.\n" +
            "Keep the ship alive and fueled until it arrives.\n\n" +
            "Click to start";

        float eventTimer;

        public void ShowEvent(string text)
        {
            if (eventText == null) return;
            eventText.text = text;
            eventTimer = 1.5f;
        }

        void Update()
        {
            GameManager game = GameManager.Instance;
            if (game == null) return;

            if (healthBar != null) healthBar.value = game.Health / game.maxHealth;
            if (fuelBar != null) fuelBar.value = game.Fuel / game.maxFuel;
            if (progressBar != null) progressBar.value = game.Progress;

            if (scoreText != null) scoreText.text = $"Score {game.Score}\nBest {game.HighScore}";
            if (comboText != null) comboText.text = game.ComboCount > 1 ? $"Combo {game.ComboCount}  x{game.ComboMultiplier}" : "";
            if (tilesText != null && game.slasher != null)
                tilesText.text = $"Tiles {game.slasher.Tiles.Count} / {game.slasher.maxTiles}";

            if (eventText != null)
            {
                eventTimer -= Time.deltaTime;
                eventText.enabled = eventTimer > 0f && game.State == GameState.Playing;
            }

            bool showPanel = game.State != GameState.Playing;
            if (messagePanel != null && messagePanel.activeSelf != showPanel) messagePanel.SetActive(showPanel);
            if (messageText != null && showPanel) messageText.text = Message(game);
        }

        static string Message(GameManager game)
        {
            if (game.State == GameState.Title) return TitleMessage;

            string best = game.NewHighScore ? "NEW BEST!" : $"Best {game.HighScore}";
            string again = game.StateTime > 1f ? "Click to fly again" : "";
            return $"{game.EndReason}\n\n" +
                   $"Score {game.Score}    {best}\n" +
                   $"Meteors sliced {game.MeteorsSliced}    Fragments collected {game.FragmentsCollected}\n" +
                   $"Perfect splits {game.PerfectSplits}    Tiles shattered {game.TilesShattered}\n" +
                   $"Hull {game.Health:0}    Fuel {game.Fuel:0}    Distance {game.Progress * 100f:0}%\n\n" +
                   again;
        }
    }
}
