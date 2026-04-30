using UnityEngine;

// ──────────────────────────────────────────────
//  StalkerProjectile
//  추격자 보스가 발사하는 관통형 투사체입니다.
//  지형을 무시하고 플레이어에게 데미지를 줍니다.
// ──────────────────────────────────────────────
public class StalkerProjectile : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] public float speed = 9f;         // 발사 속도
    [SerializeField] private float lifeTime = 5f;     // 유지 시간
    [SerializeField] private int damage = 1;          // 데미지

    private Vector3 moveDirection;

    private void Start()
    {
        // 5초 뒤에 자동으로 오브젝트 파괴
        Destroy(gameObject, lifeTime);

        // 생성될 때 이미 StalkerMonster 쪽에서 발사 각도를 지정해 두었으므로,
        // 무조건 자신의 정면(오른쪽) 방향으로만 나아갑니다.
        moveDirection = transform.right;
    }

    private void Update()
    {
        // 지정된 방향과 속도로 모든 것을 관통하며 이동
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    // 모든 것을 관통하기 위해 Collider의 Is Trigger가 켜져 있어야 합니다.
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어 감지 시 체력 감소
        if (other.CompareTag("Player"))
        {
            PlayerHealth hp = other.GetComponent<PlayerHealth>();
            if (hp != null)
            {
                hp.TakeDamage(damage);
                Debug.Log("<color=red>[Stalker Attack] 플레이어가 에너지파에 저격당했습니다!</color>");
            }
            
            // 관통을 위해 여기서 Destroy하지 않음
        }
    }
}
