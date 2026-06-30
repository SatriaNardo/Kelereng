using UnityEngine;
using System.Collections;

public class TargetMarble : MonoBehaviour
{
    [Header("Common Visuals")]
    public Sprite idleSprite;
    public Sprite[] rollingSprites;

    [Header("Shadow Visuals")]
    public Sprite shadowSprite;

    [Header("Exit Animation Settings")]
    public float delayBeforeShrink = 0.4f; 
    public float shrinkDuration = 0.3f;

    [Header("Spawn Animation")]
    public bool playSpawnAnimation = true;
    public float spawnDuration = 0.22f;
    public AnimationCurve spawnScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private MarbleRollAnimator rollAnimator;
    private PixelMarbleShadow pixelShadow;
    private Collider2D marbleCollider;
    private bool isOut = false;
    private bool isSpawning = false;
    private Vector3 originalScale;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        pixelShadow = GetComponent<PixelMarbleShadow>();
        marbleCollider = GetComponent<Collider2D>();
        originalScale = transform.localScale;
        ApplyVisuals();

        if (playSpawnAnimation)
        {
            StartCoroutine(AnimateSpawnIn());
        }
    }

    private void ApplyVisuals()
    {
        if (spriteRenderer == null) return;

        Sprite selectedIdleSprite = idleSprite != null ? idleSprite : spriteRenderer.sprite;
        if (selectedIdleSprite != null)
        {
            spriteRenderer.sprite = selectedIdleSprite;
        }

        rollAnimator = GetComponent<MarbleRollAnimator>();
        if (rollAnimator == null)
        {
            rollAnimator = gameObject.AddComponent<MarbleRollAnimator>();
        }

        rollAnimator.Configure(selectedIdleSprite, rollingSprites);

        if (pixelShadow == null && shadowSprite != null)
        {
            pixelShadow = gameObject.AddComponent<PixelMarbleShadow>();
        }

        if (pixelShadow != null)
        {
            pixelShadow.ConfigureSprite(shadowSprite);
        }
    }

    private void Update()
    {
        if (isOut || isSpawning) return;

        // Kalkulasi jarak flat 2D terhadap pusat lingkaran ring
        float distance = Vector2.Distance(transform.position, ArenaManager.Instance.arenaCenter.position);

        if (distance > ArenaManager.Instance.circleRadius)
        {
            TriggerExitLogic();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isOut) return;

        if (collision.gameObject.CompareTag("Gacoan") || collision.gameObject.name.Contains("PlayerMarble"))
        {
            MarbleElementHandler handler = collision.gameObject.GetComponent<MarbleElementHandler>();

            if (handler != null && handler.activeElement == null)
            {
                if (ProgressionManager.Instance != null)
                {
                    ProgressionManager.Instance.currentEnergy++;
                    if (ProgressionManager.Instance.currentEnergy > ProgressionManager.Instance.maxEnergyThisTurn)
                    {
                        ProgressionManager.Instance.currentEnergy = ProgressionManager.Instance.maxEnergyThisTurn;
                    }
                    Debug.Log($"⚡ Bonus +1 Energy! Total saat ini: {ProgressionManager.Instance.currentEnergy}");
                }
            }
        }
    }

    private void TriggerExitLogic()
    {
        isOut = true;
        
        // Hapus dari list pelacak pergerakan secara instan
        ArenaManager.Instance.allMarblesInArena.Remove(rb);

        // KIRIM KE SINI: Pintu terpusat ArenaManager untuk kalkulasi damage
        ArenaManager.Instance.OnMarbleExited(gameObject);

        StartCoroutine(AnimateToPocket());
    }

    private IEnumerator AnimateSpawnIn()
    {
        isSpawning = true;

        if (marbleCollider != null)
        {
            marbleCollider.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        float duration = Mathf.Max(0.01f, spawnDuration);
        float timer = 0f;
        transform.localScale = Vector3.zero;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            float scale = spawnScaleCurve != null ? spawnScaleCurve.Evaluate(progress) : progress;
            transform.localScale = originalScale * scale;
            yield return null;
        }

        transform.localScale = originalScale;

        if (marbleCollider != null)
        {
            marbleCollider.enabled = true;
        }

        isSpawning = false;
    }

    private IEnumerator AnimateToPocket()
    {
        yield return new WaitForSeconds(delayBeforeShrink);

        if (GetComponent<Collider2D>() != null) GetComponent<Collider2D>().enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        float timer = 0f;
        Vector3 startScale = transform.localScale;
        while (timer < shrinkDuration)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, timer / shrinkDuration);
            yield return null;
        }

        Destroy(gameObject);
    }
}
