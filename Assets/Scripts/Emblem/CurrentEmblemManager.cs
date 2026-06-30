using UnityEngine;

public class CurrentEmblemManager : MonoBehaviour
{
    public static CurrentEmblemManager Instance;

    public BaseEmblemSO currentEmblem;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void SelectEmblem(BaseEmblemSO emblem)
    {
        currentEmblem = emblem;

        Debug.Log("Selected Emblem: " + emblem.emblemName);
    }

    public void ConsumeCurrentEmblem()
    {
        currentEmblem = null;
    }
}