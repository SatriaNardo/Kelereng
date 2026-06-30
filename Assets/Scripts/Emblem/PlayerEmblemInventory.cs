using System.Collections.Generic;
using UnityEngine;

public class PlayerEmblemInventory : MonoBehaviour
{
    public List<OwnedEmblem> ownedEmblems = new();

    public void UseEmblem(BaseEmblemSO emblem, GameObject marble)
    {
        OwnedEmblem owned = ownedEmblems.Find(e => e.emblem == emblem);

        if (owned == null)
        {
            Debug.Log("Emblem tidak dimiliki.");
            return;
        }

        if (!owned.IsAvailable())
        {
            Debug.Log("Emblem habis.");
            return;
        }

        emblem.Activate(marble);
        owned.Consume();

        Debug.Log($"{emblem.emblemName} digunakan.");
    }
}