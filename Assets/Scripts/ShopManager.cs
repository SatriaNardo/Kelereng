using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // BARU: Diperlukan untuk memanipulasi komponen Button
using TMPro;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class ShopManager : MonoBehaviour
{
    private enum ShopOfferType
    {
        NewMarble,
        Element,
        Emblem
    }

    private class ShopOffer
    {
        public ShopOfferType type;
        public MarbleElementSO element;
        public BaseEmblemSO emblem;
        public int price;
        public bool sold;
    }

    [Header("UI Panels")]
    public GameObject itemsPanel;       
    public GameObject inventoryPanel;   
    public TMP_Text shopCurrencyText;

    [Header("Item Prices & Refresh Cost")]
    public int priceMaxAmmo = 1;
    public int priceFireElement = 1;
    public int priceWindElement = 1;
    public int priceWaterElement = 1;
    public int priceEarthElement = 1;
    public int priceRefreshShop = 5; // BARU: Biaya untuk me-refresh toko

    [Header("Emblem Shop")]
    public int priceEmblem = 12;
    public BaseEmblemSO recallZoneEmblem;
    public BaseEmblemSO ricochetMasterEmblem;
    public BaseEmblemSO cloneArmyEmblem;
    public BaseEmblemSO phoenixEmblem;
    public BaseEmblemSO eagleEyeEmblem;
    public BaseEmblemSO hydraEmblem;
    public BaseEmblemSO blackHoleEmblem;
    public BaseEmblemSO luckyDrawEmblem;
    public BaseEmblemSO crusherEmblem;
    [Tooltip("Optional extra emblem offers beyond the named slots above.")]
    public List<BaseEmblemSO> emblemsForSale = new List<BaseEmblemSO>();

    [Header("Random Shop Offers")]
    [Range(1, 4)] public int visibleOfferCount = 4;
    public List<MarbleElementSO> elementsForSale = new List<MarbleElementSO>();

    [Header("Shop Offer Icons")]
    public Sprite commonMarbleOfferSprite;
    public Sprite fireElementOfferSprite;
    public Sprite waterElementOfferSprite;
    public Sprite windElementOfferSprite;
    public Sprite earthElementOfferSprite;

    [Header("Offer Button Slots")]
    [FormerlySerializedAs("maxAmmoButton")] public Button offerSlot1Button;
    [FormerlySerializedAs("fireElementButton")] public Button offerSlot2Button;
    [FormerlySerializedAs("windElementButton")] public Button offerSlot3Button;
    [FormerlySerializedAs("waterElementButton")] public Button offerSlot4Button;
    [FormerlySerializedAs("earthElementButton")] public Button unusedLegacyOfferButton;

    [Header("ScriptableObject Asset References")]
    public MarbleElementSO fireElementAsset;
    public MarbleElementSO windElementAsset;
    public MarbleElementSO waterElementAsset;
    public MarbleElementSO earthElementAsset;

    [Header("UI Inventory Connector")]
    public UIInventoryManager uiInventoryManager;

    private readonly List<ShopOffer> currentOffers = new List<ShopOffer>();
    private readonly List<Button> stockButtons = new List<Button>();
    private GameObject inventoryPanelRoot;

    private void Start()
    {
        ResolveInventoryPanelRoot();
        ConfigureStockButtons();
        RollShopOffers();

        // 1. Amankan status UI: Aktifkan dulu panel inventori agar komponen anaknya bisa di-build oleh kode
        SetInventoryPanelVisible(true);
        
        // 2. Bangun isi peluru kelereng lokal agar datanya langsung sinkron sejak detik pertama
        if (uiInventoryManager != null)
        {
            uiInventoryManager.BuildDynamicInventoryUI();
        }

        // 3. BARU: Setelah ter-build dengan selamat, sekarang sembunyikan kembali panel inventori
        // sehingga pemain murni hanya melihat ItemsPanel di awal masuk toko
        SetInventoryPanelVisible(false);
        if (itemsPanel != null) itemsPanel.SetActive(true);
        
        // 4. Update data finansial dan teks tombol
        UpdateShopCurrencyUI();
        UpdateAllButtonStates(); 
    }

    private void UpdateShopCurrencyUI()
    {
        if (shopCurrencyText != null && ProgressionManager.Instance != null)
            shopCurrencyText.text = ProgressionManager.Instance.playerCurrency.ToString();
    }

    // BARU: Fungsi internal untuk mengatur tombol UI aktif/nonaktif beserta teks penanda "SOLD OUT"
    private void UpdateAllButtonStates()
    {
        UpdateOfferButtonStates();
    }

    private void SetButtonState(Button btn, bool isAvailable, string originalText)
    {
        if (btn == null) return;
        
        btn.interactable = isAvailable;
        TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();
        if (btnText != null)
        {
            btnText.text = isAvailable ? originalText : "<color=red>SOLD OUT</color>";
        }
    }

    // ========================================================
    // AKSI HAMPIRAN TOMBOL ITEM
    // ========================================================

    public void BuyMaxAmmoUpgrade()
    {
        BuyFirstOfferOfType(ShopOfferType.NewMarble);
    }

    public void BuyFireElement()
    {
        BuyFirstOfferForElement(fireElementAsset);
    }

    public void BuyWindElement()
    {
        BuyFirstOfferForElement(windElementAsset);
    }

    public void BuyWaterElement()
    {
        BuyFirstOfferForElement(waterElementAsset);
    }

    public void BuyEarthElement()
    {
        BuyFirstOfferForElement(earthElementAsset);
    }

    private bool TryOpenInfusionMode(MarbleElementSO element, int price)
    {
        if (element == null)
        {
            Debug.LogWarning("Element asset is not assigned in ShopManager.");
            return false;
        }

        if (ProgressionManager.Instance.playerCurrency >= price)
        {
            ProgressionManager.Instance.AddCurrency(-price);
            UpdateShopCurrencyUI();

            ProgressionManager.Instance.pendingElementFromShop = element;

            if (itemsPanel != null) itemsPanel.SetActive(false);
            SetInventoryPanelVisible(true);

            if (uiInventoryManager != null)
            {
                uiInventoryManager.BuildDynamicInventoryUI();
                uiInventoryManager.EnterInfusionMode(this, element.elementName);
            }

            return true;
        }

        return false;
    }

    // Dipanggil otomatis oleh UIInventoryManager setelah pemain memilih salah satu kotak peluru
    public void CompleteInfusionTransaction(int selectedSlotIndex)
    {
        MarbleElementSO purchasedElement = ProgressionManager.Instance.pendingElementFromShop;
        
        ProgressionManager.Instance.LoadElementToSlot(selectedSlotIndex, purchasedElement);
        ProgressionManager.Instance.pendingElementFromShop = null;

        // PERBAIKAN: Menghapus baris pemindahan Scene otomatis dan pengubahan nomor lantai.
        // Sekarang tampilan hanya akan berganti kembali memperlihatkan menu Item Toko.
        SetInventoryPanelVisible(false);
        if (itemsPanel != null) itemsPanel.SetActive(true);
        
        // Memaksa UI Inventory lokal memperbarui simbol barunya (F atau W) seketika
        if (uiInventoryManager != null) uiInventoryManager.BuildDynamicInventoryUI();
    }

    // BARU: Fungsi untuk merestok ulang seluruh item di toko menggunakan koin kelereng
    public void RefreshShopButton()
    {
        // 1. Cek apakah koin pemain mencukupi untuk biaya refresh saat ini
        if (ProgressionManager.Instance.playerCurrency >= priceRefreshShop)
        {
            // Potong koin pemain sesuai harga saat ini
            ProgressionManager.Instance.AddCurrency(-priceRefreshShop);
            
            RollShopOffers();

            Debug.Log($"🔄 Toko di-restock! Biaya sebelumnya: {priceRefreshShop}");

            // ========================================================
            // BARU: Naikkan biaya refresh x2 untuk ditekan berikutnya
            // ========================================================
            priceRefreshShop *= 2; 

            // Segarkan UI koin dan perbarui teks harga tombol di layar toko
            UpdateShopCurrencyUI();
            UpdateAllButtonStates();
            UpdateRefreshButtonText(); // Fungsi pembantu baru di bawah
        }
        else
        {
            Debug.LogWarning($"❌ Uang tidak cukup! Butuh {priceRefreshShop} koin untuk refresh.");
        }
    }

    // BARU: Fungsi pembantu untuk memperbarui teks tombol refresh di Inspector secara dinamis
    private void UpdateRefreshButtonText()
    {
        // Cari tombol Refresh di dalam itemsPanel (asumsi tombol memiliki komponen Text/TMP)
        // Jika kamu ingin mereferensikan langsung via variabel seperti tombol item lainnya, 
        // kamu bisa membuat public Button refreshButton di atas dan mengubah teksnya di sini.
        Debug.Log($"Harga baru untuk refresh berikutnya: {priceRefreshShop} koin.");
    }

    public void ExitShopButton()
    {
        // Pindah lantai dan kembali ke MapScene baru dieksekusi saat tombol Keluar/Exit ditekan manual
        ProgressionManager.Instance.currentFloor++;
        SceneManager.LoadScene("MapScene");
    }

    private void ConfigureStockButtons()
    {
        stockButtons.Clear();
        AddStockButton(offerSlot1Button);
        AddStockButton(offerSlot2Button);
        AddStockButton(offerSlot3Button);
        AddStockButton(offerSlot4Button);

        if (unusedLegacyOfferButton != null)
        {
            unusedLegacyOfferButton.gameObject.SetActive(false);
        }
    }

    private void AddStockButton(Button button)
    {
        if (button == null) return;

        button.gameObject.SetActive(stockButtons.Count < visibleOfferCount);
        button.onClick = new Button.ButtonClickedEvent();
        int capturedIndex = stockButtons.Count;
        button.onClick.AddListener(() => BuyOffer(capturedIndex));
        stockButtons.Add(button);
    }

    private void RollShopOffers()
    {
        EnsureDefaultOfferPools();
        currentOffers.Clear();

        List<ShopOffer> offerPool = BuildOfferPool();
        int count = Mathf.Min(visibleOfferCount, stockButtons.Count);

        for (int i = 0; i < count && offerPool.Count > 0; i++)
        {
            int pickedIndex = Random.Range(0, offerPool.Count);
            currentOffers.Add(offerPool[pickedIndex]);
            offerPool.RemoveAt(pickedIndex);
        }

        UpdateOfferButtonStates();
    }

    private List<ShopOffer> BuildOfferPool()
    {
        List<ShopOffer> offerPool = new List<ShopOffer>
        {
            new ShopOffer { type = ShopOfferType.NewMarble, price = priceMaxAmmo }
        };

        foreach (MarbleElementSO element in elementsForSale)
        {
            if (element == null) continue;
            offerPool.Add(new ShopOffer
            {
                type = ShopOfferType.Element,
                element = element,
                price = GetElementPrice(element)
            });
        }

        foreach (BaseEmblemSO emblem in GetAllEmblemsForSale())
        {
            if (emblem == null) continue;
            if (ProgressionManager.Instance != null && ProgressionManager.Instance.HasPlayableEmblem(emblem)) continue;

            offerPool.Add(new ShopOffer
            {
                type = ShopOfferType.Emblem,
                emblem = emblem,
                price = priceEmblem
            });
        }

        return offerPool;
    }

    private List<BaseEmblemSO> GetAllEmblemsForSale()
    {
        List<BaseEmblemSO> allEmblems = new List<BaseEmblemSO>();

        AddUniqueEmblem(allEmblems, recallZoneEmblem);
        AddUniqueEmblem(allEmblems, ricochetMasterEmblem);
        AddUniqueEmblem(allEmblems, cloneArmyEmblem);
        AddUniqueEmblem(allEmblems, phoenixEmblem);
        AddUniqueEmblem(allEmblems, eagleEyeEmblem);
        AddUniqueEmblem(allEmblems, hydraEmblem);
        AddUniqueEmblem(allEmblems, blackHoleEmblem);
        AddUniqueEmblem(allEmblems, luckyDrawEmblem);
        AddUniqueEmblem(allEmblems, crusherEmblem);

        foreach (BaseEmblemSO emblem in emblemsForSale)
        {
            AddUniqueEmblem(allEmblems, emblem);
        }

        return allEmblems;
    }

    private void AddUniqueEmblem(List<BaseEmblemSO> emblems, BaseEmblemSO emblem)
    {
        if (emblem != null && !emblems.Contains(emblem))
        {
            emblems.Add(emblem);
        }
    }

    private void EnsureDefaultOfferPools()
    {
        if (elementsForSale.Count == 0)
        {
            AddElementToPool(fireElementAsset);
            AddElementToPool(windElementAsset);
            AddElementToPool(waterElementAsset);
            AddElementToPool(earthElementAsset);
        }
    }

    private void AddElementToPool(MarbleElementSO element)
    {
        if (element != null && !elementsForSale.Contains(element))
        {
            elementsForSale.Add(element);
        }
    }

    private int GetElementPrice(MarbleElementSO element)
    {
        if (element == fireElementAsset) return priceFireElement;
        if (element == windElementAsset) return priceWindElement;
        if (element == waterElementAsset) return priceWaterElement;
        if (element == earthElementAsset) return priceEarthElement;
        return priceFireElement;
    }

    private void BuyOffer(int offerIndex)
    {
        if (ProgressionManager.Instance == null) return;
        if (offerIndex < 0 || offerIndex >= currentOffers.Count) return;

        ShopOffer offer = currentOffers[offerIndex];
        if (offer == null || offer.sold) return;

        bool purchased = false;
        switch (offer.type)
        {
            case ShopOfferType.NewMarble:
                purchased = BuyNewMarble(offer.price);
                break;
            case ShopOfferType.Element:
                purchased = TryOpenInfusionMode(offer.element, offer.price);
                break;
            case ShopOfferType.Emblem:
                purchased = BuyEmblemOffer(offer.emblem, offer.price);
                break;
        }

        if (purchased)
        {
            offer.sold = true;
            UpdateShopCurrencyUI();
            UpdateOfferButtonStates();
        }
    }

    private bool BuyNewMarble(int price)
    {
        if (ProgressionManager.Instance.playerCurrency < price) return false;

        ProgressionManager.Instance.AddCurrency(-price);
        ProgressionManager.Instance.BASE_AMMO++;
        ProgressionManager.Instance.AddAmmoToChamber(null);
        Debug.Log("Bought a new common marble.");
        return true;
    }

    private bool BuyEmblemOffer(BaseEmblemSO emblem, int price)
    {
        if (emblem == null) return false;
        if (ProgressionManager.Instance.HasPlayableEmblem(emblem)) return false;
        if (ProgressionManager.Instance.playerCurrency < price) return false;

        ProgressionManager.Instance.AddCurrency(-price);
        ProgressionManager.Instance.AddPlayableEmblem(emblem);
        return true;
    }

    private void BuyFirstOfferOfType(ShopOfferType type)
    {
        for (int i = 0; i < currentOffers.Count; i++)
        {
            if (currentOffers[i] != null && currentOffers[i].type == type)
            {
                BuyOffer(i);
                return;
            }
        }
    }

    private void BuyFirstOfferForElement(MarbleElementSO element)
    {
        for (int i = 0; i < currentOffers.Count; i++)
        {
            ShopOffer offer = currentOffers[i];
            if (offer != null && offer.type == ShopOfferType.Element && offer.element == element)
            {
                BuyOffer(i);
                return;
            }
        }
    }

    private void UpdateOfferButtonStates()
    {
        for (int i = 0; i < stockButtons.Count; i++)
        {
            Button button = stockButtons[i];
            if (button == null) continue;

            bool hasOffer = i < currentOffers.Count && currentOffers[i] != null;
            button.gameObject.SetActive(i < visibleOfferCount && hasOffer);

            if (!hasOffer) continue;

            ShopOffer offer = currentOffers[i];
            button.interactable = !offer.sold;
            SetOfferButtonVisual(button, offer);
        }
    }

    private void SetOfferButtonVisual(Button button, ShopOffer offer)
    {
        if (button == null || offer == null) return;

        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = offer.sold
                ? $"{GetOfferName(offer)}\n<color=red>SOLD OUT</color>"
                : $"{GetOfferName(offer)}\n{offer.price}";
            text.fontSize = 22f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = 22f;
            text.textWrappingMode = TextWrappingModes.Normal;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            Sprite icon = GetOfferSprite(offer);
            if (icon != null)
            {
                image.sprite = icon;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
            }
            else
            {
                image.sprite = null;
            }

            image.color = offer.type == ShopOfferType.Element && offer.element != null && icon == null
                ? offer.element.elementColor
                : Color.white;
        }
    }

    private string GetOfferName(ShopOffer offer)
    {
        if (offer == null) return "Offer";
        switch (offer.type)
        {
            case ShopOfferType.NewMarble:
                return "New Marble";
            case ShopOfferType.Element:
                return offer.element != null ? offer.element.elementName : "Element";
            case ShopOfferType.Emblem:
                return GetEmblemDisplayName(offer.emblem);
            default:
                return "Offer";
        }
    }

    private Sprite GetOfferSprite(ShopOffer offer)
    {
        if (offer == null) return null;
        switch (offer.type)
        {
            case ShopOfferType.NewMarble:
                return commonMarbleOfferSprite;
            case ShopOfferType.Element:
                return GetElementOfferSprite(offer.element);
            case ShopOfferType.Emblem:
                return offer.emblem != null ? offer.emblem.icon : null;
            default:
                return null;
        }
    }

    private Sprite GetElementOfferSprite(MarbleElementSO element)
    {
        if (element == null) return null;
        if (element == fireElementAsset) return fireElementOfferSprite;
        if (element == waterElementAsset) return waterElementOfferSprite;
        if (element == windElementAsset) return windElementOfferSprite;
        if (element == earthElementAsset) return earthElementOfferSprite;
        return element.idleSprite;
    }

    private string GetEmblemDisplayName(BaseEmblemSO emblem)
    {
        if (emblem == null) return "Emblem";
        return !string.IsNullOrWhiteSpace(emblem.emblemName) ? emblem.emblemName : emblem.name;
    }

    private void ResolveInventoryPanelRoot()
    {
        if (inventoryPanel == null) return;

        inventoryPanelRoot = inventoryPanel;
        Transform current = inventoryPanel.transform;
        while (current != null)
        {
            if (current.name == "InventoryPanel")
            {
                inventoryPanelRoot = current.gameObject;
                return;
            }

            current = current.parent;
        }
    }

    private void SetInventoryPanelVisible(bool isVisible)
    {
        ResolveInventoryPanelRoot();

        if (inventoryPanelRoot != null)
        {
            inventoryPanelRoot.SetActive(isVisible);
        }

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isVisible);
        }
    }
}
