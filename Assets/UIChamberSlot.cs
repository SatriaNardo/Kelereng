using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIChamberSlot : MonoBehaviour
{
    [HideInInspector] public int slotIndex; // Sekarang diisi otomatis oleh manager
    public TMP_Text elementLabelText;
    public Image backgroundImage;

    private UIInventoryManager manager;

    // Tambahkan parameter index di sini
    public void SetupSlot(UIInventoryManager inventoryManager, int index)
    {
        manager = inventoryManager;
        slotIndex = index;
        GetComponent<Button>().onClick.AddListener(OnSlotClicked);
    }

    public void RefreshDisplay(MarbleElementSO element)
    {
        if (element != null)
        {
            elementLabelText.text = element.elementName[0].ToString().ToUpper();
            backgroundImage.color = element.elementColor;
        }
        else
        {
            elementLabelText.text = "0"; 
            backgroundImage.color = Color.gray;
        }
    }

    private void OnSlotClicked()
    {
        manager.SelectSlot(slotIndex);
    }
}