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
        Dust,
        Steam,
        Lava
    }

    [Header("Fusion Type Settings")]
    public FusionType fusionType;

    [Header("Cyclone")]
    public float cycloneRadius = 0.5f;
    public float cycloneForce = 5f;

    [Header("Explosion/Flood")]
    public float explosionRadius = 1f;
    public float explosionForce = 3f;

    [Header("Quake")]
    public int quakeChainCount = 5;
    public float quakeLaunchForce = 1f;
    public float quakeSmallAoeRadius = 0.65f;
    public float quakeSmallAoeForce = 1f;
    public float quakeSearchRadius = 6f;

    [Header("Ice")]
    public float iceTrailSpotSpacing = 0.25f;
    public float iceTrailSpotSize = 0.45f;

    [Header("Blaze")]
    public int blazeChainCount = 6;
    public float blazeChainForce = 5f;
    public float blazeSearchRadius = 7f;

    [Header("Steam")]
    public int steamChainCount = 6;
    public float steamChainForce = 8f;
    public float steamSearchRadius = 7f;
    public float steamMassMultiplier = 0.5f;
    public float steamBuffDuration = 8f;

    [Header("Lava")]
    public float lavaLaunchMultiplier = 1.2f;
    public float lavaClashForce = 5f;
    public float lavaPreserveSpeedMultiplier = 0.95f;

    public override float GetLaunchForceMultiplier(float baseMultiplier)
    {
        if (fusionType == FusionType.Lava)
        {
            return baseMultiplier * lavaLaunchMultiplier;
        }

        return baseMultiplier;
    }

    public override void OnLaunch(Rigidbody2D marble)
    {
        if (marble == null) return;

        if (fusionType == FusionType.Ice)
        {
            IceTrailEffect trail = marble.gameObject.GetComponent<IceTrailEffect>();
            if (trail == null) trail = marble.gameObject.AddComponent<IceTrailEffect>();

            trail.spotSpacing = iceTrailSpotSpacing;
            trail.spotSize = iceTrailSpotSize;
            trail.iceColor = elementColor;
        }
        else if (fusionType == FusionType.Lava)
        {
            LavaPierceEffect pierce = marble.gameObject.GetComponent<LavaPierceEffect>();
            if (pierce == null) pierce = marble.gameObject.AddComponent<LavaPierceEffect>();

            pierce.preserveSpeedMultiplier = lavaPreserveSpeedMultiplier;
        }
    }

    public override void OnClash(Rigidbody2D attacker, Rigidbody2D victim, Vector2 collisionPoint)
    {
        switch (fusionType)
        {
            case FusionType.Cyclone:
                PushAoe(attacker, collisionPoint, cycloneRadius, cycloneForce, false);
                break;

            case FusionType.Explosion:
            case FusionType.Flood:
                PushAoe(attacker, collisionPoint, explosionRadius, explosionForce, true);
                break;

            case FusionType.Quake:
                ChainLaunchWithSmallAoe(attacker, collisionPoint, quakeChainCount, quakeSearchRadius, quakeLaunchForce, quakeSmallAoeRadius, quakeSmallAoeForce);
                break;

            case FusionType.Ice:
                break;

            case FusionType.Blaze:
                ChainLaunch(attacker, collisionPoint, blazeChainCount, blazeSearchRadius, blazeChainForce);
                break;

            case FusionType.Dust:
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

    private void ChainLaunch(Rigidbody2D attacker, Vector2 center, int count, float searchRadius, float force)
    {
        List<Rigidbody2D> nearest = FindNearestMarbles(attacker, center, count, searchRadius);

        foreach (Rigidbody2D rb in nearest)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            if (randomDirection == Vector2.zero) randomDirection = Vector2.up;
            rb.AddForce(randomDirection * force, ForceMode2D.Impulse);
        }
    }

    private void ChainLaunchWithSmallAoe(Rigidbody2D attacker, Vector2 center, int count, float searchRadius, float force, float smallRadius, float smallForce)
    {
        List<Rigidbody2D> nearest = FindNearestMarbles(attacker, center, count, searchRadius);

        foreach (Rigidbody2D rb in nearest)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            if (randomDirection == Vector2.zero) randomDirection = Vector2.up;

            rb.AddForce(randomDirection * force, ForceMode2D.Impulse);
            PushAoe(attacker, rb.position, smallRadius, smallForce, false);
        }
    }

    private void SteamChain(Rigidbody2D attacker, Vector2 center)
    {
        List<Rigidbody2D> nearest = FindNearestMarbles(attacker, center, steamChainCount, steamSearchRadius);

        foreach (Rigidbody2D rb in nearest)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            if (randomDirection == Vector2.zero) randomDirection = Vector2.up;

            rb.AddForce(randomDirection * steamChainForce, ForceMode2D.Impulse);

            SteamLaunchBuff buff = rb.GetComponent<SteamLaunchBuff>();
            if (buff == null) buff = rb.gameObject.AddComponent<SteamLaunchBuff>();
            buff.Configure(steamMassMultiplier, steamBuffDuration);
        }
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
