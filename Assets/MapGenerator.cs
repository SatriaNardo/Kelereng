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

    private void Start()
    {
        GenerateRandomMap();
    }

    public void GenerateRandomMap()
    {
        // Ambil referensi dari ArenaManager yang sudah ada
        float radius = ArenaManager.Instance.circleRadius - spawnMargin;
        Vector2 center = ArenaManager.Instance.arenaCenter.position;

        // Tentukan jumlah kelereng acak untuk ronde ini
        int totalMarblesToSpawn = Random.Range(minMarbles, maxMarbles + 1);
        
        int spawnedCount = 0;
        int maxAttempts = 100; // Batas percobaan biar Unity tidak crash/freeze jika area penuh
        int attempts = 0;

        while (spawnedCount < totalMarblesToSpawn && attempts < maxAttempts)
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

        // Jalankan UI update atau log setelah selesai generate
        ArenaManager.Instance.OnMapGenerated();
        Debug.Log($"Berhasil generate {spawnedCount} kelereng di tengah lingkaran!");
    }
}