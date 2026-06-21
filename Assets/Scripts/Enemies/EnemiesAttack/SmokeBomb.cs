using UnityEngine;

public class SmokeBomb : MonoBehaviour
{
    [Header("Smoke Visual")]
    public float radius = 1.6f;
    [Range(0f, 1f)] public float opacity = 1f;
    public Color smokeColor = new Color(0.08f, 0.08f, 0.08f, 1f);
    public int sortingOrder = 100;

    private const int TextureSize = 128;
    private static Sprite smokeSprite;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (smokeSprite == null)
        {
            smokeSprite = CreateSmokeSprite();
        }

        spriteRenderer.sprite = smokeSprite;
        spriteRenderer.color = new Color(smokeColor.r, smokeColor.g, smokeColor.b, opacity);
        spriteRenderer.sortingOrder = sortingOrder;

        transform.localScale = Vector3.one * (radius * 2f);
    }

    private static Sprite CreateSmokeSprite()
    {
        Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((TextureSize - 1) * 0.5f, (TextureSize - 1) * 0.5f);
        float maxDistance = TextureSize * 0.5f;

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;
                float edgeFade = Mathf.Clamp01(1f - distance);
                float alpha = Mathf.SmoothStep(0f, 1f, edgeFade);

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, TextureSize, TextureSize),
            new Vector2(0.5f, 0.5f),
            TextureSize
        );
    }
}
