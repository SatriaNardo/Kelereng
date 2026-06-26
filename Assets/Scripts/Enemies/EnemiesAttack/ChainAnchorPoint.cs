using System.Collections.Generic;
using UnityEngine;

public class ChainAnchorPoint : MonoBehaviour
{
    [Header("Anchor Settings")]
    public float radius = 0.28f;
    public int chainMarbleCount = 3;
    public float chainSearchRadius = 1.1f;
    public float chainForce = 4f;
    public Color anchorColor = new Color(0.55f, 0.35f, 0.15f, 0.9f);
    public int sortingOrder = 2;
    public Sprite anchorSprite;
    public bool usePrefabVisual;

    private static readonly List<ChainAnchorPoint> ActiveAnchors = new List<ChainAnchorPoint>();

    public void Initialize(Vector2 center, float anchorRadius)
    {
        Initialize(center, anchorRadius, null);
    }

    public void Initialize(Vector2 center, float anchorRadius, Sprite sprite)
    {
        radius = anchorRadius;
        if (sprite != null)
        {
            anchorSprite = sprite;
        }
        transform.position = center;
        RefreshVisual();
    }

    private void OnEnable()
    {
        if (!ActiveAnchors.Contains(this))
        {
            ActiveAnchors.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveAnchors.Remove(this);
    }

    private void Awake()
    {
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        CircleCollider2D trigger = GetComponent<CircleCollider2D>();
        if (trigger == null)
        {
            trigger = gameObject.AddComponent<CircleCollider2D>();
        }

        trigger.isTrigger = true;
        trigger.radius = radius;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (usePrefabVisual)
        {
            transform.localScale = Vector3.one * (radius * 2f);
            return;
        }

        renderer.sprite = anchorSprite != null ? anchorSprite : CreateAnchorSprite();
        renderer.color = anchorColor;
        renderer.sortingOrder = sortingOrder;
        transform.localScale = Vector3.one * (radius * 2f);
    }

    public static bool TryTriggerChainAt(Vector2 contactPoint, Rigidbody2D attacker, Rigidbody2D victim)
    {
        ChainAnchorPoint anchor = FindAnchorAt(contactPoint);
        if (anchor == null) return false;

        anchor.TriggerChain(contactPoint, attacker, victim);
        return true;
    }

    private static ChainAnchorPoint FindAnchorAt(Vector2 point)
    {
        for (int i = ActiveAnchors.Count - 1; i >= 0; i--)
        {
            ChainAnchorPoint anchor = ActiveAnchors[i];
            if (anchor == null)
            {
                ActiveAnchors.RemoveAt(i);
                continue;
            }

            if (Vector2.Distance(point, anchor.transform.position) <= anchor.radius)
            {
                return anchor;
            }
        }

        return null;
    }

    private void TriggerChain(Vector2 contactPoint, Rigidbody2D attacker, Rigidbody2D victim)
    {
        List<Rigidbody2D> nearbyMarbles = FindNearestMarbles(contactPoint, attacker, victim);

        int launchCount = Mathf.Min(chainMarbleCount, nearbyMarbles.Count);
        for (int i = 0; i < launchCount; i++)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            if (direction == Vector2.zero) direction = Vector2.up;
            nearbyMarbles[i].AddForce(direction * chainForce, ForceMode2D.Impulse);
        }

        Debug.Log($"Chain anchor triggered and launched {launchCount} marbles.");
    }

    private List<Rigidbody2D> FindNearestMarbles(Vector2 center, Rigidbody2D attacker, Rigidbody2D victim)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(center, chainSearchRadius);
        List<Rigidbody2D> marbles = new List<Rigidbody2D>();

        foreach (Collider2D col in colliders)
        {
            Rigidbody2D rb = col.GetComponent<Rigidbody2D>();
            if (rb == null || rb == attacker || rb == victim || marbles.Contains(rb)) continue;
            marbles.Add(rb);
        }

        marbles.Sort((a, b) =>
        {
            float distanceA = Vector2.SqrMagnitude(a.position - center);
            float distanceB = Vector2.SqrMagnitude(b.position - center);
            return distanceA.CompareTo(distanceB);
        });

        return marbles;
    }

    private static Sprite CreateAnchorSprite()
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
                float alpha = distance > 0.72f ? 1f : 0.35f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
    }
}
