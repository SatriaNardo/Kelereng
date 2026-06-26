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
    public static MarbleElementSO PendingStartingElement;
    public static int PendingWorldMapIndex = -1;
    public static WorldMapConfigSO PendingWorldMapConfig;

    [Header("Player Global Progress")]
    public int startingCurrency = 10;
    public int startingBaseAmmo = 4;
    public int playerCurrency = 10;      
    public int BASE_AMMO = 4;     
    public int currentFloor = 1;
    public int currentColumn = 0;
    public int currentWorldMapIndex = 0;
    public int highestUnlockedWorldMapIndex = 0;
    public int totalWorldMapNodes = 6;
    public int currentPathMapFloorCount = 0;
    public WorldMapConfigSO selectedWorldMapConfig = null;

    [Header("Elemental Inventory System")]
    public List<MarbleElementSO> equippedChamber = new List<MarbleElementSO>();

    [Header("Fusion Asset Templates")]
    public CombinedElementSO cycloneFusionAsset;   // Tarik asset SO Cyclone ke sini di Inspector
    public CombinedElementSO explosionFusionAsset;
    public CombinedElementSO floodFusionAsset;
    public CombinedElementSO quakeFusionAsset;
    public CombinedElementSO iceFusionAsset;
    public CombinedElementSO blazeFusionAsset;
    public CombinedElementSO dustFusionAsset;
    public CombinedElementSO steamFusionAsset;
    public CombinedElementSO lavaFusionAsset;

    // Menyimpan data belanja sementara antar scene
    [HideInInspector] public MarbleElementSO pendingElementFromShop = null;

    [Header("Event State")]
    [HideInInspector] public GameEventSO selectedEventForEventScene = null;
    [HideInInspector] public bool hasPendingEventFightReward = false;
    [HideInInspector] public EventRewardData pendingEventFightReward = null;
    [HideInInspector] public EnemySO pendingFightEnemy = null;

    [Header("Energy System")]
    public int currentEnergy = 0;
    public int maxEnergyThisTurn = 1; // Kapasitas maksimal yang meningkat per turn

    [Header("Emblem System")]
    public List<EmblemSO> equippedEmblems = new List<EmblemSO>();

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
            ApplyPendingStartingElement();
            ApplyPendingWorldMapSelection();
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

    public bool TryRemoveAmmoFromChamber()
    {
        if (BASE_AMMO <= 1 || equippedChamber.Count <= 1)
        {
            Debug.LogWarning("Tidak bisa memberikan kelereng terakhir.");
            return false;
        }

        BASE_AMMO--;

        int removeIndex = equippedChamber.Count - 1;
        for (int i = equippedChamber.Count - 1; i >= 0; i--)
        {
            if (equippedChamber[i] == null)
            {
                removeIndex = i;
                break;
            }
        }

        equippedChamber.RemoveAt(removeIndex);
        Debug.Log($"Kelereng diberikan. Sisa kapasitas amunisi: {BASE_AMMO}");
        return true;
    }

    public void GrantElementToChamber(MarbleElementSO element)
    {
        if (element == null || equippedChamber.Count == 0) return;

        for (int i = 0; i < equippedChamber.Count; i++)
        {
            if (equippedChamber[i] == null)
            {
                equippedChamber[i] = element;
                Debug.Log($"Hadiah elemen {element.elementName} masuk ke slot kosong {i + 1}.");
                return;
            }
        }

        int targetSlot = Random.Range(0, equippedChamber.Count);
        LoadElementToSlot(targetSlot, element);
    }

    public bool ApplyEventReward(EventRewardData reward)
    {
        if (reward == null) return true;

        if (reward.removeAmmo && !TryRemoveAmmoFromChamber())
        {
            return false;
        }

        if (reward.currencyMax > 0 || reward.currencyMin > 0)
        {
            int min = Mathf.Min(reward.currencyMin, reward.currencyMax);
            int max = Mathf.Max(reward.currencyMin, reward.currencyMax);
            AddCurrency(Random.Range(min, max + 1));
        }

        if (reward.randomElementRewards != null && reward.randomElementRewards.Count > 0)
        {
            List<MarbleElementSO> validElements = new List<MarbleElementSO>();
            foreach (MarbleElementSO element in reward.randomElementRewards)
            {
                if (element != null) validElements.Add(element);
            }

            if (validElements.Count > 0)
            {
                GrantElementToChamber(validElements[Random.Range(0, validElements.Count)]);
            }
        }

        return true;
    }

    public void SetPendingEventFightReward(EventRewardData reward)
    {
        hasPendingEventFightReward = reward != null;
        pendingEventFightReward = reward;
    }

    public void ClaimPendingEventFightReward()
    {
        if (!hasPendingEventFightReward) return;

        hasPendingEventFightReward = false;
        selectedEventForEventScene = null;
        ApplyEventReward(pendingEventFightReward);
        pendingEventFightReward = null;
    }

    public void SetPendingFightEnemy(EnemySO enemy)
    {
        pendingFightEnemy = enemy;
    }

    public void ClearPendingFightEnemy()
    {
        pendingFightEnemy = null;
    }

    public EnemySO ConsumePendingFightEnemy()
    {
        EnemySO enemy = pendingFightEnemy;
        pendingFightEnemy = null;
        return enemy;
    }

    public void LoadElementToSlot(int slotIndex, MarbleElementSO newElement)
    {
        if (slotIndex < 0 || slotIndex >= equippedChamber.Count) return;

        MarbleElementSO currentElement = equippedChamber[slotIndex];

        CombinedElementSO fusionResult = GetFusionResult(currentElement, newElement);
        if (fusionResult != null)
        {
            equippedChamber[slotIndex] = fusionResult;
            Debug.Log($"🧬 EVOLUTION! Slot {slotIndex} berevolusi menjadi {fusionResult.elementName}!");
        }
        else
        {
            equippedChamber[slotIndex] = newElement;
            Debug.Log($"🔮 Slot {slotIndex} diisi elemen: {(newElement != null ? newElement.elementName : "Polos")}");
        }
    }

    private CombinedElementSO GetFusionResult(MarbleElementSO currentElement, MarbleElementSO newElement)
    {
        if (currentElement == null || newElement == null) return null;

        string first = currentElement.elementName;
        string second = newElement.elementName;

        if (IsFusionPair(first, second, "Wind", "Wind")) return cycloneFusionAsset;
        if (IsFusionPair(first, second, "Fire", "Fire")) return explosionFusionAsset;
        if (IsFusionPair(first, second, "Water", "Water")) return floodFusionAsset;
        if (IsFusionPair(first, second, "Earth", "Earth")) return quakeFusionAsset;
        if (IsFusionPair(first, second, "Water", "Wind")) return iceFusionAsset;
        if (IsFusionPair(first, second, "Fire", "Wind")) return blazeFusionAsset;
        if (IsFusionPair(first, second, "Earth", "Wind")) return dustFusionAsset;
        if (IsFusionPair(first, second, "Water", "Fire")) return steamFusionAsset;
        if (IsFusionPair(first, second, "Earth", "Fire")) return lavaFusionAsset;

        return null;
    }

    private bool IsFusionPair(string first, string second, string elementA, string elementB)
    {
        return (first == elementA && second == elementB) || (first == elementB && second == elementA);
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

    public bool HasEmblemEffect(EmblemEffectType effectType)
    {
        foreach (EmblemSO emblem in equippedEmblems)
        {
            if (emblem != null && emblem.effectType == effectType)
            {
                return true;
            }
        }

        return false;
    }

    public void AddEmblem(EmblemSO emblem)
    {
        if (emblem == null || equippedEmblems.Contains(emblem))
        {
            return;
        }

        equippedEmblems.Add(emblem);
        ApplyEmblemPassiveOnAcquire(emblem);
        Debug.Log($"Emblem acquired: {emblem.emblemName}");
    }

    private void ApplyEmblemPassiveOnAcquire(EmblemSO emblem)
    {
        if (emblem.bonusAmmoSlot <= 0)
        {
            return;
        }

        BASE_AMMO += emblem.bonusAmmoSlot;
        for (int i = 0; i < emblem.bonusAmmoSlot; i++)
        {
            equippedChamber.Add(null);
        }
    }

    public float GetGuideLineRangeBonus()
    {
        float totalBonus = 0f;

        foreach (EmblemSO emblem in equippedEmblems)
        {
            if (emblem != null && emblem.effectType == EmblemEffectType.ExtendedGuideLine)
            {
                totalBonus += emblem.guideLineRangeBonus;
            }
        }

        return totalBonus;
    }

    public int GetRicochetPreviewCount()
    {
        int totalBounces = 0;

        foreach (EmblemSO emblem in equippedEmblems)
        {
            if (emblem != null && emblem.effectType == EmblemEffectType.RicochetPreview)
            {
                totalBounces += emblem.ricochetPreviewCount;
            }
        }

        return totalBounces;
    }

    public float GetEmblemLaunchForceMultiplier()
    {
        float multiplier = 1f;

        foreach (EmblemSO emblem in equippedEmblems)
        {
            if (emblem != null && emblem.bonusForceMultiplier > 1f)
            {
                multiplier *= emblem.bonusForceMultiplier;
            }
        }

        return multiplier;
    }

    public bool HasSlimeImmunity()
    {
        foreach (EmblemSO emblem in equippedEmblems)
        {
            if (emblem != null && emblem.kebalLendirSlime)
            {
                return true;
            }
        }

        return false;
    }

    public void ResetProgressForNewGame()
    {
        currentFloor = 1;
        currentColumn = 0;
        currentWorldMapIndex = 0;
        highestUnlockedWorldMapIndex = 0;
        currentPathMapFloorCount = 0;
        selectedWorldMapConfig = null;
        playerCurrency = startingCurrency;
        BASE_AMMO = startingBaseAmmo;
        
        // Bersihkan memori peta lama agar run baru mendapatkan acakan graf baru
        savedMapNodes.Clear();
        isMapAlreadyGenerated = false;
        selectedEventForEventScene = null;
        hasPendingEventFightReward = false;
        pendingEventFightReward = null;
        pendingFightEnemy = null;
        
        equippedEmblems.Clear();
        ResetChamberToDefault();
        Debug.Log("🧼 Seluruh struktur peta persisten telah direset bersih!");
    }

    public void StartWorldMapNode(int worldMapIndex, WorldMapConfigSO worldMapConfig = null)
    {
        currentWorldMapIndex = worldMapIndex;
        selectedWorldMapConfig = worldMapConfig;
        currentFloor = 1;
        currentColumn = 0;
        currentPathMapFloorCount = 0;
        savedMapNodes.Clear();
        isMapAlreadyGenerated = false;
    }

    public void CompleteCurrentWorldMapNode()
    {
        if (currentWorldMapIndex >= highestUnlockedWorldMapIndex && highestUnlockedWorldMapIndex < totalWorldMapNodes - 1)
        {
            highestUnlockedWorldMapIndex++;
        }

        currentFloor = 1;
        currentColumn = 0;
        currentPathMapFloorCount = 0;
        selectedWorldMapConfig = null;
        savedMapNodes.Clear();
        isMapAlreadyGenerated = false;
    }

    public void StartNewRunWithStartingElement(MarbleElementSO startingElement)
    {
        ResetProgressForNewGame();
        PendingStartingElement = null;

        if (startingElement != null && equippedChamber.Count > 0)
        {
            equippedChamber[0] = startingElement;
            Debug.Log($"Starting marble selected: {startingElement.elementName}");
        }
    }

    public static void SetPendingStartingElement(MarbleElementSO startingElement)
    {
        PendingStartingElement = startingElement;
    }

    public static void SetPendingWorldMapNode(int worldMapIndex, WorldMapConfigSO worldMapConfig)
    {
        PendingWorldMapIndex = worldMapIndex;
        PendingWorldMapConfig = worldMapConfig;
    }

    private void ApplyPendingStartingElement()
    {
        if (PendingStartingElement == null || equippedChamber.Count == 0) return;

        equippedChamber[0] = PendingStartingElement;
        Debug.Log($"Starting marble applied: {PendingStartingElement.elementName}");
        PendingStartingElement = null;
    }

    private void ApplyPendingWorldMapSelection()
    {
        if (PendingWorldMapIndex < 0) return;

        StartWorldMapNode(PendingWorldMapIndex, PendingWorldMapConfig);
        PendingWorldMapIndex = -1;
        PendingWorldMapConfig = null;
    }
}
