using UnityEngine;

[CreateAssetMenu(
    fileName = "RecallZone",
    menuName = "Emblems/Utility/Recall Zone")]
public class RecallZoneSO : BaseEmblemSO
{
    public override bool IsInstantSkill()
    {
        return true;
    }

    public override void Activate(GameObject marble)
    {
        if (ArenaManager.Instance == null)
            return;

        ArenaManager.Instance.RecallAllPlayerMarbles();

        Debug.Log("🌀 Recall Zone Activated!");
    }
}