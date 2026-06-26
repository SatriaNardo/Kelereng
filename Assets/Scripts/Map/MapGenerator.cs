using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("Prefab Settings")]
    public GameObject targetMarblePrefab; // Masukkan prefab kelereng target di sini

    [Header("Spawn Settings")]
    public int minMarbles = 5;            // Jumlah minimal kelereng di tengah
    public int maxMarbles = 12;           // Jumlah maksimal kelereng di tengah
    public float marbleRadius = 0.3f;     // Ukuran fisik kelereng untuk deteksi tumpukan
    public float spawnMargin = 0.5f;      // Jarak aman agar tidak spawn terlalu dekat dengan garis luar

    private int targetMarblesPerTurn;

    private void Start()
    {
        GenerateRandomMap();
    }

    public void GenerateRandomMap()
    {
        // Tentukan jumlah kelereng acak untuk ronde ini
        targetMarblesPerTurn = GetRandomTargetMarbleCount();

        int spawnedCount = SpawnMarbles(targetMarblesPerTurn);

        // Jalankan UI update atau log setelah selesai generate
        ArenaManager.Instance.OnMapGenerated();
        Debug.Log($"Berhasil generate {spawnedCount}/{targetMarblesPerTurn} kelereng di tengah lingkaran!");
    }

    /// <summary>
    /// Dipanggil setiap turn/giliran. Menambahkan kelereng baru di posisi acak
    /// TANPA menghapus kelereng yang sudah ada, dibatasi sampai jumlah target fight ini.
    /// Panggil method ini dari script turn/fight manager setiap kali turn baru dimulai.
    /// </summary>
    public void TopUpMarblesForTurn()
    {
        if (targetMarblesPerTurn <= 0)
        {
            targetMarblesPerTurn = Mathf.Max(minMarbles, 1);
        }

        // Bersihkan referensi null (kelereng yang sudah dihancurkan/dimakan)
        ArenaManager.Instance.allMarblesInArena.RemoveAll(rb => rb == null);

        int currentCount = CountActiveTargetMarbles();
        int needed = targetMarblesPerTurn - currentCount;

        if (needed <= 0)
        {
            Debug.Log("Jumlah target kelereng sudah sesuai target turn, tidak perlu spawn tambahan.");
            return;
        }

        int spawnedCount = SpawnMarbles(needed);

        ArenaManager.Instance.OnMapGenerated();
        Debug.Log($"Top-up turn: menambahkan {spawnedCount} kelereng baru (total sekarang: {currentCount + spawnedCount}/{targetMarblesPerTurn}).");
    }

    public bool HasActiveTargetMarbles()
    {
        return CountActiveTargetMarbles() > 0;
    }

    private int CountActiveTargetMarbles()
    {
        if (ArenaManager.Instance == null) return 0;

        int count = 0;
        foreach (Rigidbody2D rb in ArenaManager.Instance.allMarblesInArena)
        {
            if (rb != null && rb.CompareTag("TargetMarble"))
            {
                count++;
            }
        }

        return count;
    }

    private int GetRandomTargetMarbleCount()
    {
        int min = Mathf.Min(minMarbles, maxMarbles);
        int max = Mathf.Max(minMarbles, maxMarbles);
        return Random.Range(Mathf.Max(min, 1), Mathf.Max(max, 1) + 1);
    }

    /// <summary>
    /// Helper inti untuk spawn N kelereng di posisi acak yang valid (tidak overlap).
    /// Mengembalikan jumlah kelereng yang berhasil di-spawn.
    /// </summary>
    private int SpawnMarbles(int countToSpawn)
    {
        float radius = ArenaManager.Instance.circleRadius - spawnMargin;
        Vector2 center = ArenaManager.Instance.arenaCenter.position;

        int spawnedCount = 0;
        int maxAttempts = 100 * Mathf.Max(1, countToSpawn); // skala dengan jumlah yang diminta
        int attempts = 0;

        while (spawnedCount < countToSpawn && attempts < maxAttempts)
        {
            attempts++;

            // 1. Ambil koordinat acak di dalam unit circle (Radius 1) lalu kalikan dengan radius arena
            Vector2 randomPoint = center + (Random.insideUnitCircle * radius);

            // 2. Cek apakah di titik tersebut sudah ada kelereng lain
            Collider2D overlap = Physics2D.OverlapCircle(randomPoint, marbleRadius);

            if (overlap == null)
            {
                // 3. Jika kosong/aman, spawn kelereng baru
                GameObject newMarble = Instantiate(targetMarblePrefab, randomPoint, Quaternion.identity);

                // Pastikan tag-nya benar agar sistem logika kemarin berjalan
                newMarble.tag = "TargetMarble";

                // 4. Daftarkan langsung ke list ArenaManager
                if (newMarble.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    ArenaManager.Instance.allMarblesInArena.Add(rb);
                }

                spawnedCount++;
            }
        }

        return spawnedCount;
    }
}
