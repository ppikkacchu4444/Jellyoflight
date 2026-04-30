using UnityEngine;
using System.Collections;

// ──────────────────────────────────────────────
//  MonsterHitEffect
//  몬스터 피격 시 빨간 번쩍임 + 넉백 연출
//  MonsterHealth와 같은 오브젝트에 추가
// ──────────────────────────────────────────────
public class MonsterHitEffect : MonoBehaviour
{
    [Header("빨간 번쩍임")]
    [SerializeField] private float flashDuration = 0.1f;   // 빨간색 유지 시간
    [SerializeField] private int flashCount = 1;           // 번쩍이는 횟수 (몬스터는 짧게)
    [SerializeField] private Color hitColor = new Color(1f, 0.3f, 0.3f, 1f); // 피격 색상

    [Header("넉백")]
    [SerializeField] private bool canKnockback = true;     // 보스 등은 꺼둘 수 있음
    [SerializeField] private float knockbackForce = 5f;    // 넉백 세기
    [SerializeField] private float knockbackDuration = 0.15f; // 넉백 지속 시간

    private SpriteRenderer[] spriteRenderers;
    private Rigidbody2D rb;
    private Color[] originalColors;
    private bool isFlashing = false;
    private bool isKnockbacking = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        CacheRenderers();
    }

    private void CacheRenderers()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
            originalColors[i] = spriteRenderers[i].color;
    }

    // MonsterHealth.TakeDamage() 등에서 호출
    public void PlayHitEffect(Vector3 damageSourcePos)
    {
        // 1. 빨간색 깜빡임
        if (!isFlashing)
            StartCoroutine(FlashRoutine());

        // 2. 넉백 (Rigidbody가 있고 넉백이 켜져있을 때)
        if (canKnockback && rb != null && !isKnockbacking)
        {
            ApplyKnockback(damageSourcePos);
        }
    }

    private IEnumerator FlashRoutine()
    {
        isFlashing = true;

        // 빨간색 적용
        SetColor(hitColor);
        yield return new WaitForSeconds(flashDuration);

        // 깜빡임 횟수만큼 반복
        for (int i = 0; i < flashCount; i++)
        {
            SetColor(Color.clear);
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
        if (spriteRenderers == null) return;
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = originalColors[i];
        }
    }

    private void ApplyKnockback(Vector3 damageSourcePos)
    {
        float knockDirX;
        if (damageSourcePos == Vector3.zero)
        {
            // 위치 모를 때: 방향 반전
            knockDirX = -Mathf.Sign(transform.localScale.x);
        }
        else
        {
            knockDirX = Mathf.Sign(transform.position.x - damageSourcePos.x);
        }
        
        if (knockDirX == 0) knockDirX = 1f;

        StartCoroutine(KnockbackRoutine(knockDirX));
    }

    private IEnumerator KnockbackRoutine(float dirX)
    {
        isKnockbacking = true;
        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            if (rb != null)
                rb.linearVelocity = new Vector2(dirX * knockbackForce, rb.linearVelocity.y);
            
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        isKnockbacking = false;
    }
}
