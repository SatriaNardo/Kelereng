using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CombinedElement", menuName = "Elements/Combined Element")]
public class CombinedElementSO : MarbleElementSO
{
    public enum FusionType
    {
        Cyclone,
        Explosion,
        Flood,
        Quake,
        Ice,
        Blaze,
        Sand,
        Steam,
        Lava,
        Mud
    }

    [Header("Fusion Type Settings")]
    public FusionType fusionType;

    [Header("Movement Trail")]
    public bool useMovementTrail = true;
    public Material movementParticleMaterial;
    public Sprite movementParticleSprite;
    public float movementTrailParticlesPerSecond = 24f;
    public float movementTrailLifetime = 0.28f;
    public float movementTrailStartSize = 0.1f;
    public float movementTrailSpeed = 0.55f;
    public int movementTrailSortingOrder = 18;

    [Header("Movement Trail Colors")]
    public Color movementTrailStartColor = Color.white;
    public Color movementTrailMidColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    public Color movementTrailEndColor = new Color(1f, 1f, 1f, 0f);

    [Header("Cyclone")]
    public float cycloneRadius = 0.5f;
    public float cycloneForce = 5f;

    [Header("Explosion/Flood")]
    public float explosionRadius = 1f;
    public float explosionForce = 3f;
    public GameObject explosionClashEffectPrefab;
    public Sprite[] explosionClashEffectFrames;
    public float explosionClashEffectFramesPerSecond = 18f;
    public float explosionClashEffectScale = 1f;
    public float explosionClashEffectLifetime = 1f;
    public int explosionClashEffectSortingOrder = 20;

    [Header("Quake")]
    public int quakeChainCount = 5;
    public float quakeSmallAoeRadius = 0.65f;
    public float quakeSmallAoeForce = 1f;
    public float quakeSearchRadius = 6f;
    public GameObject quakeImpactEffectPrefab;
    public float quakeImpactEffectScale = 1f;
    public GameObject quakeAffectedMarbleEffectPrefab;
    public Sprite[] quakeAffectedMarbleEffectFrames;
    public float quakeEffectFramesPerSecond = 18f;
    public float quakeAffectedMarbleEffectScale = 0.8f;
    public float quakeEffectLifetime = 1f;
    public int quakeEffectSortingOrder = -1;
    public float quakeChainStepDelay = 0.12f;
    public bool useQuakeDustBurst = true;
    public Material quakeParticleMaterial;
    public Color quakeDustStartColor = new Color(0.65f, 0.52f, 0.32f, 1f);
    public Color quakeDustEndColor = new Color(0.25f, 0.18f, 0.1f, 0f);
    public float quakeDustLifetime = 0.45f;
    public float quakeDustSpeed = 1.8f;
    public float quakeDustStartSize = 0.16f;
    public int quakeDustParticleCount = 20;

    [Header("Ice")]
    public float iceTrailSpotSpacing = 0.25f;
    public float iceTrailSpotSize = 0.45f;
    public Sprite[] iceTrailSprites;
    [Range(0f, 1f)] public float iceTrailLinearDampingMultiplier = 0.08f;
    [Range(0f, 1f)] public float iceTrailAngularDampingMultiplier = 0.15f;
    [Range(0f, 1f)] public float iceTrailFriction = 0f;

    [Header("Blaze")]
    public float blazeLaunchSpeedMultiplier = 2f;
    public int blazeChainCount = 6;
    public float blazeChainForce = 5f;
    public float blazeSearchRadius = 7f;
    public float blazeChainStepDelay = 0.12f;
    public GameObject blazeClashEffectPrefab;
    public GameObject blazeChainEffectPrefab;
    public float blazeEffectFramesPerSecond = 18f;
    public float blazeClashEffectScale = 1f;
    public float blazeChainEffectScale = 1f;
    public float blazeEffectLifetime = 1f;
    public int blazeEffectSortingOrder = 20;

    [Header("Sand")]
    public GameObject sandClashEffectPrefab;
    public float sandEffectFramesPerSecond = 18f;
    public float sandClashEffectScale = 1f;
    public float sandEffectLifetime = 1f;
    public int sandEffectSortingOrder = 20;

    [Header("Steam")]
    public int steamChainCount = 6;
    public float steamSearchRadius = 7f;
    public float steamMassMultiplier = 0.5f;
    public float steamBuffDuration = 8f;
    [Range(0f, 1f)] public float steamLinearDampingMultiplier = 0.2f;
    [Range(0f, 1f)] public float steamAngularDampingMultiplier = 0.35f;
    [Range(0f, 1f)] public float steamFriction = 0f;

    [Header("Lava")]
    public float lavaLaunchMultiplier = 1.2f;
    public float lavaClashForce = 5f;
    public float lavaPreserveSpeedMultiplier = 0.95f;

    [Header("Mud")]
    public int mudMaxStickCount = 5;
    [Range(0f, 1f)] public float mudImmediateVelocityMultiplier = 0.2f;

