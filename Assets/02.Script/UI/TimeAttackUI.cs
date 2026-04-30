using UnityEngine;
using TMPro;

// ──────────────────────────────────────────────
//  TimeAttackUI
//  StageManager의 시간을 화면에 표시합니다.
// ──────────────────────────────────────────────
public class TimeAttackUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;   // 남은 시간 표시 텍스트
    [SerializeField] private GameObject timerObject;      // 타이머 UI 부모 오브젝트

    private void Start()
    {
        // 타임어택 모드가 아닐 경우 UI 비활성화
        if (StageManager.Instance != null)
        {
            if (!StageManager.Instance.IsTimeAttack)
            {
                if (timerObject != null) timerObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (StageManager.Instance != null && StageManager.Instance.IsTimeAttack)
        {
            UpdateTimerDisplay();
        }
    }

    private void UpdateTimerDisplay()
    {
        float time = StageManager.Instance.CurrentTime;

        // 분:초 형식으로 변환
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);

        if (timerText != null)
        {
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            // 시간이 얼마 안 남았을 때 빨간색으로 강조 (예: 10초 미만)
            if (time < 10f)
            {
                timerText.color = Color.red;
            }
        }
    }
}
