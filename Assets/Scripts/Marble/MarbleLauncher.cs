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

    [Header("Emblem Trajectory Preview")]
    [Tooltip("Matches Gacoan CircleCollider2D radius for ricochet prediction.")]
    public float marblePreviewRadius = 0.13f;
    [Tooltip("Used to match ricochet preview with in-game marble physics.")]
    public PhysicsMaterial2D marblePhysicsMaterial;

    // --- Private State Variables ---
    private Vector2 dragStartPos;
    private Vector2 launchOriginPos;
    private bool isDragging = false;
    private GameObject currentGacoan;
    private Rigidbody2D currentGacoanRb;

    // ==========================================
    // UNITY LIFECYCLE
    // ==========================================

    private void Awake()
    {
        if (marblePhysicsMaterial == null && gacoanPrefab != null)
        {
            CircleCollider2D prefabCollider = gacoanPrefab.GetComponent<CircleCollider2D>();
            if (prefabCollider != null)
            {
                marblePhysicsMaterial = prefabCollider.sharedMaterial;
            }
        }
    }

    private void Start()
    {
        PrepareNextShot();
    }

    private void Update()
    {
        if (!ArenaManager.Instance.IsTurnActive && 
            ArenaManager.Instance.IsPlayerTurn && 
            currentGacoan == null && 
            ArenaManager.Instance.HasAmmoForShot())
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
        UpdateTrajectoryLine(Vector2.zero);
    }

    private void ContinueDrag(Vector2 screenPosition)
    {
        if (currentGacoan == null) return;

        Vector2 currentTouchWorld = Camera.main.ScreenToWorldPoint(screenPosition);
        Vector2 pullVector = ClampPullVector(currentTouchWorld - dragStartPos);

        // Kelereng tetap diam; hanya garis bidik yang memanjang mengikuti tarikan jari.
        if (CurrentEmblemManager.Instance != null &&
            CurrentEmblemManager.Instance.currentEmblem is EagleEyeSO)
        {
            UpdateRicochetTrajectory(-pullVector);
        }
        else
        {
            UpdateTrajectoryLine(-pullVector);
        }
    }

    private void ReleaseAndFire(Vector2 screenPosition)
    {
        isDragging = false;
        trajectoryLine.enabled = false;

        if (currentGacoan == null) return;

        Vector2 currentTouchWorld = Camera.main.ScreenToWorldPoint(screenPosition);
        Vector2 pullVector = ClampPullVector(currentTouchWorld - dragStartPos);

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

        if (ProgressionManager.Instance != null)
        {
            finalLaunchForceMultiplier *= ProgressionManager.Instance.GetEmblemLaunchForceMultiplier();
        }

        Vector2 launchForce = -pullVector * finalLaunchForceMultiplier;
        if (activeElement != null)
        {
            activeElement.OnLaunch(currentGacoanRb);
        }

        currentGacoanRb.AddForce(launchForce, ForceMode2D.Impulse);

        if (CurrentEmblemManager.Instance != null &&
            CurrentEmblemManager.Instance.currentEmblem is CloneArmySO cloneSO)
        {
            SpawnCloneMarbles(
                cloneSO,
                launchForce,
                currentGacoan.transform.position);
        }

        if (CurrentEmblemManager.Instance != null &&
            CurrentEmblemManager.Instance.currentEmblem != null)
        {
            CurrentEmblemManager.Instance.currentEmblem.Activate(currentGacoan);
            CurrentEmblemManager.Instance.ConsumeCurrentEmblem();
        }

        ArenaManager.Instance.OnMarbleFlicked(currentGacoanRb, true);
        
        currentGacoan = null;
        currentGacoanRb = null;
    }

    private void SpawnCloneMarbles(
    CloneArmySO cloneSO,
    Vector2 originalForce,
    Vector2 spawnPosition)
    {
        for (int i = 0; i < cloneSO.cloneCount; i++)
        {
            float angle;

            if (cloneSO.cloneCount == 1)
                angle = cloneSO.spreadAngle;
            else
                angle = Mathf.Lerp(
                    -cloneSO.spreadAngle,
                    cloneSO.spreadAngle,
                    (float)i / (cloneSO.cloneCount - 1));

            Vector2 rotatedForce =
                Quaternion.Euler(0, 0, angle) * originalForce;

            GameObject clone =
                Instantiate(gacoanPrefab, spawnPosition, Quaternion.identity);

            Rigidbody2D cloneRb =
                clone.GetComponent<Rigidbody2D>();

            cloneRb.bodyType = RigidbodyType2D.Dynamic;
            cloneRb.AddForce(rotatedForce, ForceMode2D.Impulse);

            ArenaManager.Instance.allMarblesInArena.Add(cloneRb);

            clone.tag = "PlayerMarble";

            if (clone.GetComponent<PlayerMarbleHitTracker>() == null)
            {
                clone.AddComponent<PlayerMarbleHitTracker>();
            }
            ArenaManager.Instance.allMarblesInArena.Add(cloneRb);
            ArenaManager.Instance.OnMarbleFlicked(cloneRb, false);

            Debug.Log($"👥 Clone spawned with angle {angle}");
        }
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

    private float GetEffectiveMaxDragDistance()
    {
        float bonus = ProgressionManager.Instance != null
            ? ProgressionManager.Instance.GetGuideLineRangeBonus()
            : 0f;

        return maxDragDistance + bonus;
    }

    private Vector2 ClampPullVector(Vector2 pullVector)
    {
        float effectiveMaxDrag = GetEffectiveMaxDragDistance();
        if (pullVector.magnitude > effectiveMaxDrag)
        {
            pullVector = pullVector.normalized * effectiveMaxDrag;
        }

        return pullVector;
    }

    private void UpdateTrajectoryLine(Vector2 launchDirection)
    {
        Collider2D ignoreCollider = currentGacoan != null
            ? currentGacoan.GetComponent<Collider2D>()
            : null;

        int ricochetPreviewCount = ProgressionManager.Instance != null
            ? ProgressionManager.Instance.GetRicochetPreviewCount()
            : 0;

        List<Vector2> points = TrajectoryPredictor.BuildTrajectoryPoints(
            launchOriginPos,
            launchDirection,
            launchDirection.magnitude,
            marblePreviewRadius,
            ricochetPreviewCount,
            ignoreCollider,
            GetMarbleCollidersForPrediction(ignoreCollider),
            marblePhysicsMaterial);

        trajectoryLine.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            trajectoryLine.SetPosition(i, points[i]);
        }
    }
    private void UpdateRicochetTrajectory(Vector2 launchDirection)
    {
        Collider2D ignoreCollider = currentGacoan != null
            ? currentGacoan.GetComponent<Collider2D>()
            : null;

        // Eagle Eye selalu melihat 1 pantulan tambahan
        int ricochetPreviewCount = 1;

        List<Vector2> points = TrajectoryPredictor.BuildTrajectoryPoints(
            launchOriginPos,
            launchDirection,
            launchDirection.magnitude,
            marblePreviewRadius,
            ricochetPreviewCount,
            ignoreCollider,
            GetMarbleCollidersForPrediction(ignoreCollider),
            marblePhysicsMaterial);

        trajectoryLine.positionCount = points.Count;

        for (int i = 0; i < points.Count; i++)
        {
            trajectoryLine.SetPosition(i, points[i]);
        }
    }

    private List<CircleCollider2D> GetMarbleCollidersForPrediction(Collider2D ignoreCollider)
    {
        List<CircleCollider2D> marbleColliders = new List<CircleCollider2D>();

        if (ArenaManager.Instance == null)
        {
            return marbleColliders;
        }

        foreach (Rigidbody2D marbleBody in ArenaManager.Instance.allMarblesInArena)
        {
            if (marbleBody == null)
            {
                continue;
            }

            CircleCollider2D circleCollider = marbleBody.GetComponent<CircleCollider2D>();
            if (circleCollider == null || circleCollider == ignoreCollider || !circleCollider.enabled)
            {
                continue;
            }

            marbleColliders.Add(circleCollider);
        }

        return marbleColliders;
    }
}
