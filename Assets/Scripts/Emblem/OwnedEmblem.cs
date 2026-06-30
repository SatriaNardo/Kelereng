using System;

[Serializable]
public class OwnedEmblem
{
    public BaseEmblemSO emblem;

    public int quantity = 1;

    public bool IsAvailable()
    {
        if (emblem == null)
            return false;

        // Passive dan Cooldown tidak memiliki batas penggunaan
        if (emblem.usageType == EmblemUsageType.Passive ||
            emblem.usageType == EmblemUsageType.Cooldown)
            return true;

        // Consumable harus punya stok
        return quantity > 0;
    }

    public void Consume()
    {
        if (emblem == null)
            return;

        if (emblem.usageType == EmblemUsageType.Consumable)
        {
            quantity--;
        }
    }
}