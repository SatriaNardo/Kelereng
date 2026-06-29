using UnityEngine;

public class FireTrailEffect : MonoBehaviour
{
    private const float StopSpeedThreshold = 0.08f;

    private Rigidbody2D rb;
    private ParticleSystem trailParticles;
    private ParticleSystem.EmissionModule emission;
    private bool isStopping;
    private float cleanupTimer;

    public void Configure(
        Color startColor,
        Color endColor,
        float particlesPerSecond,
        float lifetime,
        float startSize,
        float speed,
        int sortingOrder,
        Material material)
    {
        rb = GetComponent<Rigidbody2D>();

        if (trailParticles == null)
        {
            GameObject particleObject = new GameObject("FireMoveTrail");
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
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(startSize * 0.6f, startSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.maxParticles = 80;

        emission = trailParticles.emission;
        emission.enabled = true;
        emission.rateOverTime = particlesPerSecond;

        ParticleSystem.ShapeModule shape = trailParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.08f;
        shape.randomDirectionAmount = 0.35f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = trailParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = BuildFireGradient(startColor, endColor);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = trailParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        ParticleSystemRenderer renderer = trailParticles.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = sortingOrder;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.maxParticleSize = 0.5f;
        if (material != null)
        {
            renderer.material = material;
        }

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
            Destroy(this);
        }
    }

    private Gradient BuildFireGradient(Color startColor, Color endColor)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(new Color(1f, 0.35f, 0.05f, 1f), 0.35f),
                new GradientColorKey(endColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.65f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            });

        return gradient;
    }
}
