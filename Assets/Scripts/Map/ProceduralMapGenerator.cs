using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    [Min(0.1f)] public float mapVisualScale = 2f;
    public float mapBottomPadding = 120f;

    [Header("Organic Look Settings")]
    [Range(0f, 50f)] public float xJitter = 25f; 

    [Header("Path Settings")]
    public float lineWidth = 6f;
    public int maxColumnConnectionDistance = 1; 

    [Header("Hand-Crafted Floor Layout Structure")]
    [SerializeField] private List<FloorConfig> mapLayout = new List<FloorConfig>();

    [Header("Random Nodes Per Floor")]
    public bool randomizeNodeCountPerFloor = true;
    [Range(1, 6)] public int minNodesPerFloor = 2;
    [Range(1, 6)] public int maxNodesPerFloor = 4;
    public bool keepSingleNodeFloors = true;

    [Header("World Map Return")]
    public bool returnToWorldMapWhenCleared = true;
    public string worldMapSceneName = "WorldMapScene";

    [Header("Scrollable Map Content")]
    public bool resizeMapContainerForScroll = true;
    public float verticalScrollPadding = 240f;
    public bool forceMapContentScaleOne = true;
    public bool forceMapContentBottomPivot = true;
    public bool scrollToBottomAfterGenerate = true;
    public bool autoConfigureScrollRect = false;
    public bool createFullScreenScrollDragArea = false;

    private MapManager mapManager;
    private ScrollRect mapScrollRect;
    private Dictionary<int, List<MapNode>> nodesByFloor = new Dictionary<int, List<MapNode>>();

    private void Awake()
    {
        mapManager = GetComponent<MapManager>();
        PrepareScrollContainer();
    }

    private void Start()
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        PrepareScrollContainer();
        NormalizeMapContentScale();

        // Hancurkan sisa visual lama di layar sebelum merajut peta asli
        foreach (Transform child in mapContainer) Destroy(child.gameObject);
        nodesByFloor.Clear();

        var pm = ProgressionManager.Instance;
        if (pm == null) return;

        ApplyWorldMapConfig(pm.selectedWorldMapConfig);

        pm.currentPathMapFloorCount = mapLayout.Count;
        if (returnToWorldMapWhenCleared && mapLayout.Count > 0 && pm.currentFloor > mapLayout.Count)
        {
            pm.CompleteCurrentWorldMapNode();
            SceneManager.LoadScene(worldMapSceneName);
            return;
        }

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

                List<ColumnConfig> selectedColumns = GetColumnsForFloor(floorData);
                foreach (ColumnConfig colData in selectedColumns)
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
            pm.currentPathMapFloorCount = mapLayout.Count;
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
                newNodeObj.transform.localScale = Vector3.one * mapVisualScale;
                MapNode nodeScript = newNodeObj.GetComponent<MapNode>();
                
                MapManager.NodeType savedType = (MapManager.NodeType)System.Enum.Parse(typeof(MapManager.NodeType), bp.nodeTypeString);
                Vector2 recalculatedPosition = CalculateNodePosition(savedType, bp.floorNumber, bp.columnNumber);
                
                RectTransform rect = newNodeObj.GetComponent<RectTransform>();
                if (rect != null) SetBottomAnchoredPosition(rect, recalculatedPosition);

                // Kosongkan jalur sementara, biar fungsi DrawAllConnections merajut jembatannya secara bersih
                nodeScript.incomingConnections = new List<int>();
                nodeScript.SetupNode(savedType, bp.floorNumber, bp.columnNumber, mapManager, recalculatedPosition);
                
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

        ResizeMapContainerForScroll();
        ConfigureScrollRect();
        ScrollToBottom();
    }

    private void ApplyWorldMapConfig(WorldMapConfigSO worldMapConfig)
    {
        if (worldMapConfig == null || worldMapConfig.mapLayout == null || worldMapConfig.mapLayout.Count == 0)
        {
            return;
        }

        mapLayout = new List<FloorConfig>(worldMapConfig.mapLayout);
        randomizeNodeCountPerFloor = worldMapConfig.randomizeNodeCountPerFloor;
        minNodesPerFloor = worldMapConfig.minNodesPerFloor;
        maxNodesPerFloor = worldMapConfig.maxNodesPerFloor;
        keepSingleNodeFloors = worldMapConfig.keepSingleNodeFloors;
    }

    private List<ColumnConfig> GetColumnsForFloor(FloorConfig floorData)
    {
        if (!randomizeNodeCountPerFloor || floorData.columns == null)
        {
            return floorData.columns != null ? floorData.columns : new List<ColumnConfig>();
        }

        if (floorData.columns.Count <= 1 && keepSingleNodeFloors)
        {
            return floorData.columns;
        }

        if (ContainsBossNode(floorData.columns))
        {
            return floorData.columns;
        }

        int minCount = Mathf.Clamp(minNodesPerFloor, 1, floorData.columns.Count);
        int maxCount = Mathf.Clamp(maxNodesPerFloor, minCount, floorData.columns.Count);
        int targetCount = Random.Range(minCount, maxCount + 1);

        List<ColumnConfig> shuffledColumns = new List<ColumnConfig>(floorData.columns);
        for (int i = 0; i < shuffledColumns.Count; i++)
        {
            int swapIndex = Random.Range(i, shuffledColumns.Count);
            ColumnConfig temp = shuffledColumns[i];
            shuffledColumns[i] = shuffledColumns[swapIndex];
            shuffledColumns[swapIndex] = temp;
        }

        List<ColumnConfig> selectedColumns = shuffledColumns.GetRange(0, targetCount);
        selectedColumns.Sort((a, b) => a.columnPosition.CompareTo(b.columnPosition));
        return selectedColumns;
    }

    private bool ContainsBossNode(List<ColumnConfig> columns)
    {
        foreach (ColumnConfig column in columns)
        {
            if (column.roomType == NodeSelection.Boss)
            {
                return true;
            }
        }

        return false;
    }

    private MapNode SpawnNode(MapManager.NodeType type, int floor, int columnOffset)
    {
        GameObject newNodeObj = Instantiate(nodePrefab, mapContainer);
        newNodeObj.transform.localScale = Vector3.one * mapVisualScale;
        MapNode nodeScript = newNodeObj.GetComponent<MapNode>();

        Vector2 finalAnchoredPos = CalculateNodePosition(type, floor, columnOffset);

        RectTransform rect = newNodeObj.GetComponent<RectTransform>();
        if (rect != null) SetBottomAnchoredPosition(rect, finalAnchoredPos);

        nodeScript.SetupNode(type, floor, columnOffset, mapManager, finalAnchoredPos);
        nodesByFloor[floor].Add(nodeScript);

        return nodeScript;
    }

    private Vector2 CalculateNodePosition(MapManager.NodeType type, int floor, int columnOffset)
    {
        int uniqueSeed = floor * 1000 + columnOffset;
        System.Random pseudoRandom = new System.Random(uniqueSeed);
        float horizontalJitter = (float)(pseudoRandom.NextDouble() * 2.0 - 1.0) * xJitter * mapVisualScale;

        if (type == MapManager.NodeType.Boss || (type == MapManager.NodeType.Treasure && columnOffset == 0))
        {
            horizontalJitter *= 0.1f;
        }

        float xPos = (columnOffset * spacing.x * mapVisualScale) + horizontalJitter;
        float yPos = mapBottomPadding + ((floor - 1) * spacing.y * mapVisualScale);
        return new Vector2(xPos, yPos);
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
            HashSet<MapNode> currentNodesWithOutgoing = new HashSet<MapNode>();

            foreach (MapNode currNode in currentNodes)
            {
                foreach (MapNode nxtNode in nextNodes)
                {
                    bool isNextFloorSingleChokepoint = nextNodes.Count == 1;
                    int columnDiff = Mathf.Abs(currNode.columnNumber - nxtNode.columnNumber);

                    if (columnDiff <= maxColumnConnectionDistance || isNextFloorSingleChokepoint)
                    {
                        TryCreateConnection(currNode, nxtNode, establishedConnections, currentNodesWithOutgoing, false);
                    }
                }
            }

            EnsureEveryNextNodeHasIncoming(currentNodes, nextNodes, establishedConnections, currentNodesWithOutgoing);
            EnsureEveryCurrentNodeHasOutgoing(currentNodes, nextNodes, establishedConnections, currentNodesWithOutgoing);
        }
    }

    private void EnsureEveryNextNodeHasIncoming(List<MapNode> currentNodes, List<MapNode> nextNodes, List<MapConnection> establishedConnections, HashSet<MapNode> currentNodesWithOutgoing)
    {
        foreach (MapNode nextNode in nextNodes)
        {
            if (nextNode.incomingConnections.Count > 0) continue;

            MapNode bestCurrentNode = FindNearestConnectableCurrentNode(nextNode, currentNodes, establishedConnections, false);
            if (bestCurrentNode == null)
            {
                bestCurrentNode = FindNearestConnectableCurrentNode(nextNode, currentNodes, establishedConnections, true);
            }

            if (bestCurrentNode != null)
            {
                TryCreateConnection(bestCurrentNode, nextNode, establishedConnections, currentNodesWithOutgoing, true);
            }
        }
    }

    private void EnsureEveryCurrentNodeHasOutgoing(List<MapNode> currentNodes, List<MapNode> nextNodes, List<MapConnection> establishedConnections, HashSet<MapNode> currentNodesWithOutgoing)
    {
        foreach (MapNode currentNode in currentNodes)
        {
            if (currentNodesWithOutgoing.Contains(currentNode)) continue;

            MapNode bestNextNode = FindNearestConnectableNextNode(currentNode, nextNodes, establishedConnections, false);
            if (bestNextNode == null)
            {
                bestNextNode = FindNearestConnectableNextNode(currentNode, nextNodes, establishedConnections, true);
            }

            if (bestNextNode != null)
            {
                TryCreateConnection(currentNode, bestNextNode, establishedConnections, currentNodesWithOutgoing, true);
            }
        }
    }

    private bool TryCreateConnection(MapNode currentNode, MapNode nextNode, List<MapConnection> establishedConnections, HashSet<MapNode> currentNodesWithOutgoing, bool allowCrossing)
    {
        if (currentNode == null || nextNode == null) return false;
        if (!allowCrossing && CheckIfPathCrosses(currentNode.columnNumber, nextNode.columnNumber, establishedConnections)) return false;
        if (HasConnection(currentNode.columnNumber, nextNode.columnNumber, establishedConnections)) return false;

        CreateUILine(currentNode.UIAnchoredPosition, nextNode.UIAnchoredPosition);
        establishedConnections.Add(new MapConnection(currentNode.columnNumber, nextNode.columnNumber));
        currentNodesWithOutgoing.Add(currentNode);

        if (!nextNode.incomingConnections.Contains(currentNode.columnNumber))
        {
            nextNode.incomingConnections.Add(currentNode.columnNumber);
        }

        return true;
    }

    private MapNode FindNearestConnectableCurrentNode(MapNode targetNextNode, List<MapNode> currentNodes, List<MapConnection> establishedConnections, bool allowCrossing)
    {
        MapNode bestNode = null;
        int bestDistance = int.MaxValue;

        foreach (MapNode currentNode in currentNodes)
        {
            if (HasConnection(currentNode.columnNumber, targetNextNode.columnNumber, establishedConnections)) continue;
            if (!allowCrossing && CheckIfPathCrosses(currentNode.columnNumber, targetNextNode.columnNumber, establishedConnections)) continue;

            int distance = Mathf.Abs(currentNode.columnNumber - targetNextNode.columnNumber);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestNode = currentNode;
            }
        }

        return bestNode;
    }

    private MapNode FindNearestConnectableNextNode(MapNode targetCurrentNode, List<MapNode> nextNodes, List<MapConnection> establishedConnections, bool allowCrossing)
    {
        MapNode bestNode = null;
        int bestDistance = int.MaxValue;

        foreach (MapNode nextNode in nextNodes)
        {
            if (HasConnection(targetCurrentNode.columnNumber, nextNode.columnNumber, establishedConnections)) continue;
            if (!allowCrossing && CheckIfPathCrosses(targetCurrentNode.columnNumber, nextNode.columnNumber, establishedConnections)) continue;

            int distance = Mathf.Abs(targetCurrentNode.columnNumber - nextNode.columnNumber);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestNode = nextNode;
            }
        }

        return bestNode;
    }

    private bool HasConnection(int startColumn, int endColumn, List<MapConnection> establishedConnections)
    {
        foreach (MapConnection connection in establishedConnections)
        {
            if (connection.startColumn == startColumn && connection.endColumn == endColumn)
            {
                return true;
            }
        }

        return false;
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
        lineObj.transform.localScale = Vector3.one;
        lineObj.transform.SetAsFirstSibling(); 

        RectTransform rect = lineObj.GetComponent<RectTransform>();
        Vector2 direction = endPoint - startPoint;
        float distance = direction.magnitude;

        if (rect != null)
        {
            SetBottomAnchoredPosition(rect, startPoint + (direction * 0.5f));
            rect.sizeDelta = new Vector2(distance, lineWidth * mapVisualScale);

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rect.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void SetBottomAnchoredPosition(RectTransform rect, Vector2 anchoredPosition)
    {
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.anchoredPosition = anchoredPosition;
    }

    private void ResizeMapContainerForScroll()
    {
        if (!resizeMapContainerForScroll || mapContainer == null) return;

        RectTransform containerRect = mapContainer as RectTransform;
        if (containerRect == null) return;

        bool hasAnyNode = false;
        float highestNodeY = 0f;
        foreach (List<MapNode> floorList in nodesByFloor.Values)
        {
            foreach (MapNode node in floorList)
            {
                if (node == null) continue;

                highestNodeY = hasAnyNode ? Mathf.Max(highestNodeY, node.UIAnchoredPosition.y) : node.UIAnchoredPosition.y;
                hasAnyNode = true;
            }
        }

        if (!hasAnyNode) return;

        float viewportHeight = GetScrollViewportHeight();
        float requiredHeight = Mathf.Max(viewportHeight + 1f, highestNodeY + verticalScrollPadding);
        containerRect.sizeDelta = new Vector2(containerRect.sizeDelta.x, requiredHeight);
    }

    private float GetScrollViewportHeight()
    {
        ScrollRect scrollRect = GetManualScrollRect();
        if (scrollRect != null && scrollRect.viewport != null)
        {
            return scrollRect.viewport.rect.height;
        }

        if (mapContainer != null && mapContainer.parent is RectTransform parentRect)
        {
            return parentRect.rect.height;
        }

        return 0f;
    }

    private void ConfigureScrollRect()
    {
        if (!autoConfigureScrollRect || mapContainer == null) return;

        RectTransform contentRect = mapContainer as RectTransform;
        if (contentRect == null) return;

        ScrollRect scrollRect = mapScrollRect;
        if (scrollRect == null)
        {
            scrollRect = mapContainer.GetComponentInParent<ScrollRect>();
        }

        if (scrollRect == null) return;

        scrollRect.content = contentRect;
        scrollRect.vertical = true;
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        if (scrollRect.viewport == null)
        {
            RectTransform viewportRect = null;
            if (scrollRect.transform != mapContainer)
            {
                viewportRect = scrollRect.transform as RectTransform;
            }
            else if (mapContainer.parent != null)
            {
                viewportRect = mapContainer.parent as RectTransform;
            }

            scrollRect.viewport = viewportRect;
        }

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
        EnsureFullScreenScrollDragArea(scrollRect);
    }

    private void NormalizeMapContentScale()
    {
        if (!forceMapContentScaleOne || mapContainer == null) return;

        mapContainer.localScale = Vector3.one;

        if (forceMapContentBottomPivot && mapContainer is RectTransform contentRect)
        {
            contentRect.anchorMin = new Vector2(0.5f, 0f);
            contentRect.anchorMax = new Vector2(0.5f, 0f);
            contentRect.pivot = new Vector2(0.5f, 0f);
            contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, 0f);
        }
    }

    private void ScrollToBottom()
    {
        if (!scrollToBottomAfterGenerate) return;

        ScrollRect scrollRect = GetManualScrollRect();
        if (scrollRect == null) return;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }

    private ScrollRect GetManualScrollRect()
    {
        if (mapScrollRect != null) return mapScrollRect;
        if (mapContainer == null) return null;

        mapScrollRect = mapContainer.GetComponentInParent<ScrollRect>();
        return mapScrollRect;
    }

    private void PrepareScrollContainer()
    {
        if (!autoConfigureScrollRect || mapContainer == null) return;

        ScrollRect scrollRectOnMapContainer = mapContainer.GetComponent<ScrollRect>();
        if (scrollRectOnMapContainer == null)
        {
            mapScrollRect = mapContainer.GetComponentInParent<ScrollRect>();
            return;
        }

        mapScrollRect = scrollRectOnMapContainer;

        Transform existingContent = mapContainer.Find("GeneratedMapContent");
        RectTransform contentRect = existingContent as RectTransform;
        if (contentRect == null)
        {
            GameObject contentObject = new GameObject("GeneratedMapContent", typeof(RectTransform));
            contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.SetParent(mapContainer, false);
        }

        contentRect.anchorMin = new Vector2(0f, 0f);
        contentRect.anchorMax = new Vector2(1f, 0f);
        contentRect.pivot = new Vector2(0.5f, 0f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, contentRect.sizeDelta.y);

        mapScrollRect.viewport = mapContainer as RectTransform;
        mapScrollRect.content = contentRect;
        mapScrollRect.vertical = true;
        mapScrollRect.horizontal = false;
        mapScrollRect.movementType = ScrollRect.MovementType.Clamped;

        mapContainer = contentRect;
    }

    private void EnsureFullScreenScrollDragArea(ScrollRect scrollRect)
    {
        if (!createFullScreenScrollDragArea || scrollRect == null) return;

        RectTransform scrollRectTransform = scrollRect.transform as RectTransform;
        if (scrollRectTransform == null) return;

        Transform existingDragArea = scrollRectTransform.Find("MapScrollDragArea");
        RectTransform dragAreaRect = existingDragArea as RectTransform;
        if (dragAreaRect == null)
        {
            GameObject dragAreaObject = new GameObject("MapScrollDragArea", typeof(RectTransform), typeof(Image), typeof(ScrollRectDragArea));
            dragAreaRect = dragAreaObject.GetComponent<RectTransform>();
            dragAreaRect.SetParent(scrollRectTransform, false);
        }

        dragAreaRect.SetAsFirstSibling();
        dragAreaRect.anchorMin = Vector2.zero;
        dragAreaRect.anchorMax = Vector2.one;
        dragAreaRect.offsetMin = Vector2.zero;
        dragAreaRect.offsetMax = Vector2.zero;

        Image dragAreaImage = dragAreaRect.GetComponent<Image>();
        if (dragAreaImage != null)
        {
            dragAreaImage.color = new Color(1f, 1f, 1f, 0f);
            dragAreaImage.raycastTarget = true;
        }

        ScrollRectDragArea dragArea = dragAreaRect.GetComponent<ScrollRectDragArea>();
        if (dragArea != null)
        {
            dragArea.targetScrollRect = scrollRect;
        }
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
