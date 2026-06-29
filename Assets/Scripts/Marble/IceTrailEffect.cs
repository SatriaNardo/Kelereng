using UnityEngine;

public class IceTrailEffect : MonoBehaviour
{
    public float spotSpacing = 0.25f;
    public float spotSize = 0.45f;
    public float stopSpeed = 0.08f;
    public Color iceColor = new Color(0.55f, 0.9f, 1f, 0.45f);
    public Sprite[] spotSprites;
    [Range(0f, 1f)] public float spotLinearDampingMultiplier = 0.08f;
    [Range(0f, 1f)] public float spotAngularDampingMultiplier = 0.15f;
    [Range(0f, 1f)] public float spotFriction = 0f;

    private static Sprite iceSpotSprite;
    private Rigidbody2D rb;
    private Vector2 lastSpotPosition;
    private bool hasLastSpot = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (iceSpotSprite == null)
        {
            iceSpotSprite = CreateCircleSprite();
        }
    }

    private void Update()
    {
        if (rb == null)
        {
            Destroy(this);
            return;
        }

        if (rb.linearVelocity.magnitude <= stopSpeed)
        {
            Destroy(this);
            return;
        }

        Vector2 currentPosition = transform.position;
        if (!hasLastSpot || Vector2.Distance(currentPosition, lastSpotPosition) >= spotSpacing)
        {
            SpawnSpot(currentPosition);
            lastSpotPosition = currentPosition;
            hasLastSpot = true;
        }
    }

    private void SpawnSpot(Vector2 position)
    {
        GameObject spot = new GameObject("IceTrailSpot");
        spot.transform.position = position;
        spot.transform.localScale = Vector3.one * spotSize;

        SpriteRenderer renderer = spot.AddComponent<SpriteRenderer>();
        renderer.sprite = GetRandomSpotSprite();
        renderer.color = iceColor;
        renderer.sortingOrder = -2;

        IceTrailSpot iceSpot = spot.AddComponent<IceTrailSpot>();
        iceSpot.Configure(0.5f, spotLinearDampingMultiplier, spotAngularDampingMultiplier, spotFriction);
    }

    private Sprite GetRandomSpotSprite()
    {
        if (spotSprites != null && spotSprites.Length > 0)
        {
            int startIndex = Random.Range(0, spotSprites.Length);
            for (int i = 0; i < spotSprites.Length; i++)
            {
                Sprite sprite = spotSprites[(startIndex + i) % spotSprites.Length];
                if (sprite != null) return sprite;
            }
        }

        return iceSpotSprite;
    }

    private static Sprite CreateCircleSprite()
    {
        const int textureSize = 64;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        float maxDistance = textureSize * 0.5f;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;
                float alpha = Mathf.SmoothStep(1f, 0f, distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
    }
}
