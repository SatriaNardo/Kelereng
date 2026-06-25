using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FightSceneLightingSetup : MonoBehaviour
{
    [Header("Ambient Light")]
    [Range(0f, 2f)] public float globalIntensity = 0.45f;
    public Color globalLightColor = new Color(0.74f, 0.8f, 1f, 1f);

    [Header("Key Light")]
    public Vector2 keyLightPosition = new Vector2(-2.6f, 3.2f);
    [Range(0f, 4f)] public float keyLightIntensity = 1.15f;
    [Min(0.1f)] public float keyLightRadius = 7.5f;
    public Color keyLightColor = new Color(1f, 0.9f, 0.72f, 1f);

    private const string GlobalLightName = "Fight Global Light 2D";
    private const string KeyLightName = "Fight Key Light 2D";

    private void Awake()
    {
        SetupGlobalLight();
        SetupKeyLight();
    }

    private void SetupGlobalLight()
    {
        Light2D globalLight = FindNamedLight(GlobalLightName);
        if (globalLight == null)
        {
            globalLight = new GameObject(GlobalLightName).AddComponent<Light2D>();
        }

        globalLight.lightType = Light2D.LightType.Global;
        globalLight.intensity = globalIntensity;
        globalLight.color = globalLightColor;
        globalLight.blendStyleIndex = 0;
        globalLight.targetSortingLayers = GetAllSortingLayerIds();
    }

    private void SetupKeyLight()
    {
        Light2D keyLight = FindNamedLight(KeyLightName);
        if (keyLight == null)
        {
            keyLight = new GameObject(KeyLightName).AddComponent<Light2D>();
        }

        keyLight.transform.position = new Vector3(keyLightPosition.x, keyLightPosition.y, -1f);
        keyLight.lightType = Light2D.LightType.Point;
        keyLight.intensity = keyLightIntensity;
        keyLight.color = keyLightColor;
        keyLight.pointLightInnerRadius = keyLightRadius * 0.45f;
        keyLight.pointLightOuterRadius = keyLightRadius;
        keyLight.shadowsEnabled = false;
        keyLight.blendStyleIndex = 0;
        keyLight.targetSortingLayers = GetAllSortingLayerIds();
    }

    private Light2D FindNamedLight(string lightName)
    {
        Light2D[] lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        foreach (Light2D light in lights)
        {
            if (light != null && light.name == lightName)
            {
                return light;
            }
        }

        return null;
    }

    private int[] GetAllSortingLayerIds()
    {
        SortingLayer[] layers = SortingLayer.layers;
        int[] ids = new int[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            ids[i] = layers[i].id;
        }

        return ids;
    }
}
