using UnityEngine;

public enum EmblemEffectType
{
    None,
    BonusForce,
    BonusAmmo,
    SlimeImmunity,
    RicochetPreview,
    ExtendedGuideLine
}

[CreateAssetMenu(fileName = "NewIndoEmblem", menuName = "Emblems/Indonesian Emblem")]
public class EmblemSO : ScriptableObject
{
    public string emblemName;
    [TextArea] public string deskripsiLokal;
    public Sprite iconEmblem;
    public EmblemEffectType effectType = EmblemEffectType.None;

    [Header("Passive Bonuses")]
    public float bonusForceMultiplier = 1f;
    public int bonusAmmoSlot = 0;
    public bool kebalLendirSlime = false;

    [Header("Ricochet Preview")]
    [Tooltip("How many wall bounces the aim line shows ahead.")]
    public int ricochetPreviewCount = 1;

    [Header("Extended Guide Line")]
    [Tooltip("Extra aim-line length added to MarbleLauncher maxDragDistance.")]
    public float guideLineRangeBonus = 1f;
}
