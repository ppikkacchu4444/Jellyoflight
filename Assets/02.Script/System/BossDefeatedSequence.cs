using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

// ──────────────────────────────────────────────
//  BossDefeatedSequence
//  보스 처치 시 호출됩니다.
//  [설정 방법]
//  1. 보스가 있는 씬의 아무 오브젝트에 Add Component
//  2. Victory Video Clip에 영상 파일 연결
//  3. Result Scene Name에 이동할 씬 이름 입력 (예: "Result")
// ──────────────────────────────────────────────
public class BossDefeatedSequence : MonoBehaviour
{
    public static BossDefeatedSequence Instance { get; private set; }

    [Header("영상 설정")]
    [SerializeField] private VideoClip victoryVideoClip;    // 보스 처치 후 재생할 영상
    [SerializeField] private RawImage videoScreen;          // 영상을 표시할 RawImage (Canvas 위)
    [SerializeField] private GameObject videoPanel;         // 영상 패널 오브젝트 (배경 포함)

    [Header("씬 이동")]
    [SerializeField] private string targetSceneName = "Clear";
    [SerializeField] private float delayBeforeVideo = 1.0f; // 보스 처치 후 영상 시작까지 딜레이
    [SerializeField] private float delayAfterVideo = 1.0f;  // 영상 종료 후 씬 이동까지 딜레이

    private VideoPlayer videoPlayer;
    private bool isPlaying = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 영상 패널 초기 상태는 여기서 관리하지 않고 Trigger 시점에 제어
    }

    private void Start()
    {
        // VideoPlayer 자동 생성 및 기본 설정
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;

        // AudioSource 연결
        AudioSource audioSrc = GetComponent<AudioSource>();
        if (audioSrc == null) audioSrc = gameObject.AddComponent<AudioSource>();
        videoPlayer.SetTargetAudioSource(0, audioSrc);

        // RenderTexture → RawImage 연결
        if (videoScreen != null)
        {
            RenderTexture rt = new RenderTexture(1920, 1080, 0);
            rt.Create();
            videoPlayer.targetTexture = rt;
            videoScreen.texture = rt;
            videoScreen.color = Color.white;
            videoScreen.rectTransform.SetAsLastSibling();
        }

        if (videoPanel != null)
        {
            Image panelBg = videoPanel.GetComponent<Image>();
            if (panelBg != null) panelBg.color = Color.black;
            videoPanel.SetActive(false); // 처음엔 꺼둠
        }

        // 영상 종료 시 호출될 콜백
        videoPlayer.loopPointReached += OnVideoEnded;
    }

    // BossMonster.Die()에서 호출
    public void TriggerVictorySequence()
    {
        if (isPlaying) return;
        isPlaying = true;

        // ★ 패널과 스크린 오브젝트를 모두 즉시 켬
        if (videoPanel != null) videoPanel.SetActive(true);
        if (videoScreen != null) videoScreen.gameObject.SetActive(true);

        Debug.Log("<color=yellow>[BossDefeatedSequence] 보스 처치! 엔딩 시퀀스 시작</color>");
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        // [추가] 영상 재생 전 현재 씬의 BGM을 부드럽게 끔
        FadeOutAllSceneBGM(1.0f);

        // 0. 영상 클립이 없으면 즉시 결과 화면으로 이동
        if (victoryVideoClip == null)
        {
            Debug.LogWarning("[BossDefeatedSequence] 영상 클립이 설정되지 않아 즉시 클리어 처리합니다.");
            GoToResult();
            yield break;
        }

        // 1. 영상 로드 및 준비
        videoPlayer.clip = victoryVideoClip;
        videoPlayer.Prepare();

        // 패널 준비 (투명한 상태로 시작)
        CanvasGroup cg = videoPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = videoPanel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        if (videoPanel != null) videoPanel.SetActive(true);

        // 영상 준비 대기 (최대 3초 타임아웃)
        float waitElapsed = 0f;
        while (!videoPlayer.isPrepared && waitElapsed < 3.0f)
        {
            waitElapsed += Time.deltaTime;
            yield return null;
        }

        if (!videoPlayer.isPrepared)
        {
            Debug.LogError("[BossDefeatedSequence] 영상 준비 시간 초과! 클리어 화면으로 강제 이동합니다.");
            GoToResult();
            yield break;
        }

        // 2. 보스 사망 연출을 위한 잠시 대기 및 페이드 인 시작
        float elapsed = 0f;
        float fadeInTime = 1.0f; 
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInTime);
            yield return null;
        }
        cg.alpha = 1f;

        // 3. 재생 시작
        videoPlayer.Play();
        Debug.Log("<color=yellow>[BossDefeatedSequence] 영상 재생 시작!</color>");

        if (videoScreen != null) videoScreen.color = Color.white;
    }

    private void FadeOutAllSceneBGM(float duration)
    {
        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (var src in sources)
        {
            // 현재 씬에 배치된 BGM(loop가 켜진 소스)만 페이드 아웃
            if (src.gameObject.scene.name != "DontDestroyOnLoad" && src.loop)
            {
                StartCoroutine(FadeOutRoutine(src, duration));
            }
        }
    }

    private IEnumerator FadeOutRoutine(AudioSource src, float duration)
    {
        float startVol = src.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (src == null) yield break;
            elapsed += Time.deltaTime;
            src.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }
        if (src != null)
        {
            src.volume = 0f;
            src.Stop();
        }
    }

    private void OnVideoEnded(VideoPlayer vp)
    {
        StartCoroutine(EndSequenceRoutine());
    }

    private IEnumerator EndSequenceRoutine()
    {
        Debug.Log($"<color=yellow>[BossDefeatedSequence] 영상 종료 → {delayAfterVideo}초 대기 후 이동</color>");
        
        // 영상 끝나고 잠시 여운 대기
        yield return new WaitForSeconds(delayAfterVideo);
        
        GoToResult();
    }

    private void GoToResult()
    {
        // StageManager를 통해 클리어 처리 (다음 씬 전환 포함)
        if (StageManager.Instance != null)
        {
            Debug.Log("<color=yellow>[BossDefeatedSequence] StageManager.ClearStage() 호출</color>");
            StageManager.Instance.ClearStage();
        }
        else
        {
            // 만약 StageManager가 없다면 예외적으로 직접 이동
            if (!string.IsNullOrEmpty(targetSceneName))
                SceneManager.LoadScene(targetSceneName);
            else
                Debug.LogError("[BossDefeatedSequence] Target Scene Name이 비어있습니다!");
        }
    }
}
