using UnityEngine;

public class BlackHoleVisual : MonoBehaviour
{
    public float rotationSpeed = 180f;
    public float pulseSpeed = 4f;
    public float pulseAmount = 0.15f;

    private Vector3 originalScale;

    private void Start()
    {
        originalScale = transform.localScale;
    }

    private void Update()
    {
        // Rotasi
        transform.Rotate(0f, 0f,
            rotationSpeed * Time.deltaTime);

        // Efek membesar mengecil
        float scale =
            1f + Mathf.Sin(Time.time * pulseSpeed)
            * pulseAmount;

        transform.localScale =
            originalScale * scale;
    }
}