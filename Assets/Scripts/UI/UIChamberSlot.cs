using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIChamberSlot : MonoBehaviour
{
    [HideInInspector] public int slotIndex; // Sekarang diisi otomatis oleh manager
    public TMP_Text elementLabelText;
    public Image backgroundImage;
    public Image marbleImage;
    public Sprite commonMarbleSprite;

    private UIInventoryManager manager;
    private Color defaultBackgroundColor = Color.white;

    private void Awake()
    {
        if (backgroundImage != null)
        {
            defaultBackgroundColor = backgroundImage.color;
        }

        if (marbleImage == null)
        {
            Transform marbleImageTransform = transform.Find("MarbleImage");
            if (marbleImageTransform != null)
            {
                marbleImage = marbleImageTransform.GetComponent<Image>();
            }
        }
    }

    // Tambahkan parameter index di sini
    public void SetupSlot(UIInventoryManager inventoryManager, int index)
    {
        manager = inventoryManager;
        slotIndex = index;
        GetComponent<Button>().onClick.AddListener(OnSlotClicked);
    }

    public void RefreshDisplay(MarbleElementSO element)
    {
        Sprite marbleSprite = element != null ? element.idleSprite : commonMarbleSprite;

        if (marbleImage != null)
        {
            marbleImage.sprite = marbleSprite;
            marbleImage.preserveAspect = true;
            marbleImage.color = marbleSprite != null ? Color.white : Color.clear;
            marbleImage.enabled = marbleSprite != null;
        }

        if (elementLabelText != null)
        {
            elementLabelText.gameObject.SetActive(true);
            elementLabelText.enableAutoSizing = true;
            elementLabelText.fontSizeMin = 12f;
            elementLabelText.fontSizeMax = 28f;
            elementLabelText.textWrappingMode = TextWrappingModes.Normal;
        }

        if (element != null)
        {
            if (elementLabelText != null)
            {
                elementLabelText.text = element.elementName;
            }
        }
        else
        {
            if (elementLabelText != null)
            {
                elementLabelText.text = "Common Marble";
            }
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = defaultBackgroundColor;
        }
    }

    private void OnSlotClicked()
    {
        manager.SelectSlot(slotIndex);
    }
}
