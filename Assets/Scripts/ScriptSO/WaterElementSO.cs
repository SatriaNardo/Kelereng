using UnityEngine;

[CreateAssetMenu(fileName = "WaterElement", menuName = "Elements/Water")]
public class WaterElementSO : MarbleElementSO
{
    [Header("Water Settings")]
    public float extraKnockbackForce = 15f;

    [Header("Movement Water Trail")]
    public bool useMovementTrail = true;
    public Material particleMaterial;
    public Color trailStartColor = new Color(0.65f, 0.95f, 1f, 1f);
    public Color trailEndColor = new Color(0.15f, 0.55f, 1f, 0f);
    public float trailParticlesPerSecond = 26f;
    public float trailLifetime = 0.3f;
    public float trailStartSize = 0.1f;
    public float trailSpeed = 0.35f;
    public int trailSortingOrder = 18;

    [Header("Clash Effect")]
    public GameObject clashEffectPrefab;
    public Sprite[] clashEffectFrames;
    public float clashEffectFramesPerSecond = 18f;
    public float clashEffectScale = 1f;
    public float clashEffectOffset = 0.25f;
    public float clashEffectLifetime = 1f;

    [Header("Clash Particle Burst")]
    public bool useClashParticleBurst = true;
    public Color burstStartColor = new Color(0.85f, 1f, 1f, 1f);
    public Color burstEndColor = new Color(0.1f, 0.45f, 1f, 0f);
    public float burstLifetime = 0.4f;
    public float burstSpeed = 4.2f;
    public float burstStartSize = 0.16f;
    public int burstParticleCount = 30;
    public float burstSpreadAngle = 55f;
    public int burstSortingOrder = 22;

    public override void OnLaunch(Rigidbody2D marble)
    {
        if (!useMovementTrail || marble == null) return;

        WaterTrailEffect trail = marble.GetComponent<WaterTrailEffect>();
        if (trail == null)
        {
            trail = marble.gameObject.AddComponent<WaterTrailEffect>();
        }

        trail.Configure(
            trailStartColor,
            trailEndColor,
            trailParticlesPerSecond,
            trailLifetime,
            trailStartSize,
            trailSpeed,
            trailSortingOrder,
            particleMaterial);
    }

    public override void OnClash(Rigidbody2D attacker, Rigidbody2D victim, Vector2 collisionPoint, Vector2 impactDirection)
    {
        if (attacker == null || victim == null) return;

        Vector2 forceDirection = (victim.position - attacker.position).normalized;
        if (forceDirection == Vector2.zero)
        {
            forceDirection = Random.insideUnitCircle.normalized;
        }

        Vector2 effectDirection = impactDirection.sqrMagnitude > 0.001f ? impactDirection.normalized : forceDirection;
        Vector2 effectPosition = collisionPoint + effectDirection * clashEffectOffset;

        victim.AddForce(forceDirection * extraKnockbackForce, ForceMode2D.Impulse);

        if (useClashParticleBurst)
        {
            WaterClashBurstEffect.Spawn(
                effectPosition,
                effectDirection,
                burstStartColor,
                burstEndColor,
                burstLifetime,
                burstSpeed,
                burstStartSize,
                burstParticleCount,
                burstSpreadAngle,
                burstSortingOrder,
                particleMaterial);
        }

        SpriteSheetEffect.SpawnEffect(effectPosition, clashEffectPrefab, clashEffectFrames, elementColor, clashEffectFramesPerSecond, clashEffectScale, effectDirection, clashEffectLifetime);

        Debug.Log("Water extra knockback triggered.");
    }
}
