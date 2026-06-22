using UnityEngine;

[CreateAssetMenu(fileName = "FireElement", menuName = "Elements/Fire")]
public class FireElementSO : MarbleElementSO
{
    [Header("Fire Settings")]
    public float launchSpeedMultiplier = 1.35f;

    public override float GetLaunchForceMultiplier(float baseMultiplier)
    {
        return baseMultiplier * launchSpeedMultiplier;
    }

    public override void OnClash(Rigidbody2D attacker, Rigidbody2D victim, Vector2 collisionPoint)
    {
        Debug.Log("Fire launch speed boost was already applied on shoot.");
    }
}
