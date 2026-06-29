using UnityEngine;

public class TimedEffectDestroy : MonoBehaviour
{
    public float lifetime = 1f;

    private void Start()
    {
        Destroy(gameObject, Mathf.Max(0.01f, lifetime));
    }
}
