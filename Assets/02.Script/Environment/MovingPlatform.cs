using UnityEngine;

// ──────────────────────────────────────────────
//  MovingPlatform
//  지정된 방향과 거리만큼 반복해서 움직이는 발판입니다.
//  부모-자식 관계를 사용하지 않고 속도를 직접 전달하여 스케일 상속 문제를 원천 차단합니다.
// ──────────────────────────────────────────────
public class MovingPlatform : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private Vector2 moveOffset;  // 이동할 거리와 방향
    [SerializeField] private float speed = 2f;    // 이동 속도

    private Vector2 startPos;
    private Vector2 targetPos;
    private Vector2 currentVelocity;
    private Vector2 previousVelocity;
    private Vector2 lastPosition;

    private BoxCollider2D col;

    private void Start()
    {
        startPos = transform.position;
        targetPos = startPos + moveOffset;
        lastPosition = startPos;
        previousVelocity = Vector2.zero;
        col = GetComponent<BoxCollider2D>();
    }

    private void FixedUpdate()
    {
        // 1. 다음 위치 계산 (작성자 원본 로직)
        float time = Mathf.PingPong(Time.time * speed, 1f);
        Vector2 nextPos = Vector2.Lerp(startPos, targetPos, time);

        // 2. 정확한 속도 계산 (transform 오차 방지)
        previousVelocity = currentVelocity;
        currentVelocity = (nextPos - lastPosition) / Time.fixedDeltaTime;
        
        // 3. 발판 이동 적용 (물리 엔진과 충돌하지 않도록 MovePosition 최우선 사용)
        Rigidbody2D platRb = col.attachedRigidbody;
        if (platRb != null)
        {
            platRb.MovePosition(nextPos);
        }
        else
        {
            transform.position = nextPos;
        }
        
        lastPosition = nextPos;

        // 4. 발판 위쪽의 플레이어 스캔 및 속도 동기화
        if (col != null)
        {
            Vector2 boxSize = new Vector2(col.size.x * 0.9f, 0.2f);
            Vector2 boxCenter = (Vector2)transform.position + new Vector2(0, col.size.y * 0.5f + 0.1f);
            
            // BoxCastAll을 통해 모든 물체를 감지 후 Player만 필터링
            RaycastHit2D[] hits = Physics2D.BoxCastAll(boxCenter, boxSize, 0f, Vector2.up, 0.1f);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider.CompareTag("Player"))
                {
                    PlayerMove pm = hit.collider.GetComponent<PlayerMove>();
                    if (pm != null)
                    {
                        // 수평 속도(X)는 PlayerMove에 위임하여 정상적으로 누적
                        pm.externalVelocity += new Vector2(currentVelocity.x, 0f);
                    }

                    Rigidbody2D playerRb = hit.collider.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        float playerVelY = playerRb.linearVelocity.y;
                        float platformVelY = currentVelocity.y;

                        // [핵심] 발판이 정점에서 하강할 때(ex: +10 -> -10) 속도 차이가 엄청나게 벌어집니다.
                        // 이를 "플레이어가 점프했다"고 오인하지 않도록, 이전 프레임의 속도와 비교하여 관성을 보정합니다.
                        float expectedInertia = Mathf.Max(currentVelocity.y, previousVelocity.y);

                        // 플레이어가 의도적으로 점프하지 않았고(isNotJumping), 
                        // 실제로 발판의 하강 속도를 못 따라가서 공중에 살짝 떴을 때만(isFloating) 강제 동기화합니다.
                        bool isNotJumping = playerVelY <= expectedInertia + 2.0f;
                        bool isFloating = playerVelY > platformVelY + 0.1f;

                        if (isNotJumping && isFloating)
                        {
                            // 하강하는 동안 매 프레임 찍어 누르는 것이 아니라, 관성 때문에 떴을 때만 발판 속도로 맞춰줍니다.
                            // 이렇게 하면 물리 엔진의 반발력(통통 튐)이 발생하지 않습니다.
                            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, platformVelY);
                        }
                    }
                    break;
                }
            }
        }
    }
}
