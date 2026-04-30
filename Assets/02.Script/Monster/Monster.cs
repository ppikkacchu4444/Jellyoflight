using UnityEngine;

public class Monster : MonoBehaviour
{
    [Header("--- 이동 설정 ---")]
    [SerializeField] private float speed = 2f;         // 이동 속도
    [SerializeField] private float patrolDistance = 3f; // 시작 지점으로부터 이동할 거리
    
    private Vector2 startPosition;                     // 초기 생성 위치
    private int direction = 1;                         // 이동 방향 (1: 오른쪽, -1: 왼쪽)
    private float originalScaleX;                      // 인스펙터에 설정한 원래 X 크기 저장용

    [Header("--- 공격 설정 ---")]
    [SerializeField] private bool canDealContactDamage = true; // 플레이어 접촉 데미지 사용 여부
    [SerializeField] private int damage = 1;

    /// <summary>
    /// 외부에서 데미지 발생 여부를 제어할 수 있는 프로퍼티.
    /// </summary>
    public bool CanDealContactDamage
    {
        get => canDealContactDamage;
        set => canDealContactDamage = value;
    }

    private void Start()
    {
        startPosition = transform.position;
        originalScaleX = transform.localScale.x;

        if (TryGetComponent(out Rigidbody2D rb))
        {
            rb.freezeRotation = true;
        }
    }

    private void Update()
    {
        // 강제로 회전을 0으로 고정하여 수직 유지
        if (transform.rotation.z != 0)
        {
            transform.rotation = Quaternion.identity;
        }

        float currentDistance = Vector2.Distance(startPosition, transform.position);

        if (currentDistance >= patrolDistance)
        {
            direction *= -1;
            Vector3 newScale = transform.localScale;
            newScale.x = originalScaleX * direction;
            transform.localScale = newScale;

            float offset = (currentDistance - patrolDistance) + 0.01f;
            transform.Translate(Vector3.right * direction * offset, Space.World);
        }

        transform.Translate(Vector3.right * direction * speed * Time.deltaTime, Space.World);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!canDealContactDamage) return;

        if (collision.gameObject.TryGetComponent(out PlayerHealth player))
        {
            player.TakeDamage(damage);
        }
    }
}