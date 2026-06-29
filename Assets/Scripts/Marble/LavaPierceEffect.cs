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

        Vector2 currentVelocity = rb.linearVelocity;
        float currentSpeed = currentVelocity.magnitude;

        if (currentSpeed <= minimumSpeed)
        {
            Destroy(this);
            return;
        }

        previousVelocity = currentVelocity;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (rb == null) return;
        if (!IsMarbleCollision(collision)) return;
        if (previousVelocity.sqrMagnitude <= 0.001f) return;

        rb.linearVelocity = previousVelocity * preserveSpeedMultiplier;
    }

    private bool IsMarbleCollision(Collision2D collision)
    {
        GameObject other = collision.gameObject;
        if (other == null) return false;

        return other.CompareTag("TargetMarble")
            || other.CompareTag("PlayerMarble")
            || other.CompareTag("Gacoan")
            || other.GetComponent<TargetMarble>() != null
            || other.GetComponent<MarbleElementHandler>() != null;
    }
}
