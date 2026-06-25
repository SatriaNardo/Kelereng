using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldMapNode : MonoBehaviour
{
    public Button button;
    public TMP_Text label;
    public Image backgroundImage;
    public Color lockedColor = Color.gray;
    public Color unlockedColor = Color.white;
    public Color clearedColor = new Color(0.55f, 0.9f, 0.65f);

    private WorldMapManager worldMapManager;
    private int nodeIndex;

    private void Awake()
    {
        CacheReferences();
    }

    public void Setup(WorldMapManager manager, int index, bool isUnlocked, bool isCleared)
    {
        worldMapManager = manager;
        nodeIndex = index;

        CacheReferences();

        if (label != null)
        {
            label.text = (index + 1).ToString();
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = isCleared ? clearedColor : (isUnlocked ? unlockedColor : lockedColor);
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = isUnlocked;
            button.onClick.AddListener(OnClicked);
        }
    }

    public void OnClicked()
    {
        if (worldMapManager == null) return;

        RectTransform rectTransform = transform as RectTransform;
        worldMapManager.SelectWorldNode(nodeIndex, rectTransform);
    }

    private void CacheReferences()
    {
        if (button == null) button = GetComponent<Button>();
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        if (backgroundImage == null && button != null) backgroundImage = button.image;
        if (label == null) label = GetComponentInChildren<TMP_Text>();
    }
}
