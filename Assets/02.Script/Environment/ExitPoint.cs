using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ──────────────────────────────────────────────
//  ExitPoint
//  탈출구 오브젝트에 붙이는 컴포넌트.
//  상호작용은 PlayerInteract가 Interact()를 직접 호출합니다.
//
//  [에디터 설정]
//  1. Collider2D 추가 → Is Trigger ✅
//  2. promptText : TextMeshPro 하나 연결
// ──────────────────────────────────────────────
[RequireComponent(typeof(Collider2D))]
public class ExitPoint : MonoBehaviour
{
    [Header("프롬프트 메시지 (수정 가능)")]
    [SerializeField] private string readyMessage  = "[ E ]  탈출하기";
    [SerializeField] private string lockedMessage = "열쇠가 부족합니다!  ({0} / {1})";
    [SerializeField] private Color  readyColor    = Color.white;
    [SerializeField] private Color  lockedColor   = new Color(1f, 0.35f, 0.35f);

    [Header("UI — TextMeshPro 하나만 연결하세요")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Image promptIcon;

    [Header("사운드 설정")]
    [SerializeField] private AudioClip interactSound;      // 탈출 성공 시 효과음
    [SerializeField] private AudioClip lockedSound;        // 열쇠 부족 시 효과음

    // PlayerInteract가 범위 체크 없이 바로 호출할 수 있도록 static으로 현재 활성 ExitPoint 관리
    private static ExitPoint currentNearby;

    // ── 초기화 ────────────────────────────────
    private void Start()
    {
        HidePrompt();
    }

    // ── PlayerInteract에서 호출 ───────────────
    public void Interact(PlayerInventory inventory)
    {
        if (StageManager.Instance == null)
        {
            Debug.LogWarning("[ExitPoint] StageManager가 씬에 없습니다.");
            return;
        }

        int required = StageManager.Instance.RequiredKeyCount;
        int current  = inventory != null ? inventory.GetKeyItems().Count : 0;

        if (current >= required)
        {
            // ✅ 열쇠 충분 → 클리어
            if (interactSound != null && SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(interactSound);
            StageManager.Instance.ClearStage();
        }
        else
        {
            // ❌ 열쇠 부족 → 잠김 메시지 1.5초 후 복원
            if (lockedSound != null && SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(lockedSound);
            ShowPrompt(string.Format(lockedMessage, current, required), lockedColor);
            CancelInvoke(nameof(RestoreReadyPrompt));
            Invoke(nameof(RestoreReadyPrompt), 1.5f);

            Debug.Log($"<color=red>[탈출 실패] 열쇠 {current}/{required}</color>");
        }
    }

    // ── 가장 가까운 활성 ExitPoint 반환 (PlayerInteract용) ─
    public static ExitPoint GetNearbyExit() => currentNearby;

    // ── 프롬프트 헬퍼 ─────────────────────────
    private void ShowPrompt(string message, Color color)
    {
        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text  = message;
            promptText.color = color;
        }
        if (promptIcon != null) promptIcon.gameObject.SetActive(true);
    }

    private void HidePrompt()
    {
        if (promptText != null) promptText.gameObject.SetActive(false);
        if (promptIcon != null) promptIcon.gameObject.SetActive(false);
    }

    private void RestoreReadyPrompt()
    {
        if (currentNearby == this)
            ShowPrompt(readyMessage, readyColor);
    }

    // ── 플레이어 출입 감지 ────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        currentNearby = this;
        ShowPrompt(readyMessage, readyColor);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (currentNearby == this) currentNearby = null;
        CancelInvoke(nameof(RestoreReadyPrompt));
        HidePrompt();
    }

    private void OnDisable()
    {
        if (currentNearby == this)
        {
            currentNearby = null;
            CancelInvoke(nameof(RestoreReadyPrompt));
            HidePrompt();
        }
    }
}
