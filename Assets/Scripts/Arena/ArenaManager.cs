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
    
    [Header("Enemy SO Connection")]
    public bool randomizeEnemyOnStart = true;
    public List<EnemySO> enemyPool = new List<EnemySO>();
    public EnemySO activeEnemyData;

    [Header("Enemy Overlord Stats Dynamic")]
    public int enemyHP = 100;
    public int baseDamagePerMarble = 10;

    [Header("Victory Rewards")]
    public Vector2Int normalEnemyRewardRange = new Vector2Int(10, 30);
    public Vector2Int eliteEnemyRewardRange = new Vector2Int(40, 60);
    public Vector2Int bossEnemyRewardRange = new Vector2Int(70, 90);

    private int currentTurnCount = 0; 
    private Rigidbody2D activeGacoan; 
    private bool activeGacoanHitTarget = false;
    private bool skipNextEnemyTurn = false;
    private bool isGameOver = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Reset match energy configuration
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.ResetEnergyForNewMatch();
            Debug.Log($"⚡ Match Baru Dimulai! Energi global direset ke: {ProgressionManager.Instance.currentEnergy}");
        }

        currentAmmo = ProgressionManager.Instance.BASE_AMMO;
        isGameOver = false;
        currentTurnCount = 0; 

        PickRandomEnemyForFight();

        // Initial Enemy HP Scaling
        if (activeEnemyData != null)
        {
            float kastaMultiplier = activeEnemyData.enemyType == EnemyType.Boss ? 3f : 
                                   (activeEnemyData.enemyType == EnemyType.Elite ? 1.5f : 1f);
            int floorBonus = (ProgressionManager.Instance.currentFloor - 1) * 20;
            enemyHP = Mathf.RoundToInt((activeEnemyData.baseHP + floorBonus) * kastaMultiplier);
        }
        else
        {
            enemyHP = 100;
        }

        UpdateAmmoUI();

        // Lock launcher and trigger enemy first action setup
        IsPlayerTurn = false; 
        Invoke("StartEnemyFirstTurn", 0.1f);
    }

    private void PickRandomEnemyForFight()
    {
        if (!randomizeEnemyOnStart) return;

        List<EnemySO> validEnemies = new List<EnemySO>();
        foreach (EnemySO enemy in enemyPool)
        {
            if (enemy != null)
            {
                validEnemies.Add(enemy);
            }
        }

        if (validEnemies.Count == 0)
        {
            Debug.LogWarning("Enemy randomizer has no enemies assigned. Using activeEnemyData fallback.");
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
            activeEnemyData.ExecuteEnemyAction(this); 
        }

        StartCoroutine(EnemyTurnEndDelay());
    }

    public void OnMapGenerated()
    {
        UpdateAmmoUI();
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
        }
    }

    public void RequestSkipNextEnemyTurn()
    {
        skipNextEnemyTurn = true;
        Debug.Log("Enemy next turn will be skipped.");
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

        Debug.Log($"Cleared enemy hazards. Goo: {activeGooPools.Length}, Smoke: {activeSmokeBombs.Length}");
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
            if (savedElement != null && savedElement.elementName == "Explosion") 
            {
                finalDamage *= 2; 
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
                if (activeGacoanHitTarget)
                {
                    OnMarbleExited(activeGacoan.gameObject);
                }
                else
                {
                    allMarblesInArena.Remove(activeGacoan);
                    UpdateAmmoUI();
                    Debug.Log("⚫ Player Marble keluar tanpa mengenai target. Amunisi hilang.");
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

        bool targetMarblesLeft = false;
        foreach (Rigidbody2D rb in allMarblesInArena)
        {
            if (rb != null && rb.CompareTag("TargetMarble"))
            {
                targetMarblesLeft = true;
                break;
            }
        }

        if (!targetMarblesLeft)
        {
            DetermineGameOver(true, "All target marbles cleared!");
            return;
        }

        SwitchTurns();
    }

    private void SwitchTurns()
    {
        if (isGameOver) return;

        IsPlayerTurn = !IsPlayerTurn;

        if (IsPlayerTurn)
        {
            currentTurnCount++; 
            ClearIceTrails();

            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.StartNewTurnEnergySetup(); 
            }

            if (currentAmmo <= 0)
            {
                DetermineGameOver(false, "Out of ammunition! You lost!");
                return;
            }
                
            if (currentTurnCount > maxTurns)
            {
                DetermineGameOver(false, "Turn limit reached! Defeat!");
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
                Debug.Log("Dust effect skipped the enemy turn.");
                StartCoroutine(EnemyTurnEndDelay());
                return;
            }
            
            if (activeEnemyData != null)
            {
                activeEnemyData.ExecuteEnemyAction(this);
            }

            StartCoroutine(EnemyTurnEndDelay());
        }
    }

    private IEnumerator EnemyTurnEndDelay()
    {
        yield return new WaitForSeconds(0.6f);
        SwitchTurns(); 
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

    private void UpdateAmmoUI()
    {
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
        if (isGameOver) return; 

        enemyHP -= damage;
        Debug.Log($"💥 Musuh terkena damage! HP tersisa: {enemyHP}");
        
        if (enemyHP <= 0)
        {
            enemyHP = 0;
            DetermineGameOver(true, "Enemy defeated!");
        }
    }
}
