using System.Collections.Generic;
using UnityEngine;

public class CorruptedSmokeZone : MonoBehaviour
{
    [Header("Smoke Visual")]
    public float radius = 1.35f;
    [Range(0f, 1f)] public float opacity = 0.75f;
    public Color smokeColor = new Color(0.45f, 0.12f, 0.62f, 1f);
    public int sortingOrder = 100;
    public Sprite visualSprite;
    public bool usePrefabVisual;

    [Header("Clash Chaos")]
    public float randomClashForce = 6f;

    private static readonly List<CorruptedSmokeZone> ActiveZones = new List<CorruptedSmokeZone>();

    private SpriteRenderer spriteRenderer;

    public void Initialize(Vector2 center, float zoneRadius)
    {
        Initialize(center, zoneRadius, null);
    }

    public void Initialize(Vector2 center, float zoneRadius, Sprite sprite)
    {
        radius = zoneRadius;
        if (sprite != null)
        {
            visualSprite = sprite;
        }
        transform.position = center;
        RefreshVisual();
    }

    private void OnEnable()
    {
        if (!ActiveZones.Contains(this))
        {
            ActiveZones.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveZones.Remove(this);
    }

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

        if (usePrefabVisual)
        {
            transform.localScale = Vector3.one * (radius * 2f);
            return;
        }

        spriteRenderer.sprite = visualSprite != null ? visualSprite : CreateSmokeSprite();
        spriteRenderer.color = new Color(smokeColor.r, smokeColor.g, smokeColor.b, opacity);
        spriteRenderer.sortingOrder = sortingOrder;
        transform.localScale = Vector3.one * (radius * 2f);
    }

    public static bool IsPointInsideSmoke(Vector2 point)
    {
        for (int i = ActiveZones.Count - 1; i >= 0; i--)
        {
            CorruptedSmokeZone zone = ActiveZones[i];
            if (zone == null)
            {
                ActiveZones.RemoveAt(i);
                continue;
            }

            if (Vector2.Distance(point, zone.transform.position) <= zone.radius)
            {
                return true;
            }
        }

        return false;
    }

    public static void ApplyRandomClash(Rigidbody2D attacker, Rigidbody2D victim)
    {
        float force = 6f;
        if (ActiveZones.Count > 0 && ActiveZones[0] != null)
        {
            force = ActiveZones[0].randomClashForce;
        }

        ApplyRandomImpulse(attacker, force);
        ApplyRandomImpulse(victim, force);
        Debug.Log("Corrupted smoke scrambled a clash into random directions.");
    }

    private static void ApplyRandomImpulse(Rigidbody2D rb, float force)
    {
        if (rb == null) return;

        Vector2 direction = Random.insideUnitCircle.normalized;
        if (direction == Vector2.zero) direction = Vector2.up;
        rb.AddForce(direction * force, ForceMode2D.Impulse);
    }

    private static Sprite CreateSmokeSprite()
    {
        const int textureSize = 128;
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
