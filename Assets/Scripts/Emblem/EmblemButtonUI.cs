using UnityEngine;

public class EmblemButtonUI : MonoBehaviour
{
    public BaseEmblemSO emblem;
    public bool requireOwnership = true;

    private void OnEnable()
    {
        RefreshOwnershipState();
    }

    public void OnButtonPressed()
    {
        if (emblem == null)
            return;

        if (!IsOwned())
        {
            Debug.Log($"Emblem is not owned yet: {GetEmblemDisplayName()}");
            return;
        }

        if (emblem.IsPassiveEmblem())
        {
            if (CurrentEmblemManager.Instance != null)
            {
                CurrentEmblemManager.Instance.RefreshPassiveEmblems();
            }

            Debug.Log($"Passive emblem is already active: {emblem.emblemName}");
            return;
        }

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

    public bool IsOwned()
    {
        if (!requireOwnership) return true;
        if (ProgressionManager.Instance == null) return true;
        return ProgressionManager.Instance.HasPlayableEmblem(emblem);
    }

    public void RefreshOwnershipState()
    {
        UnityEngine.UI.Button button = GetComponent<UnityEngine.UI.Button>();
        if (button != null)
        {
            button.interactable = IsOwned();
        }
    }

    private string GetEmblemDisplayName()
    {
        if (emblem == null) return "Emblem";
        return !string.IsNullOrWhiteSpace(emblem.emblemName) ? emblem.emblemName : emblem.name;
    }
}
