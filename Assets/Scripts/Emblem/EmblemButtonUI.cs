using UnityEngine;

public class EmblemButtonUI : MonoBehaviour
{
    public BaseEmblemSO emblem;

    public void OnButtonPressed()
    {
        if (emblem == null)
            return;

        if (emblem is LuckyDrawSO luckyDraw)
        {
            BaseEmblemSO reward = luckyDraw.RollReward();

            if (reward != null)
            {
                CurrentEmblemManager.Instance.SelectEmblem(reward);

                Debug.Log($"🎲 Lucky Draw menghasilkan: {reward.emblemName}");
            }

            return;
        }

        // Skill instant
        if (emblem.IsInstantSkill())
        {
            Debug.Log("INSTANT SKILL DETECTED");

            emblem.Activate(null);
            return;
        }
        

        CurrentEmblemManager.Instance.SelectEmblem(emblem);

        Debug.Log($"Selected {emblem.emblemName}");
    }
}