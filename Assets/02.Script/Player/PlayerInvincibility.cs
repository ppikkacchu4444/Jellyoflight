using UnityEngine;
using System.Collections;

/// <summary>
/// 아이템 효과로 발동되는 플레이어 무적 상태를 관리하는 클래스.
/// 무적 동안 속도 버프가 적용되고, 스프라이트가 무지개 색으로 변하며,
/// 주변 몬스터를 자동 탐지하여 넉백 후 즉사 처치한다.
/// </summary>
public class PlayerInvincibility : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  효과 설정
    // ─────────────────────────────────────────────

    [Header("효과 설정")]
    [SerializeField] private float speedMultiplier = 1.6f;     // 무적 중 이동 속도 배율. PlayerMove의 AddSpeedBuff/RemoveSpeedBuff로 제어된다.
    [SerializeField] private float knockbackForce = 18f;       // 몬스터를 튕겨내는 힘. 위쪽 벡터가 강화되어 쇟구치는 연출이 적용된다.
    [SerializeField] private float detectionRadius = 1.5f;     // 매 프레임 OverlapCircleAll로 몬스터를 감지하는 반경

    // ─── 내부 상태 (런타임 전용) ───
    private bool isInvincible = false;                 // 현재 무적 상태 여부
    /// <summary>현재 무적 상태인지 외부에서 확인하는 프로퍼티 (PlayerHealth에서 참조)</summary>
    public bool IsInvincible => isInvincible;

    private SpriteRenderer sr;                         // 무지개 색상 전환용
    private PlayerMove playerMove;                     // 속도 버프 적용/해제용
    private Vector3 originalScale;                     // 무적 종료 시 복원할 원본 크기

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        playerMove = GetComponent<PlayerMove>();
        originalScale = transform.localScale;
    }

    public void StartInvincibility(float duration, Sprite icon = null)
    {
        Debug.Log("<color=cyan><b>[GodMode]</b> 무적 루틴 시작됨!</color>");
        
        // UI 표시
        if (BuffUIManager.Instance != null && icon != null)
            BuffUIManager.Instance.AddBuff(icon, duration);

        StopAllCoroutines(); 
        StartCoroutine(InvincibleRoutine(duration));
    }

    private IEnumerator InvincibleRoutine(float duration)
    {
        isInvincible = true;
        
        if (playerMove != null) playerMove.AddSpeedBuff(speedMultiplier);

        float elapsed = 0;
        while (elapsed < duration)
        {
            // 충돌 체크가 꺼져있으므로 매 프레임 수동으로 주변 몬스터 탐색
            CheckNearMonsters();

            if (sr != null) 
            {
                float hue = (Time.time * 2f) % 1f;
                sr.color = Color.HSVToRGB(hue, 0.6f, 1.5f);
            }
            
            yield return null;
            elapsed += Time.deltaTime;
        }

        // 초기 상태로 복구
        if (sr != null) sr.color = Color.white;
        if (playerMove != null) playerMove.RemoveSpeedBuff(speedMultiplier);
        transform.localScale = originalScale;
        
        isInvincible = false;
        Debug.Log("<color=orange><b>[GodMode]</b> 무적 종료!</color>");
    }

    private void CheckNearMonsters()
    {
        // 레이어 충돌 매트릭스가 꺼져 있어도 OverlapCircle은 감지 가능함
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject || hit.transform.IsChildOf(transform)) continue;

            MonsterHealth monster = hit.GetComponentInParent<MonsterHealth>();
            if (monster != null)
            {
                TryKillTarget(monster);
            }
        }
    }

    private void TryKillTarget(MonsterHealth monster)
    {
        if (monster == null) return;

        Rigidbody2D mRb = monster.GetComponent<Rigidbody2D>();
        if (mRb != null)
        {
            // 넉백 저항 제거 (AI 정지)
            MonoBehaviour[] scripts = monster.GetComponentsInChildren<MonoBehaviour>();
            foreach (var s in scripts)
            {
                if (s.GetType().Name.Contains("Health")) continue;
                s.enabled = false;
            }

            // [중요] 물리 제약 완전 해제 (회전까지 풀어야 뒤집히며 날아감)
            mRb.constraints = RigidbodyConstraints2D.None;
            mRb.bodyType = RigidbodyType2D.Dynamic;

            // 방향 계산 (위쪽 벡터를 훨씬 강력하게 설정하여 솟구치게 함)
            Vector2 originPos = transform.position;
            Vector2 targetPos = monster.transform.position;
            Vector2 pushDir = (targetPos - originPos).normalized;
            
            if (pushDir == Vector2.zero) pushDir = Vector2.up;
            pushDir += Vector2.up * 1.5f; // 상승 기류 효과 강화
            
            // 속도 직접 대입 (넉백 힘에 추가 보정)
            mRb.linearVelocity = pushDir.normalized * (knockbackForce * 1.2f);

            // [추가] 뒤집히는 회전력 부여 (빙글빙글 돌게 함)
            float randomTorque = Random.Range(-15f, 15f) * knockbackForce;
            mRb.angularVelocity = randomTorque;
            
            Debug.Log($"<color=red><b>[Flip]</b> 몬스터 '{monster.name}' 공중으로 뒤집기!</color>");

            // 무적 킬 효과음 재생
            if (EffectManager.Instance != null) EffectManager.Instance.PlayInvincKillSound();
        }

        // 즉시 파괴 방지를 위해 지연 처치
        StartCoroutine(DelayedKillRoutine(monster));
    }

    private IEnumerator DelayedKillRoutine(MonsterHealth monster)
    {
        // 0.05초 뒤에 처치하여 넉백 가속도가 붙은 상태에서 사망 연출 시작
        yield return new WaitForSeconds(0.05f);
        if (monster != null)
        {
            monster.TakeDamage(999);
        }
    }
}
