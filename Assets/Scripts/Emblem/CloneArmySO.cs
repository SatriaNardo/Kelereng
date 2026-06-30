using UnityEngine;

[CreateAssetMenu(
    fileName = "CloneArmy",
    menuName = "Emblems/Offensive/Clone Army")]
public class CloneArmySO : BaseEmblemSO
{
    [Range(1,3)]
    public int cloneCount = 2;

    public float spreadAngle = 12f;
}