using UnityEngine;

/// <summary>
/// 이펙트 오브젝트에 붙여서 일정 시간 뒤에 자동으로 파괴해주는 범용 스크립트입니다.
/// </summary>
public class DestroyEffect : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("몇 초 뒤에 파괴될지 설정합니다. 애니메이션 길이보다 조금 길게 잡는 것이 안전합니다.")]
    [SerializeField] private float destroyDelay = 1.0f;

    private void Start()
    {
        // 지정된 시간이 지나면 이 오브젝트를 완전히 제거합니다.
        Destroy(gameObject, destroyDelay);
    }
}