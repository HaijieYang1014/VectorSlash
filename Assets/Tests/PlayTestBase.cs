using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace VectorSlash.Tests
{
    /// <summary>
    /// Loads the game scene fresh for every test and starts a run with no random meteors and no fuel drain,
    /// so each test places exactly the meteors and fragments it needs.
    /// Keep the score at 0 in tests that end a run, so they never overwrite the saved high score.
    /// </summary>
    public abstract class PlayTestBase
    {
        protected GameManager game;
        protected SlashController slasher;
        protected Meteor meteorPrefab;
        protected Meteor largeMeteorPrefab;
        protected Fragment fragmentPrefab;

        protected virtual bool StartsRun => true;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("VectorSlash", LoadSceneMode.Single);
            yield return null;

            game = GameManager.Instance;
            Assert.IsNotNull(game, "The VectorSlash scene needs a Game Manager.");
            slasher = game.slasher;
            slasher.pointerInputEnabled = false;
            game.spawner.enabled = false;
            game.fuelDrainPerSecond = 0f;

            foreach (MeteorSpawnOption option in game.spawner.meteors)
            {
                if (option.prefab == null) continue;
                if (option.prefab.NeedsAnotherCut) largeMeteorPrefab = option.prefab;
                else meteorPrefab = option.prefab;
            }
            Assert.IsNotNull(meteorPrefab, "The spawner needs a normal meteor option.");
            Assert.IsNotNull(largeMeteorPrefab, "The spawner needs a large meteor option.");
            fragmentPrefab = meteorPrefab.piecePrefab.GetComponent<Fragment>();

            if (StartsRun)
            {
                game.StartGame();
                yield return null;
            }
        }

        /// <summary>Spawns a meteor through the spawner, then moves it to a known spot and speed.</summary>
        protected Meteor PlaceMeteor(Meteor prefab, Vector2 position, Vector2 velocity)
        {
            Meteor meteor = game.spawner.Spawn(prefab, 1f);
            Teleport(meteor.GetComponent<Rigidbody2D>(), position, velocity);
            return meteor;
        }

        protected Fragment LaunchFragment(Vector2 position, Vector2 velocity, MeteorFamily family = null)
        {
            Fragment fragment = Object.Instantiate(fragmentPrefab, position, Quaternion.identity);
            fragment.Launch(velocity, family ?? new MeteorFamily());
            return fragment;
        }

        protected static void Teleport(Rigidbody2D body, Vector2 position, Vector2 velocity)
        {
            body.transform.position = position;
            body.position = position;
            body.linearVelocity = velocity;
            body.angularVelocity = 0f;
        }

        /// <summary>A whole slash in one go: press at <paramref name="from"/>, drag to <paramref name="to"/>, release.</summary>
        protected void SlashNow(Vector2 from, Vector2 to)
        {
            slasher.BeginSlash(from);
            slasher.MoveSlash(to);
            slasher.EndSlash();
        }

        /// <summary>A slash spread over frames, so the controller's own Update does the cutting.</summary>
        protected IEnumerator Swipe(Vector2 from, Vector2 to)
        {
            slasher.BeginSlash(from);
            yield return null;
            slasher.MoveSlash(to);
            yield return null;
            slasher.EndSlash();
            yield return null;
        }

        /// <summary>A slash along the line through the current meteors (works for the two halves of a large meteor).</summary>
        protected void SlashThroughAllMeteors()
        {
            Assert.AreEqual(2, Meteor.Active.Count);
            Vector2 a = Meteor.Active[0].transform.position;
            Vector2 b = Meteor.Active[1].transform.position;
            Vector2 mid = (a + b) * 0.5f;
            Vector2 along = (a - b).normalized;
            float reach = (a - b).magnitude * 0.5f + Meteor.Active[0].Radius + 0.2f;
            Assert.LessOrEqual(reach * 2f, slasher.maxTileLength, "The pieces drifted too far apart for one slash.");
            SlashNow(mid + along * reach, mid - along * reach);
        }

        protected static IEnumerator WaitFor(System.Func<bool> condition, float timeout = 3f)
        {
            float end = Time.time + timeout;
            while (!condition() && Time.time < end) yield return null;
        }

        protected Vector2 ShipPosition => game.ship.transform.position;
    }
}
