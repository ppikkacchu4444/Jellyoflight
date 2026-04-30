using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

// ──────────────────────────────────────────────
//  AllySacrifice
//  배경의 아군을 깨워 몬스터를 멈추게 하는 상호작용 오브젝트.
// ──────────────────────────────────────────────
public class AllySacrifice : MonoBehaviour
{
    [Header("효과 설정")]
    [SerializeField] private float stopDuration = 6f;      // 몬스터를 멈추는 시간
    [SerializeField] private float flySpeed = 18f;         // 아군이 날아가는 속도

    [Header("시각 연출 (대기 시 및 비행 시)")]
    [SerializeField] private GameObject allyVisual;        // 맵에 배치된 잠든 아군 (빈 오브젝트의 자식이면 좋음)
    [SerializeField] private Sprite awakenedSprite;       // 깨어났을 때 변할 진짜 스프라이트 (선택)
    [SerializeField] private float hoverSpeed = 2.0f;     // 위아래 둥둥 떠다니는 속도
    [SerializeField] private float hoverHeight = 0.2f;    // 위아래 이동 폭
    
    [Header("UI — TextMeshPro 연결")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Image promptIcon;              // 프롬프트 옆에 표시할 아이콘 (Image 컴포넌트 연결)
    [SerializeField] private string interactMessage = "[ F ]  아군 깨우기";
    [SerializeField] private Color promptColor = Color.white;

    [Header("사운드 설정")]
    [SerializeField] private AudioClip interactSound;      // 아군 깨우기 성공 시 효과음

    private static AllySacrifice currentNearby;
    private bool isUsed = false;
    private Vector3 visualStartLocalPos;

    // PlayerInteract에서 접근하기 위한 스태틱 메서드
    public static AllySacrifice GetNearby() => currentNearby;

    private void Start()
    {
        HidePrompt();

        if (allyVisual != null)
        {
            visualStartLocalPos = allyVisual.transform.localPosition;
        }
    }

    private void Update()
    {
        // 상호작용 전 대기 상태일 때, 위아래로 부드럽게 둥둥 떠다니는 효과
        if (!isUsed && allyVisual != null)
        {
            float newY = visualStartLocalPos.y + Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
            allyVisual.transform.localPosition = new Vector3(visualStartLocalPos.x, newY, visualStartLocalPos.z);
        }
    }

    // 상호작용 실행 (F키 입력 시 호출됨)
    public void Interact()
    {
        if (isUsed) return;

        // 씬에서 추격자 몬스터 찾기
        StalkerMonster monster = Object.FindFirstObjectByType<StalkerMonster>();

        if (monster != null)
        {
            isUsed = true;
            HidePrompt();

            // 상호작용 효과음 재생
            if (interactSound != null && SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(interactSound);

            Debug.Log("<color=cyan>[Ally] 아군을 깨웠습니다! 보스를 향해 돌격합니다.</color>");

            // 봉인 해제 (스프라이트 이미지 교체)
            if (awakenedSprite != null && allyVisual != null)
            {
                SpriteRenderer sr = allyVisual.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = awakenedSprite;
                
                // 만약 애니메이터가 있다면 강제로 멈추거나 상태를 바꿔줄 수도 있음
                Animator anim = allyVisual.GetComponent<Animator>();
                if (anim != null) anim.enabled = false; // 이미지 고정을 위해 애니메이터 끄기
            }

            StartCoroutine(SacrificeRoutine(monster));
        }
        else
        {
            Debug.LogWarning("[Ally] 씬에 StalkerMonster가 없어 아군을 깨울 수 없습니다.");
        }
    }

    private IEnumerator SacrificeRoutine(StalkerMonster target)
    {
        if (allyVisual != null)
        {
            // 아군이 보스 위치까지 빠르게 날아감 (월드 좌표 기준 이동)
            while (Vector2.Distance(allyVisual.transform.position, target.transform.position) > 0.8f)
            {
                allyVisual.transform.position = Vector2.MoveTowards(
                    allyVisual.transform.position, 
                    target.transform.position, 
                    flySpeed * Time.deltaTime
                );
                yield return null;
            }
            
            // 충돌 후 아군 사라짐
            allyVisual.SetActive(false);
        }

        // 보스 일시 정지
        target.Halt(stopDuration);
    }

    // ── UI 프롬프트 로직 ────
    private void ShowPrompt()
    {
        if (promptText != null && !isUsed)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = interactMessage;
            promptText.color = promptColor;
        }
        if (promptIcon != null && !isUsed) promptIcon.gameObject.SetActive(true);
    }

    private void HidePrompt()
    {
        if (promptText != null) promptText.gameObject.SetActive(false);
        if (promptIcon != null) promptIcon.gameObject.SetActive(false);
    }

    // 플레이어 감지 (상호작용 범위)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            currentNearby = this;
            ShowPrompt();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && currentNearby == this)
        {
            currentNearby = null;
            HidePrompt();
        }
    }

    private void OnDisable()
    {
        if (currentNearby == this)
        {
            currentNearby = null;
            HidePrompt();
        }
    }
}
