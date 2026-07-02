using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class UIInventoryManager : MonoBehaviour
{
    [Header("Dynamic Prefab Settings")]
    public GameObject slotPrefab;         
    public Transform slotsContainer;       

    [Header("Status Info Text")]
    public TMP_Text feedbackStatusText;

    [Header("Auto Rebuild")]
    [Tooltip("Rebuild the inventory slots whenever this panel is opened.")]
    public bool rebuildOnEnable = true;

    private List<UIChamberSlot> activeUISlots = new List<UIChamberSlot>();
    private int selectedSlotIndex = -1; 

    // BARU: State kontrol integrasi Toko
    private ShopManager activeShopRef = null;
    private EventManager activeEventRef = null;
    private MapManager activeMapRef = null;
    private bool isInInfusionMode = false;

    private void Start()
    {
        // Pastikan dipanggil lewat MapManager seperti langkah perbaikan kemarin
    }

    private void OnEnable()
    {
        if (rebuildOnEnable)
        {
            BuildDynamicInventoryUI();
        }
    }

    public void BuildDynamicInventoryUI()
    {
        if (ProgressionManager.Instance == null || slotPrefab == null || slotsContainer == null) return;

        foreach (Transform child in slotsContainer) Destroy(child.gameObject);
        activeUISlots.Clear();

        int currentMaxAmmo = ProgressionManager.Instance.BASE_AMMO;
        List<MarbleElementSO> chamber = ProgressionManager.Instance.equippedChamber;

        // Validasi pengisian list agar data sinkron dengan kapasitas maksimal amunisi terbaru
        while (chamber.Count < currentMaxAmmo) chamber.Add(null);

        for (int i = 0; i < currentMaxAmmo; i++)
        {
            GameObject newSlotObj = Instantiate(slotPrefab, slotsContainer);
            UIChamberSlot slotScript = newSlotObj.GetComponent<UIChamberSlot>();
            
            slotScript.SetupSlot(this, i);
            slotScript.RefreshDisplay(chamber[i]);
            activeUISlots.Add(slotScript);
        }

        UpdateFeedbackText($"Inventory loaded: {currentMaxAmmo} Slots");
    }

    public void RefreshAllSlots()
    {
        if (ProgressionManager.Instance == null) return;
        List<MarbleElementSO> chamber = ProgressionManager.Instance.equippedChamber;

        for (int i = 0; i < activeUISlots.Count; i++)
        {
            if (i < chamber.Count) activeUISlots[i].RefreshDisplay(chamber[i]);
        }
    }

    // BARU: Dipanggil oleh toko saat elemen berhasil dibeli
    public void EnterInfusionMode(ShopManager shop, string elementName)
    {
        activeShopRef = shop;
        activeEventRef = null;
        activeMapRef = null;
        isInInfusionMode = true;
        selectedSlotIndex = -1;
        RefreshAllSlots();

        UpdateFeedbackText($"🔮 INFUSE MODE: Tap 1 of your {activeUISlots.Count} marbles to inject {elementName}!");
    }

    public void EnterEventInfusionMode(EventManager eventManager, string elementName)
    {
        activeShopRef = null;
        activeEventRef = eventManager;
        activeMapRef = null;
        isInInfusionMode = true;
        selectedSlotIndex = -1;
        RefreshAllSlots();

        UpdateFeedbackText($"Event reward: tap a marble to fuse {elementName}.");
    }

    public void EnterMapEventInfusionMode(MapManager mapManager, string elementName)
    {
        activeShopRef = null;
        activeEventRef = null;
        activeMapRef = mapManager;
        isInInfusionMode = true;
        selectedSlotIndex = -1;
        RefreshAllSlots();

        UpdateFeedbackText($"Event fight reward: tap a marble to fuse {elementName}.");
    }

    // BARU: Keluar dari mode infus dan kembali ke mode swap reguler
    public void ExitInfusionMode()
    {
        isInInfusionMode = false;
        activeShopRef = null;
        activeEventRef = null;
        activeMapRef = null;
        RefreshAllSlots();
        UpdateFeedbackText("Returned to Map Inventory");
    }

    public void SelectSlot(int clickedIndex)
    {
        // CABANG TOKO SCENE BARU
        if (isInInfusionMode && activeShopRef != null)
        {
            // Panggil fungsi CompleteInfusionTransaction yang baru kita buat di atas
            activeShopRef.CompleteInfusionTransaction(clickedIndex);
            return; 
        }

        if (isInInfusionMode && activeEventRef != null)
        {
            activeEventRef.CompleteElementRewardInfusion(clickedIndex);
            return;
        }

        if (isInInfusionMode && activeMapRef != null)
        {
            activeMapRef.CompletePendingEventFightElementInfusion(clickedIndex);
            return;
        }

        // LOGIKA SWAP REGULER (Saat di Map Scene biasa)
        if (selectedSlotIndex == -1)
        {
            selectedSlotIndex = clickedIndex;
            UpdateFeedbackText($"Selected Slot {clickedIndex + 1}. Choose another to swap!");
            activeUISlots[clickedIndex].backgroundImage.color = Color.yellow; 
        }
        else if (selectedSlotIndex == clickedIndex)
        {
            selectedSlotIndex = -1;
            RefreshAllSlots();
            UpdateFeedbackText("Selection Cancelled");
        }
        else
        {
            SwapChamberSlots(selectedSlotIndex, clickedIndex);
            selectedSlotIndex = -1; 
        }
    }

    private void SwapChamberSlots(int indexA, int indexB)
    {
        List<MarbleElementSO> chamber = ProgressionManager.Instance.equippedChamber;
        MarbleElementSO temp = chamber[indexA];
        chamber[indexA] = chamber[indexB];
        chamber[indexB] = temp;

        RefreshAllSlots();
        UpdateFeedbackText($"Swapped Slot {indexA + 1} with Slot {indexB + 1}!");
    }

    private void UpdateFeedbackText(string message)
    {
        if (feedbackStatusText != null) feedbackStatusText.text = message;
    }

    public void ShowFeedback(string message)
    {
        UpdateFeedbackText(message);
    }
}
