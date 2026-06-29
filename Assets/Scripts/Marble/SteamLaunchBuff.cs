using UnityEngine;

public class SteamLaunchBuff : MonoBehaviour
{
    public const string ModifierId = "SteamFloaty";

    public float massMultiplier = 0.5f;
    public float duration = 8f;
    public float linearDampingMultiplier = 0.2f;
    public float angularDampingMultiplier = 0.35f;
    public float friction = 0f;

    private MarblePhysicsModifier physicsModifier;

    private void Awake()
    {
        physicsModifier = GetComponent<MarblePhysicsModifier>();
        if (physicsModifier == null)
        {
            physicsModifier = gameObject.AddComponent<MarblePhysicsModifier>();
        }
    }

    private void Start()
    {
        ApplyBuff();
    }

    public void Configure(float newMassMultiplier, float newDuration)
    {
        Configure(newMassMultiplier, newDuration, linearDampingMultiplier, angularDampingMultiplier, friction);
    }

    public void Configure(float newMassMultiplier, float newDuration, float newLinearDampingMultiplier, float newAngularDampingMultiplier, float newFriction)
    {
        massMultiplier = newMassMultiplier;
        duration = newDuration;
        linearDampingMultiplier = newLinearDampingMultiplier;
        angularDampingMultiplier = newAngularDampingMultiplier;
        friction = newFriction;

        if (physicsModifier != null)
        {
            ApplyBuff();
        }
    }

    private void ApplyBuff()
    {
        if (physicsModifier == null) return;
        physicsModifier.ApplyTimedModifier(ModifierId, duration, massMultiplier, linearDampingMultiplier, angularDampingMultiplier, friction);
        CancelInvoke();
        Invoke(nameof(Expire), duration);
    }

    private void Expire()
    {
        Destroy(this);
    }

    public static bool IsActive(GameObject target)
    {
        if (target == null) return false;

        MarblePhysicsModifier modifier = target.GetComponent<MarblePhysicsModifier>();
        if (modifier != null && modifier.HasModifier(ModifierId))
        {
            return true;
        }

        SteamLaunchBuff steamBuff = target.GetComponent<SteamLaunchBuff>();
        return steamBuff != null;
    }
}
