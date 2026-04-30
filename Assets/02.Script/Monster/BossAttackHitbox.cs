using UnityEngine;

/// <summary>
/// 보스 및 소환수의 공격 판정을 처리하는 클래스.
/// 트리거 혹은 충돌 시 플레이어의 체력을 깎는 역할을 수행함.
/// </summary>
public class BossAttackHitbox : MonoBehaviour
{
    [Header("공격 설정")]
    [Tooltip("플레이어에게 입힐 데미지 수치.")]
    [SerializeField] private int _damage = 1;

    /// <summary>
    /// 외부에서 데미지 수치를 읽거나 수정할 수 있는 프로퍼티.
    /// </summary>
    public int damage 
    { 
        get => _damage; 
        set => _damage = value; 
    }

    /// <summary>
    /// 대상 오브젝트가 플레이어인지 확인하고 데미지를 입힘.
    /// </summary>
    /// <param name="otherObj">충돌한 게임 오브젝트</param>
    private void DoDamage(GameObject otherObj)
    {
        if (otherObj == null) return;

        if (otherObj.CompareTag("Player") || otherObj.layer == LayerMask.NameToLayer("Player"))
        {
            if (otherObj.TryGetComponent(out PlayerHealth hp) || 
                (otherObj.transform.parent != null && otherObj.transform.parent.TryGetComponent(out hp)))
            {
                // 보스의 위치를 피격 방향 계산을 위해 전달 (없으면 자신 위치)
                Vector3 sourcePos = transform.parent != null ? transform.parent.position : transform.position;
                hp.TakeDamage(_damage, sourcePos);
                
                Debug.Log($"<color=red>[Boss Attack] 플레이어 타격! 데미지: {_damage}</color>");
            }
            else
            {
                Debug.LogWarning("[Boss Attack] 플레이어로 식별되었으나 PlayerHealth 컴포넌트를 찾을 수 없습니다.");
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null) DoDamage(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision != null) DoDamage(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider != null) DoDamage(collider.gameObject);
    }

    private void OnTriggerStay2D(Collider2D collider)
    {
        if (collider != null) DoDamage(collider.gameObject);
    }
}


