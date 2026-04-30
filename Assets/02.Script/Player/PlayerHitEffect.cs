using UnityEngine;
using System.Collections;

/// <summary>
/// 플레이어 피격 시 시각적 효과(빨간 깜빡임) 및 물리적 효과(넉백)를 관리하는 클래스.
/// PlayerHealth.TakeDamage()에서 피격 성공 시 PlayHitEffect()를 호출하여 사용한다.
/// </summary>
public class PlayerHitEffect : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  시각 효과 — 빨간 깜빡임
    // ─────────────────────────────────────────────

    [Header("빨간 번쩍임")]
    [SerializeField] private float flashDuration = 0.1f;       // 한 번의 깜빡임 주기(초). 값이 작을수록 빠르게 깜빡인다.
    [SerializeField] private int flashCount = 3;               // 빨간 깜빡임 반복 횟수. 최초 1회 강조 후 이 횟수만큼 추가 반복한다.
    [SerializeField] private Color hitColor = new Color(1f, 0.15f, 0.15f, 1f);  // 피격 시 적용되는 색상 (R:1 G:0.15 B:0.15 빨간색)

    // ─────────────────────────────────────────────
    //  물리 효과 — 넉백
    // ─────────────────────────────────────────────

    [Header("넉백 설정")]
    [SerializeField] private float knockbackForce = 6f;        // 넉백 기본 힘. knockbackMultiplier와 곱해져 최종 힘이 결정된다.
    [SerializeField] private float knockbackDuration = 0.15f;  // 넉백이 적용되는 시간(초). 이 동안 PlayerMove의 이동 입력이 차단된다.

    // ─── 내부 상태 (런타임 전용) ───
    private SpriteRenderer[] spriteRenderers;  // 자식 포함 모든 SpriteRenderer 캐싱
    private Rigidbody2D rb;                    // 넉백 속도 적용용
    private Color[] originalColors;            // 깜빡임 종료 후 복원할 원본 색상 배열
    private bool isFlashing = false;           // 현재 깜빡임 루틴 실행 중 여부

    /// <summary>현재 넉백 상태 여부. true이면 PlayerMove에서 이동 입력을 무시한다.</summary>
    public bool IsKnockedBack { get; private set; } = false;

    private PlayerInvincibility invincibility; // 무적 상태에서는 깜빡임을 건너뛰기 위한 참조

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        invincibility = GetComponent<PlayerInvincibility>();
        CacheRenderers();
    }

    /// <summary>
    /// 모든 자식 오브젝트의 SpriteRenderer와 초기 색상을 저장함.
    /// </summary>
    private void CacheRenderers()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (spriteRenderers == null) return;

        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                originalColors[i] = spriteRenderers[i].color;
        }
    }

    /// <summary>
    /// 피격 연출 루틴을 시작함. PlayerHealth에서 호출됨.
    /// </summary>
    /// <param name="damageSourcePos">데미지를 발생시킨 위치</param>
    /// <param name="knockbackMultiplier">넉백 강도 배율</param>
    public void PlayHitEffect(Vector3 damageSourcePos, float knockbackMultiplier = 1f)
    {
        // 무적 상태가 아닐 때만 색상 깜빡임 실행
        bool isInvincible = invincibility != null && invincibility.IsInvincible;
        if (!isFlashing && !isInvincible)
        {
            StartCoroutine(FlashRoutine());
        }

        ApplyKnockback(damageSourcePos, knockbackMultiplier);
    }

    /// <summary>
    /// 정해진 횟수만큼 스프라이트 색상을 변경하여 피격 상태를 시각화함.
    /// </summary>
    private IEnumerator FlashRoutine()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0) yield break;
        
        isFlashing = true;

        // 최초 1회 강한 강조
        SetColor(hitColor);
        yield return new WaitForSeconds(flashDuration);

        // 정해진 횟수만큼 반복 깜빡임
        for (int i = 0; i < flashCount; i++)
        {
            SetColor(new Color(1f, 1f, 1f, 0f)); // 완전 투명화 대신 알파값 조정
            yield return new WaitForSeconds(flashDuration * 0.5f);
            SetColor(hitColor);
            yield return new WaitForSeconds(flashDuration * 0.5f);
        }

        RestoreColors();
        isFlashing = false;
    }

    private void SetColor(Color color)
    {
        if (spriteRenderers == null) return;
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = color;
        }
    }

    private void RestoreColors()
    {
        if (spriteRenderers == null || originalColors == null) return;
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && i < originalColors.Length)
                spriteRenderers[i].color = originalColors[i];
        }
    }

    /// <summary>
    /// 타격 지점의 반대 방향으로 플레이어를 밀어냄.
    /// </summary>
    private void ApplyKnockback(Vector3 damageSourcePos, float knockbackMultiplier = 1f)
    {
        if (rb == null) return;

        float knockDirX;
        if (damageSourcePos == Vector3.zero)
        {
            knockDirX = -Mathf.Sign(transform.localScale.x);
        }
        else
        {
            knockDirX = Mathf.Sign(transform.position.x - damageSourcePos.x);
            if (Mathf.Approximately(knockDirX, 0)) knockDirX = 1f;
        }

        StartCoroutine(KnockbackRoutine(knockDirX, knockbackMultiplier));
    }

    /// <summary>
    /// 일정 시간 동안 Rigidbody의 속도를 제어하여 넉백을 구현함.
    /// </summary>
    private IEnumerator KnockbackRoutine(float dirX, float forceMult)
    {
        IsKnockedBack = true;
        float elapsed = 0f;
        float finalForce = knockbackForce * forceMult;
        
        while (elapsed < knockbackDuration)
        {
            if (rb != null)
                rb.linearVelocity = new Vector2(dirX * finalForce, rb.linearVelocity.y);
                
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        IsKnockedBack = false;
    }
}

