using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class UIFightInventory : MonoBehaviour
{
    [Header("Dynamic Setup")]
    public GameObject quickSlotPrefab;      
    public Transform containerGrid;         
    public MarbleLauncher marbleLauncher;   

    private void OnEnable()
    {
        RefreshAvailableElementsUI();
    }

    public void RefreshAvailableElementsUI()
    {
        if (ProgressionManager.Instance == null || containerGrid == null || quickSlotPrefab == null) return;

        // 1. Bersihkan sisa tombol lama di layar
        foreach (Transform child in containerGrid) Destroy(child.gameObject);

        List<MarbleElementSO> chamber = ProgressionManager.Instance.equippedChamber;

        // Loop dimulai dari indeks 1 (karena Indeks 0 sedang nangkring di ketapel)
        for (int i = 1; i < chamber.Count; i++)
        {
            MarbleElementSO currentElement = chamber[i];

            // 2. Buat tombol untuk SETIAP slot cadangan (baik elemen maupun polos)
            GameObject btnObj = Instantiate(quickSlotPrefab, containerGrid);
            
            TMP_Text txt = btnObj.GetComponentInChildren<TMP_Text>();
            Image img = btnObj.GetComponent<Image>();

            // 3. PERBAIKAN LOGIKA: Cabang visual untuk Elemen vs Polos
            if (currentElement != null)
            {
                // Jika berisi elemen aktif (Fire, Wind, dll)
                if (txt != null) txt.text = currentElement.elementName[0].ToString().ToUpper();
                if (img != null) img.color = currentElement.elementColor;
            }
            else
            {
                // BARU: Jika slot tersebut NULL (Berarti Kelereng Normal/Polos)
                if (txt != null) txt.text = "N"; // 'N' untuk Normal / Polos
                if (img != null) img.color = Color.white; // Warna putih standar kelereng polos
            }

            // 4. Beri fungsi klik penukar ke ketapel depan
            int indexCatcher = i; 
            btnObj.GetComponent<Button>().onClick.AddListener(() => {
                marbleLauncher.ForceSwapActiveMarble(indexCatcher);
                RefreshAvailableElementsUI(); // Segarkan ulang visual setelah sukses swap
            });
        }
    }
}