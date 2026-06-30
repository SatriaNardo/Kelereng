using UnityEngine;

public class BlackHole : MonoBehaviour
{
    [Header("Attraction")]
    public float attractionRadius = 1.2f;

    [Header("Force")]
    public float playerPullForce = 1f;
    public float enemyPullForce = 0.02f;

    [Header("Lifetime")]
    public float duration = 0.8f;

    private void Start()
    {
        Destroy(gameObject, duration);
    }

    private void FixedUpdate()
    {
        Collider2D[] colliders =
            Physics2D.OverlapCircleAll(
                transform.position,
                attractionRadius);

        foreach (Collider2D col in colliders)
        {
            Rigidbody2D rb = col.GetComponent<Rigidbody2D>();

            if (rb == null)
                continue;
                
            float distanceFromArenaCenter =
                Vector2.Distance(
                    rb.position,
                    ArenaManager.Instance.arenaCenter.position);

            if (distanceFromArenaCenter >
                ArenaManager.Instance.circleRadius - 0.7f)
            {
                continue;
            }

            Vector2 direction =
            (Vector2)transform.position - rb.position;

            float distance = direction.magnitude;

            // Jika sudah sangat dekat pusat,
            // jangan tarik lagi
            if (distance < 0.5f)
                continue;

            direction.Normalize();

            float forceMultiplier =
            Mathf.Clamp01(
                (attractionRadius - distance)
                / attractionRadius);

            if (col.CompareTag("PlayerMarble"))
            {
                rb.AddForce(
                    direction * playerPullForce * forceMultiplier,
                    ForceMode2D.Force);
            }
            else if (col.CompareTag("TargetMarble"))
            {
                rb.AddForce(
                    direction * enemyPullForce * forceMultiplier,
                    ForceMode2D.Force);
            }
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Hanya Player Marble yang diproses
        if (!other.CompareTag("PlayerMarble"))
            return;

        Debug.Log($"🌌 {other.name} masuk Black Hole!");

        // Kembalikan ammo menggunakan sistem yang sudah ada
        other.tag = "PlayerMarble";
        ArenaManager.Instance.OnMarbleExited(other.gameObject);

        // Hapus kelereng
        Destroy(other.gameObject);
    }
}