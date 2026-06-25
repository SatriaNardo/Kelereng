using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldMapManager : MonoBehaviour
{
    [Header("World Nodes")]
    public WorldMapNode[] worldNodes = new WorldMapNode[6];
    public bool autoFindChildNodes = true;

    [Header("Map Configs")]
    public WorldMapConfigSO[] worldMapConfigs = new WorldMapConfigSO[6];

    [Header("Scenes")]
    public string pathMapSceneName = "MapScene";

    [Header("Open Animation")]
    public float zoomDuration = 0.35f;
    public float zoomScale = 1.2f;

    private bool isOpeningNode = false;

    private void Start()
    {
        RefreshWorldNodes();
    }

    public void RefreshWorldNodes()
    {
        CacheWorldNodes();

        ProgressionManager progression = ProgressionManager.Instance;
        int totalNodes = worldNodes != null ? worldNodes.Length : 0;
        if (progression != null)
        {
            progression.totalWorldMapNodes = totalNodes;
        }

        for (int i = 0; i < totalNodes; i++)
        {
            if (worldNodes[i] == null) continue;

            bool isUnlocked = progression == null ? i == 0 : i <= progression.highestUnlockedWorldMapIndex;
            bool isCleared = progression != null && i < progression.highestUnlockedWorldMapIndex;
            worldNodes[i].Setup(this, i, isUnlocked, isCleared);
        }
    }

    public void SelectWorldNode(int nodeIndex, RectTransform nodeTransform)
    {
        if (isOpeningNode) return;

        ProgressionManager progression = ProgressionManager.Instance;
        bool isUnlocked = progression == null ? nodeIndex == 0 : nodeIndex <= progression.highestUnlockedWorldMapIndex;
        if (!isUnlocked) return;

        StartCoroutine(OpenWorldNode(nodeIndex, nodeTransform));
    }

    private IEnumerator OpenWorldNode(int nodeIndex, RectTransform nodeTransform)
    {
        isOpeningNode = true;

        if (nodeTransform != null)
        {
            Vector3 startScale = nodeTransform.localScale;
            Vector3 targetScale = Vector3.one * zoomScale;
            float timer = 0f;

            while (timer < zoomDuration)
            {
                timer += Time.deltaTime;
                nodeTransform.localScale = Vector3.Lerp(startScale, targetScale, timer / zoomDuration);
                yield return null;
            }
        }

        WorldMapConfigSO selectedConfig = GetWorldMapConfig(nodeIndex);
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.StartWorldMapNode(nodeIndex, selectedConfig);
        }
        else
        {
            ProgressionManager.SetPendingWorldMapNode(nodeIndex, selectedConfig);
        }

        SceneManager.LoadScene(pathMapSceneName);
    }

    private void CacheWorldNodes()
    {
        if (!autoFindChildNodes && worldNodes != null && worldNodes.Length > 0) return;

        WorldMapNode[] childNodes = GetComponentsInChildren<WorldMapNode>(true);
        if (childNodes == null || childNodes.Length == 0) return;

        List<WorldMapNode> sortedNodes = new List<WorldMapNode>(childNodes);
        sortedNodes.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
        worldNodes = sortedNodes.ToArray();
    }

    private WorldMapConfigSO GetWorldMapConfig(int nodeIndex)
    {
        if (worldMapConfigs == null) return null;
        if (nodeIndex < 0 || nodeIndex >= worldMapConfigs.Length) return null;
        return worldMapConfigs[nodeIndex];
    }
}
