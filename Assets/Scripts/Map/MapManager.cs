using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class FloorEventPool
{
    public string poolName;
    public int minFloor = 1;
    public int maxFloor = 1;
    public List<GameEventSO> events = new List<GameEventSO>();
}

public class MapManager : MonoBehaviour
{
    public enum NodeType { Random, Fight, Event, Treasure, Store, Boss, Elite }

    [Header("UI Display")]
    public TMP_Text currencyText;

    [Header("Event Pool")]
    public List<GameEventSO> possibleEvents = new List<GameEventSO>();
    public List<FloorEventPool> floorEventPools = new List<FloorEventPool>();

    [Header("Inventory UI Connection")]
    public UIInventoryManager uiInventoryManager;

    [Header("Combat Encounters")]
    public EnemySO bossEnemy;
    public List<EnemySO> normalFightPool = new List<EnemySO>();
    public List<EnemySO> eliteFightPool = new List<EnemySO>();

    private MarbleElementSO pendingEventFightElementReward;

    private void Start()
    {
        if (uiInventoryManager != null)
        {
            uiInventoryManager.BuildDynamicInventoryUI();
        }

        if (ProgressionManager.Instance != null)
        {
            ClaimPendingEventFightReward();
        }

        UpdateMapUI();
    }

    private void ClaimPendingEventFightReward()
    {
        if (!ProgressionManager.Instance.hasPendingEventFightReward) return;

        EventRewardData reward = ProgressionManager.Instance.pendingEventFightReward;
        ProgressionManager.Instance.hasPendingEventFightReward = false;
        ProgressionManager.Instance.pendingEventFightReward = null;
        ProgressionManager.Instance.selectedEventForEventScene = null;

        ProgressionManager.Instance.ApplyEventReward(reward, false);
        if (uiInventoryManager != null)
        {
            uiInventoryManager.BuildDynamicInventoryUI();
        }

        pendingEventFightElementReward = ProgressionManager.Instance.PickRandomEventElementReward(reward);

        if (pendingEventFightElementReward == null) return;

        if (uiInventoryManager == null)
        {
            Debug.LogWarning("MapManager has an event fight element reward, but uiInventoryManager is not assigned. Falling back to automatic element placement.");
            ProgressionManager.Instance.GrantElementToChamber(pendingEventFightElementReward);
            pendingEventFightElementReward = null;
            return;
        }

        ProgressionManager.Instance.pendingElementFromShop = pendingEventFightElementReward;
        uiInventoryManager.EnterMapEventInfusionMode(this, pendingEventFightElementReward.elementName);
    }

    public void CompletePendingEventFightElementInfusion(int selectedSlotIndex)
    {
        if (ProgressionManager.Instance == null || pendingEventFightElementReward == null)
        {
            pendingEventFightElementReward = null;
            return;
        }

        if (!ProgressionManager.Instance.LoadElementToSlot(selectedSlotIndex, pendingEventFightElementReward))
        {
            if (uiInventoryManager != null)
            {
                uiInventoryManager.ShowFeedback("This marble is already combined. Pick another marble.");
            }
            return;
        }

        pendingEventFightElementReward = null;
        ProgressionManager.Instance.pendingElementFromShop = null;
        if (uiInventoryManager != null)
        {
            uiInventoryManager.ExitInfusionMode();
            uiInventoryManager.BuildDynamicInventoryUI();
        }

        UpdateMapUI();
    }

    private void UpdateMapUI()
    {
        if (ProgressionManager.Instance == null) return;

        if (currencyText != null) currencyText.text = ProgressionManager.Instance.playerCurrency.ToString();
    }

