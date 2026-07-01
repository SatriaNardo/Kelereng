using UnityEngine;

[CreateAssetMenu(
    fileName = "RicochetMaster",
    menuName = "Emblems/Utility/Ricochet Master")]
public class RicochetMasterSO : BaseEmblemSO
{
    [Range(1f, 2f)]
    public float bounceMultiplier = 1.2f;

    public override void Activate(GameObject marble)
    {
        if (marble == null) return;

        RicochetBuff buff = marble.GetComponent<RicochetBuff>();
        if (buff == null)
        {
            buff = marble.AddComponent<RicochetBuff>();
        }

        buff.bounceMultiplier = bounceMultiplier;

        Debug.Log("🪃 Ricochet Master Activated!");
    }
}
