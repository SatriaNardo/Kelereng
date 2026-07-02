using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CorruptedGolem", menuName = "Enemies/Corrupted Golem Boss")]
public class CorruptedGolemEnemySO : EnemySO
{
    private enum GolemMove
    {
        Quicksand,
        RockFormation,
        CorruptedSmoke,
        SideChainVines
    }

    [Header("Phase Threshold")]
    [Range(0.1f, 0.9f)] public float phaseTwoHpRatio = 0.5f;

    [Header("Quicksand")]
    public GameObject quicksandPrefab;
    public float quicksandRadius = 0.45f;
    public Sprite quicksandSprite;

    [Header("Rock Formation")]
    public GameObject rockFormationPrefab;
    public int rockCount = 5;
    public float rockClusterRadius = 0.55f;
    public float rockSpawnRadiusMultiplier = 0.55f;
    public Sprite rockSprite;

    [Header("Corrupted Smoke")]
    public GameObject corruptedSmokePrefab;
    public float smokeRadiusMultiplier = 0.72f;
    public Sprite corruptedSmokeSprite;

    [Header("Chain Vines (Phase 2 Extra)")]
    [Tooltip("Optional prefab with RootVinePull. If empty, one is created at runtime.")]
    public GameObject chainVinePrefab;
    [Tooltip("How far left/right from arena center each chain vine spawns.")]
    [Range(0.1f, 0.95f)] public float chainSideOffsetMultiplier = 0.58f;
    public float chainVinePullRadius = 1.15f;
    public float chainVinePullStrength = 14f;

    [Header("Shared Physics")]
    public PhysicsMaterial2D hazardMaterial;

    protected override int EnemyActionSfxSlotCount => 4;

    public override void ExecuteEnemyAction(ArenaManager arena)
    {
        ClearGolemHazards();

        bool isPhaseTwo = arena.GetEnemyHpRatio() <= phaseTwoHpRatio;
        int moveCount = isPhaseTwo ? 2 : 1;
        List<GolemMove> selectedMoves = PickRandomMoves(moveCount, isPhaseTwo);

        foreach (GolemMove move in selectedMoves)
        {
            ExecuteMove(move, arena);
        }

        Debug.Log($"{enemyName} used {selectedMoves.Count} move(s) at HP {arena.GetEnemyHpRatio():P0}.");
    }

    private static List<GolemMove> PickRandomMoves(int count, bool includePhaseTwoMoves)
    {
        List<GolemMove> pool = new List<GolemMove>
        {
            GolemMove.Quicksand,
            GolemMove.RockFormation,
            GolemMove.CorruptedSmoke
        };

        if (includePhaseTwoMoves)
        {
            pool.Add(GolemMove.SideChainVines);
        }

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (pool[i], pool[swapIndex]) = (pool[swapIndex], pool[i]);
        }

        if (count >= pool.Count)
        {
            return pool;
        }

