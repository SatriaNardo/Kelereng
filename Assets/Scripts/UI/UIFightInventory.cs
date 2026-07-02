using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
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
    public Vector2 reserveCircleCenter = new Vector2(0f, -70f);
    public Vector2 reserveCircleRadius = new Vector2(170f, 115f);
    public float reserveStartAngle = 270f;
    public Vector2 activeSlotSize = new Vector2(92f, 92f);
    public Vector2 reserveSlotSize = new Vector2(62f, 62f);
    [Range(0.4f, 1f)] public float farSlotScale = 0.78f;
    public float scrollDegreesPerStep = 18f;
    public float dragDegreesPerPixel = 0.35f;
    public bool invertCarouselScroll = false;

    [Header("Panel Toggle")]
    public bool startPanelOpen = false;
    public Vector2 toggleButtonSize = new Vector2(96f, 44f);
    public Vector2 toggleButtonOffsetFromRight = new Vector2(-24f, 0f);
    public Color toggleButtonColor = new Color(0.95f, 0.18f, 0.16f, 1f);

    private readonly List<RectTransform> reserveSlotTransforms = new List<RectTransform>();
    private float carouselRotationOffset;
    private bool isDraggingCarousel;
    private Vector2 lastPointerPosition;
    private float totalDragDistance;
    private CanvasGroup panelCanvasGroup;
    private Button panelToggleButton;
    private TextMeshProUGUI panelToggleText;
    private bool isPanelOpen;

    private void OnEnable()
    {
        EnsurePanelToggleButton();
        EnsurePanelCanvasGroup();
        isPanelOpen = startPanelOpen;
        ApplyPanelVisibility();
        RefreshAvailableElementsUI();
    }

    private void OnDisable()
    {
        SetLauncherSwapVisibility(false);
    }

    private void Update()
    {
        if (!isPanelOpen || !useCarouselLayout || reserveSlotTransforms.Count <= 1) return;

        HandleCarouselInput();
    }

    public void RefreshAvailableElementsUI()
    {
        if (ProgressionManager.Instance == null || containerGrid == null || quickSlotPrefab == null) return;

        PrepareContainerForManualLayout();
        reserveSlotTransforms.Clear();

        // 1. Bersihkan sisa tombol lama di layar
        foreach (Transform child in containerGrid) Destroy(child.gameObject);

        List<MarbleElementSO> chamber = ProgressionManager.Instance.equippedChamber;
        if (chamber.Count == 0) return;

        if (useCarouselLayout)
        {
            RefreshCarousel(chamber);
            ApplyPanelVisibility();
            return;
        }

        // Loop dimulai dari indeks 1 (karena Indeks 0 sedang nangkring di ketapel)
        for (int i = 1; i < chamber.Count; i++)
        {
            GameObject btnObj = CreateSlotButton(chamber[i], i, false);
            ConfigureSwapButton(btnObj, i);
        }

        ApplyPanelVisibility();
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
            ConfigureReserveSelectButton(reserveSlot, i);
            RectTransform reserveRect = reserveSlot.GetComponent<RectTransform>();
            reserveSlotTransforms.Add(reserveRect);
            PositionReserveSlot(reserveRect, i - 1, reserveCount);
        }

        RefreshReserveDrawOrder();
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
            img.color = Color.white;
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

            if (marbleLauncher.ForceSwapActiveMarble(indexCatcher))
            {
                RefreshAvailableElementsUI();
            }
        });
    }

    private void ConfigureReserveSelectButton(GameObject btnObj, int chamberIndex)
    {
        Button button = btnObj.GetComponent<Button>();
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        int indexCatcher = chamberIndex;
        button.onClick.AddListener(() =>
        {
            if (totalDragDistance > 8f) return;
            if (marbleLauncher == null) return;

            if (marbleLauncher.ForceSwapActiveMarble(indexCatcher))
            {
                RefreshAvailableElementsUI();
                ClosePanel();
            }
        });
    }

    private void EnsurePanelCanvasGroup()
    {
        if (containerGrid == null) return;

        panelCanvasGroup = containerGrid.GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = containerGrid.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void EnsurePanelToggleButton()
    {
        if (panelToggleButton != null || containerGrid == null) return;

        Canvas canvas = containerGrid.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        GameObject buttonObject = new GameObject("SwapPanelToggleButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 0.5f);
        rectTransform.anchorMax = new Vector2(1f, 0.5f);
        rectTransform.pivot = new Vector2(1f, 0.5f);
        rectTransform.anchoredPosition = toggleButtonOffsetFromRight;
        rectTransform.sizeDelta = toggleButtonSize;

        Image image = buttonObject.GetComponent<Image>();
        image.color = toggleButtonColor;

        panelToggleButton = buttonObject.GetComponent<Button>();
        panelToggleButton.onClick.AddListener(TogglePanel);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        panelToggleText = textObject.GetComponent<TextMeshProUGUI>();
        panelToggleText.fontSize = 18f;
        panelToggleText.alignment = TextAlignmentOptions.Center;
        panelToggleText.color = Color.white;
        panelToggleText.raycastTarget = false;
    }

    private void TogglePanel()
    {
        isPanelOpen = !isPanelOpen;
        if (isPanelOpen)
        {
            RefreshAvailableElementsUI();
        }
        else
        {
            ClosePanel();
            return;
        }

        ApplyPanelVisibility();
    }

    private void ClosePanel()
    {
        isPanelOpen = false;
        ApplyPanelVisibility();
    }

    private void ApplyPanelVisibility()
    {
        EnsurePanelCanvasGroup();

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = isPanelOpen ? 1f : 0f;
            panelCanvasGroup.interactable = isPanelOpen;
            panelCanvasGroup.blocksRaycasts = isPanelOpen;
        }

        if (panelToggleText != null)
        {
            panelToggleText.text = isPanelOpen ? "Close" : "Swap";
        }

        SetLauncherSwapVisibility(isPanelOpen);
    }

    private void SetLauncherSwapVisibility(bool swapping)
    {
        if (marbleLauncher == null) return;

        marbleLauncher.SetLauncherPreviewActive(!swapping);
    }

    private void PositionReserveSlot(RectTransform slotTransform, int reserveIndex, int reserveCount)
    {
        if (slotTransform == null) return;

        float angleStep = reserveCount <= 0 ? 0f : 360f / reserveCount;
        float angle = (reserveStartAngle + carouselRotationOffset + reserveIndex * angleStep) * Mathf.Deg2Rad;
        Vector2 position = reserveCircleCenter + new Vector2(Mathf.Cos(angle) * reserveCircleRadius.x, Mathf.Sin(angle) * reserveCircleRadius.y);
        slotTransform.anchoredPosition = position;

        float frontAmount = Mathf.InverseLerp(-1f, 1f, Mathf.Sin(angle));
        float scale = Mathf.Lerp(farSlotScale, 1f, frontAmount);
        slotTransform.localScale = Vector3.one * scale;
        slotTransform.sizeDelta = reserveSlotSize;
    }

    private void HandleCarouselInput()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null) return;

        Vector2 pointerPosition = pointer.position.ReadValue();
        Vector2 scrollDelta = Mouse.current != null ? Mouse.current.scroll.ReadValue() : Vector2.zero;
        if (Mathf.Abs(scrollDelta.y) > 0.01f)
        {
            float direction = invertCarouselScroll ? -1f : 1f;
            RotateCarousel(scrollDelta.y * scrollDegreesPerStep * direction / 120f);
        }

        if (pointer.press.wasPressedThisFrame && IsPointerInsideContainer(pointerPosition))
        {
            isDraggingCarousel = true;
            lastPointerPosition = pointerPosition;
            totalDragDistance = 0f;
        }
        else if (pointer.press.wasReleasedThisFrame)
        {
            isDraggingCarousel = false;
        }

        if (!isDraggingCarousel || !pointer.press.isPressed) return;

        float deltaX = pointerPosition.x - lastPointerPosition.x;
        if (Mathf.Abs(deltaX) > 0.01f)
        {
            float direction = invertCarouselScroll ? -1f : 1f;
            RotateCarousel(deltaX * dragDegreesPerPixel * direction);
        }

        totalDragDistance += Vector2.Distance(pointerPosition, lastPointerPosition);
        lastPointerPosition = pointerPosition;
    }

    private bool IsPointerInsideContainer(Vector2 screenPosition)
    {
        RectTransform containerRect = containerGrid as RectTransform;
        if (containerRect == null) return true;

        Canvas canvas = containerGrid.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(containerRect, screenPosition, eventCamera);
    }

    private void RotateCarousel(float degrees)
    {
        carouselRotationOffset += degrees;
        RefreshReservePositions();
    }

    private void RefreshReservePositions()
    {
        int reserveCount = reserveSlotTransforms.Count;
        for (int i = 0; i < reserveCount; i++)
        {
            PositionReserveSlot(reserveSlotTransforms[i], i, reserveCount);
        }

        RefreshReserveDrawOrder();
    }

    private void RefreshReserveDrawOrder()
    {
        List<RectTransform> sortedSlots = new List<RectTransform>(reserveSlotTransforms);
        sortedSlots.Sort((a, b) => a.anchoredPosition.y.CompareTo(b.anchoredPosition.y));
        for (int i = 0; i < sortedSlots.Count; i++)
        {
            sortedSlots[i].SetSiblingIndex(i);
        }
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
