using UnityEngine;

namespace VectorSlash
{
    /// <summary>
    /// A released slash: a solid bar that fragments bounce off, which disappears after its lifetime.
    /// Put it on a Square sprite; adding this component also adds a BoxCollider2D.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class EnergyTile : MonoBehaviour
    {
        public int SlashId { get; private set; }
        public float Lifetime { get; private set; } = 3f;
        public float Age { get; private set; }
        public float Remaining01 => Mathf.Clamp01(1f - Age / Lifetime);

        SpriteRenderer sprite;
        Color baseColor;
        bool shattered;

        void Awake()
        {
            GetComponent<BoxCollider2D>().isTrigger = false;
            sprite = GetComponent<SpriteRenderer>();
            if (sprite != null) baseColor = sprite.color;
        }

        /// <summary>Stretches the tile from a to b.</summary>
        public void Setup(Vector2 a, Vector2 b, float thickness, float lifetime, int slashId)
        {
            Vector2 d = b - a;
            transform.SetPositionAndRotation((a + b) * 0.5f, Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg));
            transform.localScale = new Vector3(d.magnitude, thickness, 1f);
            Lifetime = lifetime;
            SlashId = slashId;
        }

        void Update()
        {
            Age += Time.deltaTime;
            if (Age >= Lifetime)
            {
                Destroy(gameObject);
                return;
            }

            if (sprite == null) return;
            Color color = baseColor;
            color.a *= Mathf.Lerp(0.3f, 1f, Remaining01);
            if (Lifetime - Age < 0.6f && Mathf.Repeat(Age, 0.2f) < 0.1f) color.a *= 0.3f; // blink before vanishing
            sprite.color = color;
        }

        /// <summary>Called when an intact meteor runs into this tile.</summary>
        public void Shatter()
        {
            if (shattered) return;
            shattered = true;
            GameManager.Instance.TileShattered();
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
