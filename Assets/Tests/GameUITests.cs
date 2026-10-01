using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace VectorSlash.Tests
{
    public class GameUITests : PlayTestBase
    {
        [Test]
        public void UI_IsConnectedToGameManager()
        {
            GameUI ui = game.ui;
            Assert.IsNotNull(ui, "Game Manager > Ui is empty. Run Vector Slash > Build Game UI.");
            Assert.IsNotNull(ui.healthBar, "healthBar");
            Assert.IsNotNull(ui.fuelBar, "fuelBar");
            Assert.IsNotNull(ui.progressBar, "progressBar");
            Assert.IsNotNull(ui.scoreText, "scoreText");
            Assert.IsNotNull(ui.comboText, "comboText");
            Assert.IsNotNull(ui.tilesText, "tilesText");
            Assert.IsNotNull(ui.eventText, "eventText");
            Assert.IsNotNull(ui.messagePanel, "messagePanel");
            Assert.IsNotNull(ui.messageText, "messageText");
        }

        [UnityTest]
        public IEnumerator HUD_FollowsShipState()
        {
            game.Health = 40f;
            game.Fuel = 25f;
            game.Elapsed = game.levelDuration * 0.5f;
            yield return Swipe(new Vector2(-1.5f, 0f), new Vector2(1.5f, 0f));

            GameUI ui = game.ui;
            Assert.AreEqual(0.4f, ui.healthBar.value, 0.001f);
            Assert.AreEqual(0.25f, ui.fuelBar.value, 0.001f);
            Assert.AreEqual(0.5f, ui.progressBar.value, 0.01f);
            Assert.AreEqual($"Tiles 1 / {slasher.maxTiles}", ui.tilesText.text);
            Assert.IsFalse(ui.messagePanel.activeSelf, "The message panel must be hidden during play.");
        }

        [UnityTest]
        public IEnumerator HUD_ShowsScoreComboAndEvents()
        {
            PlaceMeteor(meteorPrefab, new Vector2(-0.8f, 1f), Vector2.zero);
            PlaceMeteor(meteorPrefab, new Vector2(0.8f, 1f), Vector2.zero);
            yield return null;

            SlashNow(new Vector2(-1.6f, 1f), new Vector2(1.6f, 1f));
            yield return null;

            GameUI ui = game.ui;
            StringAssert.StartsWith($"Score {game.Score}\n", ui.scoreText.text);
            StringAssert.StartsWith("Combo 2", ui.comboText.text);
            Assert.IsTrue(ui.eventText.enabled);
            StringAssert.Contains("2x SLICE", ui.eventText.text);
        }

        [UnityTest]
        public IEnumerator HullHit_ShowsEvent()
        {
            PlaceMeteor(meteorPrefab, ShipPosition + new Vector2(0f, 3f), new Vector2(0f, -6f));
            yield return WaitFor(() => game.Health < game.maxHealth, 2f);
            yield return null;

            StringAssert.StartsWith("Hull hit", game.ui.eventText.text);
            Assert.IsTrue(game.ui.eventText.enabled);
            Assert.Less(game.ui.healthBar.value, 1f);
        }

        [UnityTest]
        public IEnumerator Loss_ShowsResults()
        {
            game.Fuel = 0f;
            yield return null;
            yield return null;

            GameUI ui = game.ui;
            Assert.IsTrue(ui.messagePanel.activeSelf);
            StringAssert.StartsWith("OUT OF FUEL", ui.messageText.text);
            StringAssert.Contains("Score 0", ui.messageText.text);
            Assert.IsFalse(ui.eventText.enabled);
        }

        [UnityTest]
        public IEnumerator Win_ShowsResults()
        {
            game.Elapsed = game.levelDuration;
            yield return null;
            yield return null;

            Assert.IsTrue(game.ui.messagePanel.activeSelf);
            StringAssert.StartsWith("DESTINATION REACHED", game.ui.messageText.text);
            StringAssert.Contains("Distance 100%", game.ui.messageText.text);
        }
    }

    public class TitleScreenTests : PlayTestBase
    {
        protected override bool StartsRun => false;

        [UnityTest]
        public IEnumerator TitleScreen_ShowsBeforeFirstRun()
        {
            yield return null;

            Assert.AreEqual(GameState.Title, game.State);
            Assert.IsTrue(game.ui.messagePanel.activeSelf);
            StringAssert.StartsWith("VECTOR SLASH", game.ui.messageText.text);
            StringAssert.Contains("Click to start", game.ui.messageText.text);
        }

        [UnityTest]
        public IEnumerator StartingRun_HidesTitleScreen()
        {
            game.StartGame();
            yield return null;

            Assert.IsFalse(game.ui.messagePanel.activeSelf);
            Assert.AreEqual(1f, game.ui.healthBar.value, 0.001f);
            Assert.AreEqual(game.startFuel / game.maxFuel, game.ui.fuelBar.value, 0.001f);
        }
    }
}
