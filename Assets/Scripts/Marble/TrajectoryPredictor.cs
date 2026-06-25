using System.Collections.Generic;
using UnityEngine;

public static class TrajectoryPredictor
{
    private const float CastSkin = 0.001f;
    private const float SurfacePushOff = 0.025f;
    private const float MinSegmentSpeed = 0.05f;
    private const float ShooterMass = 1f;

    public static List<Vector2> BuildTrajectoryPoints(
        Vector2 origin,
        Vector2 direction,
        float segmentLength,
        float circleRadius,
        int bounceCount,
        Collider2D ignoreCollider = null,
        IReadOnlyList<CircleCollider2D> marbleColliders = null,
        PhysicsMaterial2D shooterMaterial = null)
    {
        List<Vector2> points = new List<Vector2> { origin };

        if (segmentLength <= 0.001f || direction.sqrMagnitude <= 0.0001f)
        {
            points.Add(origin);
            return points;
        }

        Vector2 currentPos = origin;
        Vector2 currentVelocity = direction.normalized * segmentLength;
        int segmentsToTrace = bounceCount > 0 ? bounceCount + 1 : 1;

        for (int segment = 0; segment < segmentsToTrace; segment++)
        {
            float currentSpeed = currentVelocity.magnitude;
            if (currentSpeed <= MinSegmentSpeed)
            {
                break;
            }

            Vector2 currentDir = currentVelocity / currentSpeed;

            if (!TryGetNextHit(
                    currentPos,
                    currentDir,
                    currentSpeed,
                    circleRadius,
                    ignoreCollider,
                    marbleColliders,
                    shooterMaterial,
                    out TrajectoryHit hit))
            {
                points.Add(currentPos + currentDir * currentSpeed);
                break;
            }

            Vector2 impactCenter = currentPos + currentDir * hit.distance;
            points.Add(impactCenter);

            if (segment >= bounceCount)
            {
                break;
            }

            Vector2 bounceVelocity = ResolveBounceVelocity(currentVelocity, hit);
            if (bounceVelocity.sqrMagnitude <= MinSegmentSpeed * MinSegmentSpeed)
            {
                break;
            }

            currentPos = impactCenter + hit.normal * SurfacePushOff;
            currentVelocity = bounceVelocity;
        }

        return points;
    }

    private static Vector2 ResolveBounceVelocity(Vector2 velocity, TrajectoryHit hit)
    {
        Vector2 relativeVelocity = velocity - hit.targetVelocity;
        float relativeNormalSpeed = Vector2.Dot(relativeVelocity, hit.normal);

        if (relativeNormalSpeed > 0f)
        {
            return velocity;
        }

        float inverseMassSum = (1f / ShooterMass) + (hit.isMarbleHit ? 1f / hit.targetMass : 0f);
        float normalImpulse = -(1f + hit.restitution) * relativeNormalSpeed / inverseMassSum;
        Vector2 newVelocity = velocity + (normalImpulse / ShooterMass) * hit.normal;

        if (hit.friction <= 0f)
        {
            return newVelocity;
        }

        Vector2 tangent = new Vector2(-hit.normal.y, hit.normal.x);
        float relativeTangentSpeed = Vector2.Dot(relativeVelocity, tangent);
        float tangentImpulse = -relativeTangentSpeed / inverseMassSum;
        float maxFrictionImpulse = hit.friction * Mathf.Abs(normalImpulse);
        tangentImpulse = Mathf.Clamp(tangentImpulse, -maxFrictionImpulse, maxFrictionImpulse);

        return newVelocity + (tangentImpulse / ShooterMass) * tangent;
    }

    private static bool TryGetNextHit(
        Vector2 origin,
        Vector2 direction,
        float distance,
        float radius,
        Collider2D ignoreCollider,
        IReadOnlyList<CircleCollider2D> marbleColliders,
        PhysicsMaterial2D shooterMaterial,
        out TrajectoryHit closestHit)
    {
        closestHit = default;
        float closestDistance = float.MaxValue;
        bool found = false;

        if (marbleColliders != null)
        {
            for (int i = 0; i < marbleColliders.Count; i++)
            {
                CircleCollider2D marbleCollider = marbleColliders[i];
                if (marbleCollider == null || marbleCollider == ignoreCollider || !marbleCollider.enabled)
                {
                    continue;
                }

                if (!TryGetMarbleHit(
                        origin,
                        direction,
                        distance,
                        radius,
                        marbleCollider,
                        shooterMaterial,
                        out TrajectoryHit marbleHit))
                {
                    continue;
                }

                if (marbleHit.distance < closestDistance)
                {
                    closestDistance = marbleHit.distance;
                    closestHit = marbleHit;
                    found = true;
                }
            }
        }

        RaycastHit2D[] hits = new RaycastHit2D[16];
        int hitCount = Physics2D.CircleCast(
            origin,
            radius,
            direction,
            ContactFilter2D.noFilter,
            hits,
            distance);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = hits[i];
            if (hit.collider == null || hit.collider == ignoreCollider || hit.collider is CircleCollider2D)
            {
                continue;
            }

            float hitDistance = hit.distance;
            if (hitDistance <= CastSkin || hitDistance > distance || hitDistance >= closestDistance)
            {
                continue;
            }

            Vector2 impactCenter = origin + direction * hitDistance;
            Vector2 normal = GetSurfaceNormal(hit.collider, impactCenter, direction);
            GetCombinedMaterial(
                shooterMaterial,
                hit.collider.sharedMaterial,
                out float restitution,
                out float friction);

            closestDistance = hitDistance;
            closestHit = new TrajectoryHit
            {
                distance = hitDistance,
                normal = normal,
                isMarbleHit = false,
                restitution = restitution,
                friction = friction
            };
            found = true;
        }

