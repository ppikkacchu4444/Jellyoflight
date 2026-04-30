using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// ──────────────────────────────────────────────
//  BossSacrificeObject
//  상호작용 시 아군이 여러 명 나타나 보스 하단에 정렬하여 그로기를 유발하는 기믹
// ──────────────────────────────────────────────
public class BossSacrificeObject : MonoBehaviour
{
    [Header("효과 설정")]
    [SerializeField] private float groggyDuration = 5f;     // 보스 그로기 지속 시간
    [SerializeField] private GameObject allyPrefab;          // 스폰할 아군 프리팹
    [SerializeField] private int allyCount = 5;              // 소환할 아군 수
    [SerializeField] private float flySpeed = 15f;           // 아군이 보스로 날아가는 속도

    [Header("아군 하단 배치 설정")]
    [SerializeField] private float allySpacing = 1.2f;       // 아군 간 가로 간격
    [SerializeField] private float allyBottomOffset = 0.5f;  // ★ 더 가깝게 밀착 (보스 하단 기준 여백)
    [SerializeField] private int allySortingOrder = 110;    // ★ 보스보다 앞에서 그려지도록 설정

    [Header("UI — TextMeshPro 연결")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Image promptIcon;              // 프롬프트 옆에 표시할 아이콘 (Image 컴포넌트 연결)
    [SerializeField] private string interactMessage = "[ F ] 아군 희생 (보스 제압)";
    [SerializeField] private Color promptColor = Color.cyan;

    [Header("사운드 설정")]
    [SerializeField] private AudioClip interactSound;      // 아군 희생 발동 시 효과음

    [Header("지연 스폰 설정")]
    [SerializeField] private float spawnDelay = 0f;         // 스폰 전 대기 시간 (초). 0 = 즉시 활성화
    [SerializeField] private bool hideUntilSpawn = true;   // 대기 중 비주얼을 숨길지 여부
    [SerializeField] private GameObject spawnEffectPrefab; // 활성화 시 재생할 이펝트 (선택)

    [Header("재사용 & 자동 스폰 설정")]
    [SerializeField] [Tooltip("체크 시 첫 상호작용(F) 이후 파괴되지 않고 재사용 가능")] 
    private bool isReusable = true;
    
    [SerializeField] [Tooltip("기믹이 끝나고 다음 상호작용(또는 자동 스폰)까지의 쿨타임(초)")] 
    private float cooldownTime = 10f;

    [SerializeField] [Tooltip("체크 시 한 번 상호작용(F)하면 이후엔 플레이어가 누르지 않아도 자동으로 쿨타임마다 스폰")] 
    private bool autoSpawnAfterFirstInteract = false; 

    private bool isReady = false;
    private bool isUsed = false;
    private List<GameObject> activeAllies = new List<GameObject>();
    private static BossSacrificeObject currentNearby; // 가장 가까운 오브젝트 추적용

    private SpriteRenderer[] originalRenderers;
    private Color[] originalColors;

    public static BossSacrificeObject GetNearby() => currentNearby;

    private void Start()
    {
        HidePrompt();

        // 렌더러와 원래 색상 캐싱
        originalRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[originalRenderers.Length];
        for (int i = 0; i < originalRenderers.Length; i++)
        {
            originalColors[i] = originalRenderers[i].color;
        }

        if (spawnDelay > 0f)
        {
            if (hideUntilSpawn) SetVisualsActive(false);
            StartCoroutine(SpawnAfterDelay());
        }
        else
        {
            isReady = true;
        }
    }

    private IEnumerator SpawnAfterDelay()
    {
        yield return new WaitForSeconds(spawnDelay);
        isReady = true;
        if (hideUntilSpawn) SetVisualsActive(true);
        if (spawnEffectPrefab != null)
            Instantiate(spawnEffectPrefab, transform.position, Quaternion.identity);
        Debug.Log($"<color=cyan>[BossSacrificeObject] {spawnDelay}초 후 활성화!</color>");
    }

    private void SetVisualsActive(bool active)
    {
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            sr.enabled = active;
        // 콜라이더도 대기 중엔 비활성화 (플레이어가 범위에 들어와도 프롬프트 안 뜨게)
        foreach (var col in GetComponentsInChildren<Collider2D>(true))
            col.enabled = active;
    }

    private void SetVisualsCooldownStatus(bool isOnCooldown)
    {
        if (originalRenderers == null) return;
        Color cooldownColor = new Color(0.3f, 0.3f, 0.3f, 1f); // 어두운 색상 (회색)

        for (int i = 0; i < originalRenderers.Length; i++)
        {
            if (originalRenderers[i] != null)
            {
                originalRenderers[i].color = isOnCooldown ? cooldownColor : originalColors[i];
            }
        }
    }

    public void Interact()
    {
        if (isUsed || !isReady) return; 

        ExecuteSacrifice();
    }

    private void ExecuteSacrifice()
    {
        BossMonster boss = FindFirstObjectByType<BossMonster>();
        if (boss != null)
        {
            isUsed = true;
            HidePrompt();
            SetVisualsCooldownStatus(true);

            // 상호작용 효과음 재생
            if (interactSound != null && SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(interactSound);

            Debug.Log("<color=cyan>[Ally] 아군이 파견되어 보스 하단을 향해 날아갑니다!</color>");
            StartCoroutine(SacrificeRoutine(boss));
        }
        else
        {
            Debug.LogWarning("[Ally] 씬에 BossMonster가 없어 기믹을 수행할 수 없습니다.");
        }
    }

    private IEnumerator CooldownRoutine()
    {
        // 쿨타임 대기 (이 동안은 UI도 안 뜨고 조작 불가능)
        yield return new WaitForSeconds(cooldownTime);

        // 쿨타임 종료 후 재실행 가능 상태로 변경
        isUsed = false;
        SetVisualsCooldownStatus(false); // ★ 쿨타임 끝나면 색상 원상복구
        
        if (autoSpawnAfterFirstInteract)
        {
            // 알아서 다시 소환
            ExecuteSacrifice();
        }
        else
        {
            // 수동 조작 대기: 플레이어가 근처에 서있다면 UI 다시 활성화
            if (currentNearby == this)
            {
                ShowPrompt();
            }
        }
    }

    private IEnumerator SacrificeRoutine(BossMonster boss)
    {
        // 1. 보스 콜라이더 캐싱 (충돌 무시에 사용)
        Collider2D[] bossColliders = boss.GetComponentsInChildren<Collider2D>(true);

        // 2. 아군 소환 (기믹 오브젝트 주위에서 랜덤 생성)
        for (int i = 0; i < allyCount; i++)
        {
            if (allyPrefab != null)
            {
                Vector3 spawnOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-0.5f, 0.5f), 0);
                GameObject ally = Instantiate(allyPrefab, transform.position + spawnOffset, Quaternion.identity);
                activeAllies.Add(ally);

                // 보스보다 앞에 그려지도록 레이어 순서 조정
                SpriteRenderer sr = ally.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = allySortingOrder;

                // 보스와 아군 콜라이더 간 충돌 무시 (소환 직후 튕김 방지)
                Collider2D[] allyColliders = ally.GetComponentsInChildren<Collider2D>(true);
                foreach (var bc in bossColliders)
                    foreach (var ac in allyColliders)
                        if (bc != null && ac != null)
                            Physics2D.IgnoreCollision(bc, ac, true);
            }
        }

        // 3. 보스 하단 Y좌표 계산 (콜라이더 기준 바닥보다 allyBottomOffset 아래)
        Collider2D bossBoundCol = boss.GetComponentInChildren<Collider2D>();
        float targetY = (bossBoundCol != null)
            ? bossBoundCol.bounds.min.y - allyBottomOffset
            : boss.transform.position.y - allyBottomOffset;

        // 4. 아군들이 보스 하단에 가로 일렬로 날아감
        bool isMoving = true;
        while (isMoving && boss != null)
        {
            isMoving = false;

            // 전체 줄의 총 너비를 기준으로 중앙 정렬
            float totalWidth = (activeAllies.Count - 1) * allySpacing;
            float startX = boss.transform.position.x - totalWidth * 0.5f;

            for (int i = 0; i < activeAllies.Count; i++)
            {
                if (activeAllies[i] == null) continue;

                // Z값을 살짝 음수(-0.2f)로 주어 보스(0)보다 시각적으로 앞으로 나오게 함
                Vector3 targetPos = new Vector3(startX + i * allySpacing, targetY, -0.2f);

                if (Vector2.Distance(activeAllies[i].transform.position, targetPos) > 0.1f)
                {
                    activeAllies[i].transform.position = Vector2.MoveTowards(
                        activeAllies[i].transform.position,
                        targetPos,
                        flySpeed * Time.deltaTime
                    );
                    isMoving = true;
                }
            }
            yield return null;
        }

        // 5. 배치 완료 → 보스 그로기 발동!
        if (boss != null)
        {
            boss.TriggerGroggy(groggyDuration, this);
        }
    }

    public void ClearAllies()
    {
        foreach (var ally in activeAllies)
        {
            if (ally != null)
                Destroy(ally);
        }
        activeAllies.Clear();

        Debug.Log("<color=yellow>[Ally] 아군이 모두 사라집니다.</color>");

        if (!isReusable)
        {
            // 1회성 기믹 오브젝트 파괴
            Destroy(gameObject);
        }
        else
        {
            // 재사용 대기 쿨타임 시작
            StartCoroutine(CooldownRoutine());
        }
    }

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 동작 중이거나 쿨타임 중일 때는 UI 띄우지 않음
        if (isUsed) return; 
        
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
