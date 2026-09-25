using System.Collections.Generic;
using UnityEngine;

namespace VectorSlash
{
    /// <summary>
    /// A sliced piece of meteor. It ricochets off energy tiles, fills the fuel tank when it
    /// reaches a fuel collector, and damages the ship if it hits it.
    /// Adding this component also adds a Rigidbody2D and a CircleCollider2D.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class Fragment : MonoBehaviour
    {
        public static readonly List<Fragment> Active = new List<Fragment>();

        [Tooltip("Fuel restored when this fragment reaches a fuel collector.")]
        public float fuel = 6f;
        public int points = 25;
        [Tooltip("Hull damage if this fragment hits the ship.")]
        public float damage = 6f;
        [Tooltip("After bouncing off a tile, the fragment's speed is kept between these values.")]
        public float minSpeed = 3f;
        public float maxSpeed = 9f;

        public MeteorFamily Family { get; private set; }
        public int Bounces { get; private set; }
        public Rigidbody2D Body { get; private set; }

        Vector2 velocityBeforeStep;
        bool removed;

        void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            Body.gravityScale = 0f;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // tiles are thin
            GetComponent<CircleCollider2D>().isTrigger = false;
        }

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        public void Launch(Vector2 velocity, MeteorFamily family)
        {
            Family = family;
            Body.linearVelocity = velocity;
            velocityBeforeStep = velocity;
            family.fragments++;
            GameManager.Instance.AddPiece(family);
        }

        void FixedUpdate() => velocityBeforeStep = Body.linearVelocity;

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (removed) return;

            if (collision.collider.TryGetComponent(out EnergyTile _))
                Ricochet(collision);
            else if (collision.collider.GetComponentInParent<Ship>() != null)
            {
                GameManager.Instance.ShipHit(damage);
                Remove(true);
            }
        }

        /// <summary>Mirrors the incoming velocity about the tile surface, so the angle in equals the angle out.</summary>
        void Ricochet(Collision2D collision)
        {
            ContactPoint2D contact = collision.GetContact(0);
            Vector2 normal = contact.normal;
            if (Vector2.Dot(normal, Body.position - contact.point) < 0f) normal = -normal; // point from tile to fragment

            Vector2 incoming = velocityBeforeStep;
            if (Vector2.Dot(incoming, normal) >= 0f)
            {
                // Already moving away (e.g. a tile was placed on top of us): just keep going.
                Body.linearVelocity = incoming;
                return;
            }

            Vector2 reflected = incoming - 2f * Vector2.Dot(incoming, normal) * normal;
            Body.linearVelocity = reflected.normalized * Mathf.Clamp(incoming.magnitude, minSpeed, maxSpeed);
            Bounces++;
        }

        void Update()
        {
            if (!removed && GameManager.Instance.IsOffscreen(transform.position)) Remove(true);
        }

        /// <summary>Called by the GameManager after a fuel collector swallows this fragment.</summary>
        public void Collect() => Remove(false);

        void Remove(bool failed)
        {
            if (removed) return;
            removed = true;
            GameManager.Instance.RemovePiece(Family, failed);
            Despawn();
        }

        /// <summary>Removes the fragment immediately, without any scoring.</summary>
        public void Despawn()
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
