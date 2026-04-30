using UnityEngine;

/// <summary>
/// 플레이어의 체력 및 사망 로직을 관리하는 클래스입니다.
/// 하트 배열과 maxHp를 자동 동기화하며, 피격 무적·대쉬 무적·아이템 무적을 순차적으로 체크합니다.
/// 체력이 0 이하가 되면 Die()를 호출하여 입력 차단 및 StageManager 실패 처리를 수행합니다.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  체력 설정
    // ─────────────────────────────────────────────

    [Header("--- Health Stats ---")]
    [Tooltip("플레이어의 최대 체력입니다. 하트 UI 개수와 자동으로 동기화됩니다.")]
    [SerializeField] private int maxHp = 5;                   // 최대 체력. Start 시 heartIcons 배열 크기로 자동 보정된다.
    private int currentHp;                                     // 현재 체력 (런타임 전용)

    // ─────────────────────────────────────────────
    //  UI 연결
    // ─────────────────────────────────────────────

    [Header("--- UI Elements ---")]
    [Tooltip("체력을 표시할 하트 아이콘 오브젝트 배열입니다.")]
    [SerializeField] private GameObject[] heartIcons;          // 하트 아이콘 배열. 인덱스 0부터 순서대로 체력 1칸에 대응. 체력 감소 시 뒤에서부터 비활성화.

    // ─────────────────────────────────────────────
    //  피격 무적
    // ─────────────────────────────────────────────

    [Header("--- 피격 무적 설정 ---")]
    [Tooltip("피격 후 다시 데미지를 입을 때까지의 지연 시간(초)입니다.")]
    [SerializeField] private float invincibilityTime = 1f;     // 피격 후 무적 시간(초). 이 시간 동안 추가 데미지를 받지 않는다.
    private float nextDamageTime = 0f;                         // 다음 데미지를 받을 수 있는 Time.time 시각
    private bool isDead = false;                               // 사망 여부 — true면 모든 데미지·힐이 무시된다
    private bool isForcedInvincible = false;                    // 외부에서 강제로 설정한 무적 상태 (보스 처치 연출 등)

    // 외부에서 무적 상태를 강제로 설정하는 함수
    public void SetInvincible(bool value)
    {
        isForcedInvincible = value;
        Debug.Log($"<color=cyan>[PlayerHealth] 플레이어 무적 상태 설정: {value}</color>");
    }

    // ─── 컴포넌트 캐싱 ───
    private PlayerDashAttack dashScript;       // 대쉬 무적 확인 및 사망 시 비활성화용
    private PlayerHitEffect hitEffect;         // 피격 연출(깜빡임 + 넉백) 처리용
    private Animator anim;                     // 사망 애니메이션 트리거용

    private void Start()
    {
        InitializeComponents();
        SyncMaxHealthWithUI();
        
        currentHp = maxHp;
        UpdateHeartUI();
    }

    /// <summary>
    /// 필요한 컴포넌트들을 캐싱합니다.
    /// </summary>
    private void InitializeComponents()
    {
        dashScript = GetComponent<PlayerDashAttack>();
        hitEffect = GetComponent<PlayerHitEffect>();
        anim = GetComponent<Animator>();
    }

    /// <summary>
    /// 설정된 하트 UI 개수에 맞춰 최대 체력을 동기화합니다.
    /// </summary>
    private void SyncMaxHealthWithUI()
    {
        if (heartIcons == null || heartIcons.Length == 0) return;

        int validHeartCount = 0;
        foreach (var heart in heartIcons)
        {
            if (heart != null) validHeartCount++;
        }

        if (validHeartCount > 0 && maxHp != validHeartCount)
        {
            Debug.Log($"<color=white>[PlayerHealth] maxHp가 실제 하트 UI 개수({validHeartCount})와 동기화되었습니다.</color>");
            maxHp = validHeartCount;
        }
    }

    // 현재/최대 HP 프로퍼티 (읽기 전용)
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;

    /// <summary>
    /// 체력을 회복시키고 UI를 업데이트합니다.
    /// </summary>
    /// <param name="amount">회복량</param>
    public void Heal(int amount)
    {
        if (isDead) return;
        currentHp = Mathf.Clamp(currentHp + amount, 0, maxHp);
        UpdateHeartUI();
        Debug.Log($"<color=green>[Heal] 체력 회복! +{amount} → 현재 HP: {currentHp}/{maxHp}</color>");
    }

    /// <summary>
    /// 무적 상태를 무시하고 즉시 플레이어를 사망 상태로 만듭니다.
    /// </summary>
    public void InstantKill()
    {
        if (isDead) return;
        currentHp = 0;
        UpdateHeartUI();
        Die();
    }

    /// <summary>
    /// 데미지를 입히는 메인 함수입니다. 무적 시간 및 상태를 체크합니다.
    /// </summary>
    /// <param name="damage">데미지 양</param>
    /// <param name="sourcePosition">넉백을 발생시킬 광원의 위치</param>
    /// <param name="knockbackMultiplier">넉백 강도 배율</param>
    public void TakeDamage(int damage, Vector3 sourcePosition = default, float knockbackMultiplier = 1f)
    {
        if (isDead || isForcedInvincible) return;

        // 1. 무적 시간 체크
        if (Time.time < nextDamageTime)
        {
            return;
        }

        // 2. 특수 무적 상태 체크 (대쉬 공격 중 등)
        if (dashScript != null && dashScript.IsInvincible) 
        {
            Debug.Log("<color=cyan>[PlayerHealth] 대쉬 공격 중 무적으로 피해를 회피했습니다.</color>");
            return;
        }

        // 2-2. 아이템 무적 스킬 체크
        if (TryGetComponent(out PlayerInvincibility itemInvinc) && itemInvinc.IsInvincible)
        {
            Debug.Log("<color=cyan>[PlayerHealth] 아이템 효과로 피해를 방어했습니다.</color>");
            return;
        }

        // 3. 실제 데미지 적용
        nextDamageTime = Time.time + invincibilityTime;
        currentHp = Mathf.Max(0, currentHp - damage);
        
        Debug.Log($"<color=orange>[PlayerHealth] 피격! 현재 체력: {currentHp}/{maxHp}</color>");
        
        UpdateHeartUI();

        // 4. 피격 연출 (빨간 번쩍임 + 넉백)
        if (hitEffect != null)
        {
            hitEffect.PlayHitEffect(sourcePosition, knockbackMultiplier);
        }

        // 5. 사망 판정
        if (currentHp <= 0) 
        {
            Die();
        }
    }

    /// <summary>
    /// 현재 체력에 맞춰 하트 아이콘의 활성화 상태를 갱신합니다.
    /// </summary>
    private void UpdateHeartUI()
    {
        if (heartIcons == null) return;

        for (int i = 0; i < heartIcons.Length; i++)
        {
            if (heartIcons[i] != null)
            {
                heartIcons[i].SetActive(i < currentHp);
            }
        }
    }

    /// <summary>
    /// 플레이어 사망 처리를 수행합니다.
    /// </summary>
    private void Die()
    {
        isDead = true;
        Debug.Log("<color=red><b>[PlayerHealth] 플레이어 사망!</b></color>");

        // 사망 애니메이션 실행
        if (anim != null) anim.SetTrigger("Die");

        // ── 즉시 모든 입력 차단 ──
        DisableAllInput();

        // 스테이지 매니저를 통한 실패 처리
        if (StageManager.Instance != null)
        {
            StageManager.Instance.FailStage();
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] StageManager 인스턴스를 찾을 수 없어 직접 씬을 전환합니다.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Result");
        }
    }

    /// <summary>
    /// 사망 시 PlayerInput, 이동, 공격 컴포넌트를 모두 비활성화하여
    /// 키 입력이 즉시 처리되지 않도록 합니다.
    /// </summary>
    private void DisableAllInput()
    {
        // 1. PlayerInput 비활성화 → New Input System 이벤트 완전 차단
        var playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = false;
        }

        // 2. 이동 스크립트 비활성화
        var move = GetComponent<PlayerMove>();
        if (move != null)
        {
            move.enabled = false;
        }

        // 3. 대쉬/공격 스크립트 비활성화
        if (dashScript != null)
        {
            dashScript.enabled = false;
        }

        // 4. Rigidbody 속도 즉시 제거 (관성으로 미끄러지지 않도록)
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        Debug.Log("<color=red>[PlayerHealth] 모든 플레이어 입력이 비활성화되었습니다.</color>");
    }
}