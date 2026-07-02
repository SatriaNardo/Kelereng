using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class ArenaManager : MonoBehaviour
{
    public static ArenaManager Instance;

    [Header("Game Settings")]
    public float circleRadius = 5f;
    public Transform arenaCenter;
    public int maxTurns = 10; 

    [Header("Ammunition")]
    public int currentAmmo = 4;

    [Header("UI Canvas Connections")]
    public TMP_Text playerAmmoText; 
    public TMP_Text enemyHPText;    
    public TMP_Text turnText;       

    [Header("State Tracking (2D Flat)")]
    public List<Rigidbody2D> allMarblesInArena = new List<Rigidbody2D>();
    public bool IsTurnActive { get; private set; } = false;
    public bool IsPlayerTurn { get; private set; } = false; 

    [Header("UI Connector")]
    public UIFightInventory uiFightInventory;

    [Header("Arena Target Marble Generator")]
    public MapGenerator mapGenerator;
    
    [Header("Enemy SO Connection")]
    public bool randomizeEnemyOnStart = true;
    public List<EnemySO> enemyPool = new List<EnemySO>();
    public EnemySO activeEnemyData;

    [Header("Enemy Visual")]
    public Transform enemyPlace;
    public EnemySpriteAnimator enemySpriteAnimator;

    [Header("Enemy Overlord Stats Dynamic")]
    public int enemyHP = 100;
    public int maxEnemyHP = 100;
    public int baseDamagePerMarble = 10;

    [Header("Victory Rewards")]
    public Vector2Int normalEnemyRewardRange = new Vector2Int(10, 30);
    public Vector2Int eliteEnemyRewardRange = new Vector2Int(40, 60);
    public Vector2Int bossEnemyRewardRange = new Vector2Int(70, 90);

    private int currentTurnCount = 0; 
    private Rigidbody2D activeGacoan; 
    private Rigidbody2D lastPlayerMarbleThatHitTarget;
    private bool activeGacoanHitTarget = false;
    private bool skipNextEnemyTurn = false;
    private GameObject activeSandAreaEffect;
    public bool phoenixAvailable = false;
    private bool isGameOver = false;
    private bool isVictoryPending = false;
    private bool isVictoryRecoveryTurn = false;

    public int EnemyActionCount { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (mapGenerator == null)
        {
            mapGenerator = Object.FindFirstObjectByType<MapGenerator>();
        }

        // Reset match energy configuration
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.ResetEnergyForNewMatch();
            Debug.Log($"⚡ Match Baru Dimulai! Energi global direset ke: {ProgressionManager.Instance.currentEnergy}");
        }

        currentAmmo = ProgressionManager.Instance.BASE_AMMO;
        isGameOver = false;
        isVictoryPending = false;
        isVictoryRecoveryTurn = false;
        currentTurnCount = 0;
        EnemyActionCount = 0;

        PickRandomEnemyForFight();

        // Initial Enemy HP Scaling
        if (activeEnemyData != null)
        {
            float kastaMultiplier = activeEnemyData.enemyType == EnemyType.Boss ? 3f : 
                                   (activeEnemyData.enemyType == EnemyType.Elite ? 1.5f : 1f);
            int floorBonus = (ProgressionManager.Instance.currentFloor - 1) * 20;
            enemyHP = Mathf.RoundToInt((activeEnemyData.baseHP + floorBonus) * kastaMultiplier);
            maxEnemyHP = enemyHP;
        }
        else
        {
            enemyHP = 100;
            maxEnemyHP = 100;
        }

        ApplyActiveEnemyVisual();
        UpdateAmmoUI();

        // Lock launcher and trigger enemy first action setup
        IsPlayerTurn = false; 
        Invoke("StartEnemyFirstTurn", 0.1f);

        LuckyDrawSO lucky = Resources.Load<LuckyDrawSO>("SO/Emblems/Fun/LuckyDraw");

        if (lucky != null)
        {
            lucky.RollReward();
        }
    }

    private void PickRandomEnemyForFight()
    {
        if (ProgressionManager.Instance != null)
        {
            EnemySO pendingEnemy = ProgressionManager.Instance.ConsumePendingFightEnemy();
            if (pendingEnemy != null)
            {
                activeEnemyData = pendingEnemy;
                Debug.Log($"Map-selected enemy loaded: {activeEnemyData.enemyName}");
                return;
            }
        }

        if (!randomizeEnemyOnStart) return;

        List<EnemySO> validEnemies = new List<EnemySO>();
        foreach (EnemySO enemy in enemyPool)
        {
            if (enemy != null && enemy.enemyType != EnemyType.Boss)
            {
                validEnemies.Add(enemy);
            }
        }

        if (validEnemies.Count == 0)
        {
            Debug.LogWarning("Enemy randomizer has no non-boss enemies assigned. Using activeEnemyData fallback.");
            return;
        }

        activeEnemyData = validEnemies[Random.Range(0, validEnemies.Count)];
        Debug.Log($"Random enemy selected: {activeEnemyData.enemyName}");
    }

    private void StartEnemyFirstTurn()
    {
        if (isGameOver) return;

        Debug.Log("--- MATCH STARTED: ENEMY TAKES FIRST TURN ---");
        
        if (activeEnemyData != null)
        {
            EnemyActionCount++;
            activeEnemyData.ExecuteEnemyAction(this); 
        }

        StartCoroutine(EnemyTurnEndDelay());
    }

    public void OnMapGenerated()
    {
        UpdateAmmoUI();
    }

    public bool HasAmmoForShot()
    {
        return GetAvailableShotCount() > 0;
    }

    public void OnMarbleFlicked(Rigidbody2D gacoanRb, bool isPlayer)
    {
        if (isGameOver) return;

        IsTurnActive = true;
        activeGacoan = gacoanRb; 
        activeGacoanHitTarget = false;
        activeGacoan.gameObject.tag = "PlayerMarble";

        if (activeGacoan.gameObject.GetComponent<PlayerMarbleHitTracker>() == null)
        {
            activeGacoan.gameObject.AddComponent<PlayerMarbleHitTracker>();
        }

        if (isPlayer) currentAmmo--;

        UpdateAmmoUI();
        StartCoroutine(WaitForAllMarblesToStop());
    }

    public void RegisterPlayerMarbleHitTarget(Rigidbody2D playerMarbleRb)
    {
        if (playerMarbleRb == activeGacoan)
        {
            activeGacoanHitTarget = true;
            lastPlayerMarbleThatHitTarget = playerMarbleRb;
        }
    }

    public void RequestSkipNextEnemyTurn()
    {
        skipNextEnemyTurn = true;
        Debug.Log("Enemy next turn will be skipped.");
    }

    public void ConfigurePlaygroundMode(int availableShots, int dummyEnemyHp)
    {
        StopAllCoroutines();

        isGameOver = false;
        isVictoryPending = false;
        isVictoryRecoveryTurn = false;
        skipNextEnemyTurn = false;
        activeGacoan = null;
        activeGacoanHitTarget = false;

        activeEnemyData = null;
        ApplyActiveEnemyVisual();
        enemyHP = Mathf.Max(1, dummyEnemyHp);
        maxEnemyHP = enemyHP;
        currentAmmo = Mathf.Max(1, availableShots);
        IsTurnActive = false;
        IsPlayerTurn = true;

        UpdateAmmoUI();
    }

    public void ExecutePlaygroundEnemyAction(EnemySO enemy)
    {
        if (enemy == null) return;

        isGameOver = false;
        isVictoryPending = false;
        isVictoryRecoveryTurn = false;
        activeEnemyData = enemy;
        ApplyActiveEnemyVisual();
        enemyHP = Mathf.Max(1, Mathf.Max(enemyHP, enemy.baseHP));
        maxEnemyHP = Mathf.Max(enemyHP, maxEnemyHP);
        EnemyActionCount++;
        enemy.ExecuteEnemyAction(this);
        IsPlayerTurn = true;
        IsTurnActive = false;
        UpdateAmmoUI();
    }

    public void ClearEnemyHazards()
    {
        GooPool[] activeGooPools = Object.FindObjectsByType<GooPool>(FindObjectsSortMode.None);
        foreach (GooPool gooPool in activeGooPools)
        {
            Destroy(gooPool.gameObject);
        }

        SmokeBomb[] activeSmokeBombs = Object.FindObjectsByType<SmokeBomb>(FindObjectsSortMode.None);
        foreach (SmokeBomb smokeBomb in activeSmokeBombs)
        {
            Destroy(smokeBomb.gameObject);
        }

        EntTreeEnemySO.ClearEntTreeHazards();
        CorruptedGolemEnemySO.ClearGolemHazards();

        Debug.Log($"Cleared enemy hazards. Goo: {activeGooPools.Length}, Smoke: {activeSmokeBombs.Length}");
    }

    public EnemySpriteAnimator GetEnemySpriteAnimator()
    {
        if (enemySpriteAnimator == null)
        {
            enemySpriteAnimator = Object.FindFirstObjectByType<EnemySpriteAnimator>();
        }

        return enemySpriteAnimator;
    }

    private void ApplyActiveEnemyVisual()
    {
        EnemySpriteAnimator animator = GetEnemySpriteAnimator();
        if (animator == null) return;

        animator.SetVisualScale(activeEnemyData != null ? activeEnemyData.enemyVisualScale : 1f);
        animator.ConfigureShadow(activeEnemyData);

        if (activeEnemyData is SlimeEnemySO slimeEnemy)
        {
            slimeEnemy.ApplyIdleAnimation(animator);
            return;
        }

        animator.SetStaticSprite(activeEnemyData != null ? activeEnemyData.enemySprite : null);
    }

    public float GetEnemyHpRatio()
    {
        if (maxEnemyHP <= 0) return 1f;
        return Mathf.Clamp01((float)enemyHP / maxEnemyHP);
    }

    public void ClearIceTrails()
    {
        IceTrailSpot[] activeIceSpots = Object.FindObjectsByType<IceTrailSpot>(FindObjectsSortMode.None);
        foreach (IceTrailSpot iceSpot in activeIceSpots)
        {
            Destroy(iceSpot.gameObject);
        }
    }

    public void OnMarbleExited(GameObject marbleObj)
    {
        if (isGameOver) return;

        Rigidbody2D rb = marbleObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            allMarblesInArena.Remove(rb);
        }

        if (marbleObj.CompareTag("PlayerMarble"))
        {
            MarbleElementHandler handler = marbleObj.GetComponent<MarbleElementHandler>();
            MarbleElementSO returnedElement = handler != null ? handler.activeElement : null;

            currentAmmo++;

            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.AddAmmoToChamber(returnedElement);
            }

            string elementName = returnedElement != null ? returnedElement.elementName : "Polos";
            Debug.Log($"🟢 Player Marble kembali ke saku sebagai {elementName}! Amunisi: {currentAmmo}");
        }
        else if (marbleObj.CompareTag("TargetMarble"))
        {
            MarbleElementHandler handler = marbleObj.GetComponent<MarbleElementHandler>();
            MarbleElementSO savedElement = handler != null ? handler.activeElement : null;

            int finalDamage = baseDamagePerMarble;

            // Buff dari elemen
            if (savedElement != null && savedElement.elementName == "Explosion")
            {
                finalDamage *= 2;
            }

            // Buff dari Emblem Crusher
            MarbleDamageBuff damageBuff = null;

            if (lastPlayerMarbleThatHitTarget != null)
            {
                damageBuff =
                    lastPlayerMarbleThatHitTarget.GetComponent<MarbleDamageBuff>();
            }

            if (damageBuff != null)
            {
                finalDamage = Mathf.RoundToInt(
                    finalDamage * damageBuff.damageMultiplier);

                Debug.Log($"💥 Crusher aktif! Damage menjadi {finalDamage}");
            }

            DamageEnemy(finalDamage); 
        }

        UpdateAmmoUI();
    }

    private IEnumerator WaitForAllMarblesToStop()
    {
        yield return new WaitForSeconds(0.5f); 

        while (IsTurnActive)
        {
            bool anyMarbleMoving = false;
            foreach (Rigidbody2D rb in allMarblesInArena)
            {
                if (rb != null && rb.linearVelocity.magnitude > 0.1f) 
                {
                    anyMarbleMoving = true;
                    break;
                }
            }

            if (!anyMarbleMoving)
            {
                IsTurnActive = false;
                EndTurnEvaluation();
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void EndTurnEvaluation()
    {
        if (isGameOver) return;

        if (activeGacoan != null)
        {
            float distance = Vector2.Distance(activeGacoan.transform.position, arenaCenter.position);

            if (distance > circleRadius)
            {
                if (activeGacoanHitTarget || isVictoryRecoveryTurn)
                {
                    OnMarbleExited(activeGacoan.gameObject);
                }
                else
                {
                    allMarblesInArena.Remove(activeGacoan);
                    UpdateAmmoUI();
                    Debug.Log("⚫ Player Marble keluar tanpa mengenai target di giliran normal. Amunisi hilang.");
                }

                StartCoroutine(AnimateGacoanToPocket(activeGacoan.gameObject));
            }
            else
            {
                activeGacoan.gameObject.tag = "PlayerMarble";
                if (activeGacoan.gameObject.GetComponent<RetrievablePlayerMarble>() == null)
                {
                    activeGacoan.gameObject.AddComponent<RetrievablePlayerMarble>();
                }
            }
            activeGacoan = null; 
        }

        if (enemyHP <= 0 || isVictoryPending)
        {
            if (isVictoryRecoveryTurn)
            {
                DetermineGameOver(true, "Enemy defeated after recovery turn!");
            }
            else
            {
                BeginVictoryRecoveryTurn("Enemy defeated!");
            }
            return;
        }

        // Cek apakah player kehabisan seluruh kelereng
        // Cek kehabisan ammo
        if (currentAmmo <= 0)
        {
            // Apakah player punya Phoenix?
            PhoenixSO phoenix = CurrentEmblemManager.Instance != null
                ? CurrentEmblemManager.Instance.GetPassiveEmblem<PhoenixSO>()
                : null;
            if (phoenix == null && CurrentEmblemManager.Instance != null)
            {
                phoenix = CurrentEmblemManager.Instance.currentEmblem as PhoenixSO;
            }

            if (phoenix != null)
            {
                currentAmmo += phoenix.bonusAmmo;

                UpdateAmmoUI();

                Debug.Log("🔥 Phoenix Activated! +1 Ammo");

                if (CurrentEmblemManager.Instance != null &&
                    CurrentEmblemManager.Instance.currentEmblem == phoenix)
                {
                    CurrentEmblemManager.Instance.ConsumeCurrentEmblem();
                }
                else if (CurrentEmblemManager.Instance != null)
                {
                    CurrentEmblemManager.Instance.ConsumePassiveEmblem(phoenix);
                }

                return; // Jangan lanjut ganti turn dulu
            }
            Debug.Log($"Ammo sekarang: {currentAmmo}");

            if (CurrentEmblemManager.Instance != null)
            {
                Debug.Log($"Current Emblem = {CurrentEmblemManager.Instance.currentEmblem}");
            }

            // Tidak punya Phoenix -> kalah
            DetermineGameOver(false, "Semua kelereng habis!");
            return;
        }

        SwitchTurns();
    }

    private void SwitchTurns()
    {
        if (isGameOver) return;

        if (isVictoryPending)
        {
            if (isVictoryRecoveryTurn)
            {
                DetermineGameOver(true, "Enemy defeated after recovery turn!");
            }
            else
            {
                BeginVictoryRecoveryTurn("Enemy defeated!");
            }

            return;
        }

        IsPlayerTurn = !IsPlayerTurn;

        if (IsPlayerTurn)
        {
            currentTurnCount++; 
            TopUpTargetMarblesForPlayerTurn();

            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.StartNewTurnEnergySetup(); 
            }

            if (!HasAnyAvailablePlayerShot())
            {
                DetermineGameOver(false, "Out of ammunition! You lost!");
                return;
            }
                
            UpdateAmmoUI();
            if (uiFightInventory != null)
            {
                uiFightInventory.RefreshAvailableElementsUI();
            }
            Debug.Log("--- GILIRAN PLAYER DIMULAI ---");
        }
        else
        {
            Debug.Log("--- GILIRAN MUSUH DIMULAI ---");

            if (skipNextEnemyTurn)
            {
                skipNextEnemyTurn = false;
                ClearEnemyHazards();
                Debug.Log("Sand effect skipped the enemy turn.");
                StartCoroutine(EnemyTurnEndDelay(true));
                return;
            }
            
            if (activeEnemyData != null)
            {
                EnemyActionCount++;
                activeEnemyData.ExecuteEnemyAction(this);
            }

            StartCoroutine(EnemyTurnEndDelay());
        }
    }

    private IEnumerator EnemyTurnEndDelay(bool clearSandAreaEffect = false)
    {
        yield return new WaitForSeconds(0.6f);

        if (clearSandAreaEffect)
        {
            ClearActiveSandAreaEffect();
        }

        SwitchTurns(); 
    }

    public void SpawnSandAreaEffectAtEnemyPlace(GameObject sandAreaEffectPrefab, float scale, int sortingOrder)
    {
        if (sandAreaEffectPrefab == null) return;

        ClearActiveSandAreaEffect();

        Transform targetPlace = GetEnemyPlace();
        Vector3 spawnPosition = targetPlace != null ? targetPlace.position : Vector3.zero;
        activeSandAreaEffect = Instantiate(sandAreaEffectPrefab, spawnPosition, Quaternion.identity);
        activeSandAreaEffect.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        ApplySortingOrder(activeSandAreaEffect, sortingOrder);
    }

    private Transform GetEnemyPlace()
    {
        if (enemyPlace != null) return enemyPlace;

        if (enemySpriteAnimator != null)
        {
            enemyPlace = enemySpriteAnimator.transform;
            return enemyPlace;
        }

        GameObject enemyPlaceObject = GameObject.Find("EnemyPlace");
        if (enemyPlaceObject != null)
        {
            enemyPlace = enemyPlaceObject.transform;
        }

        return enemyPlace;
    }

    private void ClearActiveSandAreaEffect()
    {
        if (activeSandAreaEffect == null) return;

        Destroy(activeSandAreaEffect);
        activeSandAreaEffect = null;
    }

    private void ApplySortingOrder(GameObject effectObject, int sortingOrder)
    {
        SpriteRenderer[] spriteRenderers = effectObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            spriteRenderer.sortingOrder = sortingOrder;
        }

        ParticleSystemRenderer[] particleRenderers = effectObject.GetComponentsInChildren<ParticleSystemRenderer>();
        foreach (ParticleSystemRenderer particleRenderer in particleRenderers)
        {
            particleRenderer.sortingOrder = sortingOrder + 1;
        }
    }

    private void TopUpTargetMarblesForPlayerTurn()
    {
        if (mapGenerator == null)
        {
            mapGenerator = Object.FindFirstObjectByType<MapGenerator>();
        }

        if (mapGenerator != null)
        {
            mapGenerator.TopUpMarblesForTurn();
        }
    }

    private bool HasAnyAvailablePlayerShot()
    {
        return GetAvailableShotCount() > 0;
    }

    private int GetAvailableShotCount()
    {
        if (ProgressionManager.Instance != null)
        {
            return ProgressionManager.Instance.equippedChamber.Count;
        }

        return currentAmmo;
    }

    private void DetermineGameOver(bool playerWon, string reason)
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log($"GAME OVER TRIGGERED: {reason}");
        
        if (playerWon)
        {
            Debug.Log("🏆 VICTORY!");
            GrantVictoryReward();

            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.currentFloor++;
            }

            StartCoroutine(LoadSceneDelay("MapScene"));
        }
        else
        {
            Debug.Log("💀 DEFEAT!");
            
            // ========================================================
            // BARU: HAPUS STRUKTUR PETA SAAT KALAH TOTAL
            // ========================================================
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.ResetProgressForNewGame();
            }

            StartCoroutine(LoadSceneDelay("GameOverScene")); 
        }
    }

    private void GrantVictoryReward()
    {
        if (ProgressionManager.Instance == null) return;

        Vector2Int rewardRange = GetRewardRangeForActiveEnemy();
        int minReward = Mathf.Min(rewardRange.x, rewardRange.y);
        int maxReward = Mathf.Max(rewardRange.x, rewardRange.y);
        int reward = Random.Range(minReward, maxReward + 1);

        ProgressionManager.Instance.AddCurrency(reward);

        string enemyTypeName = activeEnemyData != null ? activeEnemyData.enemyType.ToString() : EnemyType.Normal.ToString();
        Debug.Log($"Victory reward granted for {enemyTypeName} enemy: +{reward} currency.");
    }

    private Vector2Int GetRewardRangeForActiveEnemy()
    {
        if (activeEnemyData == null)
        {
            return normalEnemyRewardRange;
        }

        switch (activeEnemyData.enemyType)
        {
            case EnemyType.Elite:
                return eliteEnemyRewardRange;

            case EnemyType.Boss:
                return bossEnemyRewardRange;

            case EnemyType.Normal:
            default:
                return normalEnemyRewardRange;
        }
    }

    private IEnumerator LoadSceneDelay(string sceneName)
    {
        yield return new WaitForSeconds(1.5f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName); 
    }

    private IEnumerator AnimateGacoanToPocket(GameObject gacoanObj)
    {
        if (gacoanObj == null) yield break;
        yield return new WaitForSeconds(0.4f);

        gacoanObj.GetComponent<Collider2D>().enabled = false;
        Rigidbody2D rb = gacoanObj.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        float timer = 0f;
        Vector3 startScale = gacoanObj.transform.localScale;
        while (timer < 0.3f)
        {
            timer += Time.deltaTime;
            gacoanObj.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, timer / 0.3f);
            yield return null;
        }
        Destroy(gacoanObj);
    }

    public void UpdateAmmoUI()
    {
        currentAmmo = GetAvailableShotCount();

        if (playerAmmoText != null) 
            playerAmmoText.text = currentAmmo.ToString();

        if (enemyHPText != null)
            enemyHPText.text = activeEnemyData != null ? $"{activeEnemyData.enemyName} HP: {enemyHP}" : $"Enemy HP: {enemyHP}";

        if (turnText != null) 
        {
            int displayTurn = Mathf.Max(1, Mathf.Min(currentTurnCount, maxTurns));
            turnText.text = displayTurn.ToString();
        }
    }

    public void DamageEnemy(int damage)
    {
        if (isGameOver || isVictoryPending) return;

        enemyHP -= damage;
        Debug.Log($"💥 Musuh terkena damage! HP tersisa: {enemyHP}");
        
        if (enemyHP <= 0)
        {
            enemyHP = 0;
            BeginVictoryRecoveryTurn("Enemy defeated!");
        }
    }

    private void BeginVictoryRecoveryTurn(string reason)
    {
        if (isGameOver) return;
        if (isVictoryRecoveryTurn) return;

        isVictoryPending = true;
        enemyHP = 0;

        if (IsTurnActive)
        {
            Debug.Log($"{reason} Victory pending. Waiting for current marbles to stop before recovery turn.");
            return;
        }

        isVictoryRecoveryTurn = true;
        IsPlayerTurn = true;
        ClearEnemyHazards();
        TopUpTargetMarblesForPlayerTurn();

        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.StartNewTurnEnergySetup();
        }

        if (!HasAnyAvailablePlayerShot())
        {
            DetermineGameOver(true, $"{reason} No recovery shot available.");
            return;
        }

        UpdateAmmoUI();
        if (uiFightInventory != null)
        {
            uiFightInventory.RefreshAvailableElementsUI();
        }

        Debug.Log($"{reason} Recovery turn started so player can retrieve marbles before leaving.");
    }

    public void RecallAllPlayerMarbles()
    {
        List<Rigidbody2D> marbles =
            new List<Rigidbody2D>(allMarblesInArena);

        foreach (Rigidbody2D rb in marbles)
        {
            if (rb == null) continue;

            GameObject marble = rb.gameObject;

            if (marble.CompareTag("PlayerMarble"))
            {
                // Tambahkan ammo
                currentAmmo++;

                MarbleElementHandler handler =
                    marble.GetComponent<MarbleElementHandler>();

                if (handler != null)
                {
                    ProgressionManager.Instance
                        .AddAmmoToChamber(handler.activeElement);
                }

                // Hapus dari arena
                allMarblesInArena.Remove(rb);

                Destroy(marble);
            }
        }

        UpdateAmmoUI();

        Debug.Log("🌀 Recall Zone berhasil memanggil semua kelereng.");
    }
}
