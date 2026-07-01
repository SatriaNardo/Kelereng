using UnityEngine;

[CreateAssetMenu(fileName = "WindElement", menuName = "Elements/Wind")]
public class WindElementSO : MarbleElementSO
{
    [Header("Wind Settings")]
    public float aoeRadius = 2.5f;
    public float pushForce = 12f;

    [Header("Movement Wind Trail")]
    public bool useMovementTrail = true;
    public Material particleMaterial;
    public Color trailStartColor = new Color(0.8f, 1f, 0.82f, 1f);
    public Color trailEndColor = new Color(0.35f, 1f, 0.6f, 0f);
    public float trailParticlesPerSecond = 24f;
    public float trailLifetime = 0.25f;
    public float trailStartSize = 0.09f;
    public float trailSpeed = 0.7f;
    public int trailSortingOrder = 18;

    [Header("Clash Effect")]
    public GameObject clashEffectPrefab;
    public Sprite[] clashEffectFrames;
    public float clashEffectFramesPerSecond = 18f;
    public float clashEffectScale = 1.25f;
    public float clashEffectOffset = 0.3f;
    public float clashEffectLifetime = 1f;

    [Header("Clash Particle Burst")]
    public bool useClashParticleBurst = true;
    public Color burstStartColor = new Color(0.9f, 1f, 0.9f, 1f);
    public Color burstEndColor = new Color(0.25f, 1f, 0.55f, 0f);
    public float burstLifetime = 0.32f;
    public float burstSpeed = 5.2f;
    public float burstStartSize = 0.13f;
    public int burstParticleCount = 28;
    public float burstSpreadAngle = 75f;
    public int burstSortingOrder = 22;

    public override void OnLaunch(Rigidbody2D marble)
    {
        if (!useMovementTrail || marble == null) return;

        WindTrailEffect trail = marble.GetComponent<WindTrailEffect>();
        if (trail == null)
        {
            trail = marble.gameObject.AddComponent<WindTrailEffect>();
        }

        trail.Configure(trailStartColor, trailEndColor, trailParticlesPerSecond, trailLifetime, trailStartSize, trailSpeed, trailSortingOrder, particleMaterial);
    }

    public override void OnClash(Rigidbody2D attacker, Rigidbody2D victim, Vector2 collisionPoint, Vector2 impactDirection)
    {
        Vector2 effectDirection = GetEffectDirection(attacker, victim, impactDirection);
        Vector2 effectPosition = collisionPoint + effectDirection * clashEffectOffset;

        if (useClashParticleBurst)
        {
            WindClashBurstEffect.Spawn(effectPosition, effectDirection, burstStartColor, burstEndColor, burstLifetime, burstSpeed, burstStartSize, burstParticleCount, burstSpreadAngle, burstSortingOrder, particleMaterial);
        }

        SpriteSheetEffect.SpawnEffect(effectPosition, clashEffectPrefab, clashEffectFrames, elementColor, clashEffectFramesPerSecond, clashEffectScale, clashEffectLifetime);

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

    private Vector2 GetEffectDirection(Rigidbody2D attacker, Rigidbody2D victim, Vector2 impactDirection)
    {
        if (impactDirection.sqrMagnitude > 0.001f)
        {
            return impactDirection.normalized;
        }

        if (attacker != null && victim != null)
        {
            Vector2 direction = victim.position - attacker.position;
            if (direction.sqrMagnitude > 0.001f) return direction.normalized;
        }

        return Vector2.right;
    }
}
