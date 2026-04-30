using System.Collections;
using UnityEngine;
using TMPro;

public class NarrationManager : MonoBehaviour
{
    [Header("UI 연결")]
    public TextMeshProUGUI textUI;    // 글자가 바뀔 텍스트 컴포넌트
    public CanvasGroup canvasGroup; // 투명도 조절용

    [Header("설정")]
    [TextArea]
    public string[] sentences;          // 여러 문장을 적을 수 있는 배열
    public float fadeDuration = 1.0f;   // 페이드 인/아웃에 걸리는 시간 (초)
    public float stayTime = 2.0f;       // 문장이 보이고 멈춰있는 시간 (초)

    void Start()
    {
        // 게임 시작 시 여러 문장 재생 코루틴 실행
        StartCoroutine(PlayAllSentences());
    }

    IEnumerator PlayAllSentences()
    {
        // 조작 가이드가 떠 있다면 사라질 때까지 대기
        // (Start 실행 순서 차이를 위해 한 프레임 대기 후 체크)
        yield return null;
        while (ControlGuideUI.IsShowing)
        {
            yield return null;
        }

        // 문장 배열에 있는 걸 하나씩 꺼내서 반복
        foreach (string sentence in sentences)
        {
            // 1. 문장 교체 및 초기화
            textUI.text = sentence;
            canvasGroup.alpha = 0;

            // 2. Fade In (나타나기)
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;

            // 3. Stay (대기)
            yield return new WaitForSeconds(stayTime);

            // 4. Fade Out (사라지기)
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(1f - elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            
            // 한 문장이 끝나고 다음 문장 시작 전 약간의 여백 시간(0.5초)
            yield return new WaitForSeconds(0.5f);
        }
    }
}
