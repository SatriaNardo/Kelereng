using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class MapNode : MonoBehaviour
{
    public MapManager.NodeType nodeType;
    public int floorNumber;
    public int columnNumber;
    
    [HideInInspector] public Vector2 UIAnchoredPosition; 
    
    // BARU: Mencatat kolom lantai sebelumnya yang terhubung ke node ini
    [HideInInspector] public List<int> incomingConnections = new List<int>(); 

    public Button nodeButton;
    public Image nodeImage;
    public TMP_Text nodeText;

    private MapManager mapManager;

    private void Awake()
    {
        if (nodeButton == null) nodeButton = GetComponent<Button>();
        if (nodeImage == null)
        {
            nodeImage = nodeButton != null ? nodeButton.image : GetComponent<Image>();
        }
    }

    public void SetupNode(MapManager.NodeType type, int floor, int column, MapManager manager, Vector2 position)
    {
        nodeType = type;
        floorNumber = floor;
        columnNumber = column;
        mapManager = manager;
        UIAnchoredPosition = position;

        nodeText.text = type == MapManager.NodeType.Elite ? "EL" : type.ToString()[0].ToString(); 
        nodeButton.onClick.AddListener(OnNodeClicked);
        
        // Catatan: UpdateNodeInteractivity() dihapus dari sini karena harus menunggu garis digambar dahulu
    }

    public void SetNodeSprite(Sprite nodeSprite, bool hideTextWhenSpriteExists)
    {
        if (nodeImage == null)
        {
            nodeImage = nodeButton != null ? nodeButton.image : GetComponent<Image>();
        }

        if (nodeImage != null && nodeSprite != null)
        {
            nodeImage.sprite = nodeSprite;
            nodeImage.type = Image.Type.Simple;
            nodeImage.preserveAspect = true;
            nodeImage.color = Color.white;
        }

        if (nodeText != null)
        {
            nodeText.gameObject.SetActive(nodeSprite == null || !hideTextWhenSpriteExists);
        }
    }

    // LOGIKA BARU: Mengecek apakah node ini valid untuk diklik oleh pemain
    public void UpdateNodeInteractivity()
    {
        int globalFloor = ProgressionManager.Instance.currentFloor;
        int globalColumn = ProgressionManager.Instance.currentColumn;

        // Aturan 1: Hanya node di lantai aktif saat ini yang bisa diklik
        if (floorNumber == globalFloor)
        {
            // Aturan 2: Jika masih di Lantai 1 (Awal Game), semua node lantai 1 terbuka gratis
            if (floorNumber == 1)
            {
                SetNodeActive(true);
            }
            // Aturan 3: Untuk lantai 2 ke atas, kolom pemain sebelumnya harus terdaftar di jalur koneksi node ini
            else if (incomingConnections.Contains(globalColumn))
            {
                SetNodeActive(true);
            }
            else
            {
                SetNodeActive(false);
            }
        }
        else
        {
            SetNodeActive(false);
        }
    }

    private void SetNodeActive(bool isActive)
    {
        nodeButton.interactable = isActive;
    }

    private void OnNodeClicked()
    {
        // BARU: Simpan lokasi kolom yang dipilih pemain ke data global sebelum pindah scene
        ProgressionManager.Instance.currentColumn = columnNumber;

        mapManager.OnNodeSelected(nodeType.ToString());
    }
}
