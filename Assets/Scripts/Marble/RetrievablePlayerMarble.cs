using UnityEngine;

public class RetrievablePlayerMarble : MonoBehaviour
{
    private bool isTriggered = false;

    private void Update()
    {
        if (isTriggered || ArenaManager.Instance == null) return;

        // Cek apakah kelereng player yang stuck ini sukses terdorong keluar dari lingkaran ring
        float distance = Vector2.Distance(transform.position, ArenaManager.Instance.arenaCenter.position);

        if (distance > ArenaManager.Instance.circleRadius)
        {
            isTriggered = true;
            
            // Picu fungsi pengembalian amunisi di ArenaManager tanpa memberi damage ke Boss
            ArenaManager.Instance.OnMarbleExited(gameObject);
            
            // Hancurkan objek secara halus masuk saku
            StartCoroutine(AnimateToPocket());
        }
    }

    private System.Collections.IEnumerator AnimateToPocket()
    {
        if (GetComponent<Collider2D>() != null) GetComponent<Collider2D>().enabled = false;
        
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        float timer = 0f;
        Vector3 startScale = transform.localScale;
        while (timer < 0.3f)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, timer / 0.3f);
            yield return null;
        }

        Destroy(gameObject);
    }
}