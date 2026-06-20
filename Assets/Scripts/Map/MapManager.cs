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
    public GameObject eventPanel; 
    public TMP_Text eventDescriptionText;

    [Header("Testing Elements Assets")]
    public MarbleElementSO fireElementAsset; 
    public MarbleElementSO windElementAsset; 

    [Header("Inventory UI Connection")]
    public UIInventoryManager uiInventoryManager;

    private bool needsMapRefresh = false;

    private void Start()
    {
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
        if (eventPanel == null) return;

        eventPanel.SetActive(true);
        needsMapRefresh = false; 
        
        if (Random.value > 0.5f)
        {
            if (ProgressionManager.Instance != null)
            {
                int eventReward = Random.Range(2, 6);
                eventDescriptionText.text = $"Kamu menemukan kantong tua tergeletak di jalan. Di dalamnya berisi {eventReward} butir kelereng emas!";
                ProgressionManager.Instance.AddCurrency(eventReward);
                ProgressionManager.Instance.currentFloor++;
                
                needsMapRefresh = true; 
            }
            UpdateMapUI();
        }
        else
        {
            eventDescriptionText.text = "Sesosok bayangan misterius menghadang jalanmu dan menantangmu bertaruh gacoan! Bersiaplah bertarung!";
            Invoke("LoadCombatScene", 2.5f);
        }
    }

    public void CloseEventPanel()
    {
        if (eventPanel != null) eventPanel.SetActive(false);

        if (needsMapRefresh)
        {
            needsMapRefresh = false;
            SceneManager.LoadScene("MapScene");
        }
    }
}