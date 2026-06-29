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

    [Header("Playground Settings")]
    public int ammoSlots = 12;
    public int targetCount = 12;
    public float visibleArenaRadius = 4.2f;
    public float targetSpawnRadius = 2.4f;
    public float targetSpacingRadius = 0.45f;
    public int dummyEnemyHp = 9999;
    public bool includeCommonMarble = true;

    private readonly List<GameObject> spawnedTargets = new List<GameObject>();
    private Vector2 menuScrollPosition;

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