    public void OnNodeSelected(string nodeTypeString)
    {
        NodeType selectedType = (NodeType)System.Enum.Parse(typeof(NodeType), nodeTypeString, true);

        switch (selectedType)
        {
            case NodeType.Fight:
                PrepareFightEncounter(selectedType);
                LoadCombatScene();
                break;

            case NodeType.Elite:
                PrepareFightEncounter(selectedType);
                LoadCombatScene();
                break;

            case NodeType.Treasure:
                TriggerTreasureNode();
                break;

            case NodeType.Event:
                TriggerEventNode();
                break;

            case NodeType.Boss:
                PrepareFightEncounter(selectedType);
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

    private void PrepareFightEncounter(NodeType nodeType)
    {
        if (ProgressionManager.Instance == null) return;

        if (nodeType == NodeType.Boss)
        {
            if (bossEnemy == null)
            {
                Debug.LogWarning("Boss node selected but no bossEnemy is assigned on MapManager.");
                ProgressionManager.Instance.ClearPendingFightEnemy();
                return;
            }

            ProgressionManager.Instance.SetPendingFightEnemy(bossEnemy);
            Debug.Log($"Boss encounter queued: {bossEnemy.enemyName}");
            return;
        }

        EnemySO selectedEnemy = nodeType == NodeType.Elite
            ? PickRandomEnemyFromPool(eliteFightPool, EnemyType.Elite)
            : PickRandomEnemyFromPool(normalFightPool, EnemyType.Normal);

        if (selectedEnemy != null)
        {
            ProgressionManager.Instance.SetPendingFightEnemy(selectedEnemy);
            Debug.Log($"{nodeType} encounter queued: {selectedEnemy.enemyName}");
        }
        else
        {
            ProgressionManager.Instance.ClearPendingFightEnemy();
        }
    }

    private EnemySO PickRandomEnemyFromPool(List<EnemySO> sourcePool, EnemyType expectedType)
    {
        if (sourcePool == null || sourcePool.Count == 0) return null;

        List<EnemySO> validEnemies = new List<EnemySO>();
        foreach (EnemySO enemy in sourcePool)
        {
            if (enemy != null && enemy.enemyType == expectedType)
            {
                validEnemies.Add(enemy);
            }
        }

        if (validEnemies.Count == 0) return null;
        return validEnemies[Random.Range(0, validEnemies.Count)];
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

        List<GameEventSO> validEvents = GetValidEventsForCurrentFloor();

        if (validEvents.Count == 0)
        {
            Debug.LogWarning("No unused events are available for this floor.");
            ProgressionManager.Instance.currentFloor++;
            SceneManager.LoadScene("MapScene");
            return;
        }

        GameEventSO selectedEvent = validEvents[Random.Range(0, validEvents.Count)];
        ProgressionManager.Instance.MarkEventUsed(selectedEvent);
        ProgressionManager.Instance.selectedEventForEventScene = selectedEvent;
        SceneManager.LoadScene("EventScene");
    }

    private List<GameEventSO> GetValidEventsForCurrentFloor()
    {
        int currentFloor = ProgressionManager.Instance != null ? ProgressionManager.Instance.currentFloor : 1;
        List<GameEventSO> validEvents = new List<GameEventSO>();
        bool hasMatchingFloorPool = false;

        foreach (FloorEventPool floorPool in floorEventPools)
        {
            if (floorPool == null || currentFloor < floorPool.minFloor || currentFloor > floorPool.maxFloor)
            {
                continue;
            }

            hasMatchingFloorPool = true;
            foreach (GameEventSO gameEvent in floorPool.events)
            {
                if (IsUnusedEvent(gameEvent))
                {
                    validEvents.Add(gameEvent);
                }
            }
        }

        if (hasMatchingFloorPool)
        {
            return validEvents;
        }

        foreach (GameEventSO gameEvent in possibleEvents)
        {
            if (IsUnusedEvent(gameEvent))
            {
                validEvents.Add(gameEvent);
            }
        }

        return validEvents;
    }

    private bool IsUnusedEvent(GameEventSO gameEvent)
    {
        if (gameEvent == null) return false;
        if (ProgressionManager.Instance == null) return true;
        return !ProgressionManager.Instance.HasEventBeenUsed(gameEvent);
    }
}
