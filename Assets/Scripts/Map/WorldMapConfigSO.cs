using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WorldMapConfig", menuName = "Kelereng/World Map Config")]
public class WorldMapConfigSO : ScriptableObject
{
    [Header("Map Identity")]
    public string mapName = "New Path";

    [Header("Path Layout")]
    public List<ProceduralMapGenerator.FloorConfig> mapLayout = new List<ProceduralMapGenerator.FloorConfig>();

    [Header("Random Nodes Per Floor")]
    public bool randomizeNodeCountPerFloor = true;
    [Range(1, 6)] public int minNodesPerFloor = 2;
    [Range(1, 6)] public int maxNodesPerFloor = 4;
    public bool keepSingleNodeFloors = true;

    [Header("Elite Nodes")]
    public int eliteMinFloor = 6;
    public int eliteMaxFloor = 9;
    [Range(0, 4)] public int minEliteNodesPerMap = 1;
    [Range(0, 4)] public int maxEliteNodesPerMap = 2;
}
