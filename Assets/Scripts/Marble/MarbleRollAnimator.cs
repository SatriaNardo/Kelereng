using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class MarbleRollAnimator : MonoBehaviour
{
    [Header("Speed")]
    public float movingThreshold = 0.08f;
    public float speedForBaseFrameRate = 2f;
    public float baseFramesPerSecond = 18f;
    public float maxFramesPerSecond = 60f;
    public float speedSmoothing = 18f;
    public float directionSwitchThreshold = 0.12f;
    public float idleGraceTime = 0.08f;
    public bool reverseWhenMovingDown = true;
    public bool holdLastFrameWhenStopped = true;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Sprite idleSprite;
    private Sprite[] rollingSprites;
    private float frameCursor;
    private int lastRenderedFrame = -1;
    private float smoothedSpeed;
    private float idleTimer;
    private int rollDirection = 1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = GetComponentInParent<Rigidbody2D>();
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (spriteRenderer == null || rb == null || rollingSprites == null || rollingSprites.Length == 0)
        {
            return;
        }

        Vector2 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, speed, 1f - Mathf.Exp(-speedSmoothing * Time.deltaTime));

        if (speed < movingThreshold)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleGraceTime)
            {
                smoothedSpeed = 0f;
                if (!holdLastFrameWhenStopped && idleSprite != null && spriteRenderer.sprite != idleSprite)
                {
                    frameCursor = 0f;
                    lastRenderedFrame = -1;
                    spriteRenderer.sprite = idleSprite;
                }
            }

            return;
        }

        idleTimer = 0f;
        UpdateRollDirection(velocity);

        float speedRatio = speedForBaseFrameRate > 0f ? smoothedSpeed / speedForBaseFrameRate : 1f;
        float framesPerSecond = Mathf.Clamp(baseFramesPerSecond * speedRatio, 1f, maxFramesPerSecond);
        frameCursor = WrapFrameCursor(frameCursor + Time.deltaTime * framesPerSecond * rollDirection);

        int frameIndex = Mathf.FloorToInt(frameCursor);
        if (frameIndex == lastRenderedFrame) return;

        Sprite nextFrame = rollingSprites[frameIndex];
        if (nextFrame != null && spriteRenderer.sprite != nextFrame)
        {
            spriteRenderer.sprite = nextFrame;
        }

        lastRenderedFrame = frameIndex;
    }

    public void Configure(Sprite newIdleSprite, Sprite[] newRollingSprites)
    {
        idleSprite = newIdleSprite;
        rollingSprites = newRollingSprites;
        frameCursor = 0f;
        lastRenderedFrame = -1;
        smoothedSpeed = 0f;
        idleTimer = 0f;
        rollDirection = 1;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = GetComponentInParent<Rigidbody2D>();
            }
        }

        if (spriteRenderer != null && idleSprite != null)
        {
            spriteRenderer.sprite = idleSprite;
        }
    }

    private float WrapFrameCursor(float cursor)
    {
        if (rollingSprites == null || rollingSprites.Length == 0) return 0f;

        while (cursor < 0f)
        {
            cursor += rollingSprites.Length;
        }

        while (cursor >= rollingSprites.Length)
        {
            cursor -= rollingSprites.Length;
        }

        return cursor;
    }

    private void UpdateRollDirection(Vector2 velocity)
    {
        if (!reverseWhenMovingDown)
        {
            rollDirection = 1;
            return;
        }

        if (velocity.y < -directionSwitchThreshold)
        {
            rollDirection = -1;
        }
        else if (velocity.y > directionSwitchThreshold)
        {
            rollDirection = 1;
        }
    }
}
