using UnityEngine;

[CreateAssetMenu(fileName = "FireElement", menuName = "Elements/Fire")]
public class FireElementSO : MarbleElementSO
{
    [Header("Fire Settings")]
    public float extraKnockbackForce = 15f;

    public override void OnClash(Rigidbody2D attacker, Rigidbody2D victim, Vector2 collisionPoint)
    {
        if (victim == null) return;

        // Calculate direct impact push direction
        Vector2 forceDirection = (victim.position - attacker.position).normalized;

        // Apply massive direct momentum impulse
        victim.AddForce(forceDirection * extraKnockbackForce, ForceMode2D.Impulse);
        
        Debug.Log("🔥 Fire Extra Knockback triggered!");
    }
}