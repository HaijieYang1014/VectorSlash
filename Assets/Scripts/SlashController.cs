using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VectorSlash
{
    /// <summary>
    /// Turns mouse drags into slashes. While dragging, the blade cuts every intact meteor it passes
    /// all the way through. On release, the blade becomes an energy tile at the same angle.
    /// </summary>
    public class SlashController : MonoBehaviour
    {
        [Tooltip("The Energy Tile prefab.")]
        public EnergyTile tilePrefab;
        [Tooltip("A Square sprite in the scene (no collider) shown while dragging.")]
        public SpriteRenderer slashPreview;

        [Header("Tiles")]
        [Tooltip("Longest possible slash / tile. Longer drags keep only the last part.")]
        public float maxTileLength = 3.2f;
        [Tooltip("Slashes shorter than this leave no tile.")]
        public float minTileLength = 0.4f;
        public float tileThickness = 0.15f;
        [Tooltip("Seconds a tile stays before disappearing.")]
        public float tileLifetime = 3f;
        [Tooltip("Making another tile beyond this removes the oldest one.")]
        public int maxTiles = 3;

        /// <summary>Tests turn this off to drive slashes by code.</summary>
        [System.NonSerialized] public bool pointerInputEnabled = true;

        public IReadOnlyList<EnergyTile> Tiles => tiles;
        public bool IsSlashing { get; private set; }

        readonly List<EnergyTile> tiles = new List<EnergyTile>();
        Camera cam;
        Color previewColor = Color.white;
        Vector2 anchor, tip, lastTip;
        int slashId;
        int nextSlashId = 1;
        bool waitForRelease;

        void Awake()
        {
            cam = Camera.main;
            if (slashPreview != null)
            {
                previewColor = slashPreview.color;
                slashPreview.enabled = false;
            }
        }

        void Update()
        {
            tiles.RemoveAll(tile => tile == null || !tile.gameObject.activeSelf);

            GameManager game = GameManager.Instance;
            if (game == null || game.State != GameState.Playing)
            {
                CancelSlash();
                return;
            }

            if (pointerInputEnabled) ReadPointer();
            if (IsSlashing)
            {
                CutMeteors();
                lastTip = tip;
            }
        }

        void ReadPointer()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null) return;

            bool held = pointer.press.isPressed;
            if (waitForRelease)
            {
                if (!held) waitForRelease = false;
                return;
            }

            Vector2 world = cam.ScreenToWorldPoint(pointer.position.ReadValue());
            if (!IsSlashing && pointer.press.wasPressedThisFrame) BeginSlash(world);
            else if (IsSlashing && held) MoveSlash(world);
            else if (IsSlashing) EndSlash();
        }

        public void BeginSlash(Vector2 point)
        {
            IsSlashing = true;
            anchor = tip = lastTip = point;
            slashId = nextSlashId++;
            UpdatePreview();
        }

        public void MoveSlash(Vector2 point)
        {
            if (!IsSlashing) return;
            tip = point;
            // Fixed maximum length: on long drags the start of the blade follows the pointer.
            Vector2 span = tip - anchor;
            if (span.magnitude > maxTileLength) anchor = tip - span.normalized * maxTileLength;
            UpdatePreview();
        }

        public void EndSlash()
        {
            if (!IsSlashing) return;
            CutMeteors();
            CancelSlash();
            if ((tip - anchor).magnitude >= minTileLength) CreateTile(anchor, tip);
        }

        public void CancelSlash()
        {
            IsSlashing = false;
            if (slashPreview != null) slashPreview.enabled = false;
        }

        /// <summary>Ignores the mouse until the button is released (e.g. the click that started the game).</summary>
        public void IgnoreUntilRelease()
        {
            CancelSlash();
            waitForRelease = true;
        }

        public void ClearTiles()
        {
            foreach (EnergyTile tile in tiles)
                if (tile != null) Destroy(tile.gameObject);
            tiles.Clear();
        }

        void CutMeteors()
        {
            Vector2 sweep = tip - lastTip;
            bool moved = sweep.sqrMagnitude > 1e-4f;
            Vector2 swipe = moved ? sweep : tip - anchor;

            foreach (Meteor meteor in Meteor.Active.ToArray())
            {
                if (meteor.SourceSlashId == slashId) continue;
                Vector2 centre = meteor.transform.position;
                float radius = meteor.Radius;
                if (PassesThrough(anchor, tip, centre, radius))
                    meteor.Slice(tip - anchor, swipe, slashId);
                else if (moved && PassesThrough(lastTip, tip, centre, radius)) // fast swipes
                    meteor.Slice(sweep, swipe, slashId);
            }
        }

        /// <summary>True when segment a-b goes all the way through the circle (in one side and out the other).</summary>
        static bool PassesThrough(Vector2 a, Vector2 b, Vector2 centre, float radius)
        {
            float r2 = radius * radius;
            if ((a - centre).sqrMagnitude <= r2 || (b - centre).sqrMagnitude <= r2) return false;
            Vector2 ab = b - a;
            if (ab.sqrMagnitude < 1e-6f) return false;
            float t = Mathf.Clamp01(Vector2.Dot(centre - a, ab) / ab.sqrMagnitude);
            return (a + ab * t - centre).sqrMagnitude < r2;
        }

        void CreateTile(Vector2 a, Vector2 b)
        {
            if (tilePrefab == null)
            {
                Debug.LogWarning("SlashController has no Tile Prefab assigned.", this);
                return;
            }

            tiles.RemoveAll(tile => tile == null || !tile.gameObject.activeSelf);
            while (tiles.Count >= maxTiles)
            {
                tiles[0].gameObject.SetActive(false);
                Destroy(tiles[0].gameObject);
                tiles.RemoveAt(0);
            }

            EnergyTile newTile = Instantiate(tilePrefab);
            newTile.Setup(a, b, tileThickness, tileLifetime, slashId);
            tiles.Add(newTile);
        }

        void UpdatePreview()
        {
            if (slashPreview == null) return;
            Vector2 d = tip - anchor;
            Transform t = slashPreview.transform;
            t.SetPositionAndRotation((anchor + tip) * 0.5f, Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg));
            t.localScale = new Vector3(Mathf.Max(d.magnitude, 0.05f), tileThickness * 0.6f, 1f);

            Color color = previewColor;
            if (d.magnitude < minTileLength) color.a *= 0.4f; // too short to leave a tile
            slashPreview.color = color;
            slashPreview.enabled = true;
        }
    }
}
