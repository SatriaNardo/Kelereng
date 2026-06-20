using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProceduralMapGenerator : MonoBehaviour
{
    public enum NodeSelection { Random, Fight, Event, Treasure, Store, Boss }

    [System.Serializable]
    public struct ColumnConfig
    {
        public int columnPosition; 
        public NodeSelection roomType;
    }

    [System.Serializable]
    public struct FloorConfig
    {
        public string floorLabel; 
        public List<ColumnConfig> columns;
    }

    private struct MapConnection
    {
        public int startColumn;
        public int endColumn;

        public MapConnection(int start, int end)
        {
            startColumn = start;
            endColumn = end;
        }
    }

    [Header("Prefab Settings")]
    public GameObject nodePrefab;       
    public GameObject linePrefab;       
    public Transform mapContainer;      

    [Header("Map Layout Configuration")]
    public Vector2 spacing = new Vector2(150f, 120f); 

    [Header("Organic Look Settings")]
    [Range(0f, 50f)] public float xJitter = 25f; 

    [Header("Path Settings")]
    public float lineWidth = 6f;
    public int maxColumnConnectionDistance = 1; 

    [Header("Hand-Crafted Floor Layout Structure")]
    [SerializeField] private List<FloorConfig> mapLayout = new List<FloorConfig>();

    private MapManager mapManager;
    private Dictionary<int, List<MapNode>> nodesByFloor = new Dictionary<int, List<MapNode>>();

    private void Awake()
    {
        mapManager = GetComponent<MapManager>();
    }

    private void Start()
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        // Hancurkan sisa visual lama di layar sebelum merajut peta asli
        foreach (Transform child in mapContainer) Destroy(child.gameObject);
        nodesByFloor.Clear();

        var pm = ProgressionManager.Instance;
        if (pm == null) return;

        // =======================================================================
        // KONDISI A: RUN PERMAINAN BARU (Belum Pernah Generate Peta)
        // =======================================================================
        if (!pm.isMapAlreadyGenerated)
        {
            pm.savedMapNodes.Clear();

            // 1. Lahirkan struktur node sesuai rancangan mentah di Inspector
            for (int i = 0; i < mapLayout.Count; i++)
            {
                int floorNumber = i + 1; 
                FloorConfig floorData = mapLayout[i];
                nodesByFloor[floorNumber] = new List<MapNode>();

                foreach (ColumnConfig colData in floorData.columns)
                {
                    MapManager.NodeType finalType = DetermineFinalNodeType(colData.roomType);
                    SpawnNode(finalType, floorNumber, colData.columnPosition);
                }
            }

            // 2. Hubungkan jalur garis antar node (Mengisi incomingConnections secara otomatis)
            DrawAllConnections();

            // 3. KUNCI DATA: Simpan cetak biru node acak yang sah ini ke ProgressionManager
            foreach (var floorList in nodesByFloor.Values)
            {
                foreach (MapNode node in floorList)
                {
                    MapNodeBlueprint bp = new MapNodeBlueprint();
                    bp.nodeTypeString = node.nodeType.ToString();
                    bp.floorNumber = node.floorNumber;
                    bp.columnNumber = node.columnNumber;
                    bp.uiAnchoredPosition = node.UIAnchoredPosition;
                    bp.incomingConnections = new List<int>(node.incomingConnections);

                    pm.savedMapNodes.Add(bp);
                }
            }

            pm.isMapAlreadyGenerated = true;
            Debug.Log("🎲 [PETA BARU] Susunan tipe ruangan sukses diacak dan dikunci abadi.");
        }
        // =======================================================================
        // KONDISI B: KEMBALI DARI COMBAT / SHOP (Muat Peta yang Sama)
        // =======================================================================
        else
        {
            Debug.Log("💾 [LOAD PETA] Memulihkan susunan tipe node ruangan asli dari memori.");

            // 1. Bangun ulang objek tombol murni berbasis memori blueprint yang tersimpan
            foreach (MapNodeBlueprint bp in pm.savedMapNodes)
            {
                if (!nodesByFloor.ContainsKey(bp.floorNumber))
                {
                    nodesByFloor[bp.floorNumber] = new List<MapNode>();
                }

                GameObject newNodeObj = Instantiate(nodePrefab, mapContainer);
                MapNode nodeScript = newNodeObj.GetComponent<MapNode>();
                
                MapManager.NodeType savedType = (MapManager.NodeType)System.Enum.Parse(typeof(MapManager.NodeType), bp.nodeTypeString);
                
                RectTransform rect = newNodeObj.GetComponent<RectTransform>();
                if (rect != null) rect.anchoredPosition = bp.uiAnchoredPosition;

                // Kosongkan jalur sementara, biar fungsi DrawAllConnections merajut jembatannya secara bersih
                nodeScript.incomingConnections = new List<int>();
                nodeScript.SetupNode(savedType, bp.floorNumber, bp.columnNumber, mapManager, bp.uiAnchoredPosition);
                
                nodesByFloor[bp.floorNumber].Add(nodeScript);
            }

            // 2. Gambar ulang jembatan garis visualnya secara presisi
            DrawAllConnections();
        }

        // =======================================================================
        // EVALUASI AKHIR: Nyalakan/Matikan Interaktivitas Tombol Klik Peta
        // =======================================================================
        foreach (var floorList in nodesByFloor.Values)
        {
            foreach (MapNode node in floorList)
            {
                node.UpdateNodeInteractivity();
            }
        }
    }

    private MapNode SpawnNode(MapManager.NodeType type, int floor, int columnOffset)
    {
        GameObject newNodeObj = Instantiate(nodePrefab, mapContainer);
        MapNode nodeScript = newNodeObj.GetComponent<MapNode>();

        int uniqueSeed = floor * 1000 + columnOffset;
        System.Random pseudoRandom = new System.Random(uniqueSeed);
        float horizontalJitter = (float)(pseudoRandom.NextDouble() * 2.0 - 1.0) * xJitter;

        if (type == MapManager.NodeType.Boss || (type == MapManager.NodeType.Treasure && columnOffset == 0))
        {
            horizontalJitter *= 0.1f; 
        }

        float xPos = (columnOffset * spacing.x) + horizontalJitter;
        float yPos = floor * spacing.y;
        Vector2 finalAnchoredPos = new Vector2(xPos, yPos);

        RectTransform rect = newNodeObj.GetComponent<RectTransform>();
        rect.anchoredPosition = finalAnchoredPos;

        nodeScript.SetupNode(type, floor, columnOffset, mapManager, finalAnchoredPos);
        nodesByFloor[floor].Add(nodeScript);

        return nodeScript;
    }

    private void DrawAllConnections()
    {
        if (linePrefab == null) return;

        // Menggunakan hitungan total lantai dinamis berdasarkan isi Dictionary data riil
        for (int currentFloor = 1; currentFloor < nodesByFloor.Count; currentFloor++)
        {
            int nextFloor = currentFloor + 1;

            if (!nodesByFloor.ContainsKey(currentFloor) || !nodesByFloor.ContainsKey(nextFloor)) continue;

            List<MapNode> currentNodes = nodesByFloor[currentFloor];
            List<MapNode> nextNodes = nodesByFloor[nextFloor];

            currentNodes.Sort((a, b) => a.columnNumber.CompareTo(b.columnNumber));
            nextNodes.Sort((a, b) => a.columnNumber.CompareTo(b.columnNumber));

            List<MapConnection> establishedConnections = new List<MapConnection>();

            foreach (MapNode currNode in currentNodes)
            {
                foreach (MapNode nxtNode in nextNodes)
                {
                    bool isNextFloorSingleChokepoint = nextNodes.Count == 1;
                    int columnDiff = Mathf.Abs(currNode.columnNumber - nxtNode.columnNumber);

                    if (columnDiff <= maxColumnConnectionDistance || isNextFloorSingleChokepoint)
                    {
                        if (CheckIfPathCrosses(currNode.columnNumber, nxtNode.columnNumber, establishedConnections))
                        {
                            continue; 
                        }

                        CreateUILine(currNode.UIAnchoredPosition, nxtNode.UIAnchoredPosition);
                        establishedConnections.Add(new MapConnection(currNode.columnNumber, nxtNode.columnNumber));

                        if (!nxtNode.incomingConnections.Contains(currNode.columnNumber))
                        {
                            nxtNode.incomingConnections.Add(currNode.columnNumber);
                        }
                    }
                }
            }
        }
    }

    private bool CheckIfPathCrosses(int startCol, int endCol, List<MapConnection> existingConnections)
    {
        foreach (MapConnection existing in existingConnections)
        {
            if ((existing.startColumn < startCol && existing.endColumn > endCol) ||
                (existing.startColumn > startCol && existing.endColumn < endCol))
            {
                return true; 
            }
        }
        return false; 
    }

    private void CreateUILine(Vector2 startPoint, Vector2 endPoint)
    {
        GameObject lineObj = Instantiate(linePrefab, mapContainer);
        lineObj.transform.SetAsFirstSibling(); 

        RectTransform rect = lineObj.GetComponent<RectTransform>();
        Vector2 direction = endPoint - startPoint;
        float distance = direction.magnitude;

        rect.anchoredPosition = startPoint + (direction * 0.5f);
        rect.sizeDelta = new Vector2(distance, lineWidth);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rect.rotation = Quaternion.Euler(0, 0, angle);
    }

    private MapManager.NodeType DetermineFinalNodeType(NodeSelection selection)
    {
        if (selection != NodeSelection.Random)
        {
            return (MapManager.NodeType)System.Enum.Parse(typeof(MapManager.NodeType), selection.ToString());
        }

        float rand = Random.value;
        if (rand < 0.45f) return MapManager.NodeType.Fight;    
        if (rand < 0.70f) return MapManager.NodeType.Event;    
        if (rand < 0.85f) return MapManager.NodeType.Store;    
        return MapManager.NodeType.Treasure;                   
    }
}