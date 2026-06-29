using UnityEngine;

public abstract class MarbleElementSO : ScriptableObject
{
    [Header("Identity")]
    public string elementName;
    public Color elementColor = Color.white;

    [Header("Visuals")]
    public Sprite idleSprite;
    public Sprite[] rollingSprites;

    [Header("Balance Cost")]
    public int energyCost = 1; // BIYAYA ENERGY: Set 1 untuk Wind/Fire, set 2 atau 3 untuk Cyclone/Explosion

    public virtual float GetLaunchForceMultiplier(float baseMultiplier)
    {
        return baseMultiplier;
    }

    public virtual void OnLaunch(Rigidbody2D marble)
    {
    }

    public abstract void OnClash(Rigidbody2D attacker, Rigidbody2D victim, Vector2 collisionPoint, Vector2 impactDirection);
}
