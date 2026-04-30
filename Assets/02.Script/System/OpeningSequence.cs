using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

// ──────────────────────────────────────────────
//  OpeningSequence
//  타이틀에서 게임 시작 시 영상을 재생합니다.
//  [설정 방법]
//  1. Title 씬의 아무 오브젝트에 Add Component
//  2. Opening Video Clip에 영상 파일 연결
//  3. Target Scene Name에 이동할 씬 이름 입력 (예: "Stage1")
// ──────────────────────────────────────────────
public class OpeningSequence : MonoBehaviour
{
    [Header("영상 설정")]
    [SerializeField] private VideoClip openingVideoClip;    // 재생할 영상
    [SerializeField] private RawImage videoScreen;          // 영상을 표시할 RawImage (Canvas 위)
    [SerializeField] private GameObject videoPanel;         // 영상 패널 오브젝트 (배경 포함)

    [Header("씬 이동")]
    [SerializeField] private string targetSceneName = "Stage1";
    [SerializeField] private float delayAfterVideo = 0.5f;  // 영상 종료 후 씬 이동까지 딜레이

    private VideoPlayer videoPlayer;
    private bool isPlaying = false;
    private bool isSkipped = false;

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
        }

        if (videoPanel != null)
        {
            videoPanel.SetActive(false); // 처음엔 꺼둠
        }

        // 영상 종료 시 호출될 콜백
        videoPlayer.loopPointReached += OnVideoEnded;
    }

    private void Update()
    {
        // 영상 재생 중 아무 키나 누르면 스킵 (원하는 경우)
        if (isPlaying && !isSkipped)
        {
            if (Input.anyKeyDown)
            {
                SkipOpening();
            }
        }
    }

    // 타이틀의 [Game Start] 버튼 OnClick()에서 호출하도록 설정
    public void StartOpeningSequence()
    {
        if (isPlaying) return;
        isPlaying = true;

        // ★ 패널과 스크린 오브젝트를 모두 즉시 켬
        if (videoPanel != null) videoPanel.SetActive(true);
        if (videoScreen != null) videoScreen.gameObject.SetActive(true);
        
        Debug.Log("<color=cyan>[OpeningSequence] 오프닝 시퀀스 시작</color>");
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        // 0. 영상 클립이 없으면 즉시 다음 씬으로 이동
        if (openingVideoClip == null)
        {
            Debug.LogWarning("[OpeningSequence] 영상 클립이 설정되지 않아 즉시 게임을 시작합니다.");
            StartGame();
            yield break;
        }

        // 1. 영상 로드 및 준비
        videoPlayer.clip = openingVideoClip;
        videoPlayer.Prepare();

        // 패널 페이드 인 (선택 사항)
        CanvasGroup cg = videoPanel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            float elapsed = 0f;
            float fadeInTime = 0.5f;
            while (elapsed < fadeInTime)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInTime);
                yield return null;
            }
            cg.alpha = 1f;
        }

        // 영상 준비 대기 (최대 3초 타임아웃)
        float waitElapsed = 0f;
        while (!videoPlayer.isPrepared && waitElapsed < 3.0f)
        {
            waitElapsed += Time.deltaTime;
            yield return null;
        }

        if (!videoPlayer.isPrepared)
        {
            Debug.LogError("[OpeningSequence] 영상 준비 시간 초과! 바로 게임을 시작합니다.");
            StartGame();
            yield break;
        }

        // 2. 재생 시작
        videoPlayer.Play();
        Debug.Log("<color=cyan>[OpeningSequence] 영상 재생 시작!</color>");
    }

    public void SkipOpening()
    {
        if (isSkipped) return;
        isSkipped = true;
        
        Debug.Log("<color=yellow>[OpeningSequence] 오프닝 스킵!</color>");
        videoPlayer.Stop();
        StartGame();
    }

    private void OnVideoEnded(VideoPlayer vp)
    {
        if (isSkipped) return;
        StartCoroutine(EndSequenceRoutine());
    }

    private IEnumerator EndSequenceRoutine()
    {
        yield return new WaitForSeconds(delayAfterVideo);
        StartGame();
    }

    private void StartGame()
    {
        Debug.Log($"<color=cyan>[OpeningSequence] 게임 시작! 이동 씬: {targetSceneName}</color>");
        
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(targetSceneName);
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }
}
