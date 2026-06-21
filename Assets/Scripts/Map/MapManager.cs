using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class MapManager : MonoBehaviour
{
    public enum NodeType { Random, Fight, Event, Treasure, Store, Boss }

    [Header("UI Display")]
    public TMP_Text currencyText;
    public TMP_Text floorText;

    [Header("Event Pool")]
    public List<GameEventSO> possibleEvents = new List<GameEventSO>();

    [Header("Inventory UI Connection")]
    public UIInventoryManager uiInventoryManager;

    private void Start()
    {
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.ClaimPendingEventFightReward();
        }

        if (uiInventoryManager != null)
        {
            uiInventoryManager.BuildDynamicInventoryUI();
        }

        UpdateMapUI();
    }

    private void UpdateMapUI()
    {
        if (ProgressionManager.Instance == null) return;

        if (currencyText != null) currencyText.text = ProgressionManager.Instance.playerCurrency.ToString();
        if (floorText != null) floorText.text = "Floor: " + ProgressionManager.Instance.currentFloor;
    }

    public void OnNodeSelected(string nodeTypeString)
    {
        NodeType selectedType = (NodeType)System.Enum.Parse(typeof(NodeType), nodeTypeString, true);

        switch (selectedType)
        {
            case NodeType.Fight:
                LoadCombatScene();
                break;

            case NodeType.Treasure:
                TriggerTreasureNode();
                break;

            case NodeType.Event:
                TriggerEventNode();
                break;

            case NodeType.Boss:
                LoadCombatScene(); 
                break;
            case NodeType.Store:
                LoadShopScene();
                break;
        }
    }

    private void LoadCombatScene()
    {
        SceneManager.LoadScene("FightScene");
    }
    
    public void LoadShopScene()
    {
        SceneManager.LoadScene("ShopScene");
    }

    private void TriggerTreasureNode()
    {
        if (ProgressionManager.Instance == null) return;

        int reward = Random.Range(3, 8);
        ProgressionManager.Instance.AddCurrency(reward);
        
        Debug.Log($"Membuka peti harta! Mendapat {reward} mata uang kelereng.");
        ProgressionManager.Instance.currentFloor++;
        
        SceneManager.LoadScene("MapScene");
    }

    private void TriggerEventNode()
    {
        if (ProgressionManager.Instance == null) return;

        List<GameEventSO> validEvents = new List<GameEventSO>();
        foreach (GameEventSO gameEvent in possibleEvents)
        {
            if (gameEvent != null)
            {
                validEvents.Add(gameEvent);
            }
        }

        if (validEvents.Count == 0)
        {
            Debug.LogWarning("No possible events assigned to MapManager.");
            return;
        }

        ProgressionManager.Instance.selectedEventForEventScene = validEvents[Random.Range(0, validEvents.Count)];
        SceneManager.LoadScene("EventScene");
    }
}
