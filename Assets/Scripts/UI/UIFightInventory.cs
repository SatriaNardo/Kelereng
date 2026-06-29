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
    public Sprite commonMarbleSprite;

    [Header("Carousel Layout")]
    public bool useCarouselLayout = true;
    public bool showActiveMarbleInCenter = true;
    public Vector2 carouselCenter = new Vector2(0f, 110f);
    public Vector2 carouselRadius = new Vector2(170f, 90f);
    [Range(90f, 270f)] public float arcStartAngle = 205f;
    [Range(270f, 450f)] public float arcEndAngle = 335f;
    public Vector2 activeSlotSize = new Vector2(92f, 92f);
    public Vector2 reserveSlotSize = new Vector2(62f, 62f);
    [Range(0.4f, 1f)] public float farSlotScale = 0.78f;

    private void OnEnable()
    {
        RefreshAvailableElementsUI();
    }

    public void RefreshAvailableElementsUI()
    {
        if (ProgressionManager.Instance == null || containerGrid == null || quickSlotPrefab == null) return;

        PrepareContainerForManualLayout();

        // 1. Bersihkan sisa tombol lama di layar
        foreach (Transform child in containerGrid) Destroy(child.gameObject);

        List<MarbleElementSO> chamber = ProgressionManager.Instance.equippedChamber;
        if (chamber.Count == 0) return;

        if (useCarouselLayout)
        {
            RefreshCarousel(chamber);
            return;
        }

        // Loop dimulai dari indeks 1 (karena Indeks 0 sedang nangkring di ketapel)
        for (int i = 1; i < chamber.Count; i++)
        {
            GameObject btnObj = CreateSlotButton(chamber[i], i, false);
            ConfigureSwapButton(btnObj, i);
        }
    }

    private void RefreshCarousel(List<MarbleElementSO> chamber)
    {
        if (showActiveMarbleInCenter)
        {
            GameObject activeSlot = CreateSlotButton(chamber[0], 0, true);
            RectTransform activeRect = activeSlot.GetComponent<RectTransform>();
            activeRect.anchoredPosition = carouselCenter;
            activeRect.sizeDelta = activeSlotSize;
            activeSlot.transform.SetAsLastSibling();

            Button activeButton = activeSlot.GetComponent<Button>();
            if (activeButton != null)
            {
                activeButton.onClick.RemoveAllListeners();
            }
        }

        int reserveCount = chamber.Count - 1;
        for (int i = 1; i < chamber.Count; i++)
        {
            GameObject reserveSlot = CreateSlotButton(chamber[i], i, false);
            ConfigureSwapButton(reserveSlot, i);
            PositionReserveSlot(reserveSlot.GetComponent<RectTransform>(), i - 1, reserveCount);
        }
    }

    private GameObject CreateSlotButton(MarbleElementSO element, int chamberIndex, bool isActiveSlot)
    {
        GameObject btnObj = Instantiate(quickSlotPrefab, containerGrid);
        btnObj.name = isActiveSlot ? "ActiveMarbleButton" : $"ReserveMarbleButton_{chamberIndex}";

        TMP_Text txt = btnObj.GetComponentInChildren<TMP_Text>();
        Image img = btnObj.GetComponent<Image>();
        Sprite marbleSprite = GetMarbleSprite(element);

        if (img != null)
        {
            img.sprite = marbleSprite;
            img.preserveAspect = true;
            img.color = marbleSprite != null
                ? Color.white
                : (element != null ? element.elementColor : Color.white);
        }

        if (txt != null)
        {
            bool hasSprite = marbleSprite != null;
            txt.gameObject.SetActive(!hasSprite);
            txt.text = GetFallbackLabel(element);
        }

        RectTransform rectTransform = btnObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = isActiveSlot ? activeSlotSize : reserveSlotSize;
            rectTransform.localScale = Vector3.one;
        }

        return btnObj;
    }

    private void ConfigureSwapButton(GameObject btnObj, int chamberIndex)
    {
        Button button = btnObj.GetComponent<Button>();
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        int indexCatcher = chamberIndex;
        button.onClick.AddListener(() =>
        {
            if (marbleLauncher == null) return;

            marbleLauncher.ForceSwapActiveMarble(indexCatcher);
            RefreshAvailableElementsUI();
        });
    }

    private void PositionReserveSlot(RectTransform slotTransform, int reserveIndex, int reserveCount)
    {
        if (slotTransform == null) return;

        float t = reserveCount <= 1 ? 0.5f : (float)reserveIndex / (reserveCount - 1);
        float angle = Mathf.Lerp(arcStartAngle, arcEndAngle, t) * Mathf.Deg2Rad;
        Vector2 position = carouselCenter + new Vector2(Mathf.Cos(angle) * carouselRadius.x, Mathf.Sin(angle) * carouselRadius.y);
        slotTransform.anchoredPosition = position;

        float distanceFromCenter = Mathf.Abs(t - 0.5f) * 2f;
        float scale = Mathf.Lerp(1f, farSlotScale, distanceFromCenter);
        slotTransform.localScale = Vector3.one * scale;
        slotTransform.sizeDelta = reserveSlotSize;

        slotTransform.SetSiblingIndex(reserveIndex);
    }

    private void PrepareContainerForManualLayout()
    {
        LayoutGroup[] layoutGroups = containerGrid.GetComponents<LayoutGroup>();
        foreach (LayoutGroup layoutGroup in layoutGroups)
        {
            layoutGroup.enabled = !useCarouselLayout;
        }

        ContentSizeFitter fitter = containerGrid.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            fitter.enabled = !useCarouselLayout;
        }
    }

    private Sprite GetMarbleSprite(MarbleElementSO element)
    {
        if (element != null && element.idleSprite != null)
        {
            return element.idleSprite;
        }

        if (commonMarbleSprite != null)
        {
            return commonMarbleSprite;
        }

        MarbleElementHandler prefabHandler = marbleLauncher != null && marbleLauncher.gacoanPrefab != null
            ? marbleLauncher.gacoanPrefab.GetComponent<MarbleElementHandler>()
            : null;

        return prefabHandler != null ? prefabHandler.commonIdleSprite : null;
    }

    private string GetFallbackLabel(MarbleElementSO element)
    {
        if (element == null) return "N";
        return string.IsNullOrEmpty(element.elementName) ? "?" : element.elementName[0].ToString().ToUpper();
    }
}
