using UnityEngine;

public class QuakeDustBurstEffect : MonoBehaviour
{
    public static void Spawn(Vector2 position, Color startColor, Color endColor, float lifetime, float speed, float size, int particleCount, int sortingOrder, Material material)
    {
        GameObject burstObject = new GameObject("QuakeDustBurst");
        burstObject.transform.position = position;

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

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = false;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = BuildGradient(startColor, endColor);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = sortingOrder;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.maxParticleSize = 0.8f;
        if (material != null) renderer.material = material;

        EmitRing(particles, position, startColor, speed, size, lifetime, particleCount);

        TimedEffectDestroy destroyer = burstObject.AddComponent<TimedEffectDestroy>();
        destroyer.lifetime = lifetime + 0.25f;
    }

    private static void EmitRing(ParticleSystem particles, Vector2 position, Color color, float speed, float size, float lifetime, int particleCount)
    {
        int count = Mathf.Max(1, particleCount);
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i + Random.Range(-10f, 10f);
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = direction * speed * Random.Range(0.45f, 1f),
                startColor = color,
                startSize = size * Random.Range(0.65f, 1.2f),
                startLifetime = lifetime * Random.Range(0.65f, 1f),
                rotation = Random.Range(0f, Mathf.PI * 2f)
            };
            particles.Emit(emitParams, 1);
        }
    }

    private static Gradient BuildGradient(Color startColor, Color endColor)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(new Color(0.55f, 0.42f, 0.25f, 1f), 0.45f),
                new GradientColorKey(endColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.75f, 0f),
                new GradientAlphaKey(0.45f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }
}
