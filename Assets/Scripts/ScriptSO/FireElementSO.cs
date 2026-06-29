using UnityEngine;

[CreateAssetMenu(fileName = "FireElement", menuName = "Elements/Fire")]
public class FireElementSO : MarbleElementSO
{
    [Header("Fire Settings")]
    public float launchSpeedMultiplier = 1.35f;

    [Header("Movement Fire Trail")]
    public bool useMovementTrail = true;
    public Material particleMaterial;
    public Color trailStartColor = new Color(1f, 0.85f, 0.2f, 1f);
    public Color trailEndColor = new Color(1f, 0.1f, 0f, 0f);
    public float trailParticlesPerSecond = 28f;
    public float trailLifetime = 0.28f;
    public float trailStartSize = 0.11f;
    public float trailSpeed = 0.45f;
    public int trailSortingOrder = 18;

    [Header("Clash Effect")]
    public GameObject clashEffectPrefab;
    public Sprite[] clashEffectFrames;
    public float clashEffectFramesPerSecond = 18f;
    public float clashEffectScale = 1f;
    public float clashEffectOffset = 0.35f;
    public float clashEffectLifetime = 1f;

    [Header("Clash Particle Burst")]
    public bool useClashParticleBurst = true;
    public Color burstStartColor = new Color(1f, 0.95f, 0.25f, 1f);
    public Color burstEndColor = new Color(1f, 0.08f, 0f, 0f);
    public float burstLifetime = 0.36f;
    public float burstSpeed = 4.8f;
    public float burstStartSize = 0.18f;
    public int burstParticleCount = 34;
    public float burstSpreadAngle = 42f;
    public int burstSortingOrder = 22;

    public override float GetLaunchForceMultiplier(float baseMultiplier)
    {
        return baseMultiplier * launchSpeedMultiplier;
    }

    public override void OnLaunch(Rigidbody2D marble)
    {
        if (!useMovementTrail || marble == null) return;

        FireTrailEffect trail = marble.GetComponent<FireTrailEffect>();
        if (trail == null)
        {
            trail = marble.gameObject.AddComponent<FireTrailEffect>();
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
        Vector2 effectDirection = GetEffectDirection(attacker, victim, impactDirection);
        Vector2 effectPosition = collisionPoint + effectDirection * clashEffectOffset;

        if (useClashParticleBurst)
        {
            FireClashBurstEffect.Spawn(
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
        Debug.Log("Fire launch speed boost was already applied on shoot.");
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
