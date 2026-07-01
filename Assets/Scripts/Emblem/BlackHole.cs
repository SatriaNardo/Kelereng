using UnityEngine;

public class BlackHole : MonoBehaviour
{
    [Header("Attraction")]
    public float attractionRadius = 4f;

    [Header("Force")]
    public float playerPullForce = 0.6f;
    public float enemyPullForce = 0.15f;

    [Header("Velocity Limit")]
    public float maxPullVelocity = 3f;

    [Header("Lifetime")]
    public float duration = 3f;

    [Header("Swallow")]
    public float swallowRadius = 0.15f;

    private void Start()
    {
        Destroy(gameObject, duration);
        SetupVisual();
    }

    private void SetupVisual()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite == null)
        {
            sr.sprite = CreateBlackHoleSprite(64);
            sr.sortingOrder = 5;
        }

        if (GetComponent<BlackHoleVisual>() == null)
        {
            gameObject.AddComponent<BlackHoleVisual>();
        }
    }

    /// <summary>
    /// Membuat sprite lingkaran gradasi hitam-ungu secara programatis
    /// untuk fallback jika sprite belum di-assign di prefab.
    /// </summary>
    private Sprite CreateBlackHoleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float center = size / 2f;
        float maxRadius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(
                    new Vector2(x, y),
                    new Vector2(center, center));

                float t = dist / maxRadius;

                if (t > 1f)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    // Gradasi: pusat hitam → pinggir ungu transparan
                    Color coreColor = new Color(0.05f, 0f, 0.1f, 1f);
                    Color edgeColor = new Color(0.4f, 0f, 0.6f, 0f);
                    tex.SetPixel(x, y, Color.Lerp(coreColor, edgeColor, t));
                }
            }
        }

        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size);
    }

    private void FixedUpdate()
    {
        Collider2D[] colliders =
            Physics2D.OverlapCircleAll(
                transform.position,
                attractionRadius);

        foreach (Collider2D col in colliders)
        {
            bool isPlayer = col.CompareTag("PlayerMarble");
            bool isEnemy = col.CompareTag("TargetMarble");

            if (!isPlayer && !isEnemy)
                continue;

            Rigidbody2D rb = col.GetComponent<Rigidbody2D>();

            if (rb == null)
                continue;

            Vector2 direction =
                (Vector2)transform.position - rb.position;

            float distance = direction.magnitude;

            // Jika player marble sudah sangat dekat pusat,
            // langsung telan dan kembalikan sebagai ammo
            if (isPlayer && distance < swallowRadius)
            {
                SwallowPlayerMarble(col);
                continue;
            }

            // Jangan tarik kelereng musuh yang sudah dekat pinggir arena
            // agar tidak terdorong keluar
            if (isEnemy)
            {
                float distFromCenter =
                    Vector2.Distance(
                        rb.position,
                        ArenaManager.Instance.arenaCenter.position);

                if (distFromCenter >
                    ArenaManager.Instance.circleRadius - 0.7f)
                {
                    continue;
                }
            }

            direction.Normalize();

            // Force makin kuat semakin dekat ke pusat blackhole
            float forceMultiplier =
                Mathf.Clamp01(
                    (attractionRadius - distance)
                    / attractionRadius);

            float pullForce = isPlayer
                ? playerPullForce
                : enemyPullForce;

            rb.AddForce(
                direction * pullForce * forceMultiplier,
                ForceMode2D.Force);

            // Clamp velocity agar kelereng tidak terlontar
            float maxVel = isPlayer
                ? maxPullVelocity
                : maxPullVelocity * 0.5f;

            if (rb.linearVelocity.magnitude > maxVel)
            {
                rb.linearVelocity =
                    rb.linearVelocity.normalized * maxVel;
            }
        }
    }

    private void SwallowPlayerMarble(Collider2D other)
    {
        Debug.Log($"🌌 {other.name} masuk Black Hole! Dikembalikan sebagai ammo.");

        // Kembalikan ammo menggunakan sistem yang sudah ada
        other.tag = "PlayerMarble";
        ArenaManager.Instance.OnMarbleExited(other.gameObject);

        // Hapus kelereng
        Destroy(other.gameObject);
    }

    // Swallow hanya via jarak di FixedUpdate
    // agar kelereng terlihat sampai tengah dulu
}