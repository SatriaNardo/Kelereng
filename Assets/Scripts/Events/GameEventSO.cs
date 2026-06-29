using System.Collections.Generic;
using UnityEngine;

public enum EventChoiceAction
{
    GoToNextNode,
    EndEvent,
    ApplyRewardAndEnd,
    StartFightWithReward,
    ResolveRandomOutcome
}

[System.Serializable]
public class EventRewardData
{
    public int currencyMin = 0;
    public int currencyMax = 0;
    public int ammoMin = 0;
    public int ammoMax = 0;
    public bool removeAmmo = false;
    [Range(0f, 1f)] public float removeAmmoChance = 0f;
    public bool removeRandomEmblem = false;
    [Range(0f, 1f)] public float removeRandomEmblemChance = 0f;
    public List<MarbleElementSO> randomElementRewards = new List<MarbleElementSO>();
    public List<EmblemSO> randomEmblemRewards = new List<EmblemSO>();
}

[System.Serializable]
public class EventRandomOutcomeData
{
    public string outcomeText;
    [Min(1)] public int weight = 1;
    public EventChoiceAction action = EventChoiceAction.ApplyRewardAndEnd;
    public EventRewardData reward = new EventRewardData();
    public List<EnemySO> fightEnemyPool = new List<EnemySO>();
}

[System.Serializable]
public class EventChoiceData
{
    public string choiceText;
    public EventChoiceAction action = EventChoiceAction.EndEvent;
    public int nextNodeIndex = -1;
    public EventRewardData reward = new EventRewardData();
    public List<EnemySO> fightEnemyPool = new List<EnemySO>();
    public List<EventRandomOutcomeData> randomOutcomes = new List<EventRandomOutcomeData>();
}

[System.Serializable]
public class EventConversationNode
{
    public string speakerName;
    [TextArea(3, 8)] public string bodyText;
    public EventChoiceData[] choices = new EventChoiceData[3];
}

[CreateAssetMenu(fileName = "NewGameEvent", menuName = "Events/Game Event")]
public class GameEventSO : ScriptableObject
{
    [Header("Event Info")]
    public string eventName;

    [Header("Conversation")]
    public List<EventConversationNode> conversationNodes = new List<EventConversationNode>();

    private void OnValidate()
    {
        foreach (EventConversationNode node in conversationNodes)
        {
            if (node.choices == null || node.choices.Length != 3)
            {
                EventChoiceData[] oldChoices = node.choices;
                node.choices = new EventChoiceData[3];

                for (int i = 0; i < node.choices.Length; i++)
                {
                    node.choices[i] = oldChoices != null && i < oldChoices.Length ? oldChoices[i] : new EventChoiceData();
                }
            }

            for (int i = 0; i < node.choices.Length; i++)
            {
                if (node.choices[i] == null)
                {
                    node.choices[i] = new EventChoiceData();
                }

                if (node.choices[i].reward == null)
                {
                    node.choices[i].reward = new EventRewardData();
                }

                if (node.choices[i].fightEnemyPool == null)
                {
                    node.choices[i].fightEnemyPool = new List<EnemySO>();
                }

                if (node.choices[i].randomOutcomes == null)
                {
                    node.choices[i].randomOutcomes = new List<EventRandomOutcomeData>();
                }

                foreach (EventRandomOutcomeData outcome in node.choices[i].randomOutcomes)
                {
                    if (outcome == null) continue;
                    if (outcome.reward == null) outcome.reward = new EventRewardData();
                    if (outcome.fightEnemyPool == null) outcome.fightEnemyPool = new List<EnemySO>();
                    if (outcome.weight < 1) outcome.weight = 1;
                }
            }
        }
    }
}
