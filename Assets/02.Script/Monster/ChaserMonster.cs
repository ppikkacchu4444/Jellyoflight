using UnityEngine;

// ──────────────────────────────────────────────
//  ChaserMonster
//  범위 내에 플레이어가 들어오면 추격하고, 그렇지 않으면 주변을 순찰하는 몬스터입니다.
// ──────────────────────────────────────────────
public class ChaserMonster : MonoBehaviour
{
    [Header("이동 및 감지")]
    [SerializeField] private float moveSpeed = 3f;        // 추격 속도
    [SerializeField] private float patrolSpeed = 1.5f;     // 순찰 속도
    [SerializeField] private float detectionRange = 7f;   // 플레이어 감지 사거리
    [SerializeField] private float detectionHeight = 3f;  // 플레이어 감지 높이 제한
    [SerializeField] private float stopDistance = 0.5f;   // 플레이어와 유지할 최소 거리
    [SerializeField] private float patrolRange = 3f;      // 순찰 범위 (시작 지점 기준)

    [Header("컴포넌트 연결")]
    [SerializeField] private Animator anim;               // 애니메이터 (선택)

    private Transform playerTransform;
    private Rigidbody2D rb;
    private bool isChasing = false;

    private Vector2 startPos;
    private int patrolDir = 1;
    private float originalScaleX;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.freezeRotation = true;

        startPos = transform.position;
        originalScaleX = transform.localScale.x;
        
        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    private void Update()
    {
        // 0. 강제로 회전을 0으로 고정하여 구름 방지
        if (transform.rotation.z != 0)
        {
            transform.rotation = Quaternion.identity;
        }

        if (playerTransform == null)
        {
            Patrol();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        float verticalDistance = Mathf.Abs(transform.position.y - playerTransform.position.y);

        // 1. 감지 사거리 내에 있고, 높이 차이가 너무 크지 않을 때만 추격
        // (발판 위에 있는 플레이어를 보고 제자리 걸음 하는 것을 방지)
        if (distanceToPlayer <= detectionRange && verticalDistance <= detectionHeight)
        {
            isChasing = true;
        }
        else
        {
            isChasing = false;
        }

        // 2. 상태에 따른 이동 수행
        if (isChasing)
        {
            MoveToPlayer(distanceToPlayer);
        }
        else
        {
            Patrol();
        }
    }

    private void MoveToPlayer(float distance)
    {
        // 방향 계산
        float direction = playerTransform.position.x - transform.position.x;
        float moveDir = (direction > 0) ? 1 : -1;

        // 너무 가깝지 않을 때만 이동
        if (distance > stopDistance)
        {
            rb.linearVelocity = new Vector2(moveDir * moveSpeed, rb.linearVelocity.y);
            
            if (anim != null) anim.SetFloat("Speed", 1f);
        }
        else
        {
            StopMoving();
        }

        // 보는 방향 전환 (플립)
        Flip(moveDir);
    }

    private void Patrol()
    {
        // 시작 위치로부터의 수평 거리 체크
        float currentOffset = transform.position.x - startPos.x;

        if (Mathf.Abs(currentOffset) >= patrolRange)
        {
            // 범위를 벗어나면 방향 전환
            patrolDir = (currentOffset > 0) ? -1 : 1;
        }

        // 순찰 이동
        rb.linearVelocity = new Vector2(patrolDir * patrolSpeed, rb.linearVelocity.y);
        
        if (anim != null) anim.SetFloat("Speed", 0.5f);

        // 보는 방향 전환
        Flip(patrolDir);
    }

    private void StopMoving()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (anim != null) anim.SetFloat("Speed", 0f);
    }

    private void Flip(float direction)
    {
        if (direction != 0)
        {
            Vector3 newScale = transform.localScale;
            newScale.x = Mathf.Abs(originalScaleX) * direction;
            transform.localScale = newScale;
        }
    }

    // 에디터에서 감지 사거리와 순찰 범위를 시각적으로 확인
    private void OnDrawGizmosSelected()
    {
        // 감지 사거리 (노란색 원)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 감지 가능 높이 (파란색 선)
        Gizmos.color = Color.cyan;
        Vector3 pos = transform.position;
        Gizmos.DrawLine(pos + Vector3.up * detectionHeight, pos + Vector3.down * detectionHeight);

        // 순찰 범위 (초록색 선, 게임 시작 후 확인 가능)
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(startPos.x - patrolRange, pos.y, 0), new Vector3(startPos.x + patrolRange, pos.y, 0));
        }
    }
}
