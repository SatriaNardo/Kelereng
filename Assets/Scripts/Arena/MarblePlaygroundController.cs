using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MarblePlaygroundController : MonoBehaviour
{
    [Header("Scene References")]
    public ArenaManager arenaManager;
    public MarbleLauncher marbleLauncher;
    public Transform arenaCenter;
    public GameObject targetMarblePrefab;

    [Header("Test Elements")]
    public List<MarbleElementSO> testElements = new List<MarbleElementSO>();
    public int selectedElementIndex = 0;

    [Header("Test Enemies")]
    public List<EnemySO> testEnemies = new List<EnemySO>();
    public int selectedEnemyIndex = 0;

    [Header("Test Emblems")]
    public List<BaseEmblemSO> testEmblems = new List<BaseEmblemSO>();
    public int selectedEmblemIndex = 0;
    public bool includeNoEmblem = true;

    [Header("Playground Settings")]
    public int ammoSlots = 12;
    public int targetCount = 12;
    public float visibleArenaRadius = 4.2f;
    public float targetSpawnRadius = 2.4f;
    public float targetSpacingRadius = 0.45f;
    public int dummyEnemyHp = 9999;
    public bool includeCommonMarble = true;

    [Header("Fight Scene Physics Match")]
    public bool matchFightScenePhysics = true;
    public float fightSceneArenaRadius = 1.8f;
    public float fightSceneMaxDragDistance = 2f;
    public float fightSceneLaunchForceMultiplier = 5f;
    [Range(0.1f, 0.5f)] public float fightSceneBottomScreenPercentage = 0.3f;

    private readonly List<GameObject> spawnedTargets = new List<GameObject>();
    private Vector2 menuScrollPosition;
    private CurrentEmblemManager emblemManager;

    private void Start()
    {
        if (arenaManager == null)
        {
            arenaManager = FindFirstObjectByType<ArenaManager>();
        }

        if (marbleLauncher == null)
        {
            marbleLauncher = FindFirstObjectByType<MarbleLauncher>();
        }

        if (arenaCenter == null && arenaManager != null)
        {
            arenaCenter = arenaManager.arenaCenter;
        }

        EnsureEmblemManager();
        ConfigurePlayground();
        ResetTargets();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.rKey.wasPressedThisFrame)
        {
            ResetPlayground();
        }

        if (keyboard.cKey.wasPressedThisFrame)
        {
            ClearPlayerMarbles();
        }

        if (keyboard.tKey.wasPressedThisFrame)
        {
            SpawnTarget();
        }

        if (keyboard.eKey.wasPressedThisFrame)
        {
            ExecuteSelectedEnemyAttack();
        }

        if (keyboard.hKey.wasPressedThisFrame)
        {
            ClearHazards();
        }

        if (keyboard.mKey.wasPressedThisFrame)
        {
            ApplySelectedEmblem();
        }

        if (keyboard.nKey.wasPressedThisFrame)
        {
            ClearSelectedEmblem();
        }

        Key[] numberKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
        };

        for (int i = 0; i < Mathf.Min(numberKeys.Length, GetChoiceCount()); i++)
        {
            if (keyboard[numberKeys[i]].wasPressedThisFrame)
            {
                SelectChoice(i);
            }
        }

        KeepPlaygroundReady();
    }

    private void OnGUI()
    {
        const int width = 240;
        GUILayout.BeginArea(new Rect(12, 12, width, Screen.height - 24), GUI.skin.box);
        menuScrollPosition = GUILayout.BeginScrollView(menuScrollPosition);
        GUILayout.Label("Marble Playground");
        GUILayout.Label("Drag from bottom screen to shoot.");
        GUILayout.Space(6);

        for (int i = 0; i < GetChoiceCount(); i++)
        {
            string prefix = i == selectedElementIndex ? "> " : "";
            if (GUILayout.Button($"{prefix}{i + 1}. {GetChoiceName(i)}"))
            {
                SelectChoice(i);
            }
        }

        GUILayout.Space(8);
        GUILayout.Label("Enemy Attacks");
        for (int i = 0; i < testEnemies.Count; i++)
        {
            string prefix = i == selectedEnemyIndex ? "> " : "";
            if (GUILayout.Button($"{prefix}{GetEnemyName(i)}"))
            {
                SelectEnemy(i);
            }
        }

        if (GUILayout.Button("Use Enemy Attack (E)"))
        {
            ExecuteSelectedEnemyAttack();
        }

        if (GUILayout.Button("Clear Hazards (H)"))
        {
            ClearHazards();
        }

        GUILayout.Space(8);
        GUILayout.Label("Emblems");
        for (int i = 0; i < GetEmblemChoiceCount(); i++)
        {
            string prefix = i == selectedEmblemIndex ? "> " : "";
            if (GUILayout.Button($"{prefix}{GetEmblemChoiceName(i)}"))
            {
                SelectEmblem(i);
            }
        }

        if (GUILayout.Button("Equip Emblem (M)"))
        {
            ApplySelectedEmblem();
        }

        if (GUILayout.Button("Clear Emblem (N)"))
        {
            ClearSelectedEmblem();
        }

        GUILayout.Space(8);
        if (GUILayout.Button("Reset Arena (R)"))
        {
            ResetPlayground();
        }

        if (GUILayout.Button("Clear Player Marbles (C)"))
        {
            ClearPlayerMarbles();
        }

        if (GUILayout.Button("Spawn Target (T)"))
        {
            SpawnTarget();
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void ConfigurePlayground()
    {
        ApplyFightScenePhysicsSettings();

        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.BASE_AMMO = Mathf.Max(1, ammoSlots);
            ProgressionManager.Instance.maxEnergyThisTurn = 99;
            ProgressionManager.Instance.currentEnergy = 99;
        }

        if (arenaManager != null)
        {
            arenaManager.circleRadius = Mathf.Max(0.5f, visibleArenaRadius);
            arenaManager.ConfigurePlaygroundMode(ammoSlots, dummyEnemyHp);
        }

        FillChamberWithSelectedElement();
        ApplySelectedEmblem();
    }

    private void ApplyFightScenePhysicsSettings()
    {
        if (!matchFightScenePhysics) return;

        visibleArenaRadius = fightSceneArenaRadius;

        if (marbleLauncher != null)
        {
            marbleLauncher.maxDragDistance = fightSceneMaxDragDistance;
            marbleLauncher.launchForceMultiplier = fightSceneLaunchForceMultiplier;
            marbleLauncher.bottomScreenPercentage = fightSceneBottomScreenPercentage;
        }
    }

    private void KeepPlaygroundReady()
    {
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.maxEnergyThisTurn = 99;
            ProgressionManager.Instance.currentEnergy = 99;

            if (ProgressionManager.Instance.equippedChamber.Count <= 1)
            {
                FillChamberWithSelectedElement();
            }
        }

        if (arenaManager != null && !arenaManager.IsTurnActive)
        {
            arenaManager.ConfigurePlaygroundMode(ammoSlots, dummyEnemyHp);
        }
    }

    private void SelectChoice(int choiceIndex)
    {
        selectedElementIndex = Mathf.Clamp(choiceIndex, 0, Mathf.Max(0, GetChoiceCount() - 1));
        FillChamberWithSelectedElement();
        ApplySelectedElementToStandbyMarble();
    }

    private void SelectEnemy(int enemyIndex)
    {
        selectedEnemyIndex = Mathf.Clamp(enemyIndex, 0, Mathf.Max(0, testEnemies.Count - 1));
    }

    private void SelectEmblem(int emblemIndex)
    {
        selectedEmblemIndex = Mathf.Clamp(emblemIndex, 0, Mathf.Max(0, GetEmblemChoiceCount() - 1));
        ApplySelectedEmblem();
    }

    private void ApplySelectedEmblem()
    {
        EnsureEmblemManager();
        if (emblemManager == null) return;

        BaseEmblemSO selectedEmblem = GetSelectedEmblem();
        if (selectedEmblem == null)
        {
            emblemManager.ClearPlaygroundEmblems();
            Debug.Log("Playground emblem cleared.");
            return;
        }

        if (selectedEmblem.IsPassiveEmblem())
        {
            emblemManager.currentEmblem = null;
            emblemManager.SetPlaygroundPassiveEmblem(selectedEmblem);
            Debug.Log($"Playground passive emblem active: {GetEmblemName(selectedEmblem)}");
            return;
        }

        emblemManager.SetPlaygroundPassiveEmblem(null);
        emblemManager.SelectEmblem(selectedEmblem);
        Debug.Log($"Playground active emblem equipped: {GetEmblemName(selectedEmblem)}");
    }

    private void ClearSelectedEmblem()
    {
        EnsureEmblemManager();
        if (emblemManager != null)
        {
            emblemManager.ClearPlaygroundEmblems();
        }

        selectedEmblemIndex = 0;
    }

    private void ExecuteSelectedEnemyAttack()
    {
        if (arenaManager == null || testEnemies.Count == 0) return;

        EnemySO selectedEnemy = GetSelectedEnemy();
        if (selectedEnemy == null) return;

        arenaManager.ExecutePlaygroundEnemyAction(selectedEnemy);
    }

    private EnemySO GetSelectedEnemy()
    {
        if (selectedEnemyIndex < 0 || selectedEnemyIndex >= testEnemies.Count) return null;
        return testEnemies[selectedEnemyIndex];
    }

    private string GetEnemyName(int enemyIndex)
    {
        EnemySO enemy = enemyIndex >= 0 && enemyIndex < testEnemies.Count ? testEnemies[enemyIndex] : null;
        if (enemy == null) return "Empty Enemy";
        return string.IsNullOrEmpty(enemy.enemyName) ? enemy.name : enemy.enemyName;
    }

    private void FillChamberWithSelectedElement()
    {
        if (ProgressionManager.Instance == null) return;

        ProgressionManager.Instance.equippedChamber.Clear();
        MarbleElementSO selectedElement = GetSelectedElement();
        int safeAmmoSlots = Mathf.Max(1, ammoSlots);
        for (int i = 0; i < safeAmmoSlots; i++)
        {
            ProgressionManager.Instance.equippedChamber.Add(selectedElement);
        }
    }

    private MarbleElementSO GetSelectedElement()
    {
        int elementIndex = includeCommonMarble ? selectedElementIndex - 1 : selectedElementIndex;
        if (elementIndex < 0 || elementIndex >= testElements.Count) return null;
        return testElements[elementIndex];
    }

    private int GetChoiceCount()
    {
        return testElements.Count + (includeCommonMarble ? 1 : 0);
    }

    private int GetEmblemChoiceCount()
    {
        return testEmblems.Count + (includeNoEmblem ? 1 : 0);
    }

    private string GetChoiceName(int choiceIndex)
    {
        if (includeCommonMarble && choiceIndex == 0)
        {
            return "Common";
        }

        int elementIndex = includeCommonMarble ? choiceIndex - 1 : choiceIndex;
        if (elementIndex < 0 || elementIndex >= testElements.Count || testElements[elementIndex] == null)
        {
            return "Empty";
        }

        return testElements[elementIndex].elementName;
    }

    private BaseEmblemSO GetSelectedEmblem()
    {
        int emblemIndex = includeNoEmblem ? selectedEmblemIndex - 1 : selectedEmblemIndex;
        if (emblemIndex < 0 || emblemIndex >= testEmblems.Count) return null;
        return testEmblems[emblemIndex];
    }

    private string GetEmblemChoiceName(int choiceIndex)
    {
        if (includeNoEmblem && choiceIndex == 0)
        {
            return "No Emblem";
        }

        int emblemIndex = includeNoEmblem ? choiceIndex - 1 : choiceIndex;
        if (emblemIndex < 0 || emblemIndex >= testEmblems.Count)
        {
            return "Empty Emblem";
        }

        return GetEmblemName(testEmblems[emblemIndex]);
    }

    private string GetEmblemName(BaseEmblemSO emblem)
    {
        if (emblem == null) return "No Emblem";
        if (!string.IsNullOrWhiteSpace(emblem.emblemName)) return emblem.emblemName;
        return emblem.name;
    }

    private void EnsureEmblemManager()
    {
        if (emblemManager != null) return;

        emblemManager = CurrentEmblemManager.Instance;
        if (emblemManager != null) return;

        GameObject managerObject = new GameObject("CurrentEmblemManager");
        emblemManager = managerObject.AddComponent<CurrentEmblemManager>();
    }

    private void ApplySelectedElementToStandbyMarble()
    {
        MarbleElementSO selectedElement = GetSelectedElement();
        GameObject[] gacoans = GameObject.FindGameObjectsWithTag("Gacoan");
        foreach (GameObject gacoan in gacoans)
        {
            if (gacoan == null) continue;

            Rigidbody2D rb = gacoan.GetComponent<Rigidbody2D>();
            if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic) continue;

            MarbleElementHandler handler = gacoan.GetComponent<MarbleElementHandler>();
            if (handler != null)
            {
                handler.SetActiveElement(selectedElement);
            }
        }
    }

    private void ResetPlayground()
    {
        ClearAllMarbles();
        ConfigurePlayground();
        ResetTargets();
    }

    private void ResetTargets()
    {
        for (int i = spawnedTargets.Count - 1; i >= 0; i--)
        {
            if (spawnedTargets[i] != null)
            {
                Destroy(spawnedTargets[i]);
            }
        }

        spawnedTargets.Clear();

        if (arenaManager != null)
        {
            arenaManager.allMarblesInArena.RemoveAll(rb => rb == null || rb.CompareTag("TargetMarble"));
        }

        for (int i = 0; i < targetCount; i++)
        {
            SpawnTarget();
        }
    }

    private void SpawnTarget()
    {
        if (targetMarblePrefab == null || arenaCenter == null) return;

        Vector2 position = arenaCenter.position;
        bool foundSpot = false;
        for (int attempt = 0; attempt < 80; attempt++)
        {
            Vector2 candidate = (Vector2)arenaCenter.position + Random.insideUnitCircle * targetSpawnRadius;
            if (Physics2D.OverlapCircle(candidate, targetSpacingRadius) != null) continue;

            position = candidate;
            foundSpot = true;
            break;
        }

        if (!foundSpot)
        {
            position = (Vector2)arenaCenter.position + Random.insideUnitCircle * targetSpawnRadius;
        }

        GameObject target = Instantiate(targetMarblePrefab, position, Quaternion.identity);
        target.tag = "TargetMarble";
        spawnedTargets.Add(target);

        if (arenaManager != null && target.TryGetComponent(out Rigidbody2D rb))
        {
            arenaManager.allMarblesInArena.Add(rb);
        }
    }

    private void ClearPlayerMarbles()
    {
        DestroyTaggedMarbles("PlayerMarble");
        DestroyTaggedMarbles("Gacoan");
        ConfigurePlayground();
    }

    private void ClearHazards()
    {
        if (arenaManager != null)
        {
            arenaManager.ClearEnemyHazards();
        }
    }

    private void ClearAllMarbles()
    {
        ClearHazards();
        DestroyTaggedMarbles("PlayerMarble");
        DestroyTaggedMarbles("Gacoan");
        DestroyTaggedMarbles("TargetMarble");

        if (arenaManager != null)
        {
            arenaManager.allMarblesInArena.Clear();
        }
    }

    private void DestroyTaggedMarbles(string tagName)
    {
        GameObject[] marbles = GameObject.FindGameObjectsWithTag(tagName);
        foreach (GameObject marble in marbles)
        {
            Destroy(marble);
        }
    }
}
