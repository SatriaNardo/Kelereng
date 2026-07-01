using UnityEngine;

public class GooPool : MonoBehaviour
{
    [Header("Goo Friction Adjustments")]
    [Tooltip("Semakin besar angkanya, kelereng di dalam area akan berhenti semakin cepat.")]
    public float gooLinearDrag = 7f; 

    private void OnTriggerStay2D(Collider2D collision)
    {
        // Deteksi jika objek yang berada di dalam area lendir memiliki komponen fisik 2D
        Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
        
        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
        {
            if (SteamLaunchBuff.IsActive(collision.gameObject))
            {
                return;
            }

            // Kurangi kecepatan gerak kelereng secara halus berbasis DeltaTime
            rb.linearVelocity *= (1f - (gooLinearDrag * Time.deltaTime));

            // Potong paksa ke nol jika sisa kecepatan sudah sangat merayap agar tidak meluncur selamanya
            if (rb.linearVelocity.magnitude < 0.08f)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }
}
