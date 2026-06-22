using UnityEngine;

[CreateAssetMenu(fileName = "WaterElement", menuName = "Elements/Water")]
public class WaterElementSO : MarbleElementSO
{
    [Header("Water Settings")]
    public float extraKnockbackForce = 15f;

    public override void OnClash(Rigidbody2D attacker, Rigidbody2D victim, Vector2 collisionPoint)
    {
        if (attacker == null || victim == null) return;

        Vector2 forceDirection = (victim.position - attacker.position).normalized;
        if (forceDirection == Vector2.zero)
        {
            forceDirection = Random.insideUnitCircle.normalized;
        }

        victim.AddForce(forceDirection * extraKnockbackForce, ForceMode2D.Impulse);

        Debug.Log("Water extra knockback triggered.");
    }
}
