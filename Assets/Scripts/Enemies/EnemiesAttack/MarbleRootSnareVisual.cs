using UnityEngine;

public class MarbleRootSnareVisual : MonoBehaviour
{
    [Header("Root Vine")]
    public int stalkSegmentCount = 6;
    public float stalkThickness = 0.06f;
    public float marbleEndGap = 0.18f;

    [Header("Visual")]
    public Color stalkColor = new Color(0.18f, 0.52f, 0.14f, 0.9f);
    public Color stalkTipColor = new Color(0.28f, 0.62f, 0.18f, 0.85f);
    public int sortingOrder = 3;

    private Transform vineRoot;
    private static Sprite blockSprite;

    public void Build(int segments, float thickness)
    {
        stalkSegmentCount = segments;
        stalkThickness = thickness;

        if (blockSprite == null)
        {
            blockSprite = CreateBlockSprite();
        }

        if (vineRoot == null)
        {
            vineRoot = new GameObject("RootVine").transform;
            vineRoot.SetParent(transform, false);
        }
    }

    public void UpdateSnare(Vector2 pullCenter, Vector2 marblePosition)
    {
        transform.position = pullCenter;
        UpdateVine(pullCenter, marblePosition);
    }

    private void UpdateVine(Vector2 pullCenter, Vector2 marblePosition)
    {
        if (vineRoot == null)
        {
            return;
        }

        ClearChildren(vineRoot);

        Vector2 toMarble = marblePosition - pullCenter;
        float distance = toMarble.magnitude;
        if (distance <= marbleEndGap + 0.05f)
        {
            return;
        }

        Vector2 direction = toMarble / distance;
        float vineLength = distance - marbleEndGap;
        int visibleSegments = Mathf.Max(1, stalkSegmentCount);
        float segmentLength = vineLength / visibleSegments;

        Vector2 cursor = Vector2.zero;
        for (int i = 0; i < visibleSegments; i++)
        {
            cursor += direction * segmentLength;
            Color color = i == visibleSegments - 1 ? stalkTipColor : stalkColor;
            CreateSegment(
                vineRoot,
                cursor - direction * (segmentLength * 0.5f),
                direction,
                segmentLength,
                stalkThickness,
                color,
                $"Vine_{i}"
            );
        }
    }

    private void CreateSegment(
        Transform parent,
        Vector2 localPosition,
        Vector2 direction,
        float length,
        float thickness,
        Color color,
        string segmentName)
    {
        GameObject segment = new GameObject(segmentName);
        segment.transform.SetParent(parent, false);
        segment.transform.localPosition = localPosition;
        segment.transform.right = direction;

        SpriteRenderer renderer = segment.AddComponent<SpriteRenderer>();
        renderer.sprite = blockSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        segment.transform.localScale = new Vector3(length, thickness, 1f);
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private static Sprite CreateBlockSprite()
    {
        Texture2D texture = Texture2D.whiteTexture;
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            texture.width
        );
    }
}
