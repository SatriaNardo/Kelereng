using UnityEngine;

[CreateAssetMenu(fileName = "NewSlimeEnemy", menuName = "Enemies/Slime Enemy")]
public class SlimeEnemySO : EnemySO
{
    [Header("Slime Animation")]
    public Sprite[] idleAnimationSprites;
    public Sprite[] attackAnimationSprites;
    public float idleAnimationFramesPerSecond = 6f;
    public float attackAnimationFramesPerSecond = 10f;

    [Header("Slime Special Skill")]
    [Tooltip("Prefab genangan lendir yang memiliki Trigger Collider2D dan skrip GooPool.")]
    public GameObject gooPoolPrefab;
    public int spawnCountPerTurn = 2;

    public void ApplyIdleAnimation(EnemySpriteAnimator animator)
    {
        if (animator == null) return;

        animator.SetIdleAnimation(idleAnimationSprites, idleAnimationFramesPerSecond, enemySprite);
    }

    public void PlayAttackAnimation(EnemySpriteAnimator animator)
    {
        if (animator == null) return;

        animator.PlayAttackAnimation(attackAnimationSprites, attackAnimationFramesPerSecond);
    }

    public override void ExecuteEnemyAction(ArenaManager arena)
    {
        if (arena != null)
        {
            PlayAttackAnimation(arena.GetEnemySpriteAnimator());
        }

        if (gooPoolPrefab == null) return;

        // ========================================================
        // SAKTI: BERSIHKAN LENDIR LAMA SEBELUM SPAWN YANG BARU
        // ========================================================
        // Mencari semua objek GooPool yang saat ini aktif di dalam arena
        GooPool[] activeGools = Object.FindObjectsByType<GooPool>(FindObjectsSortMode.None);
        
        foreach (GooPool oldGoo in activeGools)
        {
            // Hancurkan lendir lama dari map biar gak numpuk kaku
            Object.Destroy(oldGoo.gameObject);
        }

        Debug.Log($"🧼 Lapangan dibersihkan! Menghapus {activeGools.Length} genangan lendir usang.");

        // ========================================================
        // SEBAR LENDIR BARU SECARA ACAK
        // ========================================================
        for (int i = 0; i < spawnCountPerTurn; i++)
        {
            // Ambil titik acak di dalam lingkaran arena (0.6f menjaga jarak aman agar tidak keluar ring)
            Vector2 randomOffset = Random.insideUnitCircle * (arena.circleRadius * 0.6f);
            Vector2 spawnPosition = (Vector2)arena.arenaCenter.position + randomOffset;

            // Spawn kolam lendir baru ke arena
            GameObject spawnedGoo = Instantiate(gooPoolPrefab, spawnPosition, Quaternion.identity);
            
            // Masukkan jadi anak ArenaManager agar rapi di Hierarchy
            spawnedGoo.transform.SetParent(arena.transform);

            Debug.Log($"🤢 {enemyName} memuntahkan Lendir Goo Baru di koordinat: {spawnPosition}!");
        }
    }
}