    public override float GetLaunchForceMultiplier(float baseMultiplier)
    {
        if (fusionType == FusionType.Lava)
        {
            return baseMultiplier * lavaLaunchMultiplier;
        }

        if (fusionType == FusionType.Blaze)
        {
            return baseMultiplier * blazeLaunchSpeedMultiplier;
        }

        return baseMultiplier;
    }

    public override void OnLaunch(Rigidbody2D marble)
    {
        if (marble == null) return;

        ApplyMovementTrail(marble);

        if (fusionType == FusionType.Ice)
        {
            IceTrailEffect trail = marble.gameObject.GetComponent<IceTrailEffect>();
            if (trail == null) trail = marble.gameObject.AddComponent<IceTrailEffect>();

            trail.spotSpacing = iceTrailSpotSpacing;
            trail.spotSize = iceTrailSpotSize;
            trail.iceColor = elementColor;
            trail.spotSprites = iceTrailSprites;
            trail.spotLinearDampingMultiplier = iceTrailLinearDampingMultiplier;
            trail.spotAngularDampingMultiplier = iceTrailAngularDampingMultiplier;
            trail.spotFriction = iceTrailFriction;
        }
        else if (fusionType == FusionType.Lava)
        {
            LavaPierceEffect pierce = marble.gameObject.GetComponent<LavaPierceEffect>();
            if (pierce == null) pierce = marble.gameObject.AddComponent<LavaPierceEffect>();

            pierce.preserveSpeedMultiplier = lavaPreserveSpeedMultiplier;
        }
    }

    private void ApplyMovementTrail(Rigidbody2D marble)
    {
        if (!useMovementTrail || marble == null) return;

        GetMovementTrailColors(out Color startColor, out Color midColor, out Color endColor);

        GenericMarbleTrailEffect trail = marble.GetComponent<GenericMarbleTrailEffect>();
        if (trail == null)
        {
            trail = marble.gameObject.AddComponent<GenericMarbleTrailEffect>();
        }

        trail.Configure(
            $"{fusionType}MoveTrail",
            startColor,
            midColor,
            endColor,
            movementTrailParticlesPerSecond,
            movementTrailLifetime,
            movementTrailStartSize,
            movementTrailSpeed,
            movementTrailSortingOrder,
            movementParticleMaterial,
            movementParticleSprite);
    }

    private void GetMovementTrailColors(out Color startColor, out Color midColor, out Color endColor)
    {
        startColor = movementTrailStartColor;
        midColor = movementTrailMidColor;
        endColor = movementTrailEndColor;
    }

    public override void OnClash(Rigidbody2D attacker, Rigidbody2D victim, Vector2 collisionPoint, Vector2 impactDirection)
    {
        switch (fusionType)
        {
            case FusionType.Cyclone:
                PushAoe(attacker, collisionPoint, cycloneRadius, cycloneForce, false);
                break;

            case FusionType.Explosion:
                SpriteSheetEffect.SpawnEffect(collisionPoint, explosionClashEffectPrefab, explosionClashEffectFrames, elementColor, explosionClashEffectFramesPerSecond, explosionClashEffectScale, impactDirection, explosionClashEffectLifetime, explosionClashEffectSortingOrder);
                PushAoe(attacker, collisionPoint, explosionRadius, explosionForce, true);
                break;

            case FusionType.Flood:
                PushAoe(attacker, collisionPoint, explosionRadius, explosionForce, true);
                break;

            case FusionType.Quake:
                SpriteSheetEffect.SpawnEffect(collisionPoint, quakeImpactEffectPrefab, null, elementColor, quakeEffectFramesPerSecond, quakeImpactEffectScale, impactDirection, quakeEffectLifetime, quakeEffectSortingOrder);
                ChainLaunchWithSmallAoe(attacker, collisionPoint, quakeChainCount, quakeSearchRadius, quakeSmallAoeRadius, quakeSmallAoeForce);
                break;

            case FusionType.Ice:
                break;

            case FusionType.Blaze:
                SpriteSheetEffect.SpawnEffect(collisionPoint, blazeClashEffectPrefab, null, elementColor, blazeEffectFramesPerSecond, blazeClashEffectScale, impactDirection, blazeEffectLifetime, blazeEffectSortingOrder);
                ChainLaunch(attacker, collisionPoint, blazeChainCount, blazeSearchRadius, blazeChainForce, blazeChainStepDelay);
                break;

            case FusionType.Sand:
                SpriteSheetEffect.SpawnEffect(collisionPoint, sandClashEffectPrefab, null, elementColor, sandEffectFramesPerSecond, sandClashEffectScale, impactDirection, sandEffectLifetime, sandEffectSortingOrder);
                if (ArenaManager.Instance != null)
                {
                    ArenaManager.Instance.RequestSkipNextEnemyTurn();
                }
                break;

            case FusionType.Steam:
                SteamChain(attacker, collisionPoint);
                break;

            case FusionType.Lava:
                if (victim != null)
                {
                    Vector2 pushDirection = (victim.position - attacker.position).normalized;
                    if (pushDirection == Vector2.zero) pushDirection = Random.insideUnitCircle.normalized;
                    victim.AddForce(pushDirection * lavaClashForce, ForceMode2D.Impulse);
                }
                break;

            case FusionType.Mud:
                ApplyMudSticky(attacker, victim);
                break;
        }

        Debug.Log($"{fusionType} fusion triggered.");
    }

