using UnityEngine;

[CreateAssetMenu(fileName = "CombinedElement", menuName = "Elements/Combined Element")]
public class CombinedElementSO : MarbleElementSO
{
    public enum FusionType { Cyclone, Explosion }

    [Header("Fusion Type Settings")]
    public FusionType fusionType;

    [Header("Cyclone Physics Config (Small Range, Huge Push)")]
    public float cycloneRadius = 0.5f;
    public float cycloneForce = 18f;

    [Header("Explosion Physics Config (Medium Range, Small Push)")]
    public float explosionRadius = 1f;
    public float explosionForce = 8f;

    public override void OnClash(Rigidbody2D attacker, Rigidbody2D victim, Vector2 collisionPoint)
    {
        float targetRadius = (fusionType == FusionType.Cyclone) ? cycloneRadius : explosionRadius;
        float targetForce = (fusionType == FusionType.Cyclone) ? cycloneForce : explosionForce;
        string debugSymbol = (fusionType == FusionType.Cyclone) ? "🌪️" : "💥";

        // Gambar visual lingkaran debug di Scene View selama 1.5 detik
        DrawDebugCircle(collisionPoint, targetRadius, elementColor, 1.5f);

        // Cari semua objek fisik di dalam area ledakan kombinasi
        Collider2D[] surroundingObjects = Physics2D.OverlapCircleAll(collisionPoint, targetRadius);

        foreach (Collider2D col in surroundingObjects)
        {
            Rigidbody2D rb = col.GetComponent<Rigidbody2D>();
            
            if (rb != null && rb != attacker)
            {
                // Khusus Explosion: Nol-kan dulu kecepatan korban agar pentalan seragam ke segala arah
                if (fusionType == FusionType.Explosion)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }

                Vector2 pushDirection = (rb.position - collisionPoint).normalized;
                if (pushDirection == Vector2.zero) pushDirection = Random.insideUnitCircle.normalized;

                // Hempaskan objek keluar dari pusat ledakan
                rb.AddForce(pushDirection * targetForce, ForceMode2D.Impulse);
            }
        }

        Debug.Log($"{debugSymbol} {fusionType.ToString()} Fusion Burst Triggered!");
    }

    private void DrawDebugCircle(Vector2 center, float radius, Color color, float duration)
    {
        int segments = 32; 
        float angleStep = 360f / segments;
        Vector2 prevPoint = center + new Vector2(radius, 0);

        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = i * angleStep * Mathf.Deg2Rad;
            Vector2 nextPoint = center + new Vector2(Mathf.Cos(currentAngle) * radius, Mathf.Sin(currentAngle) * radius);
            Debug.DrawLine(prevPoint, nextPoint, color, duration);
            prevPoint = nextPoint;
        }
    }
}