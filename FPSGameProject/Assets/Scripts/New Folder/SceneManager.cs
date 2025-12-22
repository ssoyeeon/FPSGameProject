using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class SceneManager : SingletonMonoBehaviour<SceneManager>
{
    [Header("Scene Settings")]
    public string mainMenuSceneName = "MainMenu";
    public string gameSceneName = "GameScene";
    public string loadingSceneName = "Loading";

    [Header("Loading Settings")]
    public float minimumLoadingTime = 2f;
    public bool useLoadingScreen = true;
    public bool useFadeEffect = true;

    [Header("Fade Settings")]
    public float fadeSpeed = 1f;
    public Color fadeColor = Color.black;

    private string currentSceneName;
    private string targetSceneName;
    private bool isLoading = false;
    private CanvasGroup fadeCanvasGroup;
    private GameObject fadeObject;

    // 씬 로딩 진행률 추적
    public float LoadingProgress { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        // 현재 씬 이름 저장
        currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // 페이드 UI 생성
        if (useFadeEffect)
        {
            CreateFadeUI();
        }

        Debug.Log("SceneManager 초기화 완료");
    }

    private void Start()
    {
        // 씬 로딩 이벤트 구독
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    #region Fade UI 생성

    private void CreateFadeUI()
    {
        // 페이드용 Canvas 생성
        fadeObject = new GameObject("FadeCanvas");
        //fadeObject.transform.SetParent(transform);

        Canvas canvas = fadeObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // 가장 위에 렌더링

        // CanvasGroup 추가
        fadeCanvasGroup = fadeObject.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;

        // 페이드 이미지 생성
        GameObject fadeImage = new GameObject("FadeImage");
        fadeImage.transform.SetParent(fadeObject.transform);

        UnityEngine.UI.Image image = fadeImage.AddComponent<UnityEngine.UI.Image>();
        image.color = fadeColor;

        // 전체 화면 크기로 설정
        RectTransform rect = fadeImage.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        DontDestroyOnLoad(fadeObject);
    }

    #endregion

    #region Scene Loading Methods

    /// <summary>
    /// 기본 씬 로딩 (로딩 화면 없음)
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (isLoading)
        {
            Debug.LogWarning("이미 씬 로딩 중입니다.");
            return;
        }

        targetSceneName = sceneName;

        if (useFadeEffect)
        {
            StartCoroutine(LoadSceneWithFade(sceneName));
        }
        else
        {
            StartCoroutine(LoadSceneAsync(sceneName));
        }
    }

    /// <summary>
    /// 로딩 화면을 사용한 씬 로딩
    /// </summary>
    public void LoadSceneWithLoadingScreen(string sceneName)
    {
        if (isLoading)
        {
            Debug.LogWarning("이미 씬 로딩 중입니다.");
            return;
        }

        targetSceneName = sceneName;
        StartCoroutine(LoadSceneWithLoading(sceneName));
    }

    /// <summary>
    /// 페이드 효과와 함께 씬 로딩
    /// </summary>
    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        isLoading = true;
        GameManager.Instance?.ChangeGameState(GameState.Loading);

        // 페이드 인
        yield return StartCoroutine(FadeIn());

        // 씬 로딩
        yield return StartCoroutine(LoadSceneAsync(sceneName));

        // 페이드 아웃
        yield return StartCoroutine(FadeOut());

        isLoading = false;
    }

    /// <summary>
    /// 로딩 화면을 사용한 씬 로딩
    /// </summary>
    private IEnumerator LoadSceneWithLoading(string sceneName)
    {
        isLoading = true;
        GameManager.Instance?.ChangeGameState(GameState.Loading);

        // 로딩 화면 로드
        if (useLoadingScreen && !string.IsNullOrEmpty(loadingSceneName))
        {
            yield return StartCoroutine(LoadSceneAsync(loadingSceneName));

            // 최소 로딩 시간 대기
            float startTime = Time.time;

            // 실제 씬 로딩
            AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;

            while (!asyncLoad.isDone)
            {
                LoadingProgress = asyncLoad.progress;

                // 로딩이 90% 완료되면 대기
                if (asyncLoad.progress >= 0.9f)
                {
                    LoadingProgress = 1f;

                    // 최소 로딩 시간 체크
                    if (Time.time - startTime >= minimumLoadingTime)
                    {
                        asyncLoad.allowSceneActivation = true;
                    }
                }

                yield return null;
            }
        }
        else
        {
            // 로딩 화면 없이 직접 로딩
            yield return StartCoroutine(LoadSceneAsync(sceneName));
        }

        isLoading = false;
    }

    /// <summary>
    /// 비동기 씬 로딩
    /// </summary>
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            LoadingProgress = asyncLoad.progress;
            yield return null;
        }

        LoadingProgress = 1f;
    }

    #endregion

    #region Fade Effects

    private IEnumerator FadeIn()
    {
        if (fadeCanvasGroup == null) yield break;

        fadeCanvasGroup.blocksRaycasts = true;

        while (fadeCanvasGroup.alpha < 1f)
        {
            fadeCanvasGroup.alpha += fadeSpeed * Time.unscaledDeltaTime;
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        if (fadeCanvasGroup == null) yield break;

        while (fadeCanvasGroup.alpha > 0f)
        {
            fadeCanvasGroup.alpha -= fadeSpeed * Time.unscaledDeltaTime;
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }

    #endregion

    #region Quick Load Methods

    public void LoadMainMenu()
    {
        LoadScene(mainMenuSceneName);
    }

    public void LoadGameScene()
    {
        LoadScene(gameSceneName);
    }

    public void ReloadCurrentScene()
    {
        LoadScene(currentSceneName);
    }

    public void LoadNextScene()
    {
        int currentIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        int nextIndex = (currentIndex + 1) % UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;

        UnityEngine.SceneManagement.SceneManager.LoadScene(nextIndex);
    }

    public void LoadPreviousScene()
    {
        int currentIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        int prevIndex = currentIndex - 1;

        if (prevIndex < 0)
            prevIndex = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings - 1;

        UnityEngine.SceneManagement.SceneManager.LoadScene(prevIndex);
    }

    #endregion

    #region Scene Events

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        LoadingProgress = 0f;

        // SceneConfig가 GameState를 처리하도록 이벤트만 발생
        GameEvents.SceneChanged(currentSceneName);

        Debug.Log($"씬 로딩 완료: {scene.name}");
    }

    private void OnSceneUnloaded(Scene scene)
    {
        Debug.Log($"씬 언로드 완료: {scene.name}");
    }

    #endregion

    #region Utility Methods

    public string GetCurrentSceneName()
    {
        return currentSceneName;
    }

    public bool IsLoading()
    {
        return isLoading;
    }

    public bool IsSceneLoaded(string sceneName)
    {
        return currentSceneName == sceneName;
    }

    public void SetFadeColor(Color color)
    {
        fadeColor = color;

        if (fadeObject != null)
        {
            UnityEngine.UI.Image fadeImage = fadeObject.GetComponentInChildren<UnityEngine.UI.Image>();
            if (fadeImage != null)
            {
                fadeImage.color = color;
            }
        }
    }

    #endregion
}