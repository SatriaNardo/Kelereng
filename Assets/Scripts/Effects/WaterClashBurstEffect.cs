using UnityEngine;

public class WaterClashBurstEffect : MonoBehaviour
{
    public static void Spawn(
        Vector2 position,
        Vector2 direction,
        Color startColor,
        Color endColor,
        float lifetime,
        float speed,
        float size,
        int particleCount,
        float spreadAngle,
        int sortingOrder,
        Material material)
    {
        GameObject burstObject = new GameObject("WaterClashBurst");
        burstObject.transform.position = position;

        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = Vector2.right;
        }

        ParticleSystem particles = burstObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.2f;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.maxParticles = Mathf.Max(1, particleCount);

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = false;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = BuildWaterGradient(startColor, endColor);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = sortingOrder;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.maxParticleSize = 0.75f;
        renderer.lengthScale = 1.35f;
        if (material != null)
        {
            renderer.material = material;
        }

        EmitBurst(particles, position, direction.normalized, startColor, speed, size, lifetime, particleCount, spreadAngle);

        TimedEffectDestroy destroyer = burstObject.AddComponent<TimedEffectDestroy>();
        destroyer.lifetime = lifetime + 0.25f;
    }

    private static void EmitBurst(ParticleSystem particles, Vector2 position, Vector2 direction, Color color, float speed, float size, float lifetime, int particleCount, float spreadAngle)
    {
        int count = Mathf.Max(1, particleCount);
        float halfSpread = Mathf.Max(0f, spreadAngle) * 0.5f;
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        for (int i = 0; i < count; i++)
        {
            float angle = baseAngle + Random.Range(-halfSpread, halfSpread);
            Vector2 particleDirection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            float speedMultiplier = Random.Range(0.35f, 1f);
            float sizeMultiplier = Random.Range(0.6f, 1.2f);
            float lifetimeMultiplier = Random.Range(0.65f, 1f);

            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = particleDirection * speed * speedMultiplier,
                startColor = color,
                startSize = size * sizeMultiplier,
                startLifetime = lifetime * lifetimeMultiplier,
                rotation = Random.Range(0f, Mathf.PI * 2f)
            };

            particles.Emit(emitParams, 1);
        }
    }

    private static Gradient BuildWaterGradient(Color startColor, Color endColor)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(new Color(0.3f, 0.8f, 1f, 1f), 0.35f),
                new GradientColorKey(endColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.65f, 0.35f),
                new GradientAlphaKey(0f, 1f)
            });

        return gradient;
    }
}
