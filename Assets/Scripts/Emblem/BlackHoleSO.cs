using UnityEngine;

[CreateAssetMenu(
    fileName = "BlackHole",
    menuName = "Emblems/Control/Black Hole")]
public class BlackHoleSO : BaseEmblemSO
{
    public GameObject blackHolePrefab;
    public float spawnRadiusFromPlayer = 1.5f;

    public override bool IsInstantSkill()
    {
        return true;
    }

    public override void Activate(GameObject marble)
        {
            PlayerMarbleHitTracker[] playerMarbles =
                Object.FindObjectsByType<PlayerMarbleHitTracker>(
                    FindObjectsSortMode.None);

            if (playerMarbles.Length == 0)
            {
                Debug.LogWarning("🌌 Tidak ada Player Marble di arena.");
                return;
            }

            // Black Hole selalu muncul di area tengah arena
            // Radius kecil agar posisi dekat pusat dan
            // tidak mendorong kelereng musuh keluar
            float safeRadius =
                ArenaManager.Instance.circleRadius * 0.2f;

            Vector2 spawnPosition =
                (Vector2)ArenaManager.Instance.arenaCenter.position +
                Random.insideUnitCircle * safeRadius;

            Instantiate(
                blackHolePrefab,
                spawnPosition,
                Quaternion.identity);

            Debug.Log("🌌 Black Hole Activated!");
        }
}