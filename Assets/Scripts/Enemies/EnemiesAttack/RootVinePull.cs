using System.Collections.Generic;
using UnityEngine;

public class RootVinePull : MonoBehaviour
{
    [Header("Pull Settings")]
    public float pullRadius = 1.1f;
    public float pullStrength = 14f;
    [Tooltip("Keeps some pull at the edge so marbles are not easy to fling out.")]
    [Range(0f, 1f)] public float minEdgePull = 0.25f;
    [Tooltip("Reduces outward velocity while snared to prevent overshooting through center.")]
    [Range(0f, 1f)] public float outwardVelocityDamping = 0.75f;
    [Tooltip("Extra distance before a snared marble fully escapes the vine.")]
    public float snareReleasePadding = 0.12f;

    [Header("Per-Marble Root Vine")]
    public int stalkSegmentCount = 6;
    public float stalkThickness = 0.06f;

    private readonly Dictionary<int, MarbleRootSnareVisual> activeSnares = new Dictionary<int, MarbleRootSnareVisual>();
    private readonly HashSet<int> processedBodies = new HashSet<int>();

    public void Initialize(Vector2 center, float radius, float strength)
    {
        transform.position = center;
        pullRadius = radius;
        pullStrength = strength;
        ClearSnares();
    }

    private void FixedUpdate()
    {
        Vector2 center = transform.position;
        float queryRadius = pullRadius + snareReleasePadding;
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(center, queryRadius);
        HashSet<int> affectedMarbles = new HashSet<int>();
        processedBodies.Clear();

        foreach (Collider2D overlap in overlaps)
        {
            Rigidbody2D body = overlap.attachedRigidbody;
            if (body == null || body.bodyType != RigidbodyType2D.Dynamic)
            {
                continue;
            }

            int marbleId = body.GetInstanceID();
            if (!processedBodies.Add(marbleId))
            {
                continue;
            }

            bool alreadySnared = activeSnares.ContainsKey(marbleId);
            Vector2 toCenter = center - body.position;
            float distance = toCenter.magnitude;
            float releaseRadius = pullRadius + snareReleasePadding;

            if (!alreadySnared && distance > pullRadius)
            {
                continue;
            }

            if (alreadySnared && distance > releaseRadius)
            {
                continue;
            }

            affectedMarbles.Add(marbleId);

            if (distance > 0.05f)
            {
                Vector2 pullDirection = toCenter / distance;
                DampOutwardVelocity(body, pullDirection, outwardVelocityDamping);

                float normalizedDistance = Mathf.Clamp01(distance / pullRadius);
                float falloff = Mathf.Lerp(minEdgePull, 1f, 1f - normalizedDistance);
                body.AddForce(pullDirection * (pullStrength * falloff * body.mass), ForceMode2D.Force);
            }

            if (!activeSnares.TryGetValue(marbleId, out MarbleRootSnareVisual snare) || snare == null)
            {
                snare = CreateSnare(body);
                activeSnares[marbleId] = snare;
            }

            snare.UpdateSnare(center, body.position);
        }

        RemoveInactiveSnares(affectedMarbles);
    }

    private static void DampOutwardVelocity(Rigidbody2D body, Vector2 pullDirection, float damping)
    {
        Vector2 velocity = body.linearVelocity;
        float outwardSpeed = Vector2.Dot(velocity, -pullDirection);
        if (outwardSpeed <= 0f)
        {
            return;
        }

        body.linearVelocity = velocity + pullDirection * outwardSpeed * damping;
    }

    private MarbleRootSnareVisual CreateSnare(Rigidbody2D body)
    {
        GameObject snareObject = new GameObject($"RootSnare_{body.name}");
        snareObject.transform.SetParent(transform, false);

        MarbleRootSnareVisual snare = snareObject.AddComponent<MarbleRootSnareVisual>();
        snare.Build(stalkSegmentCount, stalkThickness);
        snare.UpdateSnare(transform.position, body.position);
        return snare;
    }

    private void RemoveInactiveSnares(HashSet<int> affectedMarbles)
    {
        List<int> staleIds = new List<int>();

        foreach (KeyValuePair<int, MarbleRootSnareVisual> entry in activeSnares)
        {
            if (!affectedMarbles.Contains(entry.Key) || entry.Value == null)
            {
                staleIds.Add(entry.Key);
            }
        }

        foreach (int staleId in staleIds)
        {
            if (activeSnares.TryGetValue(staleId, out MarbleRootSnareVisual snare) && snare != null)
            {
                Destroy(snare.gameObject);
            }

            activeSnares.Remove(staleId);
        }
    }

    private void ClearSnares()
    {
        foreach (KeyValuePair<int, MarbleRootSnareVisual> entry in activeSnares)
        {
            if (entry.Value != null)
            {
                Destroy(entry.Value.gameObject);
            }
        }

        activeSnares.Clear();
    }

    private void OnDestroy()
    {
        ClearSnares();
    }
}
