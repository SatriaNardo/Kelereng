using UnityEngine;

[CreateAssetMenu(fileName = "WindElement", menuName = "Elements/Wind")]
public class WindElementSO : MarbleElementSO
{
    [Header("Wind Settings")]
    public float aoeRadius = 2.5f;
    public float pushForce = 12f;

    public override void OnClash(Rigidbody2D attacker, Rigidbody2D victim, Vector2 collisionPoint)
    {
        // BARU: Gambar lingkaran visual untuk debug di Scene View
        DrawDebugCircle(collisionPoint, aoeRadius, Color.green, 1.0f);

        // Find all colliders inside the blast radius
        Collider2D[] surroundingObjects = Physics2D.OverlapCircleAll(collisionPoint, aoeRadius);

        foreach (Collider2D col in surroundingObjects)
        {
            Rigidbody2D rb = col.GetComponent<Rigidbody2D>();
            
            // Push any marble that has physics, EXCEPT the one that initiated the attack
            if (rb != null && rb != attacker)
            {
                Vector2 pushDirection = (rb.position - collisionPoint).normalized;
                
                // Safety check in case it's exactly at the center point
                if (pushDirection == Vector2.zero) pushDirection = Random.insideUnitCircle.normalized;

                // Apply immediate outward blast force
                rb.AddForce(pushDirection * pushForce, ForceMode2D.Impulse);
            }
        }
        Debug.Log("🍃 Wind AoE Blast triggered!");
    }

    // ========================================================
    // HELPER: Membuat Garis Lingkaran Tiruan Ala Gizmos
    // ========================================================
    private void DrawDebugCircle(Vector2 center, float radius, Color color, float duration)
    {
        int segments = 24; // Semakin tinggi angkanya, lingkaran semakin mulus
        float angleStep = 360f / segments;
        Vector2 prevPoint = center + new Vector2(radius, 0);

        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = i * angleStep * Mathf.Deg2Rad;
            Vector2 nextPoint = center + new Vector2(Mathf.Cos(currentAngle) * radius, Mathf.Sin(currentAngle) * radius);
            
            // Menggambar garis patah-patah antar segmen membentuk cincin
            Debug.DrawLine(prevPoint, nextPoint, color, duration);
            prevPoint = nextPoint;
        }
    }
}