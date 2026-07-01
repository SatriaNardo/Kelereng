using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EnemySpriteAnimator : MonoBehaviour
{
    public float defaultIdleFramesPerSecond = 6f;
    public float defaultAttackFramesPerSecond = 10f;

    private SpriteRenderer spriteRenderer;
    private Sprite[] idleFrames;
    private float idleFramesPerSecond;
    private Coroutine animationRoutine;
    private Vector3 startingScale;

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
    }

    public void SetStaticSprite(Sprite sprite)
    {
        StopCurrentAnimation();
        idleFrames = null;

        if (spriteRenderer != null && sprite != null)
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

    private static bool HasFrames(Sprite[] frames)
    {
        if (frames == null || frames.Length == 0) return false;

        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null) return true;
        }

        return false;
    }
}
