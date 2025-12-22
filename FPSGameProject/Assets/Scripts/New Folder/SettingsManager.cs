using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

[System.Serializable]
public class GameSettings
{
    [Header("Display Settings")]
    public int resolutionIndex = 0;
    public bool isFullscreen = true;
    public int targetFrameRate = 60;

    [Header("Audio Settings")]
    public float masterVolume = 1f;
    public float sfxVolume = 1f;
    public float musicVolume = 1f;

    [Header("UI Settings")]
    public float uiScale = 1f;

    [Header("Graphics Settings")]
    public int qualityLevel = 2;
    public bool vsyncEnabled = true;
}

public class SettingsManager : SingletonMonoBehaviour<SettingsManager>
{
    [Header("Audio References")]
    public AudioMixer masterMixer;

    [Header("UI Scale References")]
    public Canvas mainCanvas;

    [Header("Settings")]
    public GameSettings currentSettings = new GameSettings();

    private Resolution[] availableResolutions;
    private const string SETTINGS_KEY = "GameSettings";

    // 오디오 믹서 파라미터 이름
    private const string MASTER_VOLUME_PARAM = "MasterVolume";
    private const string SFX_VOLUME_PARAM = "SFXVolume";
    private const string MUSIC_VOLUME_PARAM = "MusicVolume";

    protected override void Awake()
    {
        base.Awake();
        InitializeSettings();
    }

    private void Start()
    {
        LoadSettings();
        ApplyAllSettings();
    }

    private void InitializeSettings()
    {
        // 사용 가능한 해상도 목록 가져오기
        availableResolutions = Screen.resolutions;

        // 현재 해상도를 기본값으로 설정
        Resolution currentRes = Screen.currentResolution;
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            if (availableResolutions[i].width == currentRes.width &&
                availableResolutions[i].height == currentRes.height)
            {
                currentSettings.resolutionIndex = i;
                break;
            }
        }

        // 현재 품질 설정 가져오기
        currentSettings.qualityLevel = QualitySettings.GetQualityLevel();
        currentSettings.vsyncEnabled = QualitySettings.vSyncCount > 0;

        Debug.Log("SettingsManager 초기화 완료");
    }

    #region Save/Load Settings

    public void SaveSettings()
    {
        string json = JsonUtility.ToJson(currentSettings, true);
        PlayerPrefs.SetString(SETTINGS_KEY, json);
        PlayerPrefs.Save();

        Debug.Log("설정 저장됨");
    }

    public void LoadSettings()
    {
        if (PlayerPrefs.HasKey(SETTINGS_KEY))
        {
            string json = PlayerPrefs.GetString(SETTINGS_KEY);
            JsonUtility.FromJsonOverwrite(json, currentSettings);

            Debug.Log("설정 로드됨");
        }
        else
        {
            Debug.Log("저장된 설정이 없어 기본값 사용");
        }
    }

    public void ResetToDefault()
    {
        currentSettings = new GameSettings();
        ApplyAllSettings();
        SaveSettings();

        Debug.Log("설정이 기본값으로 초기화됨");
    }

    #endregion

    #region Apply Settings

    public void ApplyAllSettings()
    {
        ApplyDisplaySettings();
        ApplyAudioSettings();
        ApplyUISettings();
        ApplyGraphicsSettings();
    }

    private void ApplyDisplaySettings()
    {
        // 해상도 적용
        if (availableResolutions != null && currentSettings.resolutionIndex < availableResolutions.Length)
        {
            Resolution targetRes = availableResolutions[currentSettings.resolutionIndex];
            Screen.SetResolution(targetRes.width, targetRes.height, currentSettings.isFullscreen);
        }

        // 프레임레이트 설정
        Application.targetFrameRate = currentSettings.targetFrameRate;
    }

    private void ApplyAudioSettings()
    {
        if (masterMixer != null)
        {
            // 볼륨을 dB로 변환 (0-1 범위를 -80dB-0dB로)
            float masterDB = currentSettings.masterVolume > 0 ?
                Mathf.Log10(currentSettings.masterVolume) * 20 : -80f;
            float sfxDB = currentSettings.sfxVolume > 0 ?
                Mathf.Log10(currentSettings.sfxVolume) * 20 : -80f;
            float musicDB = currentSettings.musicVolume > 0 ?
                Mathf.Log10(currentSettings.musicVolume) * 20 : -80f;

            masterMixer.SetFloat(MASTER_VOLUME_PARAM, masterDB);
            masterMixer.SetFloat(SFX_VOLUME_PARAM, sfxDB);
            masterMixer.SetFloat(MUSIC_VOLUME_PARAM, musicDB);
        }
        else
        {
            // 오디오 믹서가 없으면 AudioListener 볼륨 조정
            AudioListener.volume = currentSettings.masterVolume;
        }
    }

    private void ApplyUISettings()
    {
        if (mainCanvas != null)
        {
            var canvasScaler = mainCanvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (canvasScaler != null)
            {
                canvasScaler.scaleFactor = currentSettings.uiScale;
            }
        }
    }

    private void ApplyGraphicsSettings()
    {
        QualitySettings.SetQualityLevel(currentSettings.qualityLevel);
        QualitySettings.vSyncCount = currentSettings.vsyncEnabled ? 1 : 0;
    }

    #endregion

    #region Individual Setting Methods

    public void SetResolution(int resolutionIndex)
    {
        if (resolutionIndex >= 0 && resolutionIndex < availableResolutions.Length)
        {
            currentSettings.resolutionIndex = resolutionIndex;
            ApplyDisplaySettings();
        }
    }

    public void SetFullscreen(bool fullscreen)
    {
        currentSettings.isFullscreen = fullscreen;
        ApplyDisplaySettings();
    }

    public void SetTargetFrameRate(int frameRate)
    {
        currentSettings.targetFrameRate = frameRate;
        Application.targetFrameRate = frameRate;
    }

    public void SetMasterVolume(float volume)
    {
        currentSettings.masterVolume = Mathf.Clamp01(volume);
        ApplyAudioSettings();
        GameEvents.VolumeChanged(currentSettings.masterVolume);
    }

    public void SetSFXVolume(float volume)
    {
        currentSettings.sfxVolume = Mathf.Clamp01(volume);
        ApplyAudioSettings();
    }

    public void SetMusicVolume(float volume)
    {
        currentSettings.musicVolume = Mathf.Clamp01(volume);
        ApplyAudioSettings();
    }

    public void SetQualityLevel(int qualityLevel)
    {
        currentSettings.qualityLevel = Mathf.Clamp(qualityLevel, 0, QualitySettings.names.Length - 1);
        ApplyGraphicsSettings();
    }

    public void SetVSync(bool enabled)
    {
        currentSettings.vsyncEnabled = enabled;
        ApplyGraphicsSettings();
    }

    #endregion

    #region Getters

    public Resolution[] GetAvailableResolutions()
    {
        return availableResolutions;
    }

    public string[] GetResolutionStrings()
    {
        if (availableResolutions == null) return new string[0];

        string[] resStrings = new string[availableResolutions.Length];
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            resStrings[i] = $"{availableResolutions[i].width} x {availableResolutions[i].height}";
        }
        return resStrings;
    }

    public string[] GetQualityLevelStrings()
    {
        return QualitySettings.names;
    }

    #endregion
}