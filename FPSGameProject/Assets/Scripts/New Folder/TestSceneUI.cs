using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestSceneUI : MonoBehaviour
{
    [Header("UI References")]
    public Button loadGameSceneButton;
    public Button loadMainMenuButton;
    public Button reloadSceneButton;
    public Button pauseButton;
    public Button resumeButton;
    public Button addScoreButton;
    public Button gameOverButton;

    [Header("Display")]
    public TextMeshProUGUI currentSceneText;
    public TextMeshProUGUI gameStateText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI loadingProgressText;
    public Slider loadingProgressSlider;

    private void Start()
    {
        SetupButtons();
        UpdateUI();

        // 이벤트 구독
        GameEvents.OnSceneChanged += OnSceneChanged;
        GameEvents.OnScoreChanged += OnScoreChanged;
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        GameEvents.OnSceneChanged -= OnSceneChanged;
        GameEvents.OnScoreChanged -= OnScoreChanged;
    }

    private void Update()
    {
        UpdateUI();
    }

    private void SetupButtons()
    {
        // 씬 로딩 버튼들
        if (loadGameSceneButton != null)
            loadGameSceneButton.onClick.AddListener(() => SceneManager.Instance.LoadScene("GameScene"));

        if (loadMainMenuButton != null)
            loadMainMenuButton.onClick.AddListener(() => SceneManager.Instance.LoadMainMenu());

        if (reloadSceneButton != null)
            reloadSceneButton.onClick.AddListener(() => SceneManager.Instance.ReloadCurrentScene());

        // 게임 매니저 버튼들
        if (pauseButton != null)
            pauseButton.onClick.AddListener(() => GameManager.Instance.PauseGame());

        if (resumeButton != null)
            resumeButton.onClick.AddListener(() => GameManager.Instance.ResumeGame());

        if (addScoreButton != null)
            addScoreButton.onClick.AddListener(() => GameManager.Instance.AddScore(100));

        if (gameOverButton != null)
            gameOverButton.onClick.AddListener(() => GameManager.Instance.GameOver());
    }

    private void UpdateUI()
    {
        // 현재 씬 이름 표시
        if (currentSceneText != null && SceneManager.Instance != null)
        {
            currentSceneText.text = $"현재 씬: {SceneManager.Instance.GetCurrentSceneName()}";
        }

        // 게임 상태 표시
        if (gameStateText != null && GameManager.Instance != null)
        {
            gameStateText.text = $"게임 상태: {GameManager.Instance.currentGameState}";

            // 일시정지 상태면 색상 변경
            if (GameManager.Instance.currentGameState == GameState.Paused)
            {
                gameStateText.color = Color.red;
            }
            else
            {
                gameStateText.color = Color.white;
            }
        }

        // 점수 표시
        if (scoreText != null && GameManager.Instance != null)
        {
            scoreText.text = $"점수: {GameManager.Instance.currentScore:N0}";
        }

        // 로딩 진행률 표시
        if (SceneManager.Instance != null && SceneManager.Instance.IsLoading())
        {
            float progress = SceneManager.Instance.LoadingProgress;

            if (loadingProgressText != null)
            {
                loadingProgressText.text = $"로딩 중... {progress * 100:F1}%";
                loadingProgressText.gameObject.SetActive(true);
            }

            if (loadingProgressSlider != null)
            {
                loadingProgressSlider.value = progress;
                loadingProgressSlider.gameObject.SetActive(true);
            }
        }
        else
        {
            if (loadingProgressText != null)
                loadingProgressText.gameObject.SetActive(false);

            if (loadingProgressSlider != null)
                loadingProgressSlider.gameObject.SetActive(false);
        }
    }

    private void OnSceneChanged(string sceneName)
    {
        Debug.Log($"씬 변경 이벤트 수신: {sceneName}");
    }

    private void OnScoreChanged(int newScore)
    {
        Debug.Log($"점수 변경 이벤트 수신: {newScore}");
    }

    // 디버그용 메서드들
    public void DebugCurrentState()
    {
        Debug.Log($"=== 현재 상태 ===");
        Debug.Log($"씬: {SceneManager.Instance?.GetCurrentSceneName()}");
        Debug.Log($"게임 상태: {GameManager.Instance?.currentGameState}");
        Debug.Log($"점수: {GameManager.Instance?.currentScore}");
        Debug.Log($"로딩 중: {SceneManager.Instance?.IsLoading()}");
    }
}