using UnityEngine;

/// <summary>
/// 보스가 소환하는 파도 공격 오브젝트 클래스.
/// 생성 후 지정된 방향(왼쪽)으로 이동하며 플레이어와 충돌 시 데미지를 입힘.
/// </summary>
public class BossWave : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifeTime = 3f;
    
    private int damage = 1;
    private Rigidbody2D rb;

    public void Initialize(int waveDamage, float waveSpeed)
    {
        damage = waveDamage;
        speed = waveSpeed;
        
        // 공격 박스 설정
        if (!TryGetComponent(out BossAttackHitbox hitbox))
        {
            hitbox = gameObject.AddComponent<BossAttackHitbox>();
        }
        hitbox.damage = damage;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic; // 바닥을 타고 이동하기 위해 키네마틱 설정
        }

        // 수명 제한 삭제 (보스가 직접 관리함)
        // Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // 왼쪽으로 이동 (보스가 보통 오른쪽에 있으므로 Vector3.left)
        transform.Translate(Vector3.left * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어에게 닿으면 타격 로그 출력 (실제 데미지는 BossAttackHitbox가 처리)
        if (other.CompareTag("Player"))
        {
            Debug.Log("<color=red>[BossWave] 플레이어가 파도에 휩쓸렸습니다!</color>");
        }
    }

    /// <summary>
    /// 에디터 씬 창에서 파도의 충돌 판정을 시각화함. 
    /// 파도의 현재 투명도(Alpha)와 연동되어 함께 사라집니다.
    /// </summary>
    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        if (col != null)
        {
            // 파도의 현재 알파값을 가져옴 (없으면 1.0)
            float alpha = (sr != null) ? sr.color.a : 1.0f;

            // 투명도가 0이면 그리지 않거나 아주 흐리게 함
            if (alpha <= 0.05f) return;

            // 파도의 투명도에 맞춰 기즈모 색상도 조절
            Gizmos.color = new Color(1f, 0f, 0f, alpha * 0.5f);
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            
            Gizmos.color = new Color(1f, 0f, 0f, alpha);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}



