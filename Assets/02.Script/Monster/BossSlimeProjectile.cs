using UnityEngine;

/// <summary>
/// 보스가 발사하는 점액 투사체 기능을 관리하는 클래스.
/// </summary>
public class BossSlimeProjectile : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private GameObject puddlePrefab;
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private float autoDestroyTime = 10f;
    [SerializeField] private int _damage = 1;

    /// <summary>
    /// 외부에서 데미지 수치를 설정할 수 있는 프로퍼티.
    /// </summary>
    public int damage
    {
        get => _damage;
        set => _damage = value;
    }

    private void Start()
    {
        Destroy(gameObject, autoDestroyTime);
    }

    /// <summary>
    /// 충돌 대상에 따른 데미지 적용 및 웅덩이 생성 로직을 처리함.
    /// </summary>
    private void HandleImpact(GameObject otherObj)
    {
        if (otherObj == null) return;

        // 보스 본체나 투사체끼리의 충돌은 무시
        if (otherObj.GetComponentInParent<BossMonster>() != null || 
            otherObj.TryGetComponent(out BossMonster _) ||
            otherObj.TryGetComponent(out BossSlimeProjectile _) ||
            otherObj.TryGetComponent(out BossSlimePuddle _)) 
            return;

        // 플레이어 피격 처리
        if (otherObj.CompareTag("Player") || otherObj.layer == LayerMask.NameToLayer("Player"))
        {
            if (otherObj.TryGetComponent(out PlayerHealth hp) || 
                (otherObj.transform.parent != null && otherObj.transform.parent.TryGetComponent(out hp)))
            {
                hp.TakeDamage(_damage);
                Debug.Log($"<color=green>[Boss Slime] 점액 투사체 타격! 데미지: {_damage}</color>");
            }
            return;
        }

        // 지형 충돌 시 웅덩이 생성
        bool isMonster = otherObj.CompareTag("Monster") || 
                         otherObj.layer == LayerMask.NameToLayer("Monster") ||
                         otherObj.GetComponentInParent<BossMonster>() != null;

        if (!isMonster)
        {
            SpawnPuddle();
        }
    }

    private void SpawnPuddle()
    {
        // 터지는 애니메이션(이펙트) 생성
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
        }

        if (puddlePrefab != null)
        {
            Instantiate(puddlePrefab, transform.position, Quaternion.identity);
        }
        
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleImpact(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleImpact(other.gameObject);
    }
}

