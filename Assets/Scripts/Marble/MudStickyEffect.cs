using System.Collections.Generic;
using UnityEngine;

public class MudStickyEffect : MonoBehaviour
{
    private class MudClump
    {
        public readonly List<MudStickyEffect> members = new List<MudStickyEffect>();
        public int maxMembers;
        public float immediateVelocityMultiplier;
    }

    private Rigidbody2D rb;
    private FixedJoint2D stickyJoint;
    private MudClump clump;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public static bool TryStick(Rigidbody2D source, Rigidbody2D target, int maxMembers, float immediateVelocityMultiplier)
    {
        if (source == null || target == null || source == target) return false;
        if (!IsValidStickTarget(target.gameObject)) return false;

        MudStickyEffect sourceSticky = source.GetComponent<MudStickyEffect>();
        if (sourceSticky == null)
        {
            sourceSticky = source.gameObject.AddComponent<MudStickyEffect>();
            sourceSticky.InitializeRoot(maxMembers, immediateVelocityMultiplier);
        }
        else
        {
            sourceSticky.EnsureClump(maxMembers, immediateVelocityMultiplier);
        }

        return sourceSticky.StickTarget(target);
    }

    private void InitializeRoot(int maxMembers, float immediateVelocityMultiplier)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        clump = new MudClump
        {
            maxMembers = Mathf.Max(1, maxMembers),
            immediateVelocityMultiplier = Mathf.Clamp01(immediateVelocityMultiplier)
        };
        clump.members.Add(this);
    }

    private void EnsureClump(int maxMembers, float immediateVelocityMultiplier)
    {
        if (clump == null)
        {
            InitializeRoot(maxMembers, immediateVelocityMultiplier);
            return;
        }

        clump.maxMembers = Mathf.Max(clump.maxMembers, Mathf.Max(1, maxMembers));
        clump.immediateVelocityMultiplier = Mathf.Clamp01(immediateVelocityMultiplier);
    }

    private bool StickTarget(Rigidbody2D target)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb == null || target == null || target == rb || clump == null) return false;
        if (clump.members.Count >= clump.maxMembers) return false;

        MudStickyEffect targetSticky = target.GetComponent<MudStickyEffect>();
        if (targetSticky != null)
        {
            return false;
        }

        targetSticky = target.gameObject.AddComponent<MudStickyEffect>();
        targetSticky.ConfigureAttached(this, clump.immediateVelocityMultiplier);
        clump.members.Add(targetSticky);
        return true;
    }

    private void ConfigureAttached(MudStickyEffect stickSource, float immediateVelocityMultiplier)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb == null || stickSource == null || stickSource.rb == null || stickSource.rb == rb || stickSource.clump == null)
        {
            Destroy(this);
            return;
        }

        clump = stickSource.clump;
        float safeVelocityMultiplier = Mathf.Clamp01(immediateVelocityMultiplier);
        SnapAgainstSource(stickSource.rb);
        rb.linearVelocity *= safeVelocityMultiplier;
        rb.angularVelocity *= safeVelocityMultiplier;

        if (stickyJoint == null)
        {
            stickyJoint = gameObject.AddComponent<FixedJoint2D>();
        }

        stickyJoint.connectedBody = stickSource.rb;
        stickyJoint.autoConfigureConnectedAnchor = true;
        stickyJoint.enableCollision = false;
        stickyJoint.breakForce = Mathf.Infinity;
        stickyJoint.breakTorque = Mathf.Infinity;
    }

    private void SnapAgainstSource(Rigidbody2D stickSource)
    {
        Vector2 direction = rb.position - stickSource.position;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = rb.linearVelocity.sqrMagnitude > 0.0001f ? rb.linearVelocity.normalized : Vector2.right;
        }
        else
        {
            direction.Normalize();
        }

        float sourceRadius = EstimateRadius(stickSource);
        float targetRadius = EstimateRadius(rb);
        float targetDistance = Mathf.Max(0.01f, sourceRadius + targetRadius);
        rb.position = stickSource.position + direction * targetDistance;
        Physics2D.SyncTransforms();
    }

    private static float EstimateRadius(Rigidbody2D body)
    {
        Collider2D[] colliders = body.GetComponents<Collider2D>();
        float radius = 0f;

        foreach (Collider2D col in colliders)
        {
            if (col == null || col.isTrigger) continue;

            Bounds bounds = col.bounds;
            radius = Mathf.Max(radius, Mathf.Max(bounds.extents.x, bounds.extents.y));
        }

        return radius > 0f ? radius : 0.25f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (clump == null || clump.members.Count >= clump.maxMembers) return;

        Rigidbody2D target = collision.gameObject.GetComponent<Rigidbody2D>();
        if (target == null) return;

        StickTarget(target);
    }

    private void OnDestroy()
    {
        if (clump != null)
        {
            clump.members.Remove(this);
        }

        if (stickyJoint != null)
        {
            Destroy(stickyJoint);
        }
    }

    private static bool IsValidStickTarget(GameObject other)
    {
        return other.CompareTag("TargetMarble")
            || other.CompareTag("PlayerMarble")
            || other.CompareTag("Gacoan")
            || other.GetComponent<TargetMarble>() != null
            || other.GetComponent<MarbleElementHandler>() != null;
    }
}
