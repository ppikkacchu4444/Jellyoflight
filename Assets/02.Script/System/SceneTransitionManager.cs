using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환 시 화면 페이드 인/아웃 효과를 관리하는 싱글톤 클래스.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager instance;
    public static SceneTransitionManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<SceneTransitionManager>();
            }
            return instance;
        }
    }

    [Header("페이드 설정")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.0f;

    private Coroutine currentFadeCoroutine;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            StartFade(1, 0);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 로드 완료 후 화면을 서서히 밝게 함
        StartFade(1, 0);
    }

    private static bool isRetry = false;
    public static bool IsRetry => isRetry;

    /// <summary>
    /// 지정된 씬으로 페이드 효과와 함께 전환을 시작함.
    /// </summary>
    /// <param name="sceneName">이동할 씬의 이름</param>
    public void TransitionToScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        isRetry = false; // 일반적인 이동 시에는 리트라이 아님
        StartCoroutine(TransitionRoutine(sceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        // 1. 화면을 어둡게 만듦 (Fade Out)
        yield return StartCoroutine(FadeRoutine(0, 1));

        // 2. 씬 로드 수행
        SceneManager.LoadScene(sceneName);
    }

    private void StartFade(float startAlpha, float endAlpha)
    {
        if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
        currentFadeCoroutine = StartCoroutine(FadeRoutine(startAlpha, endAlpha));
    }

    private IEnumerator FadeRoutine(float startAlpha, float endAlpha)
    {
        if (fadeImage == null) yield break;

        // [추가] 화면이 어두워질 때(씬 전환 직전) 오디오 페이드 아웃을 위한 데이터 수집
        AudioSource[] allSources = null;
        float[] startVolumes = null;
        bool isFadingOut = endAlpha > 0.5f;

        if (isFadingOut)
        {
            AudioSource[] foundSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            List<AudioSource> validSources = new List<AudioSource>();

            foreach (var src in foundSources)
            {
                // DontDestroyOnLoad 씬에 있는 오브젝트(예: SoundManager)는 제외하고 
                // 현재 씬에 배치된 오디오(BGM 등)만 페이드 아웃 대상으로 선정합니다.
                if (src.gameObject.scene.name != "DontDestroyOnLoad")
                {
                    validSources.Add(src);
                }
            }

            allSources = validSources.ToArray();
            startVolumes = new float[allSources.Length];
            for (int i = 0; i < allSources.Length; i++)
            {
                startVolumes[i] = allSources[i].volume;
            }
        }

        // 화면이 어두워지기 시작하면(endAlpha가 1에 가까우면) 클릭을 막음
        if (endAlpha > 0.5f) fadeImage.raycastTarget = true;

        float elapsed = 0f;
        Color color = fadeImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            // 1. 화면 페이드 처리
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            fadeImage.color = color;

            // 2. 오디오 페이드 아웃 처리
            if (isFadingOut && allSources != null)
            {
                for (int i = 0; i < allSources.Length; i++)
                {
                    if (allSources[i] != null)
                        allSources[i].volume = Mathf.Lerp(startVolumes[i], 0f, t);
                }
            }

            yield return null;
        }

        color.a = endAlpha;
        fadeImage.color = color;

        // 화면이 완전히 밝아지면(endAlpha가 0이면) 클릭을 통과시킴
        if (endAlpha <= 0.01f)
        {
            fadeImage.raycastTarget = false;
        }
    }

    /// <summary>
    /// PlayerPrefs에 저장된 마지막 플레이 스테이지를 다시 불러옵니다.
    /// </summary>
    public void RetryLastStage()
    {
        isRetry = true; // 리트라이 버튼 클릭 시 플래그 설정
        string lastStage = PlayerPrefs.GetString("LastPlayedStage", "Stage1");
        Debug.Log($"<color=cyan>[Retry] 마지막 스테이지({lastStage})로 재시도합니다.</color>");
        TransitionToSceneInternal(lastStage);
    }

    // 내부 호출용 (플래그 변경 없이 이동)
    private void TransitionToSceneInternal(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        StartCoroutine(TransitionRoutine(sceneName));
    }
}

