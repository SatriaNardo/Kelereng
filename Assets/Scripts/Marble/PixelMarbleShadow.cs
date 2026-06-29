using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PixelMarbleShadow : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite shadowSprite;

    [Header("Shape")]
    [Range(8, 64)] public int textureWidth = 24;
    [Range(4, 32)] public int textureHeight = 10;
    public Vector2 localOffset = Vector2.zero;
    public Vector2 localScale = Vector2.one;

    [Header("Look")]
    [Range(0f, 1f)] public float opacity = 0.42f;
    [Range(0f, 0.5f)] public float edgeChunk = 0.18f;
    public Color shadowColor = new Color(0f, 0f, 0f, 1f);

    private const string ShadowName = "PixelShadow";
    private static Sprite cachedShadowSprite;

    private SpriteRenderer sourceRenderer;
    private SpriteRenderer shadowRenderer;

    private void Awake()
    {
        sourceRenderer = GetComponent<SpriteRenderer>();
        EnsureShadowRenderer();
        SyncShadowSort();
    }

    private void LateUpdate()
    {
        if (shadowRenderer == null || sourceRenderer == null) return;

        shadowRenderer.enabled = sourceRenderer.enabled;
        shadowRenderer.flipX = sourceRenderer.flipX;
        shadowRenderer.transform.localRotation = Quaternion.Inverse(transform.localRotation);
        SyncShadowSort();
    }

    private void EnsureShadowRenderer()
    {
        Transform shadowTransform = transform.Find(ShadowName);
        if (shadowTransform == null)
        {
            GameObject shadowObject = new GameObject(ShadowName);
            shadowTransform = shadowObject.transform;
            shadowTransform.SetParent(transform, false);
        }

        shadowTransform.localPosition = new Vector3(localOffset.x, localOffset.y, 0.02f);
        shadowTransform.localRotation = Quaternion.Inverse(transform.localRotation);
        shadowTransform.localScale = new Vector3(localScale.x, localScale.y, 1f);

        shadowRenderer = shadowTransform.GetComponent<SpriteRenderer>();
        if (shadowRenderer == null)
        {
            shadowRenderer = shadowTransform.gameObject.AddComponent<SpriteRenderer>();
        }

        shadowRenderer.color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, opacity);
        shadowRenderer.maskInteraction = SpriteMaskInteraction.None;
        ApplySprite(shadowSprite);
    }

    public void ConfigureSprite(Sprite newShadowSprite)
    {
        shadowSprite = newShadowSprite;

        if (shadowRenderer == null)
        {
            EnsureShadowRenderer();
        }
        else
        {
            ApplySprite(shadowSprite);
        }
    }

    private void ApplySprite(Sprite newShadowSprite)
    {
        if (shadowRenderer == null) return;

        shadowRenderer.sprite = newShadowSprite != null ? newShadowSprite : GetOrCreateShadowSprite();
    }

    private void SyncShadowSort()
    {
        shadowRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        shadowRenderer.sortingOrder = sourceRenderer.sortingOrder - 1;
    }

    private Sprite GetOrCreateShadowSprite()
    {
        if (cachedShadowSprite != null) return cachedShadowSprite;

        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.name = "Generated Pixel Marble Shadow";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color filled = Color.white;
        float centerX = (textureWidth - 1) * 0.5f;
        float centerY = (textureHeight - 1) * 0.5f;
        float radiusX = textureWidth * 0.5f;
        float radiusY = textureHeight * 0.5f;

        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                float normalizedX = (x - centerX) / radiusX;
                float normalizedY = (y - centerY) / radiusY;
                float distance = (normalizedX * normalizedX) + (normalizedY * normalizedY);
                float alpha = distance <= 1f - edgeChunk ? 1f : 0f;
                texture.SetPixel(x, y, alpha > 0f ? filled : clear);
            }
        }

        texture.Apply(false, true);
        cachedShadowSprite = Sprite.Create(texture, new Rect(0f, 0f, textureWidth, textureHeight), new Vector2(0.5f, 0.5f), textureWidth);
        cachedShadowSprite.name = "Pixel Marble Shadow";
        return cachedShadowSprite;
    }
}
