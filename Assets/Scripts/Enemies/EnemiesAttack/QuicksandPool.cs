using UnityEngine;

public class QuicksandPool : MonoBehaviour
{
    [Header("Quicksand Feel")]
    public float radius = 0.45f;
    public float quicksandLinearDrag = 10f;
    public Color sandColor = new Color(0.72f, 0.58f, 0.28f, 0.85f);
    public int sortingOrder = -1;
    public Sprite visualSprite;
    public bool usePrefabVisual;

    private CircleCollider2D triggerCollider;
    private SpriteRenderer spriteRenderer;

    public void Initialize(Vector2 center, float poolRadius)
    {
        Initialize(center, poolRadius, null);
    }

    public void Initialize(Vector2 center, float poolRadius, Sprite sprite)
    {
        radius = poolRadius;
        if (sprite != null)
        {
            visualSprite = sprite;
        }
        transform.position = center;
        EnsureComponents();
        RefreshVisual();
    }

    private void Awake()
    {
        EnsureComponents();
        RefreshVisual();
    }

    private void EnsureComponents()
    {
        triggerCollider = GetComponent<CircleCollider2D>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<CircleCollider2D>();
        }

        triggerCollider.isTrigger = true;
        triggerCollider.radius = radius;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    public void RefreshVisual()
    {
        EnsureComponents();
        triggerCollider.radius = radius;

        if (usePrefabVisual)
        {
            transform.localScale = Vector3.one * (radius * 2f);
            return;
        }

        spriteRenderer.sprite = visualSprite != null ? visualSprite : CreateCircleSprite();
        spriteRenderer.color = sandColor;
        spriteRenderer.sortingOrder = sortingOrder;
        transform.localScale = Vector3.one * (radius * 2f);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
        if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic) return;
        if (SteamLaunchBuff.IsActive(collision.gameObject)) return;

        rb.linearVelocity *= 1f - (quicksandLinearDrag * Time.deltaTime);

        if (rb.linearVelocity.magnitude < 0.05f)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private static Sprite CreateCircleSprite()
    {
        const int textureSize = 64;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        float maxDistance = textureSize * 0.5f;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;
                float alpha = distance <= 1f ? 1f : 0f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
    }
}
