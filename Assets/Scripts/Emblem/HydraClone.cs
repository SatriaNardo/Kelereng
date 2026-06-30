using UnityEngine;

public class HydraClone : MonoBehaviour
{
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (rb != null &&
            rb.linearVelocity.magnitude < 0.1f)
        {
            Destroy(gameObject);
        }
    }
}