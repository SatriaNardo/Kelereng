using UnityEngine;

public class RockFormation : MonoBehaviour
{
    [Header("Formation Layout")]
    public int rockCount = 2;
    public float clusterRadius = 0.55f;
    public float rockSize = 0.22f;
    public Color rockColor = new Color(0.42f, 0.38f, 0.34f, 1f);
    public int sortingOrder = 3;
    public PhysicsMaterial2D rockMaterial;
    public Sprite rockSprite;
    public bool usePrefabLayout;
    private float formationAngle;

    public void Initialize(Vector2 center, int rocks, float spread, PhysicsMaterial2D material)
    {
        Initialize(center, rocks, spread, Random.Range(0f, 180f), material);
    }

    public void Initialize(Vector2 center, int rocks, float spread, float angle, PhysicsMaterial2D material)
    {
        Initialize(center, rocks, spread, angle, material, null);
    }

    public void Initialize(Vector2 center, int rocks, float spread, float angle, PhysicsMaterial2D material, Sprite sprite)
    {
        rockCount = Mathf.Max(rocks, 1);
        clusterRadius = spread;
        formationAngle = angle;
        rockMaterial = material;
        if (sprite != null)
        {
            rockSprite = sprite;
        }
        transform.position = center;

        if (usePrefabLayout)
        {
            return;
        }

        BuildRocks();
    }

    private void BuildRocks()
    {
        ClearRocks();

        for (int i = 0; i < rockCount; i++)
        {
            float armAngle = formationAngle + (i * 90f);
            float size = rockSize * Random.Range(0.75f, 1.25f);
            float width = clusterRadius * 2f;
            float height = size;

            GameObject rock = new GameObject($"Rock_{i}");
            rock.transform.SetParent(transform, false);
            rock.transform.localPosition = Vector2.zero;
            rock.transform.rotation = Quaternion.Euler(0f, 0f, armAngle);
            rock.transform.localScale = new Vector3(width, height, 1f);

            Rigidbody2D body = rock.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;

            BoxCollider2D collider = rock.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            if (rockMaterial != null)
            {
                collider.sharedMaterial = rockMaterial;
            }

            SpriteRenderer renderer = rock.AddComponent<SpriteRenderer>();
            renderer.sprite = rockSprite != null ? rockSprite : CreateBlockSprite();
            renderer.color = rockColor;
            renderer.sortingOrder = sortingOrder;
        }
    }

    private void ClearRocks()
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
