using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EnemySpriteAnimator : MonoBehaviour
{
    public float defaultIdleFramesPerSecond = 6f;
    public float defaultAttackFramesPerSecond = 10f;

    private SpriteRenderer spriteRenderer;
    private SpriteRenderer shadowRenderer;
    private Sprite[] idleFrames;
    private float idleFramesPerSecond;
    private Coroutine animationRoutine;
    private Vector3 startingScale;
    private bool showShadow;
    private Color shadowColor = new Color(0f, 0f, 0f, 0.35f);
    private Vector2 shadowSize = new Vector2(1.25f, 0.35f);
    private Vector2 shadowOffset = new Vector2(0f, -0.35f);
    private int shadowSortingOrderOffset = -1;

    private static Sprite shadowCircleSprite;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startingScale = transform.localScale;
    }

    public void SetVisualScale(float scale)
    {
        if (startingScale == Vector3.zero)
        {
            startingScale = transform.localScale;
        }

        transform.localScale = startingScale * Mathf.Max(0.01f, scale);
        ApplyShadowTransform();
    }

    public void ConfigureShadow(EnemySO enemy)
    {
        if (enemy == null || !enemy.showEnemyShadow)
        {
            showShadow = false;
            if (shadowRenderer != null)
            {
                shadowRenderer.gameObject.SetActive(false);
            }

            return;
        }

        showShadow = true;
        shadowColor = enemy.enemyShadowColor;
        shadowSize = new Vector2(Mathf.Max(0.01f, enemy.enemyShadowWidth), Mathf.Max(0.01f, enemy.enemyShadowHeight));
        shadowOffset = enemy.enemyShadowOffset;
        shadowSortingOrderOffset = enemy.enemyShadowSortingOrderOffset;

        EnsureShadowRenderer();
        shadowRenderer.gameObject.SetActive(true);
        shadowRenderer.color = shadowColor;
        shadowRenderer.sortingLayerID = spriteRenderer != null ? spriteRenderer.sortingLayerID : shadowRenderer.sortingLayerID;
        shadowRenderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + shadowSortingOrderOffset : shadowSortingOrderOffset;
        ApplyShadowTransform();
    }

    public void SetStaticSprite(Sprite sprite)
    {
        StopCurrentAnimation();
        idleFrames = null;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }

    public void SetIdleAnimation(Sprite[] frames, float framesPerSecond, Sprite fallbackSprite)
    {
        StopCurrentAnimation();

        if (!HasFrames(frames))
        {
            SetStaticSprite(fallbackSprite);
            return;
        }

        idleFrames = frames;
        idleFramesPerSecond = Mathf.Max(0.01f, framesPerSecond);
        spriteRenderer.sprite = idleFrames[0];
        animationRoutine = StartCoroutine(LoopIdleAnimation());
    }

    public void PlayAttackAnimation(Sprite[] frames, float framesPerSecond)
    {
        if (!HasFrames(frames))
        {
            return;
        }

        StopCurrentAnimation();
        animationRoutine = StartCoroutine(PlayAttackThenReturnToIdle(frames, Mathf.Max(0.01f, framesPerSecond)));
    }

    private IEnumerator LoopIdleAnimation()
    {
        int frameIndex = 0;

        while (HasFrames(idleFrames))
        {
            spriteRenderer.sprite = idleFrames[frameIndex];
            frameIndex = (frameIndex + 1) % idleFrames.Length;
            yield return new WaitForSeconds(1f / idleFramesPerSecond);
        }
    }

    private IEnumerator PlayAttackThenReturnToIdle(Sprite[] attackFrames, float framesPerSecond)
    {
        for (int i = 0; i < attackFrames.Length; i++)
        {
            if (attackFrames[i] != null)
            {
                spriteRenderer.sprite = attackFrames[i];
            }

            yield return new WaitForSeconds(1f / framesPerSecond);
        }

        animationRoutine = null;

        if (HasFrames(idleFrames))
        {
            animationRoutine = StartCoroutine(LoopIdleAnimation());
        }
    }

    private void StopCurrentAnimation()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }
    }

    private void EnsureShadowRenderer()
    {
        if (shadowCircleSprite == null)
        {
            shadowCircleSprite = CreateCircleSprite();
        }

        if (shadowRenderer != null) return;

        Transform existingShadow = transform.Find("EnemyShadow");
        GameObject shadowObject = existingShadow != null
            ? existingShadow.gameObject
            : new GameObject("EnemyShadow");

        shadowObject.transform.SetParent(transform, false);
        shadowRenderer = shadowObject.GetComponent<SpriteRenderer>();
        if (shadowRenderer == null)
        {
            shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
        }

        shadowRenderer.sprite = shadowCircleSprite;
    }

    private void ApplyShadowTransform()
    {
        if (!showShadow || shadowRenderer == null) return;

        Transform shadowTransform = shadowRenderer.transform;
        shadowTransform.localPosition = new Vector3(shadowOffset.x, shadowOffset.y, 0f);
        shadowTransform.localRotation = Quaternion.identity;

        Vector3 parentScale = transform.lossyScale;
        float parentScaleX = Mathf.Max(0.01f, Mathf.Abs(parentScale.x));
        float parentScaleY = Mathf.Max(0.01f, Mathf.Abs(parentScale.y));
        shadowTransform.localScale = new Vector3(shadowSize.x / parentScaleX, shadowSize.y / parentScaleY, 1f);
    }

    private static bool HasFrames(Sprite[] frames)
    {
        if (frames == null || frames.Length == 0) return false;

        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null) return true;
        }

        return false;
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