    private void PushAoe(Rigidbody2D attacker, Vector2 center, float radius, float force, bool resetVelocity)
    {
        DrawDebugCircle(center, radius, elementColor, 1.5f);
        Collider2D[] surroundingObjects = Physics2D.OverlapCircleAll(center, radius);

        foreach (Collider2D col in surroundingObjects)
        {
            Rigidbody2D rb = col.GetComponent<Rigidbody2D>();
            if (rb == null || rb == attacker) continue;

            if (resetVelocity)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            Vector2 pushDirection = (rb.position - center).normalized;
            if (pushDirection == Vector2.zero) pushDirection = Random.insideUnitCircle.normalized;
            rb.AddForce(pushDirection * force, ForceMode2D.Impulse);
        }
    }

    private void ChainLaunch(Rigidbody2D attacker, Vector2 center, int count, float searchRadius, float force, float stepDelay = 0f)
    {
        List<Rigidbody2D> nearest = FindNearestMarbles(attacker, center, count, searchRadius);
        if (nearest.Count <= 0) return;

        ElementChainSequence.Run("BlazeChainSequence", nearest, stepDelay, rb =>
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            if (randomDirection == Vector2.zero) randomDirection = Vector2.up;

            SpriteSheetEffect.SpawnEffect(rb.position, blazeChainEffectPrefab, null, elementColor, blazeEffectFramesPerSecond, blazeChainEffectScale, blazeEffectLifetime, blazeEffectSortingOrder);
            rb.AddForce(randomDirection * force, ForceMode2D.Impulse);
        });
    }

    private void ChainLaunchWithSmallAoe(Rigidbody2D attacker, Vector2 center, int count, float searchRadius, float smallRadius, float smallForce)
    {
        List<Rigidbody2D> nearest = FindNearestMarbles(attacker, center, count, searchRadius);
        if (nearest.Count <= 0) return;

        ElementChainSequence.Run("QuakeChainSequence", nearest, quakeChainStepDelay, rb =>
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            if (randomDirection == Vector2.zero) randomDirection = Vector2.up;

            Vector2 effectDirection = rb.position - center;
            if (effectDirection.sqrMagnitude <= 0.001f) effectDirection = randomDirection;
            SpriteSheetEffect.SpawnEffect(rb.position, quakeAffectedMarbleEffectPrefab, quakeAffectedMarbleEffectFrames, elementColor, quakeEffectFramesPerSecond, quakeAffectedMarbleEffectScale, effectDirection, quakeEffectLifetime, quakeEffectSortingOrder);
            if (useQuakeDustBurst)
            {
                QuakeDustBurstEffect.Spawn(rb.position, quakeDustStartColor, quakeDustEndColor, quakeDustLifetime, quakeDustSpeed, quakeDustStartSize, quakeDustParticleCount, quakeEffectSortingOrder, quakeParticleMaterial);
            }
            PushAoe(attacker, rb.position, smallRadius, smallForce, false);
        });
    }

    private void SteamChain(Rigidbody2D attacker, Vector2 center)
    {
        List<Rigidbody2D> nearest = FindNearestMarbles(attacker, center, steamChainCount, steamSearchRadius);
        ApplySteamFloaty(attacker);

        foreach (Rigidbody2D rb in nearest)
        {
            ApplySteamFloaty(rb);
        }
    }

    private void ApplySteamFloaty(Rigidbody2D rb)
    {
        if (rb == null) return;

        SteamLaunchBuff buff = rb.GetComponent<SteamLaunchBuff>();
        if (buff == null) buff = rb.gameObject.AddComponent<SteamLaunchBuff>();
        buff.Configure(steamMassMultiplier, steamBuffDuration, steamLinearDampingMultiplier, steamAngularDampingMultiplier, steamFriction);
    }

    private void ApplyMudSticky(Rigidbody2D attacker, Rigidbody2D victim)
    {
        MudStickyEffect.TryStick(attacker, victim, mudMaxStickCount, mudImmediateVelocityMultiplier);
    }

    private List<Rigidbody2D> FindNearestMarbles(Rigidbody2D attacker, Vector2 center, int count, float searchRadius)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(center, searchRadius);
        List<Rigidbody2D> marbles = new List<Rigidbody2D>();

        foreach (Collider2D col in colliders)
        {
            Rigidbody2D rb = col.GetComponent<Rigidbody2D>();
            if (rb == null || rb == attacker || marbles.Contains(rb)) continue;

            marbles.Add(rb);
        }

        marbles.Sort((a, b) =>
        {
            float distanceA = Vector2.SqrMagnitude(a.position - center);
            float distanceB = Vector2.SqrMagnitude(b.position - center);
            return distanceA.CompareTo(distanceB);
        });

        if (marbles.Count > count)
        {
            marbles.RemoveRange(count, marbles.Count - count);
        }

        return marbles;
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
