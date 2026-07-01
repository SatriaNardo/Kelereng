using UnityEngine;

public enum EmblemUsageType
{
    Consumable,
    Cooldown,
    Passive
}

public abstract class BaseEmblemSO : ScriptableObject
{
    [Header("Info")]
    public string emblemName;

    public Sprite icon;

    [TextArea]
    public string description;

    [Header("Usage")]
    public EmblemUsageType usageType;

    public float cooldown = 0f;

    public virtual void Activate(GameObject marble)
    {

    }
    public virtual bool IsInstantSkill()
    {
        return false;
    }

    public virtual bool IsPassiveEmblem()
    {
        return usageType == EmblemUsageType.Passive;
    }
}
