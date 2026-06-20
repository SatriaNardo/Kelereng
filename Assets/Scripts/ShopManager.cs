using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // BARU: Diperlukan untuk memanipulasi komponen Button
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject itemsPanel;       
    public GameObject inventoryPanel;   
    public TMP_Text shopCurrencyText;

    [Header("Item Prices & Refresh Cost")]
    public int priceMaxAmmo = 1;
    public int priceFireElement = 1;
    public int priceWindElement = 1;
    public int priceRefreshShop = 5; // BARU: Biaya untuk me-refresh toko

    [Header("UI Buttons References")]
    // BARU: Seret komponen UI Button dari masing-masing item ke sini untuk dimatikan saat sold out
    public Button maxAmmoButton;
    public Button fireElementButton;
    public Button windElementButton;

    [Header("ScriptableObject Asset References")]
    public MarbleElementSO fireElementAsset;
    public MarbleElementSO windElementAsset;

    [Header("UI Inventory Connector")]
    public UIInventoryManager uiInventoryManager;

    // BARU: State pelacak apakah item sudah dibeli atau belum di round ini
    private bool isMaxAmmoAvailable = true;
    private bool isFireElementAvailable = true;
    private bool isWindElementAvailable = true;

    private void Start()
    {
        // 1. Amankan status UI: Aktifkan dulu panel inventori agar komponen anaknya bisa di-build oleh kode
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        
        // 2. Bangun isi peluru kelereng lokal agar datanya langsung sinkron sejak detik pertama
        if (uiInventoryManager != null)
        {
            uiInventoryManager.BuildDynamicInventoryUI();
        }

        // 3. BARU: Setelah ter-build dengan selamat, sekarang sembunyikan kembali panel inventori
        // sehingga pemain murni hanya melihat ItemsPanel di awal masuk toko
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
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
        SetButtonState(maxAmmoButton, isMaxAmmoAvailable, $"Buy Max Ammo ({priceMaxAmmo})");
        SetButtonState(fireElementButton, isFireElementAvailable, $"Buy Fire ({priceFireElement})");
        SetButtonState(windElementButton, isWindElementAvailable, $"Buy Wind ({priceWindElement})");
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
        // Cek uang DAN pastikan item belum sold out
        if (isMaxAmmoAvailable && ProgressionManager.Instance.playerCurrency >= priceMaxAmmo)
        {
            ProgressionManager.Instance.AddCurrency(-priceMaxAmmo);
            ProgressionManager.Instance.BASE_AMMO++;
            ProgressionManager.Instance.AddAmmoToChamber(null);

            isMaxAmmoAvailable = false; // Set menjadi Sold Out

            UpdateShopCurrencyUI();
            UpdateAllButtonStates();
            Debug.Log("🛒 Max Ammo Upgraded!");
        }
    }

    public void BuyFireElement()
    {
        if (isFireElementAvailable)
        {
            TryOpenInfusionMode(fireElementAsset, priceFireElement, out isFireElementAvailable);
        }
    }

    public void BuyWindElement()
    {
        if (isWindElementAvailable)
        {
            TryOpenInfusionMode(windElementAsset, priceWindElement, out isWindElementAvailable);
        }
    }

    private void TryOpenInfusionMode(MarbleElementSO element, int price, out bool availabilityFlag)
    {
        // Default flag di-set true jika gagal beli di blok pengecekan uang bawah
        availabilityFlag = true; 

        if (ProgressionManager.Instance.playerCurrency >= price)
        {
            ProgressionManager.Instance.AddCurrency(-price);
            UpdateShopCurrencyUI();

            availabilityFlag = false; // Sukses beli, barang habis!
            UpdateAllButtonStates();

            ProgressionManager.Instance.pendingElementFromShop = element;

            itemsPanel.SetActive(false);
            inventoryPanel.SetActive(true);

            uiInventoryManager.BuildDynamicInventoryUI();
            uiInventoryManager.EnterInfusionMode(this, element.elementName);
        }
    }

    // Dipanggil otomatis oleh UIInventoryManager setelah pemain memilih salah satu kotak peluru
    public void CompleteInfusionTransaction(int selectedSlotIndex)
    {
        MarbleElementSO purchasedElement = ProgressionManager.Instance.pendingElementFromShop;
        
        ProgressionManager.Instance.LoadElementToSlot(selectedSlotIndex, purchasedElement);
        ProgressionManager.Instance.pendingElementFromShop = null;

        // PERBAIKAN: Menghapus baris pemindahan Scene otomatis dan pengubahan nomor lantai.
        // Sekarang tampilan hanya akan berganti kembali memperlihatkan menu Item Toko.
        inventoryPanel.SetActive(false);
        itemsPanel.SetActive(true);
        
        // Memaksa UI Inventory lokal memperbarui simbol barunya (F atau W) seketika
        uiInventoryManager.BuildDynamicInventoryUI(); 
    }

    // BARU: Fungsi untuk merestok ulang seluruh item di toko menggunakan koin kelereng
public void RefreshShopButton()
    {
        // 1. Cek apakah koin pemain mencukupi untuk biaya refresh saat ini
        if (ProgressionManager.Instance.playerCurrency >= priceRefreshShop)
        {
            // Potong koin pemain sesuai harga saat ini
            ProgressionManager.Instance.AddCurrency(-priceRefreshShop);
            
            // Kembalikan semua ketersediaan barang menjadi sedia kala
            isMaxAmmoAvailable = true;
            isFireElementAvailable = true;
            isWindElementAvailable = true;

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
}