        return pool.GetRange(0, count);
    }

    private void ExecuteMove(GolemMove move, ArenaManager arena)
    {
        PlayActionSfx(arena, (int)move);

        switch (move)
        {
            case GolemMove.Quicksand:
                SpawnQuicksand(arena);
                break;
            case GolemMove.RockFormation:
                SpawnRockFormation(arena);
                break;
            case GolemMove.CorruptedSmoke:
                SpawnCorruptedSmoke(arena);
                break;
            case GolemMove.SideChainVines:
                SpawnSideChainVines(arena);
                break;
        }
    }

    private Vector2 GetArenaCenter(ArenaManager arena)
    {
        return arena.arenaCenter != null ? (Vector2)arena.arenaCenter.position : Vector2.zero;
    }

    private void SpawnQuicksand(ArenaManager arena)
    {
        Vector2 center = GetArenaCenter(arena);
        QuicksandPool quicksand = SpawnHazard<QuicksandPool>(quicksandPrefab, "QuicksandPool", arena.transform, center);
        quicksand.Initialize(center, quicksandRadius, quicksandSprite);

        Debug.Log($"{enemyName} opened quicksand at arena center.");
    }

    private void SpawnRockFormation(ArenaManager arena)
    {
        Vector2 center = GetArenaCenter(arena);
        Vector2 formationCenter = center;
        float formationAngle = Random.Range(0f, 180f);

        RockFormation formation = SpawnHazard<RockFormation>(rockFormationPrefab, "RockFormation", arena.transform, formationCenter);
        formation.Initialize(formationCenter, rockCount, rockClusterRadius, formationAngle, hazardMaterial, rockSprite);

        Debug.Log($"{enemyName} raised a cross rock formation at {formationCenter} with angle {formationAngle:0}.");
    }

    private void SpawnCorruptedSmoke(ArenaManager arena)
    {
        Vector2 center = GetArenaCenter(arena);
        float smokeRadius = arena.circleRadius * smokeRadiusMultiplier;

        CorruptedSmokeZone smoke = SpawnHazard<CorruptedSmokeZone>(corruptedSmokePrefab, "CorruptedSmokeZone", arena.transform, center);
        smoke.Initialize(center, smokeRadius, corruptedSmokeSprite);

        Debug.Log($"{enemyName} flooded the arena with corrupted smoke.");
    }

    private void SpawnSideChainVines(ArenaManager arena)
    {
        Vector2 center = GetArenaCenter(arena);
        float sideOffset = arena.circleRadius * chainSideOffsetMultiplier;

        SpawnChainVine(arena.transform, center + Vector2.left * sideOffset, "LeftChainVine");
        SpawnChainVine(arena.transform, center + Vector2.right * sideOffset, "RightChainVine");

        Debug.Log($"{enemyName} chained the arena from both sides.");
    }

    private void SpawnChainVine(Transform parent, Vector2 position, string vineName)
    {
        RootVinePull chainVine = SpawnHazard<RootVinePull>(chainVinePrefab, vineName, parent, position);
        chainVine.Initialize(position, chainVinePullRadius, chainVinePullStrength);
    }

    private T SpawnHazard<T>(GameObject prefab, string fallbackName, Transform parent, Vector2 position) where T : Component
    {
        GameObject hazardObject = prefab != null
            ? Object.Instantiate(prefab, position, Quaternion.identity, parent)
            : new GameObject(fallbackName);

        hazardObject.name = fallbackName;
        hazardObject.transform.SetParent(parent);
        hazardObject.transform.position = position;

        T hazard = hazardObject.GetComponent<T>();
        if (hazard == null)
        {
            hazard = hazardObject.AddComponent<T>();
        }

        return hazard;
    }

    public static void ClearGolemHazards()
    {
        QuicksandPool[] quicksandPools = Object.FindObjectsByType<QuicksandPool>(FindObjectsSortMode.None);
        foreach (QuicksandPool quicksand in quicksandPools)
        {
            Object.Destroy(quicksand.gameObject);
        }

        RockFormation[] rockFormations = Object.FindObjectsByType<RockFormation>(FindObjectsSortMode.None);
        foreach (RockFormation formation in rockFormations)
        {
            Object.Destroy(formation.gameObject);
        }

        CorruptedSmokeZone[] smokeZones = Object.FindObjectsByType<CorruptedSmokeZone>(FindObjectsSortMode.None);
        foreach (CorruptedSmokeZone smoke in smokeZones)
        {
            Object.Destroy(smoke.gameObject);
        }

        RootVinePull[] chainVines = Object.FindObjectsByType<RootVinePull>(FindObjectsSortMode.None);
        foreach (RootVinePull chainVine in chainVines)
        {
            Object.Destroy(chainVine.gameObject);
        }

        ChainAnchorPoint[] chainAnchors = Object.FindObjectsByType<ChainAnchorPoint>(FindObjectsSortMode.None);
        foreach (ChainAnchorPoint anchor in chainAnchors)
        {
            Object.Destroy(anchor.gameObject);
        }
    }
}