        return found;
    }

    private static bool TryGetMarbleHit(
        Vector2 origin,
        Vector2 direction,
        float distance,
        float radius,
        CircleCollider2D marbleCollider,
        PhysicsMaterial2D shooterMaterial,
        out TrajectoryHit hit)
    {
        hit = default;

        Vector2 otherCenter = marbleCollider.transform.TransformPoint(marbleCollider.offset);
        float otherRadius = GetWorldRadius(marbleCollider);
        float combinedRadius = radius + otherRadius;

        Vector2 offset = origin - otherCenter;
        float projection = Vector2.Dot(offset, direction);
        float discriminant = projection * projection - (offset.sqrMagnitude - combinedRadius * combinedRadius);

        if (discriminant < 0f)
        {
            return false;
        }

        float sqrtDiscriminant = Mathf.Sqrt(discriminant);
        float hitDistance = -projection - sqrtDiscriminant;

        if (hitDistance < 0f)
        {
            hitDistance = -projection + sqrtDiscriminant;
        }

        if (hitDistance <= CastSkin || hitDistance > distance)
        {
            return false;
        }

        Vector2 impactCenter = origin + direction * hitDistance;
        Vector2 normal = impactCenter - otherCenter;
        if (normal.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        normal.Normalize();
        Rigidbody2D targetBody = marbleCollider.attachedRigidbody;
        GetCombinedMaterial(
            shooterMaterial,
            marbleCollider.sharedMaterial,
            out float restitution,
            out float friction);

        hit = new TrajectoryHit
        {
            distance = hitDistance,
            normal = normal,
            isMarbleHit = true,
            targetMass = targetBody != null ? targetBody.mass : ShooterMass,
            targetVelocity = targetBody != null ? targetBody.linearVelocity : Vector2.zero,
            restitution = restitution,
            friction = friction
        };

        return true;
    }

    private static Vector2 GetSurfaceNormal(Collider2D collider, Vector2 impactCenter, Vector2 incomingDirection)
    {
        Vector2 closestPoint = collider.ClosestPoint(impactCenter);
        Vector2 normal = impactCenter - closestPoint;

        if (normal.sqrMagnitude <= 0.0001f)
        {
            normal = -incomingDirection;
        }
        else
        {
            normal.Normalize();
        }

        if (Vector2.Dot(normal, incomingDirection) > 0f)
        {
            normal = -normal;
        }

        return normal;
    }

    private static void GetCombinedMaterial(
        PhysicsMaterial2D materialA,
        PhysicsMaterial2D materialB,
        out float restitution,
        out float friction)
    {
        float bounceA = materialA != null ? materialA.bounciness : 0.9f;
        float bounceB = materialB != null ? materialB.bounciness : 0.9f;
        float frictionA = materialA != null ? materialA.friction : 0.05f;
        float frictionB = materialB != null ? materialB.friction : 0.05f;

        restitution = CombineBounciness(materialA, materialB, bounceA, bounceB);
        friction = CombineFriction(materialA, materialB, frictionA, frictionB);
    }

    private static float CombineBounciness(
        PhysicsMaterial2D materialA,
        PhysicsMaterial2D materialB,
        float bounceA,
        float bounceB)
    {
        PhysicsMaterialCombine2D combine = GetDominantCombine(
            materialA != null ? materialA.bounceCombine : PhysicsMaterialCombine2D.Maximum,
            materialB != null ? materialB.bounceCombine : PhysicsMaterialCombine2D.Maximum);

        return ApplyCombine(combine, bounceA, bounceB);
    }

    private static float CombineFriction(
        PhysicsMaterial2D materialA,
        PhysicsMaterial2D materialB,
        float frictionA,
        float frictionB)
    {
        PhysicsMaterialCombine2D combine = GetDominantCombine(
            materialA != null ? materialA.frictionCombine : PhysicsMaterialCombine2D.Average,
            materialB != null ? materialB.frictionCombine : PhysicsMaterialCombine2D.Average);

        return ApplyCombine(combine, frictionA, frictionB);
    }

    private static PhysicsMaterialCombine2D GetDominantCombine(
        PhysicsMaterialCombine2D combineA,
        PhysicsMaterialCombine2D combineB)
    {
        return (int)combineA >= (int)combineB ? combineA : combineB;
    }

    private static float ApplyCombine(PhysicsMaterialCombine2D combine, float valueA, float valueB)
    {
        switch (combine)
        {
            case PhysicsMaterialCombine2D.Multiply:
                return valueA * valueB;
            case PhysicsMaterialCombine2D.Minimum:
                return Mathf.Min(valueA, valueB);
            case PhysicsMaterialCombine2D.Maximum:
                return Mathf.Max(valueA, valueB);
            default:
                return (valueA + valueB) * 0.5f;
        }
    }

    private static float GetWorldRadius(CircleCollider2D circleCollider)
    {
        Vector3 scale = circleCollider.transform.lossyScale;
        float uniformScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        return circleCollider.radius * uniformScale;
    }

    private struct TrajectoryHit
    {
        public float distance;
        public Vector2 normal;
        public bool isMarbleHit;
        public float targetMass;
        public Vector2 targetVelocity;
        public float restitution;
        public float friction;
    }
}
