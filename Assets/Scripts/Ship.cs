using UnityEngine;

namespace VectorSlash
{
    /// <summary>
    /// Marks the player's ship. It needs a Collider2D (e.g. Polygon Collider 2D) so meteors and
    /// fragments can hit it, and flashes when it takes damage.
    /// </summary>
    public class Ship : MonoBehaviour
    {
        public Color hitColor = Color.red;

        SpriteRenderer sprite;
        Color baseColor;
        float flashTimer;

        void Awake()
        {
            sprite = GetComponent<SpriteRenderer>();
            if (sprite != null) baseColor = sprite.color;

            if (TryGetComponent(out Collider2D hull)) hull.isTrigger = false;
            else Debug.LogWarning("The Ship needs a Collider2D (e.g. Polygon Collider 2D) to be hit.", this);
        }

        public void Flash() => flashTimer = 0.3f;

        void Update()
        {
            if (flashTimer <= 0f || sprite == null) return;
            flashTimer -= Time.deltaTime;
            sprite.color = Color.Lerp(baseColor, hitColor, Mathf.Clamp01(flashTimer / 0.3f));
        }
    }
}
