using UnityEngine;

/// <summary>
/// 보스가 소환하는 낙석 기능을 관리하는 클래스.
/// </summary>
public class BossStone : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private float fallSpeed = 9f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private int _damage = 1;

    /// <summary>
    /// 외부에서 데미지를 설정할 수 있는 프로퍼티.
    /// </summary>
    public int damage
    {
        get => _damage;
        set => _damage = value;
    }

    public AudioClip hitSound { get; set; } // 보스 본체에서 전달받을 사운드

    private Animator anim;
    private bool isHit = false;

    private void Start()
    {
        anim = GetComponentInChildren<Animator>();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (isHit) return;

        // 바닥을 향해 지속적으로 하강함
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 충돌 대상에 따른 데미지 적용 및 오브젝트 파괴를 처리함.
    /// </summary>
    private void HandleImpact(GameObject other)
    {
        if (other == null || isHit) return;

        // 보스 본체나 일반 몬스터는 무시
        if (other.GetComponentInParent<BossMonster>() != null || other.CompareTag("Monster")) 
            return;

        // 플레이어 충돌 처리
        if (other.CompareTag("Player") || other.layer == LayerMask.NameToLayer("Player"))
        {
            if (other.TryGetComponent(out PlayerHealth hp) || 
                (other.transform.parent != null && other.transform.parent.TryGetComponent(out hp)))
            {
                // 피격 위치 전달 추가
                hp.TakeDamage(_damage, transform.position);
                Debug.Log($"<color=red>[Boss Stone] 낙석 타격! 데미지: {_damage}</color>");
            }
            TriggerHit();
            return;
        }

        // 특정 지형(Ground, Platform) 충돌 시 파괴
        int layer = other.layer;
        if (layer == LayerMask.NameToLayer("Ground") || layer == LayerMask.NameToLayer("Platform"))
        {
            TriggerHit();
        }
    }

    private void TriggerHit()
    {
        if (isHit) return;
        isHit = true;

        // 바닥 충돌 효과음 재생
        if (hitSound != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(hitSound);

        // 즉시 이동 정지 및 판정 제거
        fallSpeed = 0;
        if (TryGetComponent(out Collider2D col)) col.enabled = false;

        if (anim != null)
        {
            anim.SetTrigger("Hit");
            Debug.Log("<color=yellow>[Boss Stone] 파괴 애니메이션 트리거 발동!</color>");
            // 즉시 사라지는 느낌을 위해 0.2초로 조정
            Destroy(gameObject, 0.2f);
        }
        else
        {
            Debug.LogWarning("[Boss Stone] Animator 컴포넌트를 찾을 수 없습니다.");
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null) HandleImpact(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null) HandleImpact(collision.gameObject);
    }
}

