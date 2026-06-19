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
        // 1. SAFETY CHECKS: Exit immediately if no element OR if it already clashed once
        if (activeElement == null || rb == null || hasClashedThisTurn) return;

        // Only activate the power if THIS marble is the active attacker moving at speed
        if (rb.linearVelocity.magnitude < 0.1f) return;

        // Check if we collided with another physics marble
        Rigidbody2D targetRb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (targetRb != null)
        {
            // 2. LOCK THE FLAG: Prevent any future collisions from entering this block
            hasClashedThisTurn = true;

            // Extract the precise 2D coordinate where the two circles touched
            Vector2 contactPoint = collision.GetContact(0).point;

            // Execute the ScriptableObject power behavior (Wind AoE or Fire Knockback)
            activeElement.OnClash(rb, targetRb, contactPoint);
        }
    }
}