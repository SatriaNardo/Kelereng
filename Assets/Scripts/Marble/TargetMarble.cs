using UnityEngine;
using System.Collections;

public class TargetMarble : MonoBehaviour
{
    [Header("Exit Animation Settings")]
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

        // Kalkulasi jarak flat 2D terhadap pusat lingkaran ring
        float distance = Vector2.Distance(transform.position, ArenaManager.Instance.arenaCenter.position);

        if (distance > ArenaManager.Instance.circleRadius)
        {
            TriggerExitLogic();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isOut) return;

        if (collision.gameObject.CompareTag("Gacoan") || collision.gameObject.name.Contains("Gacoan"))
        {
            MarbleElementHandler handler = collision.gameObject.GetComponent<MarbleElementHandler>();

            if (handler != null && handler.activeElement == null)
            {
                if (ProgressionManager.Instance != null)
                {
                    ProgressionManager.Instance.currentEnergy++;
                    if (ProgressionManager.Instance.currentEnergy > ProgressionManager.Instance.maxEnergyThisTurn)
                    {
                        ProgressionManager.Instance.currentEnergy = ProgressionManager.Instance.maxEnergyThisTurn;
                    }
                    Debug.Log($"⚡ Bonus +1 Energy! Total saat ini: {ProgressionManager.Instance.currentEnergy}");
                }
            }
        }
    }

    private void TriggerExitLogic()
    {
        isOut = true;
        
        // Hapus dari list pelacak pergerakan secara instan
        ArenaManager.Instance.allMarblesInArena.Remove(rb);

        // KIRIM KE SINI: Pintu terpusat ArenaManager untuk kalkulasi damage
        ArenaManager.Instance.OnMarbleExited(gameObject);

        StartCoroutine(AnimateToPocket());
    }

    private IEnumerator AnimateToPocket()
    {
        yield return new WaitForSeconds(delayBeforeShrink);

        if (GetComponent<Collider2D>() != null) GetComponent<Collider2D>().enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;

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