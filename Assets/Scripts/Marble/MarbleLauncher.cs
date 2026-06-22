using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class MarbleLauncher : MonoBehaviour
{
    [Header("Launcher Settings")]
    public GameObject gacoanPrefab;
    [Tooltip("Titik standby awal kelereng sebelum layar disentuh.")]
    public Transform launchPoint;        
    public float maxDragDistance = 2.5f;
    public float launchForceMultiplier = 15f;
    public LineRenderer trajectoryLine;

    [Header("Screen Constraints")]
    [Range(0.1f, 0.5f)] 
    public float bottomScreenPercentage = 0.3f; 

    // --- Private State Variables ---
    private Vector2 dragStartPos;
    private Vector2 launchOriginPos;
    private bool isDragging = false;
    private GameObject currentGacoan;
    private Rigidbody2D currentGacoanRb;

    // ==========================================
    // UNITY LIFECYCLE
    // ==========================================

    private void Start()
    {
        PrepareNextShot();
    }

    private void Update()
    {
        if (!ArenaManager.Instance.IsTurnActive && 
            ArenaManager.Instance.IsPlayerTurn && 
            currentGacoan == null && 
            ArenaManager.Instance.currentAmmo > 0)
        {
            PrepareNextShot();
        }

        HandleInput();
    }

    // ==========================================
    // CORE INPUT ROUTER
    // ==========================================

    private void HandleInput()
    {
        if (Pointer.current == null) return;

        Vector2 screenPosition = Pointer.current.position.ReadValue();

        if (Pointer.current.press.wasPressedThisFrame)
            TryStartDrag(screenPosition);
        else if (Pointer.current.press.isPressed && isDragging)
            ContinueDrag(screenPosition);
        else if (Pointer.current.press.wasReleasedThisFrame && isDragging)
            ReleaseAndFire(screenPosition);
    }

    // ==========================================
    // MODULAR INPUT PHASES
    // ==========================================

    private void TryStartDrag(Vector2 screenPosition)
    {
        // Batasi sentuhan awal hanya boleh di area bawah layar
        if (screenPosition.y > Screen.height * bottomScreenPercentage) return;
        if (currentGacoan == null) return;

        MarbleElementHandler handler = currentGacoan.GetComponent<MarbleElementHandler>();
        if (handler != null && handler.activeElement != null)
        {
            int cost = handler.activeElement.energyCost;
            if (ProgressionManager.Instance.currentEnergy < cost)
            {
                Debug.LogWarning($"⚠️ Tembakan Terkunci! Kurang Energi.");
                return; 
            }
        }

        dragStartPos = Camera.main.ScreenToWorldPoint(screenPosition);
        launchOriginPos = dragStartPos;
        currentGacoan.transform.position = launchOriginPos;
        
        isDragging = true;
        trajectoryLine.enabled = true;
        
        // Garis bidik selalu dimulai dari posisi kelereng, bukan posisi jari.
        trajectoryLine.SetPosition(0, launchOriginPos);
        trajectoryLine.SetPosition(1, launchOriginPos);
    }

    private void ContinueDrag(Vector2 screenPosition)
    {
        if (currentGacoan == null) return;

        Vector2 currentTouchWorld = Camera.main.ScreenToWorldPoint(screenPosition);
        Vector2 pullVector = currentTouchWorld - dragStartPos;

        if (pullVector.magnitude > maxDragDistance)
        {
            pullVector = pullVector.normalized * maxDragDistance;
        }

        // Kelereng tetap diam; hanya garis bidik yang memanjang mengikuti tarikan jari.
        Vector2 launchDirection = -pullVector;
        trajectoryLine.SetPosition(0, launchOriginPos);
        trajectoryLine.SetPosition(1, launchOriginPos + launchDirection);
    }

    private void ReleaseAndFire(Vector2 screenPosition)
    {
        isDragging = false;
        trajectoryLine.enabled = false;

        if (currentGacoan == null) return;

        Vector2 currentTouchWorld = Camera.main.ScreenToWorldPoint(screenPosition);
        Vector2 pullVector = currentTouchWorld - dragStartPos;

        if (pullVector.magnitude > maxDragDistance)
        {
            pullVector = pullVector.normalized * maxDragDistance;
        }

        if (pullVector.magnitude > 0.2f)
        {
            ExecutePhysicsLaunch(pullVector);
        }
        else
        {
            // Jika tarikan terlalu pendek (cancel shot), kelereng tetap di titik awal tembakan.
            currentGacoan.transform.position = launchOriginPos;
        }
    }

    // ==========================================
    // HELPER METHODS (SPAWN & PHYSICS)
    // ==========================================

    private void PrepareNextShot()
    {
        if (gacoanPrefab == null || launchPoint == null) return;

        List<MarbleElementSO> chamber = ProgressionManager.Instance.equippedChamber;
        MarbleElementSO nextElement = null;

        if (chamber.Count > 0)
        {
            nextElement = chamber[0];
        }

        // Spawn awal ditaruh di launchPoint (bisa kamu sembunyikan offscreen/di belakang jika mau)
        currentGacoan = Instantiate(gacoanPrefab, launchPoint.position, Quaternion.identity);
        currentGacoanRb = currentGacoan.GetComponent<Rigidbody2D>();

        MarbleElementHandler handler = currentGacoan.GetComponent<MarbleElementHandler>();
        if (handler != null)
        {
            handler.activeElement = nextElement;
            
            SpriteRenderer sr = currentGacoan.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = nextElement != null ? nextElement.elementColor : Color.white;
            }
        }

        currentGacoanRb.bodyType = RigidbodyType2D.Kinematic;
        currentGacoanRb.linearVelocity = Vector2.zero;
        currentGacoanRb.angularVelocity = 0f;
    }

    private void ExecutePhysicsLaunch(Vector2 pullVector)
    {
        currentGacoanRb.bodyType = RigidbodyType2D.Dynamic;
        ArenaManager.Instance.allMarblesInArena.Add(currentGacoanRb);

        MarbleElementHandler handler = currentGacoan.GetComponent<MarbleElementHandler>();
        MarbleElementSO activeElement = null;
        if (handler != null && handler.activeElement != null)
        {
            activeElement = handler.activeElement;
            ProgressionManager.Instance.currentEnergy -= activeElement.energyCost;
        }

        ProgressionManager.Instance.PopNextElement();

        float finalLaunchForceMultiplier = activeElement != null
            ? activeElement.GetLaunchForceMultiplier(launchForceMultiplier)
            : launchForceMultiplier;

        Vector2 launchForce = -pullVector * finalLaunchForceMultiplier;
        if (activeElement != null)
        {
            activeElement.OnLaunch(currentGacoanRb);
        }

        currentGacoanRb.AddForce(launchForce, ForceMode2D.Impulse);

        ArenaManager.Instance.OnMarbleFlicked(currentGacoanRb, true);
        
        currentGacoan = null;
        currentGacoanRb = null;
    }

    public void ForceSwapActiveMarble(int targetIndexInChamber)
    {
        if (isDragging || currentGacoan == null) return;

        List<MarbleElementSO> chamber = ProgressionManager.Instance.equippedChamber;
        
        if (targetIndexInChamber > 0 && targetIndexInChamber < chamber.Count)
        {
            // Swap item antrean tas
            MarbleElementSO temp = chamber[0];
            chamber[0] = chamber[targetIndexInChamber];
            chamber[targetIndexInChamber] = temp;

            Destroy(currentGacoan);
            PrepareNextShot();
            
            Debug.Log($"🔄 Swap Berhasil! Elemen ketapel ditukar dengan slot cadangan indeks ke-{targetIndexInChamber}!");
        }
    }
}
