using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class MarbleLauncher : MonoBehaviour
{
    [Header("Launcher Settings")]
    public GameObject gacoanPrefab;
    public Transform launchPoint;        
    public float maxDragDistance = 2.5f;
    public float launchForceMultiplier = 15f;
    public LineRenderer trajectoryLine;

    [Header("Screen Constraints")]
    [Range(0.1f, 0.5f)] 
    public float bottomScreenPercentage = 0.3f; 

    // --- Private State Variables ---
    private Vector2 dragStartPos;
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
        isDragging = true;
        
        trajectoryLine.enabled = true;
        trajectoryLine.SetPosition(0, launchPoint.position);
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

        Vector2 launchDirection = -pullVector;
        trajectoryLine.SetPosition(1, (Vector2)launchPoint.position + launchDirection);
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
            currentGacoan.transform.position = launchPoint.position;
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

        // PERBAIKAN MUTLAK: Peluru aktif di ketapel selalu mengambil data di Indeks 0 saku tas
        if (chamber.Count > 0)
        {
            nextElement = chamber[0];
        }

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
        if (handler != null && handler.activeElement != null)
        {
            ProgressionManager.Instance.currentEnergy -= handler.activeElement.energyCost;
        }

        // Amunisi indeks 0 resmi dibuang dari antrean, saku otomatis bergeser maju rata kiri
        ProgressionManager.Instance.PopNextElement();

        Vector2 launchForce = -pullVector * launchForceMultiplier;
        currentGacoanRb.AddForce(launchForce, ForceMode2D.Impulse);

        ArenaManager.Instance.OnMarbleFlicked(currentGacoanRb, true);
        
        currentGacoan = null;
        currentGacoanRb = null;
    }

    // ========================================================
    // PERBAIKAN LOGIKA SWAP ELEMEN CADANGAN SEJATI (LIST SWAP)
    // ========================================================
    public void ForceSwapActiveMarble(int targetIndexInChamber)
    {
        if (isDragging || currentGacoan == null) return;

        List<MarbleElementSO> chamber = ProgressionManager.Instance.equippedChamber;
        
        // Pastikan indeks target aman di dalam batasan list saku cadangan
        if (targetIndexInChamber > 0 && targetIndexInChamber < chamber.Count)
        {
            // Tukar isi objek data asli di dalam saku global tas pemain
            // Menukar data Indeks 0 (ketapel saat ini) dengan Indeks Pilihan UI
            MarbleElementSO temp = chamber[0];
            chamber[0] = chamber[targetIndexInChamber];
            chamber[targetIndexInChamber] = temp;

            // Hancurkan kelereng polosan lama yang sedang diam standby di lapangan ketapel
            Destroy(currentGacoan);
            
            // Instansiasi ulang kelereng baru bermuatan data elemen hasil tukaran barumu
            PrepareNextShot();
            
            Debug.Log($"🔄 Swap Berhasil: Elemen ketapel ditukar dengan slot cadangan indeks ke-{targetIndexInChamber}!");
        }
    }
}