using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EarthElement", menuName = "Elements/Earth")]
public class EarthElementSO : MarbleElementSO
{
    [Header("Earth Chain Settings")]
    public float searchRadius = 6f;
    public int chainedMarbleCount = 3;
    public float randomLaunchForce = 10f;

    public override void OnClash(Rigidbody2D attacker, Rigidbody2D victim, Vector2 collisionPoint, Vector2 impactDirection)
    {
        List<Rigidbody2D> nearbyMarbles = FindNearestMarbles(attacker, collisionPoint);

        int launchCount = Mathf.Min(chainedMarbleCount, nearbyMarbles.Count);
        for (int i = 0; i < launchCount; i++)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            if (randomDirection == Vector2.zero)
            {
                randomDirection = Vector2.up;
            }

            nearbyMarbles[i].AddForce(randomDirection * randomLaunchForce, ForceMode2D.Impulse);
        }

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
