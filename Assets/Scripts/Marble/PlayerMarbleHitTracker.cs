using UnityEngine;

public class PlayerMarbleHitTracker : MonoBehaviour
{
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("TargetMarble")) return;
        if (ArenaManager.Instance == null) return;

        ArenaManager.Instance.RegisterPlayerMarbleHitTarget(rb);
    }
}
