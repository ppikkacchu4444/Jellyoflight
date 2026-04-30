using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// 1스테이지 최초 진입 시 조작 키 가이드 패널을 띄우는 클래스.
/// </summary>
public class ControlGuideUI : MonoBehaviour
{
    [Header("UI 설정")]
    [SerializeField] private GameObject guidePanel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float displayDuration = 5f;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private string stage1Name = "Stage1";

    public static bool IsShowing { get; private set; } = false;

    private void Start()
    {
        // 1. 패널 할당 여부 확인
        if (guidePanel == null)
        {
            Debug.LogError("[ControlGuideUI] 가이드 패널(guidePanel)이 할당되지 않았습니다! 인스펙터를 확인해주세요.");
            return;
        }

        // 2. CanvasGroup 설정
        if (canvasGroup == null) 
            canvasGroup = guidePanel.GetComponent<CanvasGroup>();

        // 3. 상태 체크 로직
        string currentScene = SceneManager.GetActiveScene().name;
        bool isStage1 = currentScene.Equals(stage1Name, System.StringComparison.OrdinalIgnoreCase);
        bool isRetry = SceneTransitionManager.IsRetry;

        Debug.Log($"[ControlGuideUI] 현재 씬: {currentScene}, Stage1 여부: {isStage1}, 리트라이 여부: {isRetry}");

        if (isStage1 && !isRetry)
        {
            Debug.Log("[ControlGuideUI] 조건을 만족하여 가이드를 표시합니다.");
            IsShowing = true;
            ShowGuide();
        }
        else
        {
            Debug.Log($"[ControlGuideUI] 가이드를 표시하지 않습니다. (이유: 스테이지 불일치 혹은 리트라이)");
            IsShowing = false;
            guidePanel.SetActive(false);
        }
    }

    private void ShowGuide()
    {
        if (guidePanel == null) return;

        // 에디터에서 꺼두었더라도 시작 시 자동으로 켬
        guidePanel.SetActive(true);

        if (canvasGroup != null) 
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        StartCoroutine(HideGuideRoutine());
    }

    private IEnumerator HideGuideRoutine()
    {
        yield return new WaitForSeconds(displayDuration);
        
        if (canvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }

        guidePanel.SetActive(false);
        IsShowing = false;
    }
}
