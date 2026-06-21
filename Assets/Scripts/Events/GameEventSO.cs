using System.Collections.Generic;
using UnityEngine;

public enum EventChoiceAction
{
    GoToNextNode,
    EndEvent,
    ApplyRewardAndEnd,
    StartFightWithReward
}

[System.Serializable]
public class EventRewardData
{
    public int currencyMin = 0;
    public int currencyMax = 0;
    public bool removeAmmo = false;
    public List<MarbleElementSO> randomElementRewards = new List<MarbleElementSO>();
}

[System.Serializable]
public class EventChoiceData
{
    public string choiceText;
    public EventChoiceAction action = EventChoiceAction.EndEvent;
    public int nextNodeIndex = -1;
    public EventRewardData reward = new EventRewardData();
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
            }
        }
    }
}
