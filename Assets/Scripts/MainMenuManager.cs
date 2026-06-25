using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject elementSelectPanel;
    public GameObject settingsPanel;

    [Header("Starting Elements")]
    public MarbleElementSO earthElementAsset;
    public MarbleElementSO windElementAsset;
    public MarbleElementSO fireElementAsset;
    public MarbleElementSO waterElementAsset;

    [Header("Selection UI")]
    public TMP_Text selectedElementNameText;

    [Header("Scene Names")]
    public string mapSceneName = "WorldMapScene";

    private MarbleElementSO selectedStartingElement;

    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        SetPanel(mainMenuPanel, true);
        SetPanel(elementSelectPanel, false);
        SetPanel(settingsPanel, false);
    }

    public void ShowElementSelect()
    {
        SetPanel(mainMenuPanel, false);
        SetPanel(elementSelectPanel, true);
        SetPanel(settingsPanel, false);

        if (selectedStartingElement == null)
        {
            SelectFire();
        }
        else
        {
            UpdateSelectedElementText();
        }
    }

    public void ShowSettings()
    {
        SetPanel(mainMenuPanel, false);
        SetPanel(elementSelectPanel, false);
        SetPanel(settingsPanel, true);
    }

    public void SelectEarth()
    {
        SelectStartingElement(earthElementAsset);
    }

    public void SelectWind()
    {
        SelectStartingElement(windElementAsset);
    }

    public void SelectFire()
    {
        SelectStartingElement(fireElementAsset);
    }

    public void SelectWater()
    {
        SelectStartingElement(waterElementAsset);
    }

    public void StartGame()
    {
        if (selectedStartingElement == null)
        {
            Debug.LogWarning("Choose a starting marble element first.");
            return;
        }

        if (ProgressionManager.Instance == null)
        {
            ProgressionManager.SetPendingStartingElement(selectedStartingElement);
        }
        else
        {
            ProgressionManager.Instance.StartNewRunWithStartingElement(selectedStartingElement);
        }

        SceneManager.LoadScene(mapSceneName);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    private void SelectStartingElement(MarbleElementSO element)
    {
        if (element == null)
        {
            Debug.LogWarning("Starting element asset is not assigned.");
            return;
        }

        selectedStartingElement = element;
        UpdateSelectedElementText();
    }

    private void UpdateSelectedElementText()
    {
        if (selectedElementNameText != null)
        {
            selectedElementNameText.text = selectedStartingElement != null ? selectedStartingElement.elementName : "";
        }
    }

    private void SetPanel(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }
}
