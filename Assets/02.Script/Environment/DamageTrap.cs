using UnityEngine;

public class DamageTrap : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private Vector2 moveOffset = new Vector2(5, 0);
    [SerializeField] private float speed = 3f;

    [Header("전투 설정")]
    [SerializeField] private int damage = 1;

    [Header("사운드 설정")]
    [SerializeField] private AudioClip loopSound;
    [Range(0f, 1f)]
    [SerializeField] private float loopVolume = 0.5f;

    private Vector2 startPos;
    private Vector2 targetPos;
    private AudioSource audioSource;

    private void Start()
    {
        startPos = transform.position;
        targetPos = startPos + moveOffset;

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

    private void Update()
    {
        float time = Mathf.PingPong(Time.time * speed, 1f);
        transform.position = Vector2.Lerp(startPos, targetPos, time);

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
        // 약간의 여유(0.2)를 주어 화면에 들어오기 직전부터 소리가 나게 함
        return viewportPos.z > 0 && viewportPos.x >= -0.2f && viewportPos.x <= 1.2f && viewportPos.y >= -0.2f && viewportPos.y <= 1.2f;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleTrigger(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        HandleTrigger(collision);
    }

    private void HandleTrigger(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerHealth hp = collision.GetComponentInParent<PlayerHealth>();
            if (hp != null)
            {
                int previousHp = hp.CurrentHp;
                hp.TakeDamage(damage);

                // 체력이 실제로 감소했을 때(무적 상태 등으로 무시되지 않았을 때)만 피격 방향 전송
                if (hp.CurrentHp < previousHp)
                {
                    PlayerHitEffect hitEffect = collision.GetComponentInParent<PlayerHitEffect>();
                    if (hitEffect != null) hitEffect.PlayHitEffect(transform.position);
                    
                    Debug.Log($"<color=orange>[DamageTrap] {collision.name} 오버랩! 대미지 전송됨.</color>");
                }
            }
        }
    }
}
