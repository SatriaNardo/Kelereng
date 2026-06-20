using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MapNodeBlueprint
{
    public string nodeTypeString; // Menyimpan tipe node dalam bentuk string agar aman
    public int floorNumber;
    public int columnNumber;
    public Vector2 uiAnchoredPosition;
    public List<int> incomingConnections = new List<int>();
}

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

    // Menyimpan data belanja sementara antar scene
    [HideInInspector] public MarbleElementSO pendingElementFromShop = null;

    [Header("Energy System")]
    public int currentEnergy = 0;
    public int maxEnergyThisTurn = 1; // Kapasitas maksimal yang meningkat per turn

    [Header("Persistent Map Data")]
    public bool isMapAlreadyGenerated = false;
    // PUSAT PENYIMPANAN DATA PETA: Menyimpan seluruh cetak biru node pertualangan
    public List<MapNodeBlueprint> savedMapNodes = new List<MapNodeBlueprint>();

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

    // ========================================================
    // PERBAIKAN: RESET ENERGI UNTUK AWAL MATCH (STARTING ENERGY)
    // ========================================================
    public void ResetEnergyForNewMatch()
    {
        maxEnergyThisTurn = 1;
        currentEnergy = maxEnergyThisTurn; // Memulai match baru dengan modal 1 Energi
        Debug.Log($"⚡ Match Baru! Energi direset awal ke: {currentEnergy}/{maxEnergyThisTurn}");
    }

    // ========================================================
    // PERBAIKAN: REGENERASI BERTAHAP (+1 PER TURN) BUKAN REFILL PENUH
    // ========================================================
    public void StartNewTurnEnergySetup()
    {
        // 1. Kapasitas maksimum naik bertahap setiap turn (Dibatasi maksimal di angka 5)
        maxEnergyThisTurn++; 
        if (maxEnergyThisTurn > 5) maxEnergyThisTurn = 5;

        // 2. Tambahkan pasokan +1 energi sebagai tabungan ronde baru
        int energyRegenAmount = 1; 
        currentEnergy += energyRegenAmount;

        // 3. Jaga agar akumulasi energi sisa tidak bocor melampaui kapasitas maksimal saat ini
        if (currentEnergy > maxEnergyThisTurn)
        {
            currentEnergy = maxEnergyThisTurn;
        }

        Debug.Log($"⚡ Turn Baru! [+{energyRegenAmount} Energi]. Total Energi saat ini: {currentEnergy}/{maxEnergyThisTurn}");
    }

    public void AddCurrency(int amount)
    {
        playerCurrency += amount;
        if (playerCurrency < 0) playerCurrency = 0; 
    }

    public void ResetProgressForNewGame()
    {
        currentFloor = 1;
        currentColumn = 0;
        playerCurrency = 10;
        
        // Bersihkan memori peta lama agar run baru mendapatkan acakan graf baru
        savedMapNodes.Clear();
        isMapAlreadyGenerated = false;
        
        ResetChamberToDefault();
        Debug.Log("🧼 Seluruh struktur peta persisten telah direset bersih!");
    }
}