using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace VectorSlash.Tests
{
    public class SlashTests : PlayTestBase
    {
        [UnityTest]
        public IEnumerator SlashThroughMeteor_SplitsItIntoFragments()
        {
            PlaceMeteor(meteorPrefab, new Vector2(0f, 1f), Vector2.zero);

            yield return Swipe(new Vector2(-1.5f, 1f), new Vector2(1.5f, 1f));

            Assert.AreEqual(0, Meteor.Active.Count);
            Assert.AreEqual(meteorPrefab.pieceCount, Fragment.Active.Count);
            Assert.AreEqual(1, game.MeteorsSliced);
            Assert.Greater(game.Score, 0);
        }

        [UnityTest]
        public IEnumerator KeepDraggingAfterCut_LeavesNoTile()
        {
            PlaceMeteor(meteorPrefab, new Vector2(0f, 1f), Vector2.zero);

            slasher.BeginSlash(new Vector2(-1.5f, 1f));
            yield return null;
            slasher.MoveSlash(new Vector2(1.5f, 1f));
            yield return null;
            Assert.AreEqual(1, game.MeteorsSliced, "The blade should cut while the button is still held.");
            Assert.IsTrue(slasher.HasCut);

            // Keep holding and drag on into empty space, long enough for a full tile, then let go.
            slasher.MoveSlash(new Vector2(3f, -1f));
            yield return null;
            slasher.MoveSlash(new Vector2(4f, -2.5f));
            yield return null;
            slasher.EndSlash();
            yield return null;

            Assert.AreEqual(0, slasher.Tiles.Count);
        }

        [UnityTest]
        public IEnumerator SlashThroughEmptySpace_LeavesTile()
        {
            yield return Swipe(new Vector2(-1.5f, 0f), new Vector2(1.5f, 0f));

            Assert.AreEqual(1, slasher.Tiles.Count);
            Assert.IsFalse(slasher.HasCut);
            Assert.AreEqual(3f, slasher.Tiles[0].transform.localScale.x, 0.01f);
        }

        [UnityTest]
        public IEnumerator ShortSlash_LeavesNoTile()
        {
            yield return Swipe(Vector2.zero, new Vector2(slasher.minTileLength * 0.5f, 0f));

            Assert.AreEqual(0, slasher.Tiles.Count);
        }

        [UnityTest]
        public IEnumerator NextSlashAfterCut_LeavesTileAgain()
        {
            PlaceMeteor(meteorPrefab, new Vector2(0f, 1f), Vector2.zero);
            yield return Swipe(new Vector2(-1.5f, 1f), new Vector2(1.5f, 1f));
            Assert.AreEqual(0, slasher.Tiles.Count);

            yield return Swipe(new Vector2(-1.5f, -1.5f), new Vector2(1.5f, -1.5f));

            Assert.AreEqual(1, slasher.Tiles.Count);
        }

        [UnityTest]
        public IEnumerator OneSlashThroughTwoMeteors_CutsBoth()
        {
            PlaceMeteor(meteorPrefab, new Vector2(-0.8f, 1f), Vector2.zero);
            PlaceMeteor(meteorPrefab, new Vector2(0.8f, 1f), Vector2.zero);
            yield return null;

            SlashNow(new Vector2(-1.6f, 1f), new Vector2(1.6f, 1f));

            Assert.AreEqual(2, game.MeteorsSliced);
            Assert.AreEqual(0, Meteor.Active.Count);
            Assert.AreEqual(0, slasher.Tiles.Count);
        }

        [UnityTest]
        public IEnumerator LargeMeteor_NeedsTwoCuts()
        {
            PlaceMeteor(largeMeteorPrefab, new Vector2(0f, 1f), Vector2.zero);

            yield return Swipe(new Vector2(-1.6f, 1f), new Vector2(1.6f, 1f));

            // First cut: two smaller meteors, which the same slash couldn't cut again.
            Assert.AreEqual(largeMeteorPrefab.pieceCount, Meteor.Active.Count);
            Assert.AreEqual(0, Fragment.Active.Count);
            Assert.AreEqual(0, slasher.Tiles.Count);

            // Second cut, through both pieces.
            SlashThroughAllMeteors();

            Assert.AreEqual(0, Meteor.Active.Count);
            Assert.AreEqual(4, Fragment.Active.Count);
            Assert.AreEqual(3, game.MeteorsSliced);
        }

        [UnityTest]
        public IEnumerator Fragment_BouncesOffTile()
        {
            yield return Swipe(new Vector2(-1.5f, 0f), new Vector2(1.5f, 0f));
            Fragment fragment = LaunchFragment(new Vector2(0f, 1.5f), new Vector2(0f, -4f));

            yield return WaitFor(() => fragment.Bounces > 0, 2f);

            Assert.AreEqual(1, fragment.Bounces);
            Assert.Greater(fragment.Body.linearVelocity.y, 0f);
            Assert.AreEqual(1, slasher.Tiles.Count, "Fragments bounce off tiles without breaking them.");
        }

        [UnityTest]
        public IEnumerator IntactMeteor_ShattersTile()
        {
            yield return Swipe(new Vector2(-1.5f, 0f), new Vector2(1.5f, 0f));
            PlaceMeteor(meteorPrefab, new Vector2(0f, 1.5f), new Vector2(0f, -4f));

            yield return WaitFor(() => game.TilesShattered > 0, 2f);
            yield return null;

            Assert.AreEqual(1, game.TilesShattered);
            Assert.AreEqual(0, slasher.Tiles.Count);
        }
    }
}
