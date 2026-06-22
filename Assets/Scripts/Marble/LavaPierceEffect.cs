using UnityEngine;

public class LavaPierceEffect : MonoBehaviour
{
    public float minimumSpeed = 0.1f;
    public float preserveSpeedMultiplier = 0.95f;

    private Rigidbody2D rb;
    private Vector2 previousVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            Destroy(this);
            return;
        }

        if (rb.linearVelocity.magnitude <= minimumSpeed)
        {
            Destroy(this);
            return;
        }

        previousVelocity = rb.linearVelocity;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (rb == null) return;

        Rigidbody2D otherRb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (otherRb == null) return;
        if (previousVelocity.sqrMagnitude <= 0.001f) return;

        float preservedSpeed = previousVelocity.magnitude * preserveSpeedMultiplier;
        rb.linearVelocity = previousVelocity.normalized * Mathf.Max(rb.linearVelocity.magnitude, preservedSpeed);
    }
}
