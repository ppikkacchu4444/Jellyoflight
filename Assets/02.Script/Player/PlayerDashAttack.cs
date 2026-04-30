using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 플레이어의 대쉬 공격 기능을 관리하는 클래스.
/// 공격 버튼을 누르면 바라보는 방향으로 대쉬 전진하며 범위 내 적에게 데미지를 입히고,
/// 전진 종료 후 원래 위치로 복귀한다. 대쉬 중에는 무적(IsInvincible) 상태가 된다.
/// 그로기 상태의 보스에게도 타격이 가능하다.
/// </summary>
public class PlayerDashAttack : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  대쉬 기본 설정
    // ─────────────────────────────────────────────

    [Header("--- Dash Settings ---")]
    [SerializeField] private float dashDistance = 5f;             // 전진하는 최대 거리(유닛). 원래 위치에서 이 거리만큼 직선 이동한다.
    [SerializeField] private float dashSpeed = 25f;               // 전진 단계의 이동 속도. 값이 클수록 순식간에 돌진한다.
    [SerializeField] private float returnSpeed = 15f;             // 복귀 단계의 이동 속도. 전진보다 느리게 설정하면 자연스러운 리듬감이 생긴다.
    [SerializeField] private int dashDamage = 1;                  // 대쉬 중 적에게 가하는 데미지
    [SerializeField] private float attackRadius = 1.5f;           // 매 프레임 OverlapCircleAll로 적을 탐지하는 반경. 기즈모(노란 원)로 확인 가능.

    // ─────────────────────────────────────────────
    //  이펙트 설정
    // ─────────────────────────────────────────────

    [Header("--- FX Settings ---")]
    [SerializeField] private GameObject dashStartEffect;          // 대쉬 시작 시 캐릭터에 부착되는 시각 이펙트 프리팹. 복귀 시 자동 파괴된다.
    [SerializeField] private GameObject dashHitEffect;            // 적 타격 시 접촉 지점에 생성되는 히트 이펙트 프리팹
    [SerializeField] private float effectDestroyDelay = 1.0f;     // 히트 이펙트가 생성 후 자동 파괴되기까지의 시간(초)
    [SerializeField] private Vector3 dashEffectOffset = new Vector3(-0.8f, 0, -0.1f);  // dashStartEffect의 로컬 위치 오프셋. 캐릭터 뒤쪽에 배치하기 위한 값.

    // ─────────────────────────────────────────────
    //  사운드 설정
    // ─────────────────────────────────────────────

    [Header("--- Sound Settings ---")]
    [SerializeField] private AudioClip dashAttackSound;           // 대쉬 시작 시 재생되는 효과음 (돌진 소리)
    [SerializeField] private AudioClip dashHitSound;              // 적 타격 시 재생되는 효과음 (타격 소리)

    // ─── 내부 상태 (런타임 전용) ───
    private bool isAttacking = false;                  // 현재 대쉬 공격 루틴 실행 중 여부
    private Vector2 originalPosition;                  // 대쉬 시작 시점의 원래 위치 (복귀 목표)
    
    private Rigidbody2D rb;                            // 물리 이동 제어용
    private Animator anim;                             // 공격 애니메이션 제어용

    private HashSet<MonsterHealth> hitMonsters = new HashSet<MonsterHealth>();  // 현재 대쉬에서 이미 타격한 몬스터 목록 (중복 데미지 방지)
    private bool hitBossThisDash = false;              // 현재 대쉬에서 보스를 이미 타격했는지 여부

    /// <summary>대쉬 공격 중이면 true. 이 동안 플레이어는 무적 상태이다.</summary>
    public bool IsInvincible => isAttacking;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    /// <summary>
    /// 대쉬 버튼 입력 시 호출. 
    /// isAttacking이 true일 동안은 어떤 중복 요청도 거절합니다.
    /// </summary>
    public void OnAttack(InputValue value)
    {
        if (ControlGuideUI.IsShowing) return;

        if (value != null && value.isPressed && !isAttacking)
        {
            StartCoroutine(DashAndReturnRoutine());
        }
    }

    /// <summary>
    /// 대쉬 전진 후 원래 위치로 복귀하는 루틴.
    /// </summary>
    private IEnumerator DashAndReturnRoutine()
    {
        if (rb == null || isAttacking) yield break;

        isAttacking = true;
        hitMonsters.Clear();
        hitBossThisDash = false;

        // 공격 애니메이션 실행
        if (anim != null)
        {
            anim.SetTrigger("Attack");
            anim.SetBool("isAttacking", true);
        }

        // 대시 시작 효과음 재생
        if (dashAttackSound != null) SoundManager.Instance.PlaySFX(dashAttackSound);

        // [복구] 캐릭터의 자식으로 생성하여 정상 크기 유지
        GameObject dashFX = null;
        if (dashStartEffect != null)
        {
            dashFX = Instantiate(dashStartEffect, transform);
            dashFX.transform.localPosition = dashEffectOffset;
            dashFX.transform.localRotation = Quaternion.identity;

            foreach (var animator in dashFX.GetComponentsInChildren<Animator>(true))
            {
                animator.speed = 1.0f;
                animator.Play(0, -1, 0f);
            }

            foreach (var ps in dashFX.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.loop = true;
                ps.Play();
            }

            foreach (var r in dashFX.GetComponentsInChildren<Renderer>(true))
            {
                r.sortingOrder = 120;
            }
        }

        float savedGravity = rb.gravityScale;
        RigidbodyType2D savedBodyType = rb.bodyType;
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        originalPosition = rb.position;
        float direction = Mathf.Sign(transform.localScale.x);
        Vector2 targetPosition = originalPosition + new Vector2(direction * dashDistance, 0);

        BossMonster boss = FindFirstObjectByType<BossMonster>();

        // 1. 전진 단계 (캐릭터 추격)
        while (Vector2.Distance(rb.position, targetPosition) > 0.15f)
        {
            rb.MovePosition(Vector2.MoveTowards(rb.position, targetPosition, dashSpeed * Time.fixedDeltaTime));
            CheckForDamage(boss);
            yield return new WaitForFixedUpdate();
        }

        // [핵심] 복귀 시작 전 즉시 본체 은폐 및 파괴
        if (dashFX != null)
        {
            dashFX.SetActive(false); // 즉시 은폐
            dashFX.transform.SetParent(null); // 분리
            Destroy(dashFX);
            dashFX = null;
        }

        yield return new WaitForSeconds(0.05f);

        // 2. 복귀 단계 (완전한 무상상태)
        while (Vector2.Distance(rb.position, originalPosition) > 0.15f)
        {
            rb.MovePosition(Vector2.MoveTowards(rb.position, originalPosition, returnSpeed * Time.fixedDeltaTime));
            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(originalPosition);
        rb.bodyType = savedBodyType;
        rb.gravityScale = savedGravity;
        rb.linearVelocity = Vector2.zero;

        isAttacking = false;
        if (anim != null) anim.SetBool("isAttacking", false);
        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator FadeOutGhost(SpriteRenderer sr, float duration)
    {
        float elapsed = 0;
        if (sr == null) yield break;
        Color startColor = sr.color;

        while (elapsed < duration)
        {
            if (sr == null) break;
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1.0f, 0f, elapsed / duration);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        if (sr != null) Destroy(sr.gameObject);
    }

    private void CheckForDamage(BossMonster currentBoss)
    {
        // [히트박스 생성 및 스캔]
        // Physics2D.OverlapCircleAll은 물리적인 콜라이더 컴포넌트를 켜고 끄는 방식이 아니라,
        // 호출되는 이 순간(프레임)에 지정된 위치(transform.position, 플레이어 중심)에
        // 수학적인 가상의 원(반경 attackRadius)을 그려서 그 범위 안에 있는 모든 대상(콜라이더)을 배열(hits)로 가져옵니다.
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRadius);

        // 원 안에 들어온 모든 대상들을 하나씩 검사합니다.
        foreach (var hit in hits)
        {
            // 나 자신(플레이어)은 타격 대상에서 제외합니다.
            if (hit == null || hit.gameObject == gameObject) continue;

            // [일반 몬스터 타격 판정]
            // 부딪힌 대상이 몬스터(MonsterHealth)인지 확인합니다. 자식/부모 오브젝트 구조도 고려합니다.
            if (hit.TryGetComponent(out MonsterHealth monster) || 
                (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out monster)))
            {
                // 다단 히트(중복 데미지) 방지
                // 대쉬가 끝날 때까지 한 번 때린 적은 hitMonsters 리스트에 기록해두어 또 때리지 않습니다.
                if (!hitMonsters.Contains(monster))
                {
                    monster.TakeDamage(dashDamage, transform.position); // 데미지 적용
                    hitMonsters.Add(monster); // 타격 명단에 추가

                    // [히트 이펙트 위치 계산]
                    // 히트박스 중심이 아닌, 실제 콜라이더가 맞닿는 가장 가까운 지점(ClosestPoint)을 찾아
                    // 타격 이펙트가 자연스러운 위치(적의 표면)에서 터지도록 계산합니다.
                    Collider2D myCol = GetComponent<Collider2D>();
                    Vector3 contactPoint = myCol != null ? (Vector3)myCol.ClosestPoint(hit.transform.position) : hit.transform.position;
                    contactPoint.y += 0.4f; // 이펙트가 살짝 위쪽에서 터지도록 보정
                    contactPoint.z = -0.6f; // 이펙트가 캐릭터보다 앞쪽에 렌더링되도록 Z축 보정

                    if (dashHitEffect != null)
                    {
                        GameObject hEffect = Instantiate(dashHitEffect, contactPoint, Quaternion.identity);
                        foreach (var sr in hEffect.GetComponentsInChildren<SpriteRenderer>(true))
                        {
                            sr.sortingOrder = 125;
                            StartCoroutine(FadeOutGhost(sr, 0.3f));
                        }
                        Destroy(hEffect, effectDestroyDelay);
                    }

                    // 타격 효과음 재생
                    if (dashHitSound != null) SoundManager.Instance.PlaySFX(dashHitSound);
                }
            }

            // 그 외 상호작용 가능한 기믹/오브젝트 타격 판정
            if (hit.TryGetComponent(out SealObject seal)) seal.TakeDamage();
            if (hit.TryGetComponent(out MonsterPortal portal)) portal.TakeDamage();
        }

        // [보스 몬스터 전용 타격 판정]
        // 보스는 덩치가 커서 OverlapCircle(중심점 기준 탐지)만으로는 타격 판정이 어려울 수 있으므로
        // 플레이어와 보스 사이의 '거리(Distance)'를 직접 계산해서 맞았는지 판정하는 별도의 로직을 사용합니다.
        if (!hitBossThisDash && currentBoss != null && currentBoss.IsGroggy)
        {
            float bossRadius = 2f; // 보스의 기본 반경 크기 설정
            Renderer bossRenderer = currentBoss.GetComponentInChildren<Renderer>();
            
            // 보스의 실제 렌더러(스프라이트) 크기를 기반으로 반경을 동적으로 계산합니다.
            if (bossRenderer != null)
            {
                bossRadius = Mathf.Max(bossRenderer.bounds.extents.x, bossRenderer.bounds.extents.y);
            }

            // 플레이어와 보스 중심 사이의 거리가 (플레이어 공격 반경 + 보스 크기 반경) 보다 작거나 같으면 충돌(타격)한 것으로 간주합니다.
            if (Vector2.Distance(transform.position, currentBoss.transform.position) <= attackRadius + bossRadius)
            {
                currentBoss.TakeHit();
                hitBossThisDash = true; // 현재 대쉬에서 보스를 쳤음을 기록 (중복 타격 방지)
                
                // 보스 타격 이펙트 생성 (일반 몬스터와 동일한 위치 보정 방식)
                if (dashHitEffect != null)
                {
                    Collider2D playerCol = GetComponent<Collider2D>();
                    Vector3 contactPoint = playerCol != null ? (Vector3)playerCol.ClosestPoint(currentBoss.transform.position) : transform.position;
                    contactPoint.y += 0.4f;
                    contactPoint.z = -0.6f;

                    GameObject hEffect = Instantiate(dashHitEffect, contactPoint, Quaternion.identity);
                    foreach (var sr in hEffect.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        sr.sortingOrder = 125;
                        StartCoroutine(FadeOutGhost(sr, 0.3f));
                    }
                    Destroy(hEffect, effectDestroyDelay);

                    // 보스 타격 시에도 효과음 재생
                    if (dashHitSound != null) SoundManager.Instance.PlaySFX(dashHitSound);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}