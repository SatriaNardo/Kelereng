using UnityEngine;

public class MarbleElementHandler : MonoBehaviour
{
    [Header("Active Power")]
    public MarbleElementSO activeElement;

    [Header("Common Visuals")]
    public Sprite commonIdleSprite;
    public Sprite[] commonRollingSprites;

    [Header("Shadow Visuals")]
    public Sprite shadowSprite;

    [Header("Clash Rules")]
    public float minimumClashSpeed = 0.02f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private MarbleRollAnimator rollAnimator;
    private PixelMarbleShadow pixelShadow;
    private Sprite defaultSprite;
    private Vector2 previousVelocity;
    
    // NEW: Tracks if this marble has already used its elemental burst this shot
    private bool hasClashedThisTurn = false; 

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rollAnimator = GetComponent<MarbleRollAnimator>();
        pixelShadow = GetComponent<PixelMarbleShadow>();
        defaultSprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        previousVelocity = rb != null ? rb.linearVelocity : Vector2.zero;
    }

    private void Start()
    {
        ApplyElementVisuals();
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            previousVelocity = rb.linearVelocity;
        }
    }

    public void SetActiveElement(MarbleElementSO element)
    {
        activeElement = element;
        ApplyElementVisuals();
    }

    private void ApplyElementVisuals()
    {
        if (spriteRenderer == null) return;

        spriteRenderer.color = activeElement != null ? activeElement.elementColor : Color.white;

        Sprite idleSprite = commonIdleSprite != null ? commonIdleSprite : defaultSprite;
        Sprite[] rollingSprites = commonRollingSprites;
        if (activeElement != null)
        {
            if (activeElement.idleSprite != null)
            {
                idleSprite = activeElement.idleSprite;
            }

            if (activeElement.rollingSprites != null && activeElement.rollingSprites.Length > 0)
            {
                rollingSprites = activeElement.rollingSprites;
            }
        }

        if (idleSprite != null)
        {
            spriteRenderer.sprite = idleSprite;
        }

        if (rollAnimator == null)
        {
            rollAnimator = GetComponent<MarbleRollAnimator>();
        }

        if (rollAnimator == null)
        {
            rollAnimator = gameObject.AddComponent<MarbleRollAnimator>();
        }

        rollAnimator.Configure(idleSprite, rollingSprites);

        if (pixelShadow == null)
        {
            pixelShadow = GetComponent<PixelMarbleShadow>();
        }

        if (pixelShadow == null && shadowSprite != null)
        {
            pixelShadow = gameObject.AddComponent<PixelMarbleShadow>();
        }

        if (pixelShadow != null)
        {
            pixelShadow.ConfigureSprite(shadowSprite);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (rb == null || hasClashedThisTurn) return;
        if (activeElement == null) return;

        Rigidbody2D targetRb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (targetRb == null) return;
        if (!IsValidClashTarget(collision.gameObject)) return;
        if (!HasEnoughClashSpeed(collision)) return;

        Vector2 contactPoint = collision.GetContact(0).point;
        Vector2 impactDirection = GetImpactDirection(collision, targetRb);

        if (CorruptedSmokeZone.IsPointInsideSmoke(contactPoint))
        {
            hasClashedThisTurn = true;
            CorruptedSmokeZone.ApplyRandomClash(rb, targetRb);
            return;
        }

        hasClashedThisTurn = true;
        activeElement.OnClash(rb, targetRb, contactPoint, impactDirection);
        ChainAnchorPoint.TryTriggerChainAt(contactPoint, rb, targetRb);
    }

    private bool HasEnoughClashSpeed(Collision2D collision)
    {
        float speedThreshold = minimumClashSpeed * minimumClashSpeed;
        return rb.linearVelocity.sqrMagnitude >= speedThreshold
            || previousVelocity.sqrMagnitude >= speedThreshold
            || collision.relativeVelocity.sqrMagnitude >= speedThreshold;
    }

    private bool IsValidClashTarget(GameObject other)
    {
        return other.CompareTag("TargetMarble")
            || other.CompareTag("PlayerMarble")
            || other.CompareTag("Gacoan")
            || other.GetComponent<TargetMarble>() != null
            || other.GetComponent<MarbleElementHandler>() != null;
    }

    private Vector2 GetImpactDirection(Collision2D collision, Rigidbody2D targetRb)
    {
        Vector2 direction = previousVelocity;
        if (direction.sqrMagnitude > 0.001f) return direction.normalized;

        if (collision.relativeVelocity.sqrMagnitude > 0.001f)
        {
            return collision.relativeVelocity.normalized;
        }

        direction = targetRb.position - rb.position;
        if (direction.sqrMagnitude > 0.001f) return direction.normalized;

        return Vector2.right;
    }
}
