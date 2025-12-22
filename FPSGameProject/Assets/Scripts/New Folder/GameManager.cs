using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public enum GameState
{
    Menu,
    Playing,
    Paused,
    GameOver,
    Loading
}

public class GameManager : SingletonMonoBehaviour<GameManager>
{
    [Header("Game Settings")]
    public GameState currentGameState = GameState.Menu;
    public bool isGamePaused = false;

    [Header("Game Stats")]
    public int currentScore = 0;
    public int currentLevel = 1;
    public float gameTime = 0f;

    private GameState previousGameState;

    protected override void Awake()
    {
        base.Awake();
        InitializeGame();
    }

    private void Start()
    {
        // 다른 매니저들이 초기화된 후 실행
        StartCoroutine(InitializeManagers());
    }

    private void Update()
    {
        if (currentGameState == GameState.Playing && !isGamePaused)
        {
            gameTime += Time.deltaTime;
        }

        HandleInput();
    }

    private void InitializeGame()
    {
        // 게임 시작 시 기본 설정
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        Debug.Log("GameManager 초기화 완료");
    }

    private IEnumerator InitializeManagers()
    {
        yield return new WaitForEndOfFrame();

        // 매니저들 초기화 순서 중요
        //if (SceneManager.Instance != null)
        //    Debug.Log("SceneManager 연결 확인");
        //if (SoundManager.Instance != null)
        //    Debug.Log("SoundManager 연결 확인");
        //if (UIManager.Instance != null)
        //    Debug.Log("UIManager 연결 확인");
        //if (SaveManager.Instance != null)
        //    Debug.Log("SaveManager 연결 확인");
    }

    private void HandleInput()
    {
        // ESC 키로 게임 일시정지/재개
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentGameState == GameState.Playing)
            {
                PauseGame();
            }
            else if (currentGameState == GameState.Paused)
            {
                ResumeGame();
            }
        }
    }

    #region Game State Management

    public void ChangeGameState(GameState newState)
    {
        if (currentGameState == newState) return;

        previousGameState = currentGameState;
        currentGameState = newState;

        OnGameStateChanged(newState);

        Debug.Log($"게임 상태 변경: {previousGameState} -> {currentGameState}");
    }

    private void OnGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Menu:
                
                break;
            case GameState.Playing:
               
                break;
            case GameState.Paused:
               
                break;
            case GameState.GameOver:
               
                break;
            case GameState.Loading:
               
                break;
        }
    }

    public void StartGame()
    {
        ResetGameStats();
        ChangeGameState(GameState.Playing);
        GameEvents.GameResumed();
    }

    public void PauseGame()
    {
        if (currentGameState != GameState.Playing) return;

        isGamePaused = true;
        ChangeGameState(GameState.Paused);
        GameEvents.GamePaused();

        // 일시정지 UI 표시
        //UIManager.Instance?.ShowPauseMenu();
    }

    public void ResumeGame()
    {
        if (currentGameState != GameState.Paused) return;

        isGamePaused = false;
        ChangeGameState(GameState.Playing);
        GameEvents.GameResumed();

        // 일시정지 UI 숨기기
        //UIManager.Instance?.HidePauseMenu();
    }

    public void GameOver()
    {
        ChangeGameState(GameState.GameOver);

        // 게임 오버 처리
        //SaveManager.Instance?.SaveHighScore(currentScore);
        //UIManager.Instance?.ShowGameOverUI();
    }

    public void RestartGame()
    {
        ResetGameStats();
        ChangeGameState(GameState.Playing);
    }

    public void GoToMainMenu()
    {
        ChangeGameState(GameState.Menu);
        //SceneManager.Instance?.LoadScene("MainMenu");
    }

    #endregion

    #region Score & Stats Management

    public void AddScore(int points)
    {
        currentScore += points;
        GameEvents.ScoreChanged(currentScore);

        Debug.Log($"점수 추가: +{points}, 총 점수: {currentScore}");
    }

    public void SetScore(int score)
    {
        currentScore = score;
        GameEvents.ScoreChanged(currentScore);
    }

    public void NextLevel()
    {
        currentLevel++;
        Debug.Log($"레벨업! 현재 레벨: {currentLevel}");
    }

    private void ResetGameStats()
    {
        currentScore = 0;
        currentLevel = 1;
        gameTime = 0f;
        GameEvents.ScoreChanged(currentScore);
    }

    #endregion

    #region Utility Methods

    public bool IsGamePlaying()
    {
        return currentGameState == GameState.Playing && !isGamePaused;
    }

    public void QuitGame()
    {
        Debug.Log("게임 종료");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && currentGameState == GameState.Playing)
        {
            PauseGame();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && currentGameState == GameState.Playing)
        {
            PauseGame();
        }
    }
}