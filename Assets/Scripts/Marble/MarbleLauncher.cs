using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MarbleLauncher : MonoBehaviour
{
    [Header("Launcher Settings")]
    public GameObject gacoanPrefab;
    [Tooltip("Titik standby awal kelereng sebelum layar disentuh.")]
    public Transform launchPoint;        
    public float maxDragDistance = 2.5f;
    public float launchForceMultiplier = 15f;
    public LineRenderer trajectoryLine;

    [Header("Shoot Button")]
    public bool useShootButtonToFire = true;
    public RectTransform shootButtonHitArea;
    public string shootButtonObjectName = "Shoot";

    [Header("Power Slider")]
    public Slider powerSlider;
    public string powerSliderObjectName = "PowerSlider";

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
    private Vector2 currentAimPullVector;
    private bool isDragging = false;
    private bool hasAimDirection = false;
    private GameObject currentGacoan;
    private Rigidbody2D currentGacoanRb;
    private LineRenderer dragZoneGuideLine;
    private Transform aimPreviewRoot;
    private Sprite aimDotSprite;
    private readonly List<SpriteRenderer> aimDots = new List<SpriteRenderer>();
    private SpriteRenderer aimArrowRenderer;
    private SpriteRenderer standbyArrowRenderer;
    private bool launcherPreviewActive = true;
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
        ResolveShootButtonHitArea();
        ResolvePowerSlider();
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
        UpdateLockedPowerSliderPreview();
        UpdateStandbyAimArrow();
    }

    // ==========================================
    // CORE INPUT ROUTER
    // ==========================================

    private void HandleInput()
    {
        if (Pointer.current == null) return;

        Vector2 screenPosition = Pointer.current.position.ReadValue();

        if (useShootButtonToFire && HandleShootButtonInput(screenPosition))
        {
            return;
        }

        if (Pointer.current.press.wasPressedThisFrame)
            TryStartDrag(screenPosition);
        else if (Pointer.current.press.isPressed && isDragging)
            ContinueDrag(screenPosition);
        else if (Pointer.current.press.wasReleasedThisFrame && isDragging)
            FinishAimDrag(screenPosition);
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

        if (!HasEnoughEnergyForCurrentMarble())
        {
            Debug.LogWarning($"⚠️ Tembakan Terkunci! Kurang Energi.");
            return;
        }

        dragStartPos = Camera.main.ScreenToWorldPoint(screenPosition);
        launchOriginPos = dragStartPos;
        currentGacoan.transform.position = launchOriginPos;
        currentAimPullVector = Vector2.zero;
        hasAimDirection = false;
        
        isDragging = true;
        if (trajectoryLine != null) trajectoryLine.enabled = false;
        SetStandbyArrowVisible(false);
        UpdateTrajectoryLine(Vector2.zero);
    }

    private void ContinueDrag(Vector2 screenPosition)
    {
        if (currentGacoan == null) return;

        Vector2 currentTouchWorld = Camera.main.ScreenToWorldPoint(screenPosition);
        Vector2 rawPullVector = currentTouchWorld - dragStartPos;
        Vector2 pullVector = GetAimPullVector(rawPullVector);

        currentAimPullVector = pullVector;
        hasAimDirection = pullVector.magnitude > 0.2f;

        // Kelereng tetap diam; hanya garis bidik yang memanjang mengikuti tarikan jari.
        UpdateAimPreview(useShootButtonToFire ? GetPowerSliderPullVector() : pullVector);
    }

    private bool HasEnoughEnergyForCurrentMarble()
    {
        if (currentGacoan == null || ProgressionManager.Instance == null) return false;

        MarbleElementHandler handler = currentGacoan.GetComponent<MarbleElementHandler>();
        if (handler == null || handler.activeElement == null) return true;

        int cost = handler.activeElement.energyCost;
        return ProgressionManager.Instance.currentEnergy >= cost;
    }

    private bool HandleShootButtonInput(Vector2 screenPosition)
    {
        if (Pointer.current == null) return false;

        if (!Pointer.current.press.wasPressedThisFrame || !IsScreenPositionOverShootButton(screenPosition))
        {
            return false;
        }

        FireWithPowerSlider();
        return true;
    }

    private void FireWithPowerSlider()
    {
        if (currentGacoan == null || !hasAimDirection) return;

        if (!HasEnoughEnergyForCurrentMarble())
        {
            Debug.LogWarning($"⚠️ Tembakan Terkunci! Kurang Energi.");
            return;
        }

        isDragging = false;
        SetStandbyArrowVisible(false);
        ReleaseAndFire(GetPowerSliderPullVector());
    }

    public void OnShootButtonPressed()
    {
        if (!useShootButtonToFire) return;

        FireWithPowerSlider();
    }

    public void OnShootButtonReleased()
    {
    }

    private void UpdateLockedPowerSliderPreview()
    {
        if (!useShootButtonToFire || isDragging || currentGacoan == null || !hasAimDirection) return;

        UpdateAimPreview(GetPowerSliderPullVector());
    }

    private float GetPowerSliderValue01()
    {
        if (powerSlider == null)
        {
            ResolvePowerSlider();
        }

        if (powerSlider == null)
        {
            return 1f;
        }

        if (Mathf.Approximately(powerSlider.minValue, powerSlider.maxValue))
        {
            return Mathf.Clamp01(powerSlider.value);
        }

        return Mathf.Clamp01(Mathf.InverseLerp(powerSlider.minValue, powerSlider.maxValue, powerSlider.value));
    }

    private Vector2 GetPowerSliderPullVector()
    {
        if (!hasAimDirection || currentAimPullVector.sqrMagnitude <= 0.0001f) return Vector2.zero;

        float power = GetPowerSliderValue01();
        return currentAimPullVector.normalized * GetEffectiveMaxDragDistance() * power;
    }

    private void FinishAimDrag(Vector2 screenPosition)
    {
        isDragging = false;

        if (currentGacoan == null) return;

        Vector2 currentTouchWorld = Camera.main.ScreenToWorldPoint(screenPosition);
        Vector2 pullVector = GetAimPullVector(currentTouchWorld - dragStartPos);
        currentAimPullVector = pullVector;
        hasAimDirection = pullVector.magnitude > 0.2f;

        if (!useShootButtonToFire)
        {
            ReleaseAndFire(pullVector);
            return;
        }

        if (hasAimDirection)
        {
            UpdateAimPreview(GetPowerSliderPullVector());
            return;
        }

        // Jika tarikan terlalu pendek (cancel aim), kelereng tetap di titik awal tembakan.
        CancelLockedAim();
    }

    private void ReleaseAndFire(Vector2 pullVector)
    {
        isDragging = false;
        HideAimPreview();
        if (trajectoryLine != null) trajectoryLine.enabled = false;

        if (currentGacoan == null) return;

        if (pullVector.magnitude > 0.2f && HasEnoughEnergyForCurrentMarble())
        {
            ExecutePhysicsLaunch(pullVector);
            hasAimDirection = false;
            currentAimPullVector = Vector2.zero;
            return;
        }

        CancelLockedAim();
    }

    private void CancelLockedAim()
    {
        isDragging = false;
        hasAimDirection = false;
        currentAimPullVector = Vector2.zero;
        HideAimPreview();
        if (trajectoryLine != null) trajectoryLine.enabled = false;

        if (currentGacoan != null)
        {
            currentGacoan.transform.position = launchOriginPos;
        }

        UpdateStandbyAimArrow();
    }

    // ==========================================
    // HELPER METHODS (SPAWN & PHYSICS)
    // ==========================================

    private void PrepareNextShot()
    {
        if (gacoanPrefab == null || launchPoint == null) return;

        isDragging = false;
        hasAimDirection = false;
        currentAimPullVector = Vector2.zero;

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

        RicochetMasterSO passiveRicochet = CurrentEmblemManager.Instance != null
            ? CurrentEmblemManager.Instance.GetPassiveEmblem<RicochetMasterSO>()
            : null;
        if (passiveRicochet != null)
        {
            passiveRicochet.Activate(currentGacoan);
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

    private bool HasEagleEyePreview()
    {
        if (CurrentEmblemManager.Instance == null) return false;

        return CurrentEmblemManager.Instance.currentEmblem is EagleEyeSO
            || CurrentEmblemManager.Instance.HasPassiveEmblem<EagleEyeSO>();
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
        if (isDragging || hasAimDirection || currentGacoan == null) return false;

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

    private Vector2 GetAimPullVector(Vector2 rawPullVector)
    {
        if (!useShootButtonToFire)
        {
            return ClampPullVector(rawPullVector);
        }

        if (rawPullVector.magnitude <= 0.001f)
        {
            return Vector2.zero;
        }

        return rawPullVector.normalized * GetEffectiveMaxDragDistance();
    }

    private void UpdateAimPreview(Vector2 pullVector)
    {
        if (HasEagleEyePreview())
        {
            UpdateRicochetTrajectory(-pullVector);
        }
        else
        {
            UpdateTrajectoryLine(-pullVector);
        }
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
            marblePhysicsMaterial,
            GetCurrentMarbleMass(),
            IsCurrentMarbleLava(out float lavaPreserveSpeedMultiplier),
            lavaPreserveSpeedMultiplier);

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

        if ((!isDragging && !hasAimDirection) || points == null || points.Count < 2 || launchDirection.sqrMagnitude <= 0.0001f)
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
        if (!launcherPreviewActive || isDragging || hasAimDirection || currentGacoan == null)
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

    public void SetLauncherPreviewActive(bool active)
    {
        launcherPreviewActive = active;

        if (currentGacoan != null)
        {
            SpriteRenderer[] renderers = currentGacoan.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer renderer in renderers)
            {
                renderer.enabled = active;
            }

            Collider2D[] colliders = currentGacoan.GetComponentsInChildren<Collider2D>(true);
            foreach (Collider2D collider in colliders)
            {
                collider.enabled = active;
            }
        }

        if (!active)
        {
            HideAimPreview();
            SetStandbyArrowVisible(false);
            return;
        }

        UpdateStandbyAimArrow();
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
            marblePhysicsMaterial,
            GetCurrentMarbleMass(),
            IsCurrentMarbleLava(out float lavaPreserveSpeedMultiplier),
            lavaPreserveSpeedMultiplier);

        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
            trajectoryLine.positionCount = 0;
        }

        DrawDottedAimPreview(points, launchDirection);
    }

    private List<CircleCollider2D> GetMarbleCollidersForPrediction(Collider2D ignoreCollider)
    {
        List<CircleCollider2D> marbleColliders = new List<CircleCollider2D>();
        HashSet<CircleCollider2D> addedColliders = new HashSet<CircleCollider2D>();

        if (ArenaManager.Instance != null)
        {
            foreach (Rigidbody2D marbleBody in ArenaManager.Instance.allMarblesInArena)
            {
                if (marbleBody == null)
                {
                    continue;
                }

                AddMarbleColliderForPrediction(
                    marbleBody.GetComponent<CircleCollider2D>(),
                    ignoreCollider,
                    marbleColliders,
                    addedColliders);
            }
        }

        CircleCollider2D[] sceneCircleColliders = Object.FindObjectsByType<CircleCollider2D>(FindObjectsSortMode.None);
        foreach (CircleCollider2D circleCollider in sceneCircleColliders)
        {
            AddMarbleColliderForPrediction(circleCollider, ignoreCollider, marbleColliders, addedColliders);
        }

        return marbleColliders;
    }

    private void AddMarbleColliderForPrediction(
        CircleCollider2D circleCollider,
        Collider2D ignoreCollider,
        List<CircleCollider2D> marbleColliders,
        HashSet<CircleCollider2D> addedColliders)
    {
        if (circleCollider == null || circleCollider == ignoreCollider || !circleCollider.enabled)
        {
            return;
        }

        if (!IsPredictableMarble(circleCollider.gameObject))
        {
            return;
        }

        if (addedColliders.Add(circleCollider))
        {
            marbleColliders.Add(circleCollider);
        }
    }

    private bool IsPredictableMarble(GameObject marbleObject)
    {
        if (marbleObject == null) return false;

        return marbleObject.CompareTag("TargetMarble")
            || marbleObject.CompareTag("PlayerMarble")
            || marbleObject.CompareTag("Gacoan")
            || marbleObject.GetComponent<TargetMarble>() != null
            || marbleObject.GetComponent<MarbleElementHandler>() != null;
    }

    private bool IsCurrentMarbleLava(out float preserveSpeedMultiplier)
    {
        preserveSpeedMultiplier = 1f;
        if (currentGacoan == null) return false;

        MarbleElementHandler handler = currentGacoan.GetComponent<MarbleElementHandler>();
        if (handler == null || handler.activeElement == null) return false;

        if (handler.activeElement is CombinedElementSO combinedElement
            && combinedElement.fusionType == CombinedElementSO.FusionType.Lava)
        {
            preserveSpeedMultiplier = combinedElement.lavaPreserveSpeedMultiplier;
            return true;
        }

        return false;
    }

    private float GetCurrentMarbleMass()
    {
        if (currentGacoanRb != null)
        {
            return Mathf.Max(0.01f, currentGacoanRb.mass);
        }

        if (currentGacoan != null && currentGacoan.TryGetComponent(out Rigidbody2D rb))
        {
            return Mathf.Max(0.01f, rb.mass);
        }

        return 1f;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

        return Pointer.current != null && MarblePlaygroundController.IsScreenPositionOverMenu(Pointer.current.position.ReadValue());
    }

    private void ResolveShootButtonHitArea()
    {
        if (shootButtonHitArea != null || string.IsNullOrEmpty(shootButtonObjectName)) return;

        GameObject shootButtonObject = GameObject.Find(shootButtonObjectName);
        if (shootButtonObject == null) return;

        shootButtonHitArea = shootButtonObject.GetComponent<RectTransform>();
    }

    private void ResolvePowerSlider()
    {
        if (powerSlider != null || string.IsNullOrEmpty(powerSliderObjectName)) return;

        GameObject sliderObject = GameObject.Find(powerSliderObjectName);
        if (sliderObject == null) return;

        powerSlider = sliderObject.GetComponent<Slider>();
    }

    private bool IsScreenPositionOverShootButton(Vector2 screenPosition)
    {
        if (shootButtonHitArea == null)
        {
            ResolveShootButtonHitArea();
        }

        if (shootButtonHitArea == null || !shootButtonHitArea.gameObject.activeInHierarchy)
        {
            return false;
        }

        Canvas canvas = shootButtonHitArea.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.RectangleContainsScreenPoint(shootButtonHitArea, screenPosition, eventCamera);
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
