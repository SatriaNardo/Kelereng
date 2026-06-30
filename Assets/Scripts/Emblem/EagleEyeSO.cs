using UnityEngine;

[CreateAssetMenu(
    fileName = "EagleEye",
    menuName = "Emblems/Utility/Eagle Eye")]
public class EagleEyeSO : BaseEmblemSO
{
    public override void Activate(GameObject marble)
    {
        Debug.Log("🦅 Eagle Eye Activated!");
    }
}