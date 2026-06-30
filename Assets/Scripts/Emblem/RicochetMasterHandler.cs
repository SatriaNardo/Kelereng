using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RicochetHandler : MonoBehaviour
{
    private Rigidbody2D rb;
    private RicochetBuff buff;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Wall"))
            return;

        buff = GetComponent<RicochetBuff>();

        if (buff == null)
            return;

        rb.linearVelocity *= buff.bounceMultiplier;

        Debug.Log($"🪃 Ricochet Boost x{buff.bounceMultiplier}");
    }
}