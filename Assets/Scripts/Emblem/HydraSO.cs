using UnityEngine;

[CreateAssetMenu(
    fileName = "Hydra",
    menuName = "Emblems/Offensive/Hydra")]
public class HydraSO : BaseEmblemSO
{
    [Range(1, 3)]
    public int splitCount = 2;

    public float spreadAngle = 20f;

    public override void Activate(GameObject marble)
    {
        HydraBuff buff = marble.AddComponent<HydraBuff>();

        buff.splitCount = splitCount;
        buff.spreadAngle = spreadAngle;

        Debug.Log("🐍 Hydra Activated!");
    }
}