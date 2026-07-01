using UnityEngine;
using System.Collections.Generic;

public class CurrentEmblemManager : MonoBehaviour
{
    public static CurrentEmblemManager Instance;

    public BaseEmblemSO currentEmblem;
    private readonly List<BaseEmblemSO> passiveEmblems = new List<BaseEmblemSO>();
    private readonly List<BaseEmblemSO> consumedPassiveEmblems = new List<BaseEmblemSO>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        RefreshPassiveEmblems();
    }

    public void RefreshPassiveEmblems()
    {
        passiveEmblems.Clear();

        EmblemButtonUI[] emblemButtons = FindObjectsByType<EmblemButtonUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (EmblemButtonUI button in emblemButtons)
        {
            if (button == null || button.emblem == null || !button.emblem.IsPassiveEmblem()) continue;
            if (!button.IsOwned()) continue;
            if (consumedPassiveEmblems.Contains(button.emblem)) continue;
            if (!passiveEmblems.Contains(button.emblem))
            {
                passiveEmblems.Add(button.emblem);
            }
        }
    }

    public void SelectEmblem(BaseEmblemSO emblem)
    {
        if (emblem != null && emblem.IsPassiveEmblem())
        {
            RefreshPassiveEmblems();
            Debug.Log("Passive Emblem already active: " + emblem.emblemName);
            return;
        }

        currentEmblem = emblem;

        Debug.Log("Selected Emblem: " + emblem.emblemName);
    }

    public void ConsumeCurrentEmblem()
    {
        currentEmblem = null;
    }

    public void ConsumePassiveEmblem(BaseEmblemSO emblem)
    {
        if (emblem == null) return;
        if (!consumedPassiveEmblems.Contains(emblem))
        {
            consumedPassiveEmblems.Add(emblem);
        }

        passiveEmblems.Remove(emblem);
    }

    public bool HasPassiveEmblem<T>() where T : BaseEmblemSO
    {
        return GetPassiveEmblem<T>() != null;
    }

    public T GetPassiveEmblem<T>() where T : BaseEmblemSO
    {
        foreach (BaseEmblemSO emblem in passiveEmblems)
        {
            if (emblem is T typedEmblem)
            {
                return typedEmblem;
            }
        }

        RefreshPassiveEmblems();

        foreach (BaseEmblemSO emblem in passiveEmblems)
        {
            if (emblem is T typedEmblem)
            {
                return typedEmblem;
            }
        }

        return null;
    }
}
