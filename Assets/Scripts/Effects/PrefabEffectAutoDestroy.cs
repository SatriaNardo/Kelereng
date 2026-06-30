using UnityEngine;

public class PrefabEffectAutoDestroy : MonoBehaviour
{
    public float fallbackLifetime = 1f;
    public float extraDelay = 0.05f;

    private Animator[] animators;
    private ParticleSystem[] particleSystems;
    private float fallbackDestroyTime;

    private void Awake()
    {
        animators = GetComponentsInChildren<Animator>(true);
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void Start()
    {
        fallbackDestroyTime = Time.time + Mathf.Max(0.05f, fallbackLifetime);
    }

    private void Update()
    {
        bool hasTimedContent = (animators != null && animators.Length > 0)
            || (particleSystems != null && particleSystems.Length > 0);

        if (Time.time >= fallbackDestroyTime)
        {
            Destroy(gameObject);
            return;
        }

        if (!hasTimedContent)
        {
            return;
        }

        if (AreAnimatorsDone() && AreParticleSystemsDone())
        {
            Destroy(gameObject, extraDelay);
            enabled = false;
        }
    }

    private bool AreAnimatorsDone()
    {
        if (animators == null) return true;

        foreach (Animator animator in animators)
        {
            if (animator == null || !animator.isActiveAndEnabled) continue;

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (animator.IsInTransition(0) || stateInfo.normalizedTime < 1f)
            {
                return false;
            }
        }

        return true;
    }

    private bool AreParticleSystemsDone()
    {
        if (particleSystems == null) return true;

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            if (particleSystem != null && particleSystem.IsAlive(true))
            {
                return false;
            }
        }

        return true;
    }
}
