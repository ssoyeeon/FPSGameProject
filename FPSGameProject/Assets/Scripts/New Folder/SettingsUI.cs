using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    [Header("Display Settings")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;
    public TMP_Dropdown frameRateDropdown;

    [Header("Audio Settings")]
    public Slider masterVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider musicVolumeSlider;
    public TextMeshProUGUI masterVolumeText;
    public TextMeshProUGUI sfxVolumeText;
    public TextMeshProUGUI musicVolumeText;

    [Header("Graphics Settings")]
    public TMP_Dropdown qualityDropdown;
    public Toggle vsyncToggle;

    [Header("Control Buttons")]
    public Button applyButton;
    public Button resetButton;
    public Button closeButton;

    private GameSettings tempSettings;
    private bool isInitializing = false;

    private void Start()
    {
        InitializeUI();
        SetupEventListeners();
        RefreshUI();
    }

    private void InitializeUI()
    {
        isInitializing = true;

        // 해상도 드롭다운 설정
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(new System.Collections.Generic.List<string>(
                SettingsManager.Instance.GetResolutionStrings()));
        }

        // 프레임레이트 드롭다운 설정
        if (frameRateDropdown != null)
        {
            frameRateDropdown.ClearOptions();
            frameRateDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "30 FPS", "60 FPS", "120 FPS", "무제한"
            });
        }

        // 품질 드롭다운 설정
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(
                SettingsManager.Instance.GetQualityLevelStrings()));
        }

        isInitializing = false;
    }

    private void SetupEventListeners()
    {
        // 디스플레이 설정
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

        if (frameRateDropdown != null)
            frameRateDropdown.onValueChanged.AddListener(OnFrameRateChanged);

        // 오디오 설정
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);       

        // 그래픽 설정
        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);

        if (vsyncToggle != null)
            vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);

        // 버튼 설정
        if (applyButton != null)
            applyButton.onClick.AddListener(OnApplyClicked);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
    }

    public void RefreshUI()
    {
        if (SettingsManager.Instance == null) return;

        isInitializing = true;
        tempSettings = SettingsManager.Instance.currentSettings;

        // 디스플레이 설정 UI 업데이트
        if (resolutionDropdown != null)
            resolutionDropdown.value = tempSettings.resolutionIndex;

        if (fullscreenToggle != null)
            fullscreenToggle.isOn = tempSettings.isFullscreen;

        if (frameRateDropdown != null)
        {
            int frameRateIndex = tempSettings.targetFrameRate switch
            {
                30 => 0,
                60 => 1,
                120 => 2,
                _ => 3
            };
            frameRateDropdown.value = frameRateIndex;
        }

        // 오디오 설정 UI 업데이트
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = tempSettings.masterVolume;
            if (masterVolumeText != null)
                masterVolumeText.text = $"{tempSettings.masterVolume * 100:F0}%";
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = tempSettings.sfxVolume;
            if (sfxVolumeText != null)
                sfxVolumeText.text = $"{tempSettings.sfxVolume * 100:F0}%";
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = tempSettings.musicVolume;
            if (musicVolumeText != null)
                musicVolumeText.text = $"{tempSettings.musicVolume * 100:F0}%";
        }

        // 그래픽 설정 UI 업데이트
        if (qualityDropdown != null)
            qualityDropdown.value = tempSettings.qualityLevel;

        if (vsyncToggle != null)
            vsyncToggle.isOn = tempSettings.vsyncEnabled;

        isInitializing = false;
    }

    #region Event Handlers

    private void OnResolutionChanged(int value)
    {
        if (isInitializing) return;
        tempSettings.resolutionIndex = value;
    }

    private void OnFullscreenChanged(bool value)
    {
        if (isInitializing) return;
        tempSettings.isFullscreen = value;
    }

    private void OnFrameRateChanged(int value)
    {
        if (isInitializing) return;

        int frameRate = value switch
        {
            0 => 30,
            1 => 60,
            2 => 120,
            _ => -1
        };

        tempSettings.targetFrameRate = frameRate;
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (isInitializing) return;

        tempSettings.masterVolume = value;
        if (masterVolumeText != null)
            masterVolumeText.text = $"{value * 100:F0}%";

        // 실시간 볼륨 적용
        SettingsManager.Instance.SetMasterVolume(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (isInitializing) return;

        tempSettings.sfxVolume = value;
        if (sfxVolumeText != null)
            sfxVolumeText.text = $"{value * 100:F0}%";

        // 실시간 볼륨 적용
        SettingsManager.Instance.SetSFXVolume(value);
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (isInitializing) return;

        tempSettings.musicVolume = value;
        if (musicVolumeText != null)
            musicVolumeText.text = $"{value * 100:F0}%";

        // 실시간 볼륨 적용
        SettingsManager.Instance.SetMusicVolume(value);
    } 

    private void OnQualityChanged(int value)
    {
        if (isInitializing) return;
        tempSettings.qualityLevel = value;
    }

    private void OnVSyncChanged(bool value)
    {
        if (isInitializing) return;
        tempSettings.vsyncEnabled = value;
    }

    private void OnApplyClicked()
    {
        // 임시 설정을 실제 설정에 적용
        SettingsManager.Instance.currentSettings = tempSettings;
        SettingsManager.Instance.ApplyAllSettings();
        SettingsManager.Instance.SaveSettings();

        Debug.Log("설정이 적용되었습니다");
    }

    private void OnResetClicked()
    {
        SettingsManager.Instance.ResetToDefault();
        RefreshUI();

        Debug.Log("설정이 초기화되었습니다");
    }

    private void OnCloseClicked()
    {
        // 설정 창 닫기
        gameObject.SetActive(false);
    }

    #endregion

    // 외부에서 설정 창 열기
    public void ShowSettings()
    {
        gameObject.SetActive(true);
        RefreshUI();
    }

    // ESC 키로 설정 창 닫기
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && gameObject.activeInHierarchy)
        {
            OnCloseClicked();
        }
    }
}