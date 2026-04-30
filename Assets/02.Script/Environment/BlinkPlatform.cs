using UnityEngine;

// ──────────────────────────────────────────────
//  BlinkPlatform
//  일정 시간 간격으로 사라졌다 나타나기를 반복하는 발판.
//  나타나거나 사라질 때 페이드 연출이 적용되며,
//  사라진 동안에는 콜라이더가 비활성화되어 플레이어가 통과한다.
// ──────────────────────────────────────────────
public class BlinkPlatform : MonoBehaviour
{
    [Header("타이밍 설정")]
    [SerializeField] private float visibleDuration = 3f;      // 발판이 보이는 시간(초)
    [SerializeField] private float hiddenDuration  = 2f;      // 발판이 숨겨진 시간(초)
    [SerializeField] private float fadeDuration    = 0.5f;    // 페이드 인/아웃 전환 시간(초). 0이면 즉시 전환된다.

    [Header("시작 옵션")]
    [SerializeField] private bool  startVisible    = true;    // true면 보이는 상태로 시작, false면 숨긴 상태로 시작
    [SerializeField] private float randomOffset    = 0f;      // 여러 개를 배치할 때 타이밍을 엇갈리게 하기 위한 랜덤 오프셋 최대값(초)

    // ─── 내부 상태 ─────────────────────────────
    private SpriteRenderer spriteRenderer;
    private Collider2D     platformCollider;
    private Color          originalColor;

    private float timer;          // 현재 페이즈 내 경과 시간
    private bool  isVisible;      // 현재 보이는 상태인지
    private bool  isFading;       // 페이드 전환 중인지
    private float fadeTimer;      // 페이드 전환 경과 시간
    private bool  fadingIn;       // true = 페이드 인(나타남), false = 페이드 아웃(사라짐)

    private void Start()
    {
        spriteRenderer   = GetComponent<SpriteRenderer>();
        platformCollider = GetComponent<Collider2D>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        // 초기 상태 설정
        isVisible = startVisible;
        ApplyVisibility(isVisible ? 1f : 0f);

        // 랜덤 오프셋 적용 — 여러 발판이 동시에 깜빡이지 않도록
        float offset = randomOffset > 0f ? Random.Range(0f, randomOffset) : 0f;
        timer = -offset; // 음수부터 시작하여 오프셋만큼 지연
    }

    private void Update()
    {
        // ── 페이드 전환 중 ──
        if (isFading)
        {
            fadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(fadeTimer / Mathf.Max(fadeDuration, 0.01f));
            float alpha = fadingIn ? t : (1f - t);

            ApplyAlpha(alpha);

            if (t >= 1f)
            {
                // 전환 완료
                isFading  = false;
                isVisible = fadingIn;
                ApplyVisibility(isVisible ? 1f : 0f);
                timer = 0f;
            }
            return;
        }

        // ── 일반 대기 ──
        timer += Time.deltaTime;

        float currentPhaseDuration = isVisible ? visibleDuration : hiddenDuration;

        if (timer >= currentPhaseDuration)
        {
            // 페이드 전환 시작
            isFading  = true;
            fadeTimer = 0f;
            fadingIn  = !isVisible; // 보이면 → 사라짐, 숨겨져 있으면 → 나타남

            // 페이드 아웃 시작 시점에 콜라이더를 바로 끔 (발판 위에 서 있는 플레이어가 빠지도록)
            if (!fadingIn && platformCollider != null)
                platformCollider.enabled = false;
        }
    }

    /// <summary>
    /// 알파값만 변경한다. (콜라이더 상태는 건드리지 않음)
    /// </summary>
    private void ApplyAlpha(float alpha)
    {
        if (spriteRenderer == null) return;
        Color c = originalColor;
        c.a = alpha;
        spriteRenderer.color = c;
    }

    /// <summary>
    /// 알파값과 콜라이더 상태를 동시에 적용한다.
    /// alpha가 0이면 콜라이더 비활성화, 0보다 크면 활성화.
    /// </summary>
    private void ApplyVisibility(float alpha)
    {
        ApplyAlpha(alpha);

        if (platformCollider != null)
            platformCollider.enabled = alpha > 0f;
    }
}
