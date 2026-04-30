using UnityEngine;
using System.Collections;

/// <summary>
/// 모든 일반 몬스터의 체력, 피격 및 사망 로직을 관리하는 클래스.
/// 주변 플레이어를 감지하여 주기적으로 데미지를 입히는 기능 포함.
/// </summary>
public class MonsterHealth : MonoBehaviour
{
    [Header("--- Monster Stats ---")]
    [SerializeField] private int maxHp = 3;
    private int currentHp;

    [Header("--- 사망 넉백 설정 ---")]
    [Tooltip("사망 시 뒤로 밀려나는 수평 힘")]
    [SerializeField] private float deathKnockbackForce = 3f;
    [Tooltip("사망 시 위로 살짝 띄우는 힘")]
    [SerializeField] private float deathPopUpForce = 4f;

    [Header("--- Attack Settings ---")]
    [SerializeField] private bool canDealContactDamage = true; // 접촉 데미지 발생 여부
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float attackRadius = 1.0f;
    [SerializeField] private float damageCooldown = 0.5f;

    [Header("--- Audio Settings ---")]
    [SerializeField] private AudioClip deathSound;

    private float lastDamageTime;
    private MonsterHitEffect hitEffect;
    private bool isDead = false;

    /// <summary>
    /// 외부에서 접촉 데미지 기능을 켜거나 끌 수 있는 프로퍼티.
    /// </summary>
    public bool CanDealContactDamage
    {
        get => canDealContactDamage;
        set => canDealContactDamage = value;
    }

    private void Start()
    {
        currentHp = maxHp;
        hitEffect = GetComponent<MonsterHitEffect>();
    }

    private void Update()
    {
        if (isDead || !canDealContactDamage) return;
        
        // 데미지 쿨타임 체크 후 플레이어 감지 실행
        if (Time.time >= lastDamageTime + damageCooldown)
        {
            CheckPlayerDamage();
        }
    }



    /// <summary>
    /// 반경 내 플레이어가 있는지 확인하고 데미지를 입힘.
    /// </summary>
    private void CheckPlayerDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRadius);

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            // 플레이어 태그 또는 레이어로 확인
            if (hit.CompareTag("Player") || hit.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                if (hit.TryGetComponent(out PlayerHealth player) || 
                    (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out player)))
                {
                    player.TakeDamage(contactDamage, transform.position);
                    lastDamageTime = Time.time;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 외부로부터 데미지를 입을 때 호출됨.
    /// </summary>
    public void TakeDamage(int damage, Vector3 damageSourcePos = default)
    {
        if (isDead) return;

        currentHp = Mathf.Max(0, currentHp - damage);

        // 피격 시각 효과 재생
        if (hitEffect != null)
        {
            hitEffect.PlayHitEffect(damageSourcePos);
        }

        if (currentHp <= 0)
        {
            // 데미지가 999 이상이면 강력한 넉백 사망(Flying)으로 간주
            Die(damage >= 999, damageSourcePos);
        }
    }

    /// <summary>
    /// 몬스터 사망 처리를 수행하고 오브젝트를 제거함.
    /// </summary>
    private void Die(bool isFlyingDeath, Vector3 damageSourcePos = default)
    {
        if (isDead) return;
        isDead = true;

        // 사망 효과음 재생
        if (deathSound != null) SoundManager.Instance.PlaySFX(deathSound);

        // 이동/AI 스크립트 비활성화 (뒤집힌 뒤에도 이동하지 않도록)
        if (TryGetComponent(out Monster monster)) monster.enabled = false;
        if (TryGetComponent(out ChaserMonster chaser)) chaser.enabled = false;
        if (TryGetComponent(out ParasiteMonster parasite)) parasite.enabled = false;

        // Animator를 멈춰서 뒤집힘 연출이 방해받지 않도록 함
        if (TryGetComponent(out Animator anim)) anim.enabled = false;

        // 콜라이더를 트리거로 전환 (다른 오브젝트와 충돌하지 않으면서 물리는 유지)
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            if (col != null) col.isTrigger = true;
        }

        if (TryGetComponent(out Rigidbody2D rb))
        {
            // 회전 잠금 해제 (사망 연출에서 Z축 회전 사용)
            rb.freezeRotation = false;

            if (isFlyingDeath)
            {
                // 강력 넉백 사망: 날아가는 물리력을 유지함
                rb.gravityScale = 1.3f;
                rb.linearDamping = 0.5f;
                // 날아가면서 빙글 도는 회전력
                float spinDir = Random.value > 0.5f ? 1f : -1f;
                rb.angularVelocity = spinDir * 360f;
            }
            else
            {
                // 일반 사망: 살짝 뒤로 넉백하며 위로 튀어오르는 연출
                rb.gravityScale = 2f;
                rb.linearDamping = 0f;
                rb.linearVelocity = Vector2.zero;

                // 데미지 소스 반대 방향으로 넉백
                float knockDir = 1f;
                if (damageSourcePos != default)
                {
                    knockDir = (transform.position.x - damageSourcePos.x) >= 0 ? 1f : -1f;
                }

                Vector2 knockback = new Vector2(knockDir * deathKnockbackForce, deathPopUpForce);
                rb.linearVelocity = knockback;

                // 넉백 방향 반대로 회전 (자연스러운 넉다운 느낌)
                rb.angularVelocity = -knockDir * 300f;
            }
        }

        // 뒤집히면서 서서히 투명해지며 파괴되는 루틴 시작
        StartCoroutine(FlipAndFadeOutRoutine(isFlyingDeath));
    }

    /// <summary>
    /// 사망 시 Z축 회전으로 물리적으로 뒤집히며, 동시에 서서히 투명해지고 파괴됩니다.
    /// </summary>
    private IEnumerator FlipAndFadeOutRoutine(bool isFlyingDeath)
    {
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        
        float fadeDuration = 0.8f;  // 투명해지는 데 걸리는 시간
        float elapsed = 0f;

        // 사망 시점의 Y좌표를 바닥 기준선으로 저장 (이 아래로는 절대 떨어지지 않음)
        float floorY = transform.position.y;
        bool hasLanded = false;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            // ── 바닥 아래로 떨어지지 않도록 클램프 ──
            if (!isFlyingDeath && !hasLanded && rb != null)
            {
                if (transform.position.y <= floorY && elapsed > 0.05f)
                {
                    // 바닥에 도달 → 물리 정지
                    transform.position = new Vector3(transform.position.x, floorY, transform.position.z);
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                    rb.gravityScale = 0f;
                    rb.simulated = false;
                    hasLanded = true;
                }
            }

            // ── 페이드 아웃 ──
            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                sr.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}
