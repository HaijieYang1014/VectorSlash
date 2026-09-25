using System.Collections.Generic;
using UnityEngine;

namespace VectorSlash
{
    /// <summary>
    /// An intact meteor. The slash blade splits it into pieces. Intact meteors shatter any
    /// energy tile they touch and damage the ship.
    /// Adding this component also adds a Rigidbody2D and a CircleCollider2D (used as a trigger).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class Meteor : MonoBehaviour
    {
        public static readonly List<Meteor> Active = new List<Meteor>();

        [Tooltip("What this meteor breaks into: the Fragment prefab, or a smaller Meteor prefab if it needs another cut.")]
        public GameObject piecePrefab;
        [Min(1)] public int pieceCount = 2;
        [Tooltip("Hull damage if this meteor reaches the ship.")]
        public float damage = 18f;
        [Tooltip("Multiplies the spawner's speed for this meteor type.")]
        public float speedMultiplier = 1f;
        [Tooltip("How fast the pieces fly apart, perpendicular to the cut.")]
        public float separationSpeed = 1.3f;
        [Tooltip("Extra speed given to the pieces in the direction of the swipe.")]
        public float swipeInfluence = 0.8f;

        public MeteorFamily Family { get; private set; }
        /// <summary>The slash that created this meteor. That slash can't cut it again, and its tile ignores it.</summary>
        public int SourceSlashId { get; private set; } = -1;
        public float Radius => circle.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        public bool NeedsAnotherCut => piecePrefab != null && piecePrefab.GetComponent<Meteor>() != null;

        Rigidbody2D body;
        CircleCollider2D circle;
        bool removed;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            circle = GetComponent<CircleCollider2D>();
            body.gravityScale = 0f;
            circle.isTrigger = true; // meteors pass through things; we react in OnTriggerEnter2D
        }

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        public void Launch(Vector2 velocity, MeteorFamily family, int sourceSlashId)
        {
            Family = family;
            SourceSlashId = sourceSlashId;
            body.linearVelocity = velocity;
            body.angularVelocity = Random.Range(-90f, 90f);
            GameManager.Instance.AddPiece(family);
        }

        /// <summary>Splits the meteor. Pieces fly apart perpendicular to the cut and are nudged along the swipe.</summary>
        public void Slice(Vector2 cutDirection, Vector2 swipeDirection, int slashId)
        {
            if (removed) return;
            if (piecePrefab == null)
            {
                Debug.LogWarning($"{name} has no Piece Prefab assigned.", this);
                return;
            }

            Vector2 across = new Vector2(-cutDirection.y, cutDirection.x).normalized;
            Vector2 push = swipeDirection.normalized * swipeInfluence;
            var pieces = new List<Collider2D>();
            for (int i = 0; i < pieceCount; i++)
            {
                float side = pieceCount == 1 ? 0f : Mathf.Lerp(-1f, 1f, i / (pieceCount - 1f));
                Vector2 position = (Vector2)transform.position + across * (side * Radius * 0.5f);
                Vector2 velocity = body.linearVelocity * 0.9f + across * (side * separationSpeed) + push;

                GameObject piece = Instantiate(piecePrefab, position, transform.rotation);
                if (piece.TryGetComponent(out Meteor meteor)) meteor.Launch(velocity, Family, slashId);
                else if (piece.TryGetComponent(out Fragment fragment)) fragment.Launch(velocity, Family);
                if (piece.TryGetComponent(out Collider2D pieceCollider)) pieces.Add(pieceCollider);
            }

            // Sibling pieces start next to each other; don't let them shove each other around.
            for (int i = 0; i < pieces.Count; i++)
                for (int j = i + 1; j < pieces.Count; j++)
                    Physics2D.IgnoreCollision(pieces[i], pieces[j]);

            GameManager.Instance.MeteorSliced(slashId, NeedsAnotherCut);
            Remove(false);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (removed || GameManager.Instance.State != GameState.Playing) return;

            if (other.TryGetComponent(out EnergyTile tile))
            {
                if (tile.SlashId != SourceSlashId) tile.Shatter();
            }
            else if (other.GetComponentInParent<Ship>() != null)
            {
                GameManager.Instance.ShipHit(damage);
                Remove(true);
            }
        }

        void Update()
        {
            if (!removed && GameManager.Instance.IsOffscreen(transform.position)) Remove(true);
        }

        void Remove(bool failed)
        {
            removed = true;
            GameManager.Instance.RemovePiece(Family, failed);
            Despawn();
        }

        /// <summary>Removes the meteor immediately, without any scoring.</summary>
        public void Despawn()
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
