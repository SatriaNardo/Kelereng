using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro; // WAJIB: Ditambahkan untuk menggunakan TextMeshPro UI

public class ArenaManager : MonoBehaviour
{
    public static ArenaManager Instance;

    [Header("Game Settings")]
    public float circleRadius = 5f;
    public Transform arenaCenter;
    public int maxTurns = 10; // Maximum rounds allowed

    [Header("Ammunition")]
    public int currentAmmo = 4;
    public int enemyAmmo = 4; 

    [Header("UI Canvas Connections")]
    public TMP_Text playerAmmoText; // Drag your Player Ammo Text here
    public TMP_Text enemyAmmoText;  // Drag your Enemy Ammo Text here
    public TMP_Text turnText;       // Drag your Turn Counter Text here

    [Header("State Tracking")]
    public List<Rigidbody2D> allMarblesInArena = new List<Rigidbody2D>();
    public bool IsTurnActive { get; private set; } = false;
    public bool IsPlayerTurn { get; private set; } = true; 

    [Header("UI Connector")]
    public UIFightInventory uiFightInventory;
    
    private int currentTurnCount = 1; // Starts at turn 1
    private Rigidbody2D activeGacoan; 

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        currentAmmo = ProgressionManager.Instance.BASE_AMMO;
        UpdateAmmoUI();
    }


    public void OnMapGenerated()
    {
        UpdateAmmoUI();
    }

    public void OnMarbleFlicked(Rigidbody2D gacoanRb, bool isPlayer)
    {
        IsTurnActive = true;
        activeGacoan = gacoanRb; 

        if (isPlayer) currentAmmo--;
        else enemyAmmo--;

        UpdateAmmoUI();
        StartCoroutine(WaitForAllMarblesToStop());
    }

    public void AddAmmoFromOutsider(MarbleElementSO savedElement)
    {
        if (IsPlayerTurn)
        {
            currentAmmo++;
            // SINKRONISASI: Masukkan kembali elemen kelereng ini ke antrean paling belakang
            ProgressionManager.Instance.AddAmmoToChamber(savedElement);
            Debug.Log("Player scored! Ammo re-added to list. Current Ammo: " + currentAmmo);
        }
        else
        {
            enemyAmmo++;
            // (Optional) Jika musuh punya sistem list elemen sendiri, bisa dimasukkan di sini
            Debug.Log("Enemy scored! Enemy Ammo: " + enemyAmmo);
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
        if (activeGacoan != null)
        {
            float distance = Vector2.Distance(activeGacoan.transform.position, arenaCenter.position);

            if (distance > circleRadius)
            {
                MarbleElementHandler handler = activeGacoan.GetComponent<MarbleElementHandler>();
                MarbleElementSO savedElement = handler != null ? handler.activeElement : null;

                AddAmmoFromOutsider(savedElement);
                allMarblesInArena.Remove(activeGacoan);
                StartCoroutine(AnimateGacoanToPocket(activeGacoan.gameObject));
            }
            else
            {
                activeGacoan.gameObject.tag = "TargetMarble";
                if (activeGacoan.gameObject.GetComponent<TargetMarble>() == null)
                {
                    activeGacoan.gameObject.AddComponent<TargetMarble>();
                }
            }
            activeGacoan = null; 
        }

        // Quick check: If all marbles in the center are cleared, end early
        if (allMarblesInArena.Count == 0)
        {
            DetermineWinner("All marbles cleared!");
            return;
        }

        SwitchTurns();
    }

    private void SwitchTurns()
    {
        IsPlayerTurn = !IsPlayerTurn;

        // If it switches back to the Player, a new full round has started
        if (IsPlayerTurn)
        {
            currentTurnCount++;
            
            // Check if turn limit exceeded
            if (currentTurnCount > maxTurns)
            {
                DetermineWinner("Turn limit reached!");
                return;
            }
        }

        // Turn skipping safety checks
        if (IsPlayerTurn && currentAmmo <= 0) IsPlayerTurn = false; 
        if (!IsPlayerTurn && enemyAmmo <= 0) IsPlayerTurn = true;  

        // Check if both run out of ammo completely before turn 10
        if (currentAmmo <= 0 && enemyAmmo <= 0)
        {
            DetermineWinner("Both players out of ammunition!");
            return;
        }

        UpdateAmmoUI(); // Update UI to reflect turn count change
        Debug.Log(IsPlayerTurn ? "--- PLAYER TURN ---" : "--- ENEMY TURN ---");

        if (!IsPlayerTurn)
        {
            EnemyLauncher.Instance.RequestAIShot();
        }
    }

    private void DetermineWinner(string reason)
    {
        Debug.Log($"GAME OVER: {reason}");
        
        if (currentAmmo > enemyAmmo) 
        {
            // LOGIKA UTAMA: Hitung ekstra kelereng yang berhasil didapatkan
            int extraMarbles = currentAmmo - ProgressionManager.Instance.BASE_AMMO;
            
            if (extraMarbles > 0)
            {
                Debug.Log($"Menang! Kamu membawa pulang {extraMarbles} kelereng ekstra sebagai mata uang.");
                ProgressionManager.Instance.AddCurrency(extraMarbles);
            }
            else
            {
                Debug.Log("Menang! Tapi tidak ada kelereng ekstra yang dibawa pulang.");
            }
        }
        else
        {
            Debug.Log("Kalah! Tidak mendapatkan kelereng tambahan.");
        }

        // Naikkan hitungan floor dan kembali ke Scene Peta setelah pertarungan selesai
        ProgressionManager.Instance.currentFloor++;
        StartCoroutine(ReturnToMapSceneDelay());
    }

    private IEnumerator ReturnToMapSceneDelay()
    {
        yield return new WaitForSeconds(2f);
        // Pastikan nama scene peta kamu di Unity Build Settings sesuai dengan string ini
        UnityEngine.SceneManagement.SceneManager.LoadScene("MapScene"); 
    }

    private IEnumerator AnimateGacoanToPocket(GameObject gacoanObj)
    {
        if (gacoanObj == null) yield break;
        yield return new WaitForSeconds(0.5f);

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

    // ==========================================
    // UI CANVAS UPDATER
    // ==========================================
    private void UpdateAmmoUI()
    {
        if (playerAmmoText != null) 
            playerAmmoText.text = currentAmmo.ToString();

        if (enemyAmmoText != null) 
            enemyAmmoText.text = enemyAmmo.ToString();

        if (turnText != null) 
        {
            // Menampilkan angka giliran saat ini saja (misal: 1, 2, 3...)
            int displayTurn = Mathf.Min(currentTurnCount, maxTurns);
            turnText.text = displayTurn.ToString();
        }
    }
    public void OnTurnSwapped()
    {
        if (uiFightInventory != null)
        {
            uiFightInventory.RefreshAvailableElementsUI();
        }
    }
}