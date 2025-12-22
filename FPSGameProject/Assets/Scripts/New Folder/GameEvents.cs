// 게임 전체에서 사용할 이벤트 정의
public static class GameEvents
{
    public static System.Action<string> OnSceneChanged;
    public static System.Action<float> OnVolumeChanged;
    public static System.Action<int> OnScoreChanged;
    public static System.Action OnGamePaused;
    public static System.Action OnGameResumed;

    // 이벤트 호출 메서드들
    public static void SceneChanged(string sceneName) => OnSceneChanged?.Invoke(sceneName);
    public static void VolumeChanged(float volume) => OnVolumeChanged?.Invoke(volume);
    public static void ScoreChanged(int score) => OnScoreChanged?.Invoke(score);
    public static void GamePaused() => OnGamePaused?.Invoke();
    public static void GameResumed() => OnGameResumed?.Invoke();

    // 새로 추가된 설정 관련 이벤트
    public static System.Action<int> OnResolutionChanged;
    public static System.Action<bool> OnFullscreenChanged;
    public static System.Action<int> OnQualityChanged;

    // 새로 추가된 설정 이벤트 호출 메서드들
    public static void ResolutionChanged(int resolutionIndex) => OnResolutionChanged?.Invoke(resolutionIndex);
    public static void FullscreenChanged(bool isFullscreen) => OnFullscreenChanged?.Invoke(isFullscreen);
    public static void QualityChanged(int qualityLevel) => OnQualityChanged?.Invoke(qualityLevel);


}