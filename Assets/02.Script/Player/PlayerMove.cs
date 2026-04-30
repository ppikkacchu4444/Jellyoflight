using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 플레이어의 이동, 점프 및 물리적 상호작용(슬로우, 버프 등)을 관리하는 클래스입니다.
/// New Input System의 OnMove/OnJump 이벤트를 수신하여 캐릭터를 제어하며,
/// 외부 기믹(컨베이어, 점프 패드 등)의 속도 주입과 스택형 속도/점프 배율 시스템을 지원합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMove : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  이동 · 점프 기본 설정
    // ─────────────────────────────────────────────

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;            // 좌우 이동 기본 속도. 슬로우/버프 배율에 의해 런타임에 변동된다.
    [SerializeField] private float jumpForce = 12f;           // 점프 시 Y축에 가해지는 순간 속도. 점프 디버프 배율의 영향을 받는다.

    // ─────────────────────────────────────────────
    //  지면 판정 · 점프 보정
    // ─────────────────────────────────────────────

    [Header("Ground Check & Jump Feel")]
    [SerializeField] private Transform groundCheck;           // 지면 판정용 기준점. 플레이어 발 아래에 빈 오브젝트를 배치하여 할당한다.
    [SerializeField] private float checkRadius = 0.2f;        // groundCheck 위치에서 OverlapCircle로 지면을 감지하는 반지름
    [SerializeField] private LayerMask groundLayer;            // 지면으로 인정할 레이어 마스크. 여기에 포함된 레이어만 착지 판정에 사용된다.
    [SerializeField] private float coyoteTime = 0.1f;         // 코요테 타임 — 발판에서 벗어난 후에도 이 시간(초) 동안 점프를 허용한다.
    [SerializeField] private float jumpBufferTime = 0.1f;     // 점프 버퍼 — 착지 직전에 미리 점프 키를 눌러도 이 시간(초) 동안 입력을 기억한다.

    // ─── 상태 변수 (런타임 전용) ───
    private float coyoteTimeCounter;          // 코요테 타임 남은 시간
    private float jumpBufferCounter;          // 점프 버퍼 남은 시간
    private bool isGrounded;                  // 현재 지면에 닿아 있는지
    private Vector2 moveInput;                // 입력 시스템에서 받은 이동 벡터
    private float idleTimer;                  // 가만히 서 있는 누적 시간
    private float lastSpecialIdleTime;        // 마지막 특수 대기 모션 재생 시각

    // ─────────────────────────────────────────────
    //  특수 대기 모션
    // ─────────────────────────────────────────────

    [Header("Special Idle Settings")]
    [SerializeField] private float specialIdleThreshold = 5.0f;   // 입력 없이 이 시간(초)이 지나면 특수 대기 모션을 재생한다.
    [SerializeField] private float specialIdleCooldown = 10.0f;   // 특수 대기 모션 재생 후 다음 재생까지의 쿨타임(초)

    // ─── 컴포넌트 캐싱 ───
    private Rigidbody2D rb;                   // 물리 이동 제어용
    private Animator anim;                    // 애니메이션 파라미터 제어용
    private PlayerHitEffect hitEffect;        // 넉백 상태 확인용 (넉백 중 이동 입력 차단)
    private PlayerDashAttack dashAttack;      // 공격 중 애니메이션 파라미터 갱신 차단용

    // ─────────────────────────────────────────────
    //  사운드
    // ─────────────────────────────────────────────

    [Header("Audio Settings")]
    [SerializeField] private AudioClip jumpSound;                 // 점프 시 재생되는 효과음 클립. null이면 무음.

    // ─────────────────────────────────────────────
    //  상태 효과 — 스택형 속도 · 점프 배율 관리
    // ─────────────────────────────────────────────

    [Header("Status Effects (Internal)")]
    private float baseSpeed;                  // Start 시점의 moveSpeed 원본값
    private float baseJumpForce;              // Start 시점의 jumpForce 원본값
    
    private float slowFactor = 1f;            // 슬로우 누적 배율 (0.1 ~ 1.0)
    private int slowStackCount = 0;           // 현재 적용 중인 슬로우 스택 수
    private float jumpDebuffFactor = 1f;      // 점프력 감소 누적 배율 (0.1 ~ 1.0)
    private int jumpDebuffStackCount = 0;     // 현재 적용 중인 점프 디버프 스택 수
    private float buffFactor = 1f;            // 속도 버프 누적 배율 (1.0 이상)
    private int buffStackCount = 0;           // 현재 적용 중인 속도 버프 스택 수

    /// <summary>
    /// 외부 환경(컨베이어 벨트, MovingPlatform 등)에서 프레임별로 강제 주입되는 속도입니다.
    /// FixedUpdate 종료 시 매 프레임 초기화되며, 필요한 곳에서 매 프레임 채워 넣어야 합니다.
    /// </summary>
    [HideInInspector] public Vector2 externalVelocity;

    private void Start()
    {
        InitializeComponents();
        baseSpeed = moveSpeed;
        baseJumpForce = jumpForce;
    }

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        hitEffect = GetComponent<PlayerHitEffect>();
        dashAttack = GetComponent<PlayerDashAttack>();

        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    #region Input Events (New Input System)
    
    public void OnMove(InputValue value)
    {
        if (ControlGuideUI.IsShowing)
        {
            moveInput = Vector2.zero;
            return;
        }

        if (value != null)
            moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (ControlGuideUI.IsShowing) return;

        if (value != null && value.isPressed)
        {
            jumpBufferCounter = jumpBufferTime;
        }
    }

    #endregion

    private void Update()
    {
        if (ControlGuideUI.IsShowing)
        {
            moveInput = Vector2.zero;
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }

        UpdateGroundStatus();
        HandleJumpLogic();
        UpdateAnimations();
        HandleSpecialIdle();
        HandleSpriteFlip();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    private void UpdateGroundStatus()
    {
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        else
            isGrounded = false;

        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;

        jumpBufferCounter -= Time.deltaTime;
    }

    private void HandleJumpLogic()
    {
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            float effectiveJumpForce = baseJumpForce * jumpDebuffFactor;
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, effectiveJumpForce);
                
                // 점프하는 순간 'Jump' 트리거 실행
                if (anim != null) anim.SetTrigger("Jump");

                // 점프 효과음 재생
                if (jumpSound != null) SoundManager.Instance.PlaySFX(jumpSound);
            }
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }
    }

    /// <summary>
    /// 외부 기믹(점프 패드 등)에 의해 강제로 공중으로 솟구칠 때 호출됩니다.
    /// </summary>
    /// <param name="force">상승 힘</param>
    public void LaunchPlayer(float force)
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
            
            // 점프 애니메이션 강제 실행
            if (anim != null)
            {
                anim.SetTrigger("Jump");
            }

            // 외부 런칭 시에도 효과음 재생
            if (jumpSound != null) SoundManager.Instance.PlaySFX(jumpSound);
        }
    }

    private void UpdateAnimations()
    {
        if (anim == null) return;

        // 공격 중이라면 이동 관련 파라미터 업데이트를 건너뜀 (공격 애니메이션 유지)
        if (dashAttack != null && dashAttack.IsInvincible) return;
        
        anim.SetFloat("InputX", moveInput.x);
        anim.SetFloat("Speed", Mathf.Abs(moveInput.x));
        anim.SetBool("isGrounded", isGrounded);
        
        // Y축 속도 전달 (상승/하강 구분용)
        if (rb != null)
        {
            anim.SetFloat("yVelocity", rb.linearVelocity.y);
        }
    }

    /// <summary>
    /// 입력이 없을 때 일정 시간 후 특수 대기 모션을 실행함
    /// </summary>
    private void HandleSpecialIdle()
    {
        if (anim == null) return;

        // 움직임이 있거나 땅에 없거나 공격 중이거나 피격 중이면 즉시 해제 및 리셋
        bool isMoving = Mathf.Abs(moveInput.x) > 0.01f || Mathf.Abs(moveInput.y) > 0.01f;
        bool isAttacking = (dashAttack != null && dashAttack.IsInvincible);
        bool isHit = (hitEffect != null && hitEffect.IsKnockedBack);
        
        if (isMoving || !isGrounded || isAttacking || isHit)
        {
            idleTimer = 0f;
            anim.SetBool("isSpecialIdle", false);
            return;
        }

        // 재생 중이라면 리턴 (입력을 기다림)
        if (anim.GetBool("isSpecialIdle"))
        {
            // 재생 중에라도 이동 입력이 들어오면 위에 if문에서 false가 되어 빠져나감
            return;
        }

        // 지면에 가만히 서 있는 경우 타이머 증가
        idleTimer += Time.deltaTime;

        // 타이머가 임계치를 넘고 쿨타임이 지났는지 확인
        if (idleTimer >= specialIdleThreshold)
        {
            if (Time.time >= lastSpecialIdleTime + specialIdleCooldown)
            {
                anim.SetBool("isSpecialIdle", true);
                lastSpecialIdleTime = Time.time;
            }
            // 트리거 후 타이머 리셋
            idleTimer = 0f;
        }
    }

    private void HandleSpriteFlip()
    {
        if (Mathf.Abs(moveInput.x) > 0.01f)
        {
            float direction = Mathf.Sign(moveInput.x);
            Vector3 scale = transform.localScale;
            scale.x = direction * Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    private void ApplyMovement()
    {
        if (rb == null) return;

        // 넉백 중에는 이동 제어권을 HitEffect에 넘김
        if (hitEffect != null && hitEffect.IsKnockedBack) return;

        float effectiveSpeed = baseSpeed * slowFactor * buffFactor;
        
        // 이동 입력 속도 + 외부 환경 속도(컨베이어 등) 결합
        float targetVelX = (moveInput.x * effectiveSpeed) + externalVelocity.x;
        float targetVelY = rb.linearVelocity.y + externalVelocity.y;
        
        rb.linearVelocity = new Vector2(targetVelX, targetVelY);

        // 외부 속도는 매 프레임 초기화 (필요한 곳에서 계속 채워줌)
        externalVelocity = Vector2.zero; 
    }

    #region Status Effect APIs (Slow, Buff, Debuff)

    public void AddSlowStack(float multiplier)
    {
        slowStackCount++;
        slowFactor *= multiplier;
        slowFactor = Mathf.Clamp(slowFactor, 0.1f, 1f);
    }

    public void RemoveSlowStack(float multiplier)
    {
        slowStackCount--;
        if (slowStackCount <= 0)
        {
            slowStackCount = 0;
            slowFactor = 1f;
        }
        else
        {
            // 부동 소수점 오차 방지를 위해 Clamp
            slowFactor = Mathf.Clamp(slowFactor / multiplier, 0.1f, 1f);
        }
    }

    public void AddSpeedBuff(float multiplier)
    {
        buffStackCount++;
        buffFactor *= multiplier;
    }

    public void RemoveSpeedBuff(float multiplier)
    {
        buffStackCount--;
        if (buffStackCount <= 0)
        {
            buffStackCount = 0;
            buffFactor = 1f;
        }
        else
        {
            buffFactor = Mathf.Max(buffFactor / multiplier, 1f);
        }
    }

    public void AddJumpDebuff(float multiplier)
    {
        jumpDebuffStackCount++;
        jumpDebuffFactor *= multiplier;
        jumpDebuffFactor = Mathf.Clamp(jumpDebuffFactor, 0.1f, 1f);
    }

    public void RemoveJumpDebuff(float multiplier)
    {
        jumpDebuffStackCount--;
        if (jumpDebuffStackCount <= 0)
        {
            jumpDebuffStackCount = 0;
            jumpDebuffFactor = 1f;
        }
        else
        {
            jumpDebuffFactor = Mathf.Clamp(jumpDebuffFactor / multiplier, 0.1f, 1f);
        }
    }

    #endregion

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }
    }
}