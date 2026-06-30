using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EarthElement", menuName = "Elements/Earth")]
public class EarthElementSO : MarbleElementSO
{
    [Header("Earth Chain Settings")]
    public float searchRadius = 6f;
    public int chainedMarbleCount = 3;
    public float randomLaunchForce = 10f;

    [Header("Movement Earth Trail")]
    public bool useMovementTrail = true;
    public Material particleMaterial;
    public Sprite particleSprite;
    public Color trailStartColor = new Color(0.55f, 0.5f, 0.43f, 1f);
    public Color trailMidColor = new Color(0.35f, 0.25f, 0.16f, 1f);
    public Color trailEndColor = new Color(0.2f, 0.16f, 0.12f, 0f);
    public float trailParticlesPerSecond = 24f;
    public float trailLifetime = 0.3f;
    public float trailStartSize = 0.1f;
    public float trailSpeed = 0.55f;
    public int trailSortingOrder = 18;

    [Header("Earth Effects")]
    public GameObject earthClashEffectPrefab;
    public GameObject earthChainEffectPrefab;
    public Sprite[] earthChainEffectFrames;
    public float earthEffectFramesPerSecond = 18f;
    public float earthClashEffectScale = 1f;
    public float earthChainEffectScale = 1f;
    public float earthEffectLifetime = 1f;
    public int earthEffectSortingOrder = 20;

    [Header("Earth Chain Timing")]
    public float chainStepDelay = 0.12f;

    public override void OnLaunch(Rigidbody2D marble)
    {
        if (!useMovementTrail || marble == null) return;

        GenericMarbleTrailEffect trail = marble.GetComponent<GenericMarbleTrailEffect>();
        if (trail == null)
        {
            trail = marble.gameObject.AddComponent<GenericMarbleTrailEffect>();
        }

        trail.Configure("EarthMoveTrail", trailStartColor, trailMidColor, trailEndColor, trailParticlesPerSecond, trailLifetime, trailStartSize, trailSpeed, trailSortingOrder, particleMaterial, particleSprite);
    }

    public override void OnClash(Rigidbody2D attacker, Rigidbody2D victim, Vector2 collisionPoint, Vector2 impactDirection)
    {
        SpriteSheetEffect.SpawnEffect(collisionPoint, earthClashEffectPrefab, null, elementColor, earthEffectFramesPerSecond, earthClashEffectScale, impactDirection, earthEffectLifetime, earthEffectSortingOrder);

        List<Rigidbody2D> nearbyMarbles = FindNearestMarbles(attacker, collisionPoint);

        int launchCount = Mathf.Min(chainedMarbleCount, nearbyMarbles.Count);
        if (launchCount <= 0)
        {
            Debug.Log("Earth chain launched 0 marbles.");
            return;
        }

        List<Rigidbody2D> chainTargets = nearbyMarbles.GetRange(0, launchCount);
        ElementChainSequence.Run("EarthChainSequence", chainTargets, chainStepDelay, rb =>
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            if (randomDirection == Vector2.zero) randomDirection = Vector2.up;

            SpriteSheetEffect.SpawnEffect(rb.position, earthChainEffectPrefab, earthChainEffectFrames, elementColor, earthEffectFramesPerSecond, earthChainEffectScale, earthEffectLifetime, earthEffectSortingOrder);

            rb.AddForce(randomDirection * randomLaunchForce, ForceMode2D.Impulse);
        });

        Debug.Log($"Earth chain launched {launchCount} marbles.");
    }

    private List<Rigidbody2D> FindNearestMarbles(Rigidbody2D attacker, Vector2 collisionPoint)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(collisionPoint, searchRadius);
        List<Rigidbody2D> marbles = new List<Rigidbody2D>();

        foreach (Collider2D collider in colliders)
        {
            Rigidbody2D rb = collider.GetComponent<Rigidbody2D>();
            if (rb == null || rb == attacker || marbles.Contains(rb)) continue;

            marbles.Add(rb);
        }

        marbles.Sort((a, b) =>
        {
            float distanceA = Vector2.SqrMagnitude(a.position - collisionPoint);
            float distanceB = Vector2.SqrMagnitude(b.position - collisionPoint);
            return distanceA.CompareTo(distanceB);
        });

        return marbles;
    }
}
