using UnityEngine;

public class GenericMarbleTrailEffect : MonoBehaviour
{
    private const float StopSpeedThreshold = 0.08f;

    private Rigidbody2D rb;
    private ParticleSystem trailParticles;
    private ParticleSystem.EmissionModule emission;
    private Material runtimeMaterial;
    private static Texture2D whiteParticleTexture;
    private bool isStopping;
    private float cleanupTimer;

    public void Configure(
        string trailName,
        Color startColor,
        Color midColor,
        Color endColor,
        float particlesPerSecond,
        float lifetime,
        float startSize,
        float speed,
        int sortingOrder,
        Material material,
        Sprite particleSprite)
    {
        rb = GetComponent<Rigidbody2D>();

        if (trailParticles == null)
        {
            GameObject particleObject = new GameObject(string.IsNullOrEmpty(trailName) ? "MarbleMoveTrail" : trailName);
            particleObject.transform.SetParent(transform, false);
            particleObject.transform.localPosition = Vector3.zero;
            trailParticles = particleObject.AddComponent<ParticleSystem>();
        }

        trailParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = trailParticles.main;
        main.duration = 1f;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.65f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.45f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(startSize * 0.6f, startSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.maxParticles = 100;

        emission = trailParticles.emission;
        emission.enabled = true;
        emission.rateOverTime = particlesPerSecond;

        ParticleSystem.ShapeModule shape = trailParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.09f;
        shape.randomDirectionAmount = 0.55f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = trailParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = BuildGradient(startColor, midColor, endColor);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = trailParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        ParticleSystemRenderer renderer = trailParticles.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = sortingOrder;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.maxParticleSize = 0.5f;
        ConfigureParticleSprite(particleSprite);
        ApplyMaterial(renderer, material, particleSprite);

        isStopping = false;
        cleanupTimer = lifetime + 0.1f;
        trailParticles.Play(true);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (trailParticles == null) return;

        bool shouldEmit = rb != null && rb.linearVelocity.sqrMagnitude > StopSpeedThreshold * StopSpeedThreshold;
        emission.enabled = shouldEmit && !isStopping;

        if (shouldEmit)
        {
            Vector2 velocity = rb.linearVelocity.normalized;
            trailParticles.transform.position = (Vector2)transform.position - velocity * 0.18f;
            cleanupTimer = trailParticles.main.startLifetime.constantMax + 0.1f;
            return;
        }

        isStopping = true;
        trailParticles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        cleanupTimer -= Time.deltaTime;
        if (cleanupTimer <= 0f)
        {
            Destroy(trailParticles.gameObject);
            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
            }

            Destroy(this);
        }
    }

    private void ApplyMaterial(ParticleSystemRenderer renderer, Material material, Sprite particleSprite)
    {
        if (material != null)
        {
            renderer.material = material;
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
        {
            Debug.LogWarning("GenericMarbleTrailEffect could not find a supported particle shader.");
            return;
        }

        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }

        runtimeMaterial = new Material(shader);
        runtimeMaterial.name = particleSprite != null ? $"{particleSprite.name} Runtime Particle Material" : "Runtime Marble Particle Material";
        Texture particleTexture = particleSprite != null ? particleSprite.texture : GetWhiteParticleTexture();
        runtimeMaterial.mainTexture = particleTexture;
        if (runtimeMaterial.HasProperty("_BaseMap"))
        {
            runtimeMaterial.SetTexture("_BaseMap", particleTexture);
        }

        if (runtimeMaterial.HasProperty("_MainTex"))
        {
            runtimeMaterial.SetTexture("_MainTex", particleTexture);
        }

        renderer.material = runtimeMaterial;
    }

    private void ConfigureParticleSprite(Sprite particleSprite)
    {
        ParticleSystem.TextureSheetAnimationModule textureSheet = trailParticles.textureSheetAnimation;
        if (particleSprite == null)
        {
            textureSheet.enabled = false;
            return;
        }

        textureSheet.enabled = true;
        textureSheet.mode = ParticleSystemAnimationMode.Sprites;
        textureSheet.timeMode = ParticleSystemAnimationTimeMode.Lifetime;
        textureSheet.fps = 1f;
        while (textureSheet.spriteCount > 0)
        {
            textureSheet.RemoveSprite(0);
        }

        textureSheet.AddSprite(particleSprite);
    }

    private static Texture2D GetWhiteParticleTexture()
    {
        if (whiteParticleTexture != null) return whiteParticleTexture;

        whiteParticleTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        whiteParticleTexture.name = "Generated White Marble Particle";
        whiteParticleTexture.filterMode = FilterMode.Point;
        whiteParticleTexture.wrapMode = TextureWrapMode.Clamp;
        whiteParticleTexture.SetPixel(0, 0, Color.white);
        whiteParticleTexture.Apply(false, true);
        return whiteParticleTexture;
    }

    private Gradient BuildGradient(Color startColor, Color midColor, Color endColor)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(midColor, 0.45f),
                new GradientColorKey(endColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(startColor.a, 0f),
                new GradientAlphaKey(Mathf.Min(startColor.a, 0.65f), 0.45f),
                new GradientAlphaKey(0f, 1f)
            });

        return gradient;
    }
}
