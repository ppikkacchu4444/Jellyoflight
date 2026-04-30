using UnityEngine;
using TMPro;
using UnityEngine.UI;

// ──────────────────────────────────────────────
//  BuffTimerUI
//  개별 버프(속도, 무적 등)의 남은 시간을 시각적으로 표시합니다.
// ──────────────────────────────────────────────
public class BuffTimerUI : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private Image iconImage;       // 버프 아이콘
    [SerializeField] private Image fillImage;       // 시간 경과 게이지 (선택)
    [SerializeField] private TextMeshProUGUI timeText; // 남은 시간 텍스트

    private float maxDuration;
    private float currentRemaining;

    // ── 초기 설정 ──────────────────────────────
    public void Setup(Sprite icon, float duration)
    {
        if (iconImage != null) iconImage.sprite = icon;
        maxDuration = duration;
        currentRemaining = duration;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        // 매 프레임 시간을 깎습니다.
        currentRemaining -= Time.deltaTime;
        if (maxDuration > 0)
        {
            // 게이지를 업데이트합니다. (0~1 사이 값으로 제한)
            if (fillImage != null) 
            {
                fillImage.fillAmount = Mathf.Clamp01(currentRemaining / maxDuration);
            }
        }
        
        // [중요] 시간이 0보다 작거나 같아지면 무조건 삭제합니다.
        if (currentRemaining <= 0)
        {
            Destroy(gameObject);
        }
    }

    // ── 플레이어 머리 위에 있을 때 뒤집힘 방지 ──────
    private void LateUpdate()
    {
        // 1. 회전 고정: 부모가 돌아가도 UI는 항상 정면을 봅니다.
        transform.rotation = Quaternion.identity;

        // 2. 스케일 고정: 부모가 좌우 반전(-1)되어도 UI는 뒤집히지 않습니다.
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        Vector3 localScale = transform.localScale;
        
        // 부모의 실제 월드 스케일 방향에 맞춰 보정
        transform.localScale = new Vector3(
            Mathf.Abs(localScale.x) * (parentScale.x > 0 ? 1 : -1), 
            localScale.y, 
            localScale.z
        );
    }
}
