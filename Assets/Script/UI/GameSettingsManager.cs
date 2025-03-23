using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using static BoardController;

public class GameSettingsManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown gameModeDropdown;
    [SerializeField] private TMP_Dropdown aiWhiteDropdown;
    [SerializeField] private TMP_Dropdown aiBlackDropdown;
    [SerializeField] private Slider depthSlider;
    [SerializeField] private TextMeshProUGUI depthValueText;
    [SerializeField] private Button startGameButton;

    // Static variables to store settings
    public static GameMode selectedGameMode;
    public static bool isAIWhite;
    public static bool isAIBlack;
    public static int aiDepth;

    private void Start()
    {
        InitializeUI();
        SetupListeners();
    }

    private void InitializeUI()
    {
        // Setup Game Mode Dropdown
        gameModeDropdown.ClearOptions();
        gameModeDropdown.AddOptions(new List<string> { 
            "Human vs AI", 
            "Human vs Human", 
            "AI vs AI" 
        });

        // Setup AI Color Dropdowns
        aiWhiteDropdown.ClearOptions();
        aiBlackDropdown.ClearOptions();
        aiWhiteDropdown.AddOptions(new List<string> { "Human", "AI" });
        aiBlackDropdown.AddOptions(new List<string> { "Human", "AI" });

        // Setup Depth Slider
        depthSlider.minValue = 1;
        depthSlider.maxValue = 13;
        depthSlider.value = 4;
        UpdateDepthText(depthSlider.value);
    }

    private void SetupListeners()
    {
        gameModeDropdown.onValueChanged.AddListener(OnGameModeChanged);
        aiWhiteDropdown.onValueChanged.AddListener(OnAIWhiteChanged);
        aiBlackDropdown.onValueChanged.AddListener(OnAIBlackChanged);
        depthSlider.onValueChanged.AddListener(OnDepthChanged);
        startGameButton.onClick.AddListener(StartGame);
    }

    private void OnGameModeChanged(int value)
    {
        selectedGameMode = (GameMode)value;
        
        // Update dropdowns based on game mode
        switch (selectedGameMode)
        {
            case GameMode.HumanVsAI:
                aiWhiteDropdown.gameObject.SetActive(true);
                aiBlackDropdown.gameObject.SetActive(true);
                break;
            case GameMode.HumanVsHuman:
                aiWhiteDropdown.gameObject.SetActive(false);
                aiBlackDropdown.gameObject.SetActive(false);
                isAIWhite = false;
                isAIBlack = false;
                break;
            case GameMode.AIVsAI:
                aiWhiteDropdown.gameObject.SetActive(false);
                aiBlackDropdown.gameObject.SetActive(false);
                isAIWhite = true;
                isAIBlack = true;
                break;
        }
    }

    private void OnAIWhiteChanged(int value)
    {
        isAIWhite = value == 1; // 1 = AI, 0 = Human
    }

    private void OnAIBlackChanged(int value)
    {
        isAIBlack = value == 1; // 1 = AI, 0 = Human
    }

    private void OnDepthChanged(float value)
    {
        aiDepth = (int)value;
        UpdateDepthText(value);
    }

    private void UpdateDepthText(float value)
    {
        depthValueText.text = $"AI Depth: {value}";
    }

    private void StartGame()
    {
        // Save settings to PlayerPrefs
        PlayerPrefs.SetInt("GameMode", (int)selectedGameMode);
        PlayerPrefs.SetInt("IsAIWhite", isAIWhite ? 1 : 0);
        PlayerPrefs.SetInt("IsAIBlack", isAIBlack ? 1 : 0);
        PlayerPrefs.SetInt("AIDepth", aiDepth);
        PlayerPrefs.Save();

        // Load the game scene
        SceneManager.LoadScene("GameScene"); // Replace with your game scene name
    }
}