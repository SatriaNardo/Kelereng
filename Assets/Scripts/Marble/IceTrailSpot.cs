using UnityEngine;

public class IceTrailSpot : MonoBehaviour
{
    public float modifierRefreshDuration = 0.18f;
    public float linearDampingMultiplier = 0.08f;
    public float angularDampingMultiplier = 0.15f;
    public float friction = 0f;
    public Rigidbody2D ownerRb;

    private const string ModifierId = "IceTrailLowFriction";
    private CircleCollider2D triggerCollider;

    private void Awake()
    {
        EnsureTriggerCollider();
    }

    public void Configure(float radius, float newLinearDampingMultiplier, float newAngularDampingMultiplier, float newFriction)
    {
        Configure(radius, newLinearDampingMultiplier, newAngularDampingMultiplier, newFriction, null);
    }

    public void Configure(float radius, float newLinearDampingMultiplier, float newAngularDampingMultiplier, float newFriction, Rigidbody2D newOwnerRb)
    {
        linearDampingMultiplier = newLinearDampingMultiplier;
        angularDampingMultiplier = newAngularDampingMultiplier;
        friction = newFriction;
        ownerRb = newOwnerRb;
        EnsureTriggerCollider();
        triggerCollider.radius = Mathf.Max(0.01f, radius);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
        if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic) return;
        if (rb == ownerRb) return;

        MarblePhysicsModifier modifier = collision.GetComponent<MarblePhysicsModifier>();
        if (modifier == null)
        {
            modifier = collision.gameObject.AddComponent<MarblePhysicsModifier>();
        }

        modifier.ApplyTimedModifier(ModifierId, modifierRefreshDuration, 1f, linearDampingMultiplier, angularDampingMultiplier, friction);
    }

    private void EnsureTriggerCollider()
    {
        triggerCollider = GetComponent<CircleCollider2D>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<CircleCollider2D>();
        }

        triggerCollider.isTrigger = true;
    }
}
