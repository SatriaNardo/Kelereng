using UnityEngine;
using System.Collections;

public class TargetMarble : MonoBehaviour
{
    [Header("Exit Animation Settings")]
    [Tooltip("How long the marble keeps rolling outside the ring before it starts disappearing.")]
    public float delayBeforeShrink = 0.4f; 
    public float shrinkDuration = 0.3f;

    private Rigidbody2D rb;
    private bool isOut = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (isOut) return;

        // Calculate distance from center
        float distance = Vector2.Distance(transform.position, ArenaManager.Instance.arenaCenter.position);

        if (distance > ArenaManager.Instance.circleRadius)
        {
            TriggerExitLogic();
        }
    }

    // ========================================================
    // BARU: Deteksi tabrakan untuk memberi Bonus Energy Hibrida
    // ========================================================
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Jika kelereng ini sudah didorong keluar lingkaran, abaikan deteksi tabrakan tambahan
        if (isOut) return;

        // Pastikan objek yang menabrak memiliki Tag "Player" atau "Gacoan" (sesuaikan dengan tag gacoanmu)
        if (collision.gameObject.CompareTag("Gacoan") || collision.gameObject.name.Contains("Gacoan"))
        {
            // Ambil komponen handler elemen dari gacoan yang menabrak kita
            MarbleElementHandler handler = collision.gameObject.GetComponent<MarbleElementHandler>();

            // VALIDASI: Hanya beri energi jika gacoan tersebut murni KELERENG POLOS (activeElement == null)
            if (handler != null && handler.activeElement == null)
            {
                if (ProgressionManager.Instance != null)
                {
                    // Tambahkan +1 energi ke penampung global
                    ProgressionManager.Instance.currentEnergy++;
                    
                    // Jaga agar energi tidak melampaui batas maksimal turn saat ini (opsional)
                    if (ProgressionManager.Instance.currentEnergy > ProgressionManager.Instance.maxEnergyThisTurn)
                    {
                        ProgressionManager.Instance.currentEnergy = ProgressionManager.Instance.maxEnergyThisTurn;
                    }

                    Debug.Log($"⚡ Tembakan Polos Sukses Mengenai Target! Bonus +1 Energy dimasukkan. Sisa saat ini: {ProgressionManager.Instance.currentEnergy}");
                }
            }
        }
    }

    private void TriggerExitLogic()
    {
        isOut = true;
        
        // Remove from the moving tracking list immediately so the turn manager 
        // doesn't wait for this coasting marble to stop before ending the turn.
        ArenaManager.Instance.allMarblesInArena.Remove(rb);

        // Ambil elemen dari kelereng ini sebelum dikembalikan
        MarbleElementHandler handler = GetComponent<MarbleElementHandler>();
        MarbleElementSO elementToSave = handler != null ? handler.activeElement : null;

        // Kirim data elemen (atau null jika polos) ke ArenaManager
        ArenaManager.Instance.AddAmmoFromOutsider(elementToSave);

        // Start the smooth delayed exit routine
        StartCoroutine(AnimateToPocket());
    }

    private IEnumerator AnimateToPocket()
    {
        // 1. DELAY: Do nothing here. Let the marble keep rolling naturally outside the line.
        yield return new WaitForSeconds(delayBeforeShrink);

        // 2. Clear physics after the rolling delay is over
        if (GetComponent<Collider2D>() != null) GetComponent<Collider2D>().enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // 3. Play the shrinking animation
        float timer = 0f;
        Vector3 startScale = transform.localScale;
        while (timer < shrinkDuration)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, timer / shrinkDuration);
            yield return null;
        }

        Destroy(gameObject);
    }
}