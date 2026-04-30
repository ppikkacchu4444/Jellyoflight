using UnityEngine;

public class PendulumTrap : MonoBehaviour
{
    [Header("진자(스윙) 설정")]
    [SerializeField] [Tooltip("실제로 회전할 중심축 오브젝트. (비워두면 이 스크립트가 붙은 오브젝트가 회전합니다)")] 
    private Transform pivotTransform;

    [SerializeField] [Tooltip("스윙 속도")] 
    private float speed = 2f;
    
    [SerializeField] [Tooltip("최대 스윙 각도(예: 90이면 좌우 90도씩 총 180도 스윙)")] 
    private float maxAngle = 90f;
    
    [SerializeField] [Tooltip("다른 진자와 타이밍을 다르게 하고 싶을 때 조절(0~10 등)")] 
    private float offset = 0f;

    [Header("데미지 설정")]
    [SerializeField] [Tooltip("플레이어에게 줄 데미지")] 
    private int damage = 1;
    
    [SerializeField] [Tooltip("넉백 세기 배율 (1=기본, 2=두배로 강하게 날아감)")]
    private float knockbackMultiplier = 2f;

    [Header("사운드 설정")]
    [SerializeField] private AudioClip loopSound;
    [Range(0f, 1f)]
    [SerializeField] private float loopVolume = 0.4f;

    private AudioSource audioSource;

    private void Start()
    {
        // 루프용 오디오 소스 초기화
        if (loopSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = loopSound;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = loopVolume;
        }
    }

    void Update()
    {
        // Sin 함수를 이용해 -maxAngle ~ +maxAngle 범위를 왕복합니다.
        // Time.time이 계속 증가하므로 부드러운 흔들림(스윙)이 연출됩니다.
        float angle = Mathf.Sin((Time.time + offset) * speed) * maxAngle;
        
        // Z축을 기준으로 회전하여 바이킹처럼 스윙
        if (pivotTransform != null)
        {
            pivotTransform.localRotation = Quaternion.Euler(0, 0, angle);
        }
        else
        {
            transform.localRotation = Quaternion.Euler(0, 0, angle);
        }

        UpdateLoopSound();
    }

    private void UpdateLoopSound()
    {
        if (audioSource == null || loopSound == null) return;

        bool isVisible = IsVisibleToCamera();
        
        if (isVisible && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
        else if (!isVisible && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private bool IsVisibleToCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Vector3 viewportPos = cam.WorldToViewportPoint(transform.position);
        // z > 0 (카메라 앞), x/y -0.2~1.2 (약간의 여유 범위)
        return viewportPos.z > 0 && viewportPos.x >= -0.2f && viewportPos.x <= 1.2f && viewportPos.y >= -0.2f && viewportPos.y <= 1.2f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HitPlayer(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HitPlayer(collision.gameObject);
    }

    private void HitPlayer(GameObject target)
    {
        PlayerHealth player = target.GetComponent<PlayerHealth>();
        if (player != null)
        {
            // 부딪힌 플레이어에게 데미지를 주고, 
            // 현재 트랩의 위치(transform.position)를 넘겨주어 반대 방향으로 튕겨나가게 합니다.
            // 트리거 충돌 시 구슬 위치를 더 정확히 하려면 
            // collider에서 충돌점을 계산할 수도 있지만, 기준점 위치만 줘도 좌/우 넉백이 정상 작동합니다.
            player.TakeDamage(damage, transform.position, knockbackMultiplier);
        }
    }

    // 에디터의 Scene 창에서 피봇(회전 중심점)을 시각적으로 명확하게 볼 수 있게 해줍니다.
    private void OnDrawGizmos()
    {
        Transform targetPivot = pivotTransform != null ? pivotTransform : transform;
        
        // 피봇 위치에 노란색 공 그리기
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(targetPivot.position, 0.2f);

        // 피봇에서 아래 방향으로 살짝 내려오는 기준선 그리기
        Gizmos.color = Color.red;
        Gizmos.DrawLine(targetPivot.position, targetPivot.position + (targetPivot.up * -1.5f));
    }
}
