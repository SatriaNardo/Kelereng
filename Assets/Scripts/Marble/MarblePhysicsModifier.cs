using System.Collections.Generic;
using UnityEngine;

public class MarblePhysicsModifier : MonoBehaviour
{
    private class Modifier
    {
        public string id;
        public float expiresAt;
        public float massMultiplier;
        public float linearDampingMultiplier;
        public float angularDampingMultiplier;
        public float friction;
    }

    private readonly List<Modifier> modifiers = new List<Modifier>();
    private readonly List<PhysicsMaterial2D> generatedMaterials = new List<PhysicsMaterial2D>();

    private Rigidbody2D rb;
    private Collider2D[] colliders;
    private PhysicsMaterial2D[] originalMaterials;
    private float originalMass;
    private float originalLinearDamping;
    private float originalAngularDamping;
    private bool hasOriginalValues;

    private void Awake()
    {
        CacheOriginalValues();
    }

    private void Update()
    {
        float now = Time.time;
        bool removedAny = false;
        for (int i = modifiers.Count - 1; i >= 0; i--)
        {
            if (modifiers[i].expiresAt <= now)
            {
                modifiers.RemoveAt(i);
                removedAny = true;
            }
        }

        if (removedAny)
        {
            ApplyCurrentModifiers();
        }
    }

    public void ApplyTimedModifier(string id, float duration, float massMultiplier, float linearDampingMultiplier, float angularDampingMultiplier, float friction)
    {
        if (string.IsNullOrEmpty(id)) return;

        CacheOriginalValues();
        float safeDuration = Mathf.Max(0.02f, duration);
        Modifier modifier = modifiers.Find(existingModifier => existingModifier.id == id);
        if (modifier == null)
        {
            modifier = new Modifier { id = id };
            modifiers.Add(modifier);
        }
        else if (Mathf.Approximately(modifier.massMultiplier, Mathf.Max(0.01f, massMultiplier))
            && Mathf.Approximately(modifier.linearDampingMultiplier, Mathf.Max(0f, linearDampingMultiplier))
            && Mathf.Approximately(modifier.angularDampingMultiplier, Mathf.Max(0f, angularDampingMultiplier))
            && Mathf.Approximately(modifier.friction, friction))
        {
            modifier.expiresAt = Time.time + safeDuration;
            return;
        }

        modifier.expiresAt = Time.time + safeDuration;
        modifier.massMultiplier = Mathf.Max(0.01f, massMultiplier);
        modifier.linearDampingMultiplier = Mathf.Max(0f, linearDampingMultiplier);
        modifier.angularDampingMultiplier = Mathf.Max(0f, angularDampingMultiplier);
        modifier.friction = friction;

        ApplyCurrentModifiers();
    }

    public bool HasModifier(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;

        float now = Time.time;
        foreach (Modifier modifier in modifiers)
        {
            if (modifier.id == id && modifier.expiresAt > now)
            {
                return true;
            }
        }

        return false;
    }

    private void CacheOriginalValues()
    {
        if (hasOriginalValues) return;

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Destroy(this);
            return;
        }

        colliders = GetComponents<Collider2D>();
        originalMaterials = new PhysicsMaterial2D[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            originalMaterials[i] = colliders[i] != null ? colliders[i].sharedMaterial : null;
        }

        originalMass = rb.mass;
        originalLinearDamping = rb.linearDamping;
        originalAngularDamping = rb.angularDamping;
        hasOriginalValues = true;
    }

    private void ApplyCurrentModifiers()
    {
        if (rb == null || !hasOriginalValues) return;

        if (modifiers.Count == 0)
        {
            RestoreOriginalValues();
            return;
        }

        float massMultiplier = 1f;
        float linearDampingMultiplier = 1f;
        float angularDampingMultiplier = 1f;
        float friction = -1f;

        foreach (Modifier modifier in modifiers)
        {
            massMultiplier *= modifier.massMultiplier;
            linearDampingMultiplier = Mathf.Min(linearDampingMultiplier, modifier.linearDampingMultiplier);
            angularDampingMultiplier = Mathf.Min(angularDampingMultiplier, modifier.angularDampingMultiplier);
            if (modifier.friction >= 0f)
            {
                friction = friction < 0f ? modifier.friction : Mathf.Min(friction, modifier.friction);
            }
        }

        rb.mass = Mathf.Max(0.01f, originalMass * massMultiplier);
        rb.linearDamping = originalLinearDamping * linearDampingMultiplier;
        rb.angularDamping = originalAngularDamping * angularDampingMultiplier;

        if (friction >= 0f)
        {
            ApplyFriction(friction);
        }
        else
        {
            RestoreColliderMaterials();
        }
    }

    private void ApplyFriction(float friction)
    {
        ClearGeneratedMaterials();

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null) continue;

            PhysicsMaterial2D original = originalMaterials[i];
            PhysicsMaterial2D material = new PhysicsMaterial2D($"{gameObject.name}_LowFriction")
            {
                friction = Mathf.Max(0f, friction),
                bounciness = original != null ? original.bounciness : 0f
            };

            generatedMaterials.Add(material);
            colliders[i].sharedMaterial = material;
        }
    }

    private void RestoreOriginalValues()
    {
        if (rb != null)
        {
            rb.mass = originalMass;
            rb.linearDamping = originalLinearDamping;
            rb.angularDamping = originalAngularDamping;
        }

        RestoreColliderMaterials();
    }

    private void RestoreColliderMaterials()
    {
        if (colliders == null || originalMaterials == null) return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].sharedMaterial = originalMaterials[i];
            }
        }

        ClearGeneratedMaterials();
    }

    private void ClearGeneratedMaterials()
    {
        foreach (PhysicsMaterial2D material in generatedMaterials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        generatedMaterials.Clear();
    }

    private void OnDestroy()
    {
        RestoreOriginalValues();
    }
}
