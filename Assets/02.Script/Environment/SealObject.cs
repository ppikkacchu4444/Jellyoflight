using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ──────────────────────────────────────────────
//  SealObject (봉인 오브젝트)
//  공격 받으면 파괴되면서 숨겨진 아군과 아이템을 소환합니다.
//  구역의 조명(LightSwitchObject)이 켜져 있어야만 타격 가능합니다.
// ──────────────────────────────────────────────
[RequireComponent(typeof(Collider2D))]
public class SealObject : MonoBehaviour
{
    [Header("봉인 해제 조건")]
    [SerializeField] private LightSwitchObject requiredSwitch; // 이 구역의 조명 스위치

    [Header("소환 대상 (맵에 배치 후 연결)")]
    [SerializeField] private GameObject allyToSpawn; // 등장할 아군 캐릭터
    [SerializeField] private GameObject itemToSpawn; // 떨어뜨릴 아이템

    [Header("UI 프롬프트")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Image promptIcon;              // 프롬프트 옆에 표시할 아이콘 (Image 컴포넌트 연결)
    [SerializeField] private string readyMessage = "공격해서 봉인을 해제해줘!";
    [SerializeField] private string lockedMessage = "(어두워서 봉인을 풀 수 없습니다)";
    [SerializeField] private Color readyColor = Color.cyan;
    [SerializeField] private Color lockedColor = Color.gray;

    [Header("사운드 설정")]
    [SerializeField] private AudioClip breakSound;         // 봉인 해제 성공 시 효과음
    [SerializeField] private AudioClip lockedSound;        // 조건 불충족 시 효과음

    private bool isPlayerNearby = false;
    private bool isBroken = false;

    private void Start()
    {
        HidePrompt();

        // 1. 시작 시 아군과 아이템 숨기기
        if (allyToSpawn != null) allyToSpawn.SetActive(false);
        if (itemToSpawn != null) itemToSpawn.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isBroken)
        {
            isPlayerNearby = true;
            UpdatePrompt();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            HidePrompt();
        }
    }

    private void UpdatePrompt()
    {
        if (promptText == null || !isPlayerNearby) return;

        promptText.gameObject.SetActive(true);
        if (promptIcon != null) promptIcon.gameObject.SetActive(true);
        if (requiredSwitch == null || requiredSwitch.IsActivated)
        {
            promptText.text = readyMessage;
            promptText.color = readyColor;
        }
        else
        {
            promptText.text = lockedMessage;
            promptText.color = lockedColor;
        }
    }

    // 플레이어가 대시 공격 등 상호작용으로 때렸을 때 호출됨
    public void TakeDamage()
    {
        if (isBroken) return; // 중복 호출 방지

        // 2. 조명이 켜져있어야 함
        if (requiredSwitch != null && !requiredSwitch.IsActivated)
        {
            if (lockedSound != null && SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(lockedSound);
            Debug.Log("<color=gray>[SealObject] 아직 조명이 켜지지 않아 봉인을 풀 수 없습니다.</color>");
            return;
        }

        isBroken = true;

        // 봉인 해제 효과음 재생
        if (breakSound != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(breakSound);

        Debug.Log("<color=cyan>[SealObject] 봉인 해제 완료! 아군과 아이템 소환!</color>");

        // 3. 아군 및 아이템 소환 (활성화)
        if (allyToSpawn != null) 
        {
            allyToSpawn.SetActive(true);
            // 아군이 나타날 위치를 봉인 오브젝트 위치로 강제하고 싶다면 아래 주석 해제
            // allyToSpawn.transform.position = transform.position;
        }
        
        if (itemToSpawn != null) 
        {
            itemToSpawn.SetActive(true);
            // 아이템 위치를 봉인 오브젝트 위치로 강제하지 않고, 씬에 배치된 원래 위치에서 그대로 나타나게 합니다.
            // itemToSpawn.transform.position = transform.position + Vector3.up * 0.5f;
        }

        // 4. UI 숨기기 및 자신(봉인 오브젝트) 파괴
        HidePrompt();
        Destroy(gameObject);
    }

    private void HidePrompt()
    {
        if (promptText != null) promptText.gameObject.SetActive(false);
        if (promptIcon != null) promptIcon.gameObject.SetActive(false);
    }
}
