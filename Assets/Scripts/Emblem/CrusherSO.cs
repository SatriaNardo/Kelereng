using UnityEngine;

[CreateAssetMenu(
    fileName = "Crusher",
    menuName = "Emblems/Offensive/Crusher")]
public class CrusherSO : BaseEmblemSO
{
    public float damageMultiplier = 1.5f;

    public override void Activate(GameObject marble)
    {
        MarbleDamageBuff buff =
            marble.AddComponent<MarbleDamageBuff>();

        buff.damageMultiplier = damageMultiplier;

        Debug.Log($"Crusher Activated pada {marble.name}");
    }
}