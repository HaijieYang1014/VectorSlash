using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace VectorSlash.Tests
{
    public class GameFlowTests : PlayTestBase
    {
        [UnityTest]
        public IEnumerator Meteor_HittingShip_DamagesHull()
        {
            yield return MeteorHitsShip(meteorPrefab);
        }

        [UnityTest]
        public IEnumerator LargeMeteor_HittingShip_DamagesHull()
        {
            yield return MeteorHitsShip(largeMeteorPrefab);
        }

        IEnumerator MeteorHitsShip(Meteor prefab)
        {
            PlaceMeteor(prefab, ShipPosition + new Vector2(0f, 3f), new Vector2(0f, -6f));

            yield return WaitFor(() => game.Health < game.maxHealth, 2f);

            Assert.AreEqual(game.maxHealth - prefab.damage, game.Health, 0.001f);
            Assert.AreEqual(0, Meteor.Active.Count, "The meteor should be gone after hitting the ship.");
        }

        [UnityTest]
        public IEnumerator Fragment_HittingShip_DamagesHull()
        {
            LaunchFragment(ShipPosition + new Vector2(0f, 2.5f), new Vector2(0f, -6f));

            yield return WaitFor(() => game.Health < game.maxHealth, 2f);

            Assert.AreEqual(game.maxHealth - fragmentPrefab.damage, game.Health, 0.001f);
            Assert.AreEqual(0, Fragment.Active.Count);
        }

        [UnityTest]
        public IEnumerator Fragment_ReachingCollector_AddsFuelAndScore()
        {
            FuelCollector collector = Object.FindAnyObjectByType<FuelCollector>();
            float fuelBefore = game.Fuel;
            LaunchFragment((Vector2)collector.transform.position + new Vector2(0f, 2.5f), new Vector2(0f, -4f));

            yield return WaitFor(() => game.FragmentsCollected > 0, 2f);

            Assert.AreEqual(1, game.FragmentsCollected);
            Assert.AreEqual(fuelBefore + fragmentPrefab.fuel, game.Fuel, 0.001f);
            Assert.AreEqual(fragmentPrefab.points, game.Score);
            Assert.AreEqual(game.maxHealth, game.Health, "Collecting must not hurt the ship.");
        }

        [UnityTest]
        public IEnumerator BothCollectors_AcceptFragments()
        {
            FuelCollector[] collectors = Object.FindObjectsByType<FuelCollector>(FindObjectsSortMode.None);
            Assert.AreEqual(2, collectors.Length);
            foreach (FuelCollector collector in collectors)
                LaunchFragment((Vector2)collector.transform.position + new Vector2(0f, 2.5f), new Vector2(0f, -4f));

            yield return WaitFor(() => game.FragmentsCollected == 2, 2f);

            Assert.AreEqual(2, game.FragmentsCollected);
        }

        [UnityTest]
        public IEnumerator CollectingEveryPieceOfLargeMeteor_GivesPerfectSplit()
        {
            PlaceMeteor(largeMeteorPrefab, new Vector2(0f, 1f), Vector2.zero);
            yield return null;
            SlashNow(new Vector2(-1.6f, 1f), new Vector2(1.6f, 1f));
            yield return null;
            SlashThroughAllMeteors();
            Assert.AreEqual(4, Fragment.Active.Count);
            float fuelBefore = game.Fuel;

            // Drop every fragment straight into a collector.
            Vector2 intake = Object.FindAnyObjectByType<FuelCollector>().transform.position;
            Fragment[] fragments = Fragment.Active.ToArray();
            for (int i = 0; i < fragments.Length; i++)
                Teleport(fragments[i].Body, intake + new Vector2(0.15f * i - 0.2f, 0f), Vector2.zero);

            yield return WaitFor(() => game.PerfectSplits > 0, 2f);

            Assert.AreEqual(4, game.FragmentsCollected);
            Assert.AreEqual(1, game.PerfectSplits);
            float expected = Mathf.Min(game.maxFuel, fuelBefore + 4 * fragmentPrefab.fuel + game.perfectSplitFuel);
            Assert.AreEqual(expected, game.Fuel, 0.001f);
        }

        [UnityTest]
        public IEnumerator ReachingDestination_WinsRun()
        {
            game.Elapsed = game.levelDuration - 0.05f;

            yield return WaitFor(() => game.State != GameState.Playing, 2f);

            Assert.AreEqual(GameState.Won, game.State);
            Assert.AreEqual("DESTINATION REACHED", game.EndReason);
        }

        [UnityTest]
        public IEnumerator HullDestroyed_LosesRun()
        {
            game.Health = 5f;
            PlaceMeteor(meteorPrefab, ShipPosition + new Vector2(0f, 3f), new Vector2(0f, -6f));

            yield return WaitFor(() => game.State != GameState.Playing, 2f);

            Assert.AreEqual(GameState.Lost, game.State);
            Assert.AreEqual("SHIP DESTROYED", game.EndReason);
            Assert.AreEqual(0f, game.Health);
        }

        [UnityTest]
        public IEnumerator FuelRunningOut_LosesRun()
        {
            game.Fuel = 0.2f;
            game.fuelDrainPerSecond = 2f;

            yield return WaitFor(() => game.State != GameState.Playing, 2f);

            Assert.AreEqual(GameState.Lost, game.State);
            Assert.AreEqual("OUT OF FUEL", game.EndReason);
        }

        [UnityTest]
        public IEnumerator NothingHappens_AfterRunEnds()
        {
            game.Fuel = 0f;
            yield return null;
            Assert.AreEqual(GameState.Lost, game.State);

            LaunchFragment(ShipPosition + new Vector2(0f, 2.5f), new Vector2(0f, -6f));
            yield return new WaitForSeconds(1f);

            Assert.AreEqual(game.maxHealth, game.Health, "The ship can't be hurt once the run is over.");
            yield return Swipe(new Vector2(-1.5f, 0f), new Vector2(1.5f, 0f));
            Assert.AreEqual(0, slasher.Tiles.Count, "No slashing once the run is over.");
        }

        [UnityTest]
        public IEnumerator Restart_ResetsRun()
        {
            game.Health = 0f;
            yield return null;
            Assert.AreEqual(GameState.Lost, game.State);
            PlaceMeteor(meteorPrefab, new Vector2(0f, 2f), Vector2.zero);
            yield return Swipe(new Vector2(-1.5f, -1f), new Vector2(1.5f, -1f));

            game.StartGame();
            yield return null;

            Assert.AreEqual(GameState.Playing, game.State);
            Assert.AreEqual(game.maxHealth, game.Health);
            Assert.AreEqual(game.startFuel, game.Fuel);
            Assert.AreEqual(0f, game.Progress, 0.01f);
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(0, Meteor.Active.Count);
            Assert.AreEqual(0, Fragment.Active.Count);
            Assert.AreEqual(0, slasher.Tiles.Count);
        }
    }
}
