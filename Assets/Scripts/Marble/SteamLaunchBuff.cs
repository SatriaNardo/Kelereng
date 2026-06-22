using UnityEngine;

public class SteamLaunchBuff : MonoBehaviour
{
    public float massMultiplier = 0.5f;
    public float duration = 8f;

    private Rigidbody2D rb;
    private float originalMass;
    private bool isApplied = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        ApplyBuff();
    }

    public void Configure(float newMassMultiplier, float newDuration)
    {
        massMultiplier = newMassMultiplier;
        duration = newDuration;

        if (isApplied)
        {
            rb.mass = Mathf.Max(0.01f, originalMass * massMultiplier);
            CancelInvoke();
            Invoke(nameof(Expire), duration);
        }
    }

    private void ApplyBuff()
    {
        if (rb == null || isApplied) return;

        isApplied = true;
        originalMass = rb.mass;
        rb.mass = Mathf.Max(0.01f, originalMass * massMultiplier);
        Invoke(nameof(Expire), duration);
    }

    private void Expire()
    {
        Destroy(this);
    }

    private void OnDestroy()
    {
        if (rb != null && isApplied)
        {
            rb.mass = originalMass;
        }
    }
}
