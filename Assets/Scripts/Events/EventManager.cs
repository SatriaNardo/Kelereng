using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class EventManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text speakerText;
    public TMP_Text bodyText;
    public Button[] choiceButtons = new Button[3];
    public TMP_Text[] choiceLabels = new TMP_Text[3];
    [Tooltip("Optional clue text shown under each choice, e.g. 'Lose marble, gain element'.")]
    public TMP_Text[] choiceClueLabels = new TMP_Text[3];
    [Tooltip("Inventory panel to show when an event gives an element reward.")]
    public GameObject inventoryPanel;
    public GameObject inventoryPanelPrefab;
    public Transform inventoryPanelParent;
    public UIInventoryManager uiInventoryManager;

    private GameEventSO activeEvent;
    private int currentNodeIndex = 0;
    private MarbleElementSO pendingElementRewardElement;
    private System.Action pendingAfterElementReward;

    private void OnValidate()
    {
        EnsureChoiceClueLabelArray();
    }

    private void Start()
    {
        EnsureChoiceClueLabelArray();
        EnsureInventoryPanelReady();
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        activeEvent = ProgressionManager.Instance != null ? ProgressionManager.Instance.selectedEventForEventScene : null;

        if (activeEvent == null || activeEvent.conversationNodes.Count == 0)
        {
            Debug.LogWarning("EventScene opened without a valid selected event.");
            EndEvent();
            return;
        }

        ShowNode(0);
    }

    private void ShowNode(int nodeIndex)
    {
        if (activeEvent == null || nodeIndex < 0 || nodeIndex >= activeEvent.conversationNodes.Count)
        {
            EndEvent();
            return;
        }

        currentNodeIndex = nodeIndex;
        EventConversationNode node = activeEvent.conversationNodes[currentNodeIndex];

        if (speakerText != null) speakerText.text = node.speakerName;
        if (bodyText != null) bodyText.text = node.bodyText;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            EventChoiceData choice = i < node.choices.Length ? node.choices[i] : null;
            SetupChoiceButton(i, choice);
        }
    }

    private void SetupChoiceButton(int index, EventChoiceData choice)
    {
        if (index < 0 || index >= choiceButtons.Length || choiceButtons[index] == null) return;

        Button button = choiceButtons[index];
        bool hasChoice = choice != null && !string.IsNullOrEmpty(choice.choiceText);

        button.gameObject.SetActive(hasChoice);
        button.onClick.RemoveAllListeners();
        button.interactable = hasChoice;

        if (index < choiceLabels.Length && choiceLabels[index] != null)
        {
            choiceLabels[index].text = hasChoice ? choice.choiceText : "";
        }

        TMP_Text clueLabel = GetChoiceClueLabel(index, button, hasChoice && !string.IsNullOrEmpty(choice.clueText));
        if (clueLabel != null)
        {
            clueLabel.gameObject.SetActive(hasChoice && !string.IsNullOrEmpty(choice.clueText));
            clueLabel.text = hasChoice ? choice.clueText : "";
        }

        if (hasChoice)
        {
            EventChoiceData capturedChoice = choice;
            button.onClick.AddListener(() => ResolveChoice(capturedChoice));
        }
    }

    private void ResolveChoice(EventChoiceData choice)
    {
        switch (choice.action)
        {
            case EventChoiceAction.GoToNextNode:
                ShowNode(choice.nextNodeIndex);
                break;

            case EventChoiceAction.ApplyRewardAndEnd:
                if (TryApplyReward(choice.reward, true))
                {
                    if (!TryOpenElementRewardInfusion(choice.reward, EndEvent))
                    {
                        EndEvent();
                    }
                }
                else
                {
                    ShowMessageAndEnd("Kamu tidak bisa memberikan kelereng terakhirmu.");
                }
                break;

            case EventChoiceAction.StartFightWithReward:
                StartFight(choice.reward, choice.fightEnemyPool);
                break;

            case EventChoiceAction.ResolveRandomOutcome:
                ResolveRandomOutcome(choice);
                break;

            case EventChoiceAction.EndEvent:
            default:
                EndEvent();
                break;
        }
    }

    private void ResolveRandomOutcome(EventChoiceData choice)
    {
        EventRandomOutcomeData outcome = PickRandomOutcome(choice.randomOutcomes);
        if (outcome == null)
        {
            EndEvent();
            return;
        }

        switch (outcome.action)
        {
            case EventChoiceAction.StartFightWithReward:
                StartFight(outcome.reward, outcome.fightEnemyPool);
                break;

            case EventChoiceAction.GoToNextNode:
                ShowNode(choice.nextNodeIndex);
                break;

            case EventChoiceAction.ApplyRewardAndEnd:
                if (TryApplyReward(outcome.reward, true))
                {
                    if (!TryOpenElementRewardInfusion(outcome.reward, () => ShowOutcomeMessageOrEnd(outcome.outcomeText)))
                    {
                        ShowOutcomeMessageOrEnd(outcome.outcomeText);
                    }
                }
                else
                {
                    ShowMessageAndEnd("Kamu tidak bisa memberikan kelereng terakhirmu.");
                }
                break;

            case EventChoiceAction.EndEvent:
            default:
                ShowOutcomeMessageOrEnd(outcome.outcomeText);
                break;
        }
    }

    private EventRandomOutcomeData PickRandomOutcome(System.Collections.Generic.List<EventRandomOutcomeData> outcomes)
    {
        if (outcomes == null || outcomes.Count == 0) return null;

        int totalWeight = 0;
        foreach (EventRandomOutcomeData outcome in outcomes)
        {
            if (outcome != null)
            {
                totalWeight += Mathf.Max(outcome.weight, 1);
            }
        }

        if (totalWeight <= 0) return null;

        int roll = Random.Range(0, totalWeight);
        foreach (EventRandomOutcomeData outcome in outcomes)
        {
            if (outcome == null) continue;

            roll -= Mathf.Max(outcome.weight, 1);
            if (roll < 0)
            {
                return outcome;
            }
        }

        return null;
    }

    private void StartFight(EventRewardData reward, System.Collections.Generic.List<EnemySO> fightEnemyPool)
    {
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.SetPendingEventFightReward(reward);
            ProgressionManager.Instance.SetPendingFightEnemy(PickRandomEnemy(fightEnemyPool));
            ProgressionManager.Instance.selectedEventForEventScene = null;
        }

        SceneManager.LoadScene("FightScene");
    }

    private EnemySO PickRandomEnemy(System.Collections.Generic.List<EnemySO> fightEnemyPool)
    {
        if (fightEnemyPool == null || fightEnemyPool.Count == 0) return null;

        System.Collections.Generic.List<EnemySO> validEnemies = new System.Collections.Generic.List<EnemySO>();
        foreach (EnemySO enemy in fightEnemyPool)
        {
            if (enemy != null)
            {
                validEnemies.Add(enemy);
            }
        }

        if (validEnemies.Count == 0) return null;
        return validEnemies[Random.Range(0, validEnemies.Count)];
    }

    private bool TryApplyReward(EventRewardData reward, bool deferElementRewards)
    {
        if (ProgressionManager.Instance == null) return false;

        return ProgressionManager.Instance.ApplyEventReward(reward, !deferElementRewards);
    }

    private bool TryOpenElementRewardInfusion(EventRewardData reward, System.Action fallbackAfterReward)
    {
        if (ProgressionManager.Instance == null || reward == null) return false;

        MarbleElementSO element = ProgressionManager.Instance.PickRandomEventElementReward(reward);
        if (element == null) return false;

        EnsureInventoryPanelReady();

        if (uiInventoryManager == null)
        {
            Debug.LogWarning("Event has an element reward, but EventManager.uiInventoryManager is not assigned. Falling back to automatic element placement.");
            ProgressionManager.Instance.GrantElementToChamber(element);
            fallbackAfterReward?.Invoke();
            return true;
        }

        pendingElementRewardElement = element;
        pendingAfterElementReward = fallbackAfterReward;
        ProgressionManager.Instance.pendingElementFromShop = element;

        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        uiInventoryManager.BuildDynamicInventoryUI();
        uiInventoryManager.EnterEventInfusionMode(this, element.elementName);
        SetChoiceButtonsInteractable(false);
        return true;
    }

    private void EnsureInventoryPanelReady()
    {
        if (uiInventoryManager != null)
        {
            if (inventoryPanel == null)
            {
                inventoryPanel = uiInventoryManager.gameObject;
            }

            return;
        }

        if (inventoryPanel == null && inventoryPanelPrefab != null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            Transform parent = inventoryPanelParent != null
                ? inventoryPanelParent
                : (canvas != null ? canvas.transform : transform.root);
            inventoryPanel = Instantiate(inventoryPanelPrefab, parent);
        }

        if (inventoryPanel != null)
        {
            uiInventoryManager = inventoryPanel.GetComponent<UIInventoryManager>();
            if (uiInventoryManager == null)
            {
                uiInventoryManager = inventoryPanel.GetComponentInChildren<UIInventoryManager>(true);
            }

            if (uiInventoryManager != null && inventoryPanel == null)
            {
                inventoryPanel = uiInventoryManager.gameObject;
            }
        }
    }

    public void CompleteElementRewardInfusion(int selectedSlotIndex)
    {
        if (ProgressionManager.Instance == null || pendingElementRewardElement == null)
        {
            FinishElementRewardInfusion();
            EndEvent();
            return;
        }

        if (!ProgressionManager.Instance.LoadElementToSlot(selectedSlotIndex, pendingElementRewardElement))
        {
            if (uiInventoryManager != null)
            {
                uiInventoryManager.ShowFeedback("This marble is already combined. Pick another marble.");
            }
            return;
        }

        System.Action afterElementReward = pendingAfterElementReward;
        FinishElementRewardInfusion();
        afterElementReward?.Invoke();
    }

    private void FinishElementRewardInfusion()
    {
        pendingElementRewardElement = null;
        pendingAfterElementReward = null;
        if (ProgressionManager.Instance != null) ProgressionManager.Instance.pendingElementFromShop = null;
        if (uiInventoryManager != null) uiInventoryManager.ExitInfusionMode();
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
    }

    private void SetChoiceButtonsInteractable(bool interactable)
    {
        foreach (Button button in choiceButtons)
        {
            if (button != null) button.interactable = interactable;
        }
    }

    private TMP_Text GetChoiceClueLabel(int index, Button button, bool createIfMissing)
    {
        EnsureChoiceClueLabelArray();

        if (index >= 0 && index < choiceClueLabels.Length && choiceClueLabels[index] != null)
        {
            return choiceClueLabels[index];
        }

        Transform existing = button.transform.Find("ChoiceClueText");
        if (existing != null && existing.TryGetComponent(out TMP_Text existingText))
        {
            AssignChoiceClueLabel(index, existingText);
            ConfigureChoiceClueLabel(existingText);
            return existingText;
        }

        if (!createIfMissing) return null;

        GameObject clueObject = new GameObject("ChoiceClueText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        clueObject.transform.SetParent(button.transform, false);
        TMP_Text clueText = clueObject.GetComponent<TMP_Text>();
        clueText.raycastTarget = false;
        clueText.fontSize = 12f;
        clueText.color = new Color(0.22f, 0.22f, 0.22f, 0.85f);
        clueText.alignment = TextAlignmentOptions.Center;
        clueText.textWrappingMode = TextWrappingModes.Normal;
        ConfigureChoiceClueLabel(clueText);
        AssignChoiceClueLabel(index, clueText);
        return clueText;
    }

    private void AssignChoiceClueLabel(int index, TMP_Text clueText)
    {
        if (index < 0 || index >= choiceClueLabels.Length) return;
        choiceClueLabels[index] = clueText;
    }

    private void ConfigureChoiceClueLabel(TMP_Text clueText)
    {
        if (clueText == null) return;

        RectTransform rectTransform = clueText.rectTransform;
        rectTransform.anchorMin = new Vector2(0f, -0.85f);
        rectTransform.anchorMax = new Vector2(1f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = new Vector2(0f, -2f);
        rectTransform.sizeDelta = new Vector2(0f, 24f);
    }

    private void EnsureChoiceClueLabelArray()
    {
        if (choiceClueLabels == null || choiceClueLabels.Length != choiceButtons.Length)
        {
            TMP_Text[] oldLabels = choiceClueLabels;
            choiceClueLabels = new TMP_Text[choiceButtons.Length];

            for (int i = 0; i < choiceClueLabels.Length; i++)
            {
                choiceClueLabels[i] = oldLabels != null && i < oldLabels.Length ? oldLabels[i] : null;
            }
        }
    }

    private void ShowMessageAndEnd(string message)
    {
        if (speakerText != null) speakerText.text = "";
        if (bodyText != null) bodyText.text = message;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] == null) continue;

            bool isContinueButton = i == 0;
            choiceButtons[i].gameObject.SetActive(isContinueButton);
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].interactable = isContinueButton;

            if (i < choiceLabels.Length && choiceLabels[i] != null)
            {
                choiceLabels[i].text = isContinueButton ? "Lanjut" : "";
            }

            if (i < choiceClueLabels.Length && choiceClueLabels[i] != null)
            {
                choiceClueLabels[i].gameObject.SetActive(false);
                choiceClueLabels[i].text = "";
            }

            if (isContinueButton)
            {
                choiceButtons[i].onClick.AddListener(EndEvent);
            }
        }
    }

    private void ShowOutcomeMessageOrEnd(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            EndEvent();
            return;
        }

        ShowMessageAndEnd(message);
    }

    private void EndEvent()
    {
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.selectedEventForEventScene = null;
            ProgressionManager.Instance.currentFloor++;
        }

        SceneManager.LoadScene("MapScene");
    }
}
