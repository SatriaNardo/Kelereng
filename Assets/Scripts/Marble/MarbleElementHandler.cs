using UnityEngine;

public class MarbleElementHandler : MonoBehaviour
{
    [Header("Active Power")]
    public MarbleElementSO activeElement;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    
    // NEW: Tracks if this marble has already used its elemental burst this shot
    private bool hasClashedThisTurn = false; 

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // Visually tint the marble to match its element color if assigned
        if (activeElement != null && spriteRenderer != null)
        {
            spriteRenderer.color = activeElement.elementColor;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (rb == null || hasClashedThisTurn) return;
        if (rb.linearVelocity.magnitude < 0.1f) return;

        Rigidbody2D targetRb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (targetRb == null) return;

        Vector2 contactPoint = collision.GetContact(0).point;

        if (CorruptedSmokeZone.IsPointInsideSmoke(contactPoint))
        {
            hasClashedThisTurn = true;
            CorruptedSmokeZone.ApplyRandomClash(rb, targetRb);
            return;
        }

        if (activeElement == null) return;

        hasClashedThisTurn = true;
        activeElement.OnClash(rb, targetRb, contactPoint);
        ChainAnchorPoint.TryTriggerChainAt(contactPoint, rb, targetRb);
    }
}