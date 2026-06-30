using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class MarbleLauncher : MonoBehaviour
{
    [Header("Launcher Settings")]
    public GameObject gacoanPrefab;
    [Tooltip("Titik standby awal kelereng sebelum layar disentuh.")]
    public Transform launchPoint;        
    public float maxDragDistance = 2.5f;
    public float launchForceMultiplier = 15f;
    public LineRenderer trajectoryLine;

    [Header("Aim Preview")]
    public Sprite aimArrowSprite;
    public Sprite standbyArrowSprite;
    public Color aimDotColor = new Color(1f, 1f, 1f, 0.85f);
    public float aimDotSpacing = 0.22f;
    public float aimDotSize = 0.08f;
    public float aimArrowSize = 0.05f;
    public float aimArrowEndOffset = 0.12f;
    public float aimArrowRotationOffset = -90f;
    public Vector2 standbyArrowOffset = new Vector2(0f, 0.45f);
    public float standbyArrowSize = 0.06f;
    public int aimPreviewSortingOrder = 55;

    [Header("Screen Constraints")]
    [Range(0.1f, 0.5f)] 
    public float bottomScreenPercentage = 0.3f;
    public bool showDragZoneGuide = true;
    public Color dragZoneGuideColor = new Color(1f, 1f, 1f, 0.45f);
    public float dragZoneGuideWidth = 0.035f;
    public float dragZoneSidePadding = 0.35f;

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
    private LineRenderer dragZoneGuideLine;
    private Transform aimPreviewRoot;
    private Sprite aimDotSprite;
    private readonly List<SpriteRenderer> aimDots = new List<SpriteRenderer>();
    private SpriteRenderer aimArrowRenderer;
    private SpriteRenderer standbyArrowRenderer;
    private int lastScreenWidth;
    private int lastScreenHeight;

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
        SetupDragZoneGuide();
        SetupAimPreview();
        PrepareNextShot();
    }

    private void Update()
    {
        UpdateDragZoneGuideIfNeeded();

        if (!ArenaManager.Instance.IsTurnActive && 
            ArenaManager.Instance.IsPlayerTurn && 
            currentGacoan == null && 
            ArenaManager.Instance.HasAmmoForShot())
        {
            PrepareNextShot();
        }

        HandleInput();
        UpdateStandbyAimArrow();
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
        if (IsPointerOverUI()) return;

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
        if (trajectoryLine != null) trajectoryLine.enabled = false;
        SetStandbyArrowVisible(false);
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
        HideAimPreview();
        if (trajectoryLine != null) trajectoryLine.enabled = false;

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
            UpdateStandbyAimArrow();
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
            handler.SetActiveElement(nextElement);
        }

        currentGacoanRb.bodyType = RigidbodyType2D.Kinematic;
        currentGacoanRb.linearVelocity = Vector2.zero;
        currentGacoanRb.angularVelocity = 0f;

        UpdateStandbyAimArrow();
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
        UpdateStandbyAimArrow();
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

    public bool ForceSwapActiveMarble(int targetIndexInChamber)

    {
        if (isDragging || currentGacoan == null) return false;

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
            return true;
        }

        return false;
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
        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
            trajectoryLine.positionCount = 0;
        }

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

        DrawDottedAimPreview(points, launchDirection);
    }

    private void SetupAimPreview()
    {
        GameObject previewObject = new GameObject("AimPreview");
        previewObject.transform.SetParent(transform);
        aimPreviewRoot = previewObject.transform;

        aimDotSprite = CreateAimDotSprite();
        aimArrowRenderer = CreateSpriteRenderer("AimArrow", aimArrowSprite, false);
        standbyArrowRenderer = CreateSpriteRenderer("StandbyArrow", standbyArrowSprite, false);

        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
        }
    }

    private SpriteRenderer CreateSpriteRenderer(string objectName, Sprite sprite, bool visible)
    {
        GameObject rendererObject = new GameObject(objectName);
        rendererObject.transform.SetParent(aimPreviewRoot != null ? aimPreviewRoot : transform);

        SpriteRenderer renderer = rendererObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = aimPreviewSortingOrder;
        renderer.enabled = visible && sprite != null;
        return renderer;
    }

    private Sprite CreateAimDotSprite()
    {
        const int size = 8;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.38f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private void DrawDottedAimPreview(List<Vector2> points, Vector2 launchDirection)
    {
        HideAimDots();

        if (!isDragging || points == null || points.Count < 2 || launchDirection.sqrMagnitude <= 0.0001f)
        {
            SetAimArrowVisible(false);
            return;
        }

        int dotIndex = 0;
        float spacing = Mathf.Max(0.03f, aimDotSpacing);
        float carryDistance = 0f;

        for (int i = 1; i < points.Count; i++)
        {
            Vector2 segmentStart = points[i - 1];
            Vector2 segmentEnd = points[i];
            Vector2 segment = segmentEnd - segmentStart;
            float segmentLength = segment.magnitude;
            if (segmentLength <= 0.001f) continue;

            Vector2 segmentDirection = segment / segmentLength;
            float distanceOnSegment = Mathf.Max(0f, spacing - carryDistance);

            while (distanceOnSegment <= segmentLength)
            {
                SpriteRenderer dot = GetAimDot(dotIndex);
                dot.transform.position = segmentStart + segmentDirection * distanceOnSegment;
                dot.transform.localScale = Vector3.one * aimDotSize;
                dot.color = aimDotColor;
                dot.sortingOrder = aimPreviewSortingOrder;
                dot.enabled = true;
                dotIndex++;

                distanceOnSegment += spacing;
            }

            carryDistance = Mathf.Max(0f, segmentLength - (distanceOnSegment - spacing));
        }

        PositionAimArrow(points);
    }

    private SpriteRenderer GetAimDot(int index)
    {
        while (aimDots.Count <= index)
        {
            SpriteRenderer dot = CreateSpriteRenderer($"AimDot_{aimDots.Count}", aimDotSprite, false);
            aimDots.Add(dot);
        }

        return aimDots[index];
    }

    private void PositionAimArrow(List<Vector2> points)
    {
        if (aimArrowRenderer == null || aimArrowSprite == null || points.Count < 2)
        {
            SetAimArrowVisible(false);
            return;
        }

        Vector2 end = points[points.Count - 1];
        Vector2 previous = points[points.Count - 2];
        Vector2 direction = end - previous;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            SetAimArrowVisible(false);
            return;
        }

        direction.Normalize();
        aimArrowRenderer.sprite = aimArrowSprite;
        aimArrowRenderer.transform.position = end + direction * aimArrowEndOffset;
        aimArrowRenderer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + aimArrowRotationOffset);
        aimArrowRenderer.transform.localScale = Vector3.one * aimArrowSize;
        aimArrowRenderer.sortingOrder = aimPreviewSortingOrder + 1;
        SetAimArrowVisible(true);
    }

    private void HideAimPreview()
    {
        HideAimDots();
        SetAimArrowVisible(false);
    }

    private void HideAimDots()
    {
        foreach (SpriteRenderer dot in aimDots)
        {
            if (dot != null) dot.enabled = false;
        }
    }

    private void SetAimArrowVisible(bool visible)
    {
        if (aimArrowRenderer != null)
        {
            aimArrowRenderer.enabled = visible && aimArrowRenderer.sprite != null;
        }
    }

    private void UpdateStandbyAimArrow()
    {
        if (isDragging || currentGacoan == null)
        {
            SetStandbyArrowVisible(false);
            return;
        }

        if (standbyArrowRenderer == null || standbyArrowSprite == null)
        {
            SetStandbyArrowVisible(false);
            return;
        }

        standbyArrowRenderer.sprite = standbyArrowSprite;
        Vector2 standbyPosition = (Vector2)currentGacoan.transform.position + standbyArrowOffset;
        standbyArrowRenderer.transform.position = new Vector3(standbyPosition.x, standbyPosition.y, currentGacoan.transform.position.z);
        standbyArrowRenderer.transform.rotation = Quaternion.identity;
        standbyArrowRenderer.transform.localScale = Vector3.one * standbyArrowSize;
        standbyArrowRenderer.sortingOrder = aimPreviewSortingOrder + 1;
        SetStandbyArrowVisible(true);
    }

    private void SetStandbyArrowVisible(bool visible)
    {
        if (standbyArrowRenderer != null)
        {
            standbyArrowRenderer.enabled = visible && standbyArrowRenderer.sprite != null;
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

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void SetupDragZoneGuide()
    {
        if (!showDragZoneGuide) return;

        GameObject guideObject = new GameObject("DragZoneGuideLine");
        guideObject.transform.SetParent(transform);
        dragZoneGuideLine = guideObject.AddComponent<LineRenderer>();
        dragZoneGuideLine.positionCount = 2;
        dragZoneGuideLine.useWorldSpace = true;
        dragZoneGuideLine.startWidth = dragZoneGuideWidth;
        dragZoneGuideLine.endWidth = dragZoneGuideWidth;
        dragZoneGuideLine.startColor = dragZoneGuideColor;
        dragZoneGuideLine.endColor = dragZoneGuideColor;
        dragZoneGuideLine.sortingOrder = 50;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            dragZoneGuideLine.material = new Material(shader);
        }

        RefreshDragZoneGuideLine();
    }

    private void UpdateDragZoneGuideIfNeeded()
    {
        if (!showDragZoneGuide)
        {
            if (dragZoneGuideLine != null)
            {
                dragZoneGuideLine.enabled = false;
            }
            return;
        }

        if (dragZoneGuideLine == null)
        {
            SetupDragZoneGuide();
            return;
        }

        dragZoneGuideLine.enabled = true;
        dragZoneGuideLine.startColor = dragZoneGuideColor;
        dragZoneGuideLine.endColor = dragZoneGuideColor;
        dragZoneGuideLine.startWidth = dragZoneGuideWidth;
        dragZoneGuideLine.endWidth = dragZoneGuideWidth;

        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            RefreshDragZoneGuideLine();
        }
    }

    private void RefreshDragZoneGuideLine()
    {
        if (dragZoneGuideLine == null || Camera.main == null) return;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        float guideY = Screen.height * bottomScreenPercentage;
        Vector3 left = Camera.main.ScreenToWorldPoint(new Vector3(0f, guideY, -Camera.main.transform.position.z));
        Vector3 right = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, guideY, -Camera.main.transform.position.z));
        left.z = 0f;
        right.z = 0f;

        Vector3 inward = (right - left).normalized * dragZoneSidePadding;
        dragZoneGuideLine.SetPosition(0, left + inward);
        dragZoneGuideLine.SetPosition(1, right - inward);
    }
}
