using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic; // Diperlukan untuk memanipulasi List amunisi

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

    [Header("Equipped Element Data")]
    public MarbleElementSO equippedElement;

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
        {
            TryStartDrag(screenPosition);
        }
        else if (Pointer.current.press.isPressed && isDragging)
        {
            ContinueDrag(screenPosition);
        }
        else if (Pointer.current.press.wasReleasedThisFrame && isDragging)
        {
            ReleaseAndFire(screenPosition);
        }
    }

    // ==========================================
    // MODULAR INPUT PHASES
    // ==========================================

    private void TryStartDrag(Vector2 screenPosition)
    {
        if (screenPosition.y > Screen.height * bottomScreenPercentage) return;
        if (currentGacoan == null) return;

        // ========================================================
        // MODIFIKASI: KUNCI DRAG JIKA ENERGI TIDAK CUKUP
        // ========================================================
        MarbleElementHandler handler = currentGacoan.GetComponent<MarbleElementHandler>();
        if (handler != null && handler.activeElement != null)
        {
            int cost = handler.activeElement.energyCost;
            if (ProgressionManager.Instance.currentEnergy < cost)
            {
                Debug.LogWarning($"⚠️ Tembakan Terkunci! {handler.activeElement.elementName} butuh {cost} Energy (Milikmu: {ProgressionManager.Instance.currentEnergy}). Harap TUKAR KELERENG!");
                return; // KELUAR! Menghalangi isDragging menjadi true agar ketapel tidak bisa ditarik
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

        // Mengintip elemen terdepan (Index 0) tanpa memotong antrean (Pop) terlebih dahulu
        // Agar jika pemain tidak memiliki cukup energi, data peluru tidak hangus dan bisa ditukar
        MarbleElementSO nextElement = null;
        if (ProgressionManager.Instance.equippedChamber.Count > 0)
        {
            nextElement = ProgressionManager.Instance.equippedChamber[0];
        }

        currentGacoan = Instantiate(gacoanPrefab, launchPoint.position, Quaternion.identity);
        currentGacoanRb = currentGacoan.GetComponent<Rigidbody2D>();

        MarbleElementHandler handler = currentGacoan.GetComponent<MarbleElementHandler>();
        
        if (handler != null)
        {
            handler.activeElement = nextElement;
            
            // Beri visual warna elemen asli sejak awal nangkring di ketapel
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

        // ========================================================
        // MODIFIKASI: EKSEKUSI PEMOTONGAN DATA SETELAH TEMBAKAN VALID
        // ========================================================
        MarbleElementHandler handler = currentGacoan.GetComponent<MarbleElementHandler>();
        if (handler != null && handler.activeElement != null)
        {
            // Potong energi global tepat saat peluru lepas dari genggaman
            ProgressionManager.Instance.currentEnergy -= handler.activeElement.energyCost;
            Debug.Log($"⚡ Efek {handler.activeElement.elementName} Dilepas! Sisa Energi: {ProgressionManager.Instance.currentEnergy}");
        }

        // Amunisi terdepan baru resmi dibuang dari antrean Chamber global setelah sukses meluncur
        ProgressionManager.Instance.PopNextElement();

        Vector2 launchForce = -pullVector * launchForceMultiplier;
        currentGacoanRb.AddForce(launchForce, ForceMode2D.Impulse);

        ArenaManager.Instance.OnMarbleFlicked(currentGacoanRb, true);
        
        currentGacoan = null;
        currentGacoanRb = null;
    }

    // ========================================================
    // MODIFIKASI: FUNGSI TUKAR POSISI DARI SELEKSI UI CADANGAN
    // ========================================================
    public void ForceSwapActiveMarble(int targetIndexInChamber)
    {
        // Jangan ijinkan menukar amunisi di tengah-tengah tarikan karet ketapel
        if (isDragging || currentGacoan == null) return;

        List<MarbleElementSO> chamber = ProgressionManager.Instance.equippedChamber;
        
        // Proteksi batas indeks array peluru cadangan
        if (targetIndexInChamber > 0 && targetIndexInChamber < chamber.Count)
        {
            // Tukar posisi elemen pilihan (Index target) dengan elemen di ketapel (Index 0)
            MarbleElementSO temp = chamber[0];
            chamber[0] = chamber[targetIndexInChamber];
            chamber[targetIndexInChamber] = temp;

            // Hancurkan objek gacoan fisik lama yang sedang diam di ketapel
            Destroy(currentGacoan);
            
            // Instansiasi ulang gacoan baru yang memuat elemen hasil pertukaran
            PrepareNextShot();
            
            Debug.Log($"🔄 Berhasil menukar peluru ketapel dengan elemen cadangan di indeks saku: {targetIndexInChamber}");
        }
    }
}