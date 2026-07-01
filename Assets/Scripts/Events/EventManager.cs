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

    private GameEventSO activeEvent;
    private int currentNodeIndex = 0;

    private void Start()
    {
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
                if (TryApplyReward(choice.reward))
                {
                    EndEvent();
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
                if (TryApplyReward(outcome.reward))
                {
                    ShowOutcomeMessageOrEnd(outcome.outcomeText);
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

    private bool TryApplyReward(EventRewardData reward)
    {
        if (ProgressionManager.Instance == null) return false;

        return ProgressionManager.Instance.ApplyEventReward(reward);
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
