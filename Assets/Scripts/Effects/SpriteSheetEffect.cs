using UnityEngine;

public class SpriteSheetEffect : MonoBehaviour
{
    public Sprite[] frames;
    public float framesPerSecond = 18f;
    public bool destroyWhenDone = true;
    public Color color = Color.white;
    public int sortingOrder = 20;
    public Vector3 worldScale = Vector3.one;
    public float rotationDegrees;

    private SpriteRenderer spriteRenderer;
    private float frameCursor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        transform.localScale = worldScale;
        transform.rotation = Quaternion.Euler(0f, 0f, rotationDegrees);
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;

        if (frames == null || frames.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        spriteRenderer.sprite = frames[0];
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;

        frameCursor += Time.deltaTime * Mathf.Max(1f, framesPerSecond);
        int frameIndex = Mathf.FloorToInt(frameCursor);

        if (frameIndex >= frames.Length)
        {
            if (destroyWhenDone)
            {
                Destroy(gameObject);
                return;
            }

            frameIndex = frames.Length - 1;
        }

        Sprite nextFrame = frames[frameIndex];
        if (nextFrame != null && spriteRenderer.sprite != nextFrame)
        {
            spriteRenderer.sprite = nextFrame;
        }
    }

    public static void Spawn(Vector2 position, Sprite[] frames, Color color, float framesPerSecond, float scale, int sortingOrder = 20)
    {
        Spawn(position, frames, color, framesPerSecond, scale, 0f, sortingOrder);
    }

    public static void Spawn(Vector2 position, Sprite[] frames, Color color, float framesPerSecond, float scale, float rotationDegrees, int sortingOrder = 20)
    {
        if (frames == null || frames.Length == 0) return;

        GameObject effectObject = new GameObject("SpriteSheetEffect");
        effectObject.transform.position = position;

        SpriteSheetEffect effect = effectObject.AddComponent<SpriteSheetEffect>();
        effect.frames = frames;
        effect.color = color;
        effect.framesPerSecond = framesPerSecond;
        effect.worldScale = Vector3.one * scale;
        effect.rotationDegrees = rotationDegrees;
        effect.sortingOrder = sortingOrder;
    }

    public static void SpawnEffect(Vector2 position, GameObject effectPrefab, Sprite[] fallbackFrames, Color color, float framesPerSecond, float scale, float prefabLifetime = 1f, int sortingOrder = 20)
    {
        SpawnEffect(position, effectPrefab, fallbackFrames, color, framesPerSecond, scale, 0f, prefabLifetime, sortingOrder);
    }

    public static void SpawnEffect(Vector2 position, GameObject effectPrefab, Sprite[] fallbackFrames, Color color, float framesPerSecond, float scale, Vector2 facingDirection, float prefabLifetime = 1f, int sortingOrder = 20)
    {
        float rotationDegrees = 0f;
        if (facingDirection.sqrMagnitude > 0.001f)
        {
            rotationDegrees = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;
        }

        SpawnEffect(position, effectPrefab, fallbackFrames, color, framesPerSecond, scale, rotationDegrees, prefabLifetime, sortingOrder);
    }

    private static void SpawnEffect(Vector2 position, GameObject effectPrefab, Sprite[] fallbackFrames, Color color, float framesPerSecond, float scale, float rotationDegrees, float prefabLifetime = 1f, int sortingOrder = 20)
    {
        if (effectPrefab != null)
        {
            GameObject effectObject = new GameObject(effectPrefab.name);
            effectObject.transform.position = position;
            effectObject.transform.rotation = Quaternion.Euler(0f, 0f, rotationDegrees);

            GameObject prefabInstance = Instantiate(effectPrefab, effectObject.transform);
            prefabInstance.transform.localPosition = Vector3.zero;
            prefabInstance.transform.localScale = Vector3.one * scale;

            SpriteRenderer[] spriteRenderers = effectObject.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer renderer in spriteRenderers)
            {
                renderer.sortingOrder = sortingOrder;
            }

            ParticleSystemRenderer[] particleRenderers = effectObject.GetComponentsInChildren<ParticleSystemRenderer>();
            foreach (ParticleSystemRenderer renderer in particleRenderers)
            {
                renderer.sortingOrder = sortingOrder + 1;
            }

            if (prefabLifetime > 0f && effectObject.GetComponent<PrefabEffectAutoDestroy>() == null)
            {
                PrefabEffectAutoDestroy destroyer = effectObject.AddComponent<PrefabEffectAutoDestroy>();
                destroyer.fallbackLifetime = prefabLifetime;
            }

            return;
        }

        Spawn(position, fallbackFrames, color, framesPerSecond, scale, rotationDegrees, sortingOrder);
    }
}
