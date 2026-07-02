using UnityEngine;

[CreateAssetMenu(fileName = "NewGoblinThiefEnemy", menuName = "Enemies/Goblin Thief Enemy")]
public class GoblinThiefEnemySO : EnemySO
{
    [Header("Smoke Bomb Skill")]
    [Tooltip("Optional prefab with SmokeBomb. If empty, the enemy creates smoke at runtime.")]
    public GameObject smokeBombPrefab;
    public int spawnCountPerTurn = 2;
    public float smokeRadius = 1.6f;
    [Range(0f, 1f)] public float smokeOpacity = 1f;
    public int smokeSortingOrder = 100;

    protected override int EnemyActionSfxSlotCount => 1;

    public override void ExecuteEnemyAction(ArenaManager arena)
    {
        PlayActionSfx(arena, 0);

        SmokeBomb[] activeSmokeBombs = Object.FindObjectsByType<SmokeBomb>(FindObjectsSortMode.None);

        foreach (SmokeBomb oldSmoke in activeSmokeBombs)
        {
            Object.Destroy(oldSmoke.gameObject);
        }

        Debug.Log($"Smoke cleared. Removed {activeSmokeBombs.Length} old smoke bombs.");

        for (int i = 0; i < spawnCountPerTurn; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * (arena.circleRadius * 0.6f);
            Vector2 spawnPosition = (Vector2)arena.arenaCenter.position + randomOffset;

            GameObject spawnedSmoke = smokeBombPrefab != null
                ? Instantiate(smokeBombPrefab, spawnPosition, Quaternion.identity)
                : new GameObject("SmokeBomb");

            spawnedSmoke.transform.position = spawnPosition;
            spawnedSmoke.transform.SetParent(arena.transform);

            SmokeBomb smokeBomb = spawnedSmoke.GetComponent<SmokeBomb>();
            if (smokeBomb == null)
            {
                smokeBomb = spawnedSmoke.AddComponent<SmokeBomb>();
            }

            smokeBomb.radius = smokeRadius;
            smokeBomb.opacity = smokeOpacity;
            smokeBomb.sortingOrder = smokeSortingOrder;
            smokeBomb.RefreshVisual();

            Debug.Log($"{enemyName} dropped a smoke bomb at: {spawnPosition}");
        }
    }
}
