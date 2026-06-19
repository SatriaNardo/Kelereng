using UnityEngine;
using System.Collections.Generic;

public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance;

    [Header("Player Global Progress")]
    public int playerCurrency = 10;      
    public int BASE_AMMO = 4;     
    public int currentFloor = 1;
    public int currentColumn = 0;

    [Header("Elemental Inventory System")]
    public List<MarbleElementSO> equippedChamber = new List<MarbleElementSO>();

    [Header("Fusion Asset Templates")]
    public CombinedElementSO cycloneFusionAsset;   // Tarik asset SO Cyclone ke sini di Inspector
    public CombinedElementSO explosionFusionAsset;

    // BARU: Menyimpan data belanja sementara antar scene
    [HideInInspector] public MarbleElementSO pendingElementFromShop = null;

    // [Header("Event Pool")]
    // public List<GameEventSO> masterEventPool = new List<GameEventSO>();

    [Header("Energy System")]
    public int currentEnergy = 0;
    public int maxEnergyThisTurn = 1; // Kapasitas maksimal yang meningkat per turn


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ResetChamberToDefault();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetChamberToDefault()
    {
        equippedChamber.Clear();
        for (int i = 0; i < BASE_AMMO; i++) equippedChamber.Add(null);
    }

// Masukkan fungsi ini di dalam ProgressionManager.cs
    public MarbleElementSO PopNextElement()
    {
        if (equippedChamber.Count == 0) return null;

        MarbleElementSO nextElement = equippedChamber[0];
        equippedChamber.RemoveAt(0); // Hapus elemen terdepan dari antrean
        return nextElement;
    }

    public void AddAmmoToChamber(MarbleElementSO element)
    {
        equippedChamber.Add(element);
        Debug.Log($"📥 Kelereng baru masuk inventory! Total Antrean: {equippedChamber.Count}");
    }

    public void LoadElementToSlot(int slotIndex, MarbleElementSO newElement)
    {
        if (slotIndex < 0 || slotIndex >= equippedChamber.Count) return;

        MarbleElementSO currentElement = equippedChamber[slotIndex];

        // LOGIKA EVOLUSI KOMBINASI:
        // 1. Jika slot target sudah punya elemen WIND, dan kamu memasukkan WIND lagi -> Ubah jadi CYCLONE
        if (currentElement != null && currentElement.elementName == "Wind" && newElement != null && newElement.elementName == "Wind")
        {
            equippedChamber[slotIndex] = cycloneFusionAsset;
            Debug.Log($"🧬 EVOLUTION! Slot {slotIndex} berevolusi menjadi 🌪️ Cyclone!");
        }
        // 2. Jika slot target sudah punya elemen FIRE, dan kamu memasukkan FIRE lagi -> Ubah jadi EXPLOSION
        else if (currentElement != null && currentElement.elementName == "Fire" && newElement != null && newElement.elementName == "Fire")
        {
            equippedChamber[slotIndex] = explosionFusionAsset;
            Debug.Log($"🧬 EVOLUTION! Slot {slotIndex} berevolusi menjadi 💥 Explosion!");
        }
        // 3. Jika slot kosong atau elemennya berbeda, lakukan infusi/timpa normal seperti biasa
        else
        {
            equippedChamber[slotIndex] = newElement;
            Debug.Log($"🔮 Slot {slotIndex} diisi elemen: {(newElement != null ? newElement.elementName : "Polos")}");
        }
    }

    // Panggil ini setiap kali ronde pertempuran baru dimulai
    public void ResetEnergyForNewMatch()
    {
        maxEnergyThisTurn = 1;
        currentEnergy = maxEnergyThisTurn;
    }

    // Panggil ini di awal turn baru (oleh ArenaManager)
    public void StartNewTurnEnergySetup()
    {
        maxEnergyThisTurn++; // Kapasitas mana naik setiap turn (Maksimal bebas, misal batasi di 5)
        if (maxEnergyThisTurn > 5) maxEnergyThisTurn = 5;

        currentEnergy = maxEnergyThisTurn; // Isi penuh kembali energi
        Debug.Log($"⚡ Energy Refilled: {currentEnergy}/{maxEnergyThisTurn}");
    }

    public void AddCurrency(int amount)
    {
        playerCurrency += amount;
        if (playerCurrency < 0) playerCurrency = 0; 
    }
}