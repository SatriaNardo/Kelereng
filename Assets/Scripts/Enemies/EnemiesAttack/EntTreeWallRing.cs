using UnityEngine;

public class EntTreeWallRing : MonoBehaviour
{
    [Header("Ring Layout")]
    public float ringRadius = 1.45f;
    public int segmentCount = 26;
    public float segmentLength = 0.42f;
    public float segmentThickness = 0.14f;
    [Tooltip("Number of wall arcs around the ring. Gaps fill the remaining space.")]
    public int wallArcCount = 4;
    [Tooltip("Angular width of each wall arc and each gap (degrees).")]
    public float sectorAngleDegrees = 45f;

    [Header("Visual")]
    public Color wallColor = new Color(0.34f, 0.22f, 0.1f, 1f);
    public int sortingOrder = 4;

    [Header("Physics")]
    public PhysicsMaterial2D wallMaterial;

    public void Initialize(Vector2 center, float radius, int segments, PhysicsMaterial2D material)
    {
        ringRadius = radius;
        segmentCount = Mathf.Max(segments, 8);
        wallMaterial = material;
        transform.position = center;
        BuildSegments();
    }

    private void BuildSegments()
    {
        ClearSegments();

        int arcCount = Mathf.Max(wallArcCount, 1);
        int segmentsPerWall = Mathf.Max(segmentCount / arcCount, 2);
        float wallSpacing = 360f / (arcCount * 2f);

        for (int wallIndex = 0; wallIndex < arcCount; wallIndex++)
        {
            float wallCenterAngle = wallIndex * wallSpacing * 2f;
            float arcStart = wallCenterAngle - sectorAngleDegrees * 0.5f;
            float angleStep = sectorAngleDegrees / segmentsPerWall;

            for (int segmentIndex = 0; segmentIndex < segmentsPerWall; segmentIndex++)
            {
                float angleDegrees = arcStart + (segmentIndex + 0.5f) * angleStep;
                float angleRadians = angleDegrees * Mathf.Deg2Rad;
                Vector2 outward = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
                Vector2 tangent = new Vector2(-outward.y, outward.x);

                GameObject segment = new GameObject($"WallSegment_{wallIndex}_{segmentIndex}");
                segment.transform.SetParent(transform, false);
                segment.transform.localPosition = outward * ringRadius;
                segment.transform.right = tangent;

                BoxCollider2D collider = segment.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(segmentLength, segmentThickness);
                if (wallMaterial != null)
                {
                    collider.sharedMaterial = wallMaterial;
                }

                SpriteRenderer renderer = segment.AddComponent<SpriteRenderer>();
                renderer.sprite = CreateBlockSprite();
                renderer.color = wallColor;
                renderer.sortingOrder = sortingOrder;
                segment.transform.localScale = new Vector3(segmentLength, segmentThickness, 1f);
            }
        }
    }

    private void ClearSegments()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
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
