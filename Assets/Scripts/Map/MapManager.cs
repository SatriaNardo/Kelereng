using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class MapManager : MonoBehaviour
{
    public enum NodeType { Random, Fight, Event, Treasure, Store, Boss }

    [Header("UI Display")]
    public TMP_Text currencyText;

    [Header("Event Pool")]
    public List<GameEventSO> possibleEvents = new List<GameEventSO>();

    [Header("Inventory UI Connection")]
    public UIInventoryManager uiInventoryManager;

    [Header("Combat Encounters")]
    public EnemySO bossEnemy;
    public List<EnemySO> normalFightPool = new List<EnemySO>();
    public List<EnemySO> eliteFightPool = new List<EnemySO>();
    [Range(0f, 1f)] public float eliteFightChance = 0.2f;

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

        EnemySO selectedEnemy = PickRandomFightEnemy();
        if (selectedEnemy != null)
        {
            ProgressionManager.Instance.SetPendingFightEnemy(selectedEnemy);
            Debug.Log($"Fight encounter queued: {selectedEnemy.enemyName}");
        }
        else
        {
            ProgressionManager.Instance.ClearPendingFightEnemy();
        }
    }

    private EnemySO PickRandomFightEnemy()
    {
        bool useElite = eliteFightPool.Count > 0 && Random.value < eliteFightChance;
        List<EnemySO> sourcePool = useElite ? eliteFightPool : normalFightPool;

        if (sourcePool.Count == 0)
        {
            sourcePool = useElite ? normalFightPool : eliteFightPool;
        }

        List<EnemySO> validEnemies = new List<EnemySO>();
        foreach (EnemySO enemy in sourcePool)
        {
            if (enemy != null && enemy.enemyType != EnemyType.Boss)
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
