using UnityEngine;

namespace VectorSlash
{
    /// <summary>
    /// A fuel intake. Fragments that touch it are collected for fuel and points.
    /// Adding this component also adds a CircleCollider2D (used as a trigger).
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class FuelCollector : MonoBehaviour
    {
        [Tooltip("Fragments closer than this are pulled toward the collector. 0 turns the pull off.")]
        public float magnetRadius = 2f;
        public float magnetStrength = 6f;

        Vector3 baseScale;
        float pulse;

        void Awake()
        {
            GetComponent<CircleCollider2D>().isTrigger = true;
            baseScale = transform.localScale;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out Fragment fragment)) GameManager.Instance.CollectFragment(fragment, this);
        }

        void FixedUpdate()
        {
            if (magnetRadius <= 0f) return;
            Vector2 centre = transform.position;
            foreach (Fragment fragment in Fragment.Active)
            {
                Vector2 toCentre = centre - fragment.Body.position;
                float distance = toCentre.magnitude;
                if (distance > 0.01f && distance < magnetRadius)
                    fragment.Body.AddForce(toCentre / distance * (magnetStrength * fragment.Body.mass));
            }
        }

        /// <summary>Quick size bump to show a fragment was collected.</summary>
        public void Pulse() => pulse = 1f;

        void Update()
        {
            pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime * 4f);
            transform.localScale = baseScale * (1f + 0.2f * pulse);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, magnetRadius);
        }
    }
}
