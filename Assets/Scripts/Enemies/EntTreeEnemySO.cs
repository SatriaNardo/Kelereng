using UnityEngine;

[CreateAssetMenu(fileName = "EntTree", menuName = "Enemies/Ent Tree Enemy")]
public class EntTreeEnemySO : EnemySO
{
    [Header("Wall Ring Skill")]
    [Tooltip("Optional prefab with EntTreeWallRing. If empty, one is created at runtime.")]
    public GameObject wallRingPrefab;
    [Range(0.5f, 1f)] public float wallRingRadiusMultiplier = 0.82f;
    public int wallSegmentCount = 26;
    public float wallSegmentLength = 0.42f;
    public float wallSegmentThickness = 0.14f;

    [Header("Root Vine Skill")]
    [Tooltip("Optional prefab with RootVinePull. If empty, one is created at runtime.")]
    public GameObject rootVinePrefab;
    [Tooltip("Radius around arena center where marbles get root-snared and pulled inward.")]
    public float vinePullRadius = 1.1f;
    public float vinePullStrength = 14f;

    [Header("Shared Physics")]
    public PhysicsMaterial2D hazardMaterial;

    protected override int EnemyActionSfxSlotCount => 2;

    public override void ExecuteEnemyAction(ArenaManager arena)
    {
        ClearEntTreeHazards();

        bool useWallRing = arena.EnemyActionCount % 2 == 1;
        if (useWallRing)
        {
            PlayActionSfx(arena, 0);
            SpawnWallRing(arena);
        }
        else
        {
            PlayActionSfx(arena, 1);
            SpawnRootVine(arena);
        }
    }

    private void SpawnWallRing(ArenaManager arena)
    {
        Vector2 center = arena.arenaCenter != null
            ? (Vector2)arena.arenaCenter.position
            : Vector2.zero;

        float ringRadius = arena.circleRadius * wallRingRadiusMultiplier;
        GameObject wallObject = wallRingPrefab != null
            ? Instantiate(wallRingPrefab, center, Quaternion.identity)
            : new GameObject("EntTreeWallRing");

        wallObject.transform.position = center;
        wallObject.transform.SetParent(arena.transform);

        EntTreeWallRing wallRing = wallObject.GetComponent<EntTreeWallRing>();
        if (wallRing == null)
        {
            wallRing = wallObject.AddComponent<EntTreeWallRing>();
        }

        wallRing.segmentLength = wallSegmentLength;
        wallRing.segmentThickness = wallSegmentThickness;
        wallRing.wallMaterial = hazardMaterial;
        wallRing.Initialize(center, ringRadius, wallSegmentCount, hazardMaterial);

        Debug.Log($"{enemyName} grew a wall ring at radius {ringRadius:0.00}.");
    }

    private void SpawnRootVine(ArenaManager arena)
    {
        Vector2 center = arena.arenaCenter != null
            ? (Vector2)arena.arenaCenter.position
            : Vector2.zero;

        GameObject vineObject = rootVinePrefab != null
            ? Instantiate(rootVinePrefab, center, Quaternion.identity)
            : new GameObject("RootVine");

        vineObject.transform.position = center;
        vineObject.transform.SetParent(arena.transform);

        RootVinePull rootVine = vineObject.GetComponent<RootVinePull>();
        if (rootVine == null)
        {
            rootVine = vineObject.AddComponent<RootVinePull>();
        }

        rootVine.Initialize(center, vinePullRadius, vinePullStrength);

        Debug.Log($"{enemyName} summoned root vines snaring marbles within radius {vinePullRadius:0.00}.");
    }

    public static void ClearEntTreeHazards()
    {
        EntTreeWallRing[] wallRings = Object.FindObjectsByType<EntTreeWallRing>(FindObjectsSortMode.None);
        foreach (EntTreeWallRing wallRing in wallRings)
        {
            Object.Destroy(wallRing.gameObject);
        }

        RootVinePull[] rootVines = Object.FindObjectsByType<RootVinePull>(FindObjectsSortMode.None);
        foreach (RootVinePull rootVine in rootVines)
        {
            Object.Destroy(rootVine.gameObject);
        }
    }
}
