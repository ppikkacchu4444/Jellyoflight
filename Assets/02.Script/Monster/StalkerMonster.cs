using UnityEngine;
using System.Collections;

// ──────────────────────────────────────────────
//  StalkerMonster
//  화면 왼쪽에서부터 쫓아오는 거대 추격자 보스입니다.
//  플레이어와 닿으면 즉시 패배 처리하며, 무적 상태입니다.
// ──────────────────────────────────────────────
public class StalkerMonster : MonoBehaviour
{
    [Header("기본 이동 설정")]
    [SerializeField] private float baseSpeed = 1.3f;    // 평소 전진 속도
    [SerializeField] private float rushSpeed = 4.8f;    // 갑자기 빨라지는 속도
    [SerializeField] private float rushDuration = 2.5f; // 돌진 지속 시간
    [SerializeField] private float rushInterval = 9f;   // 돌진 주기 (간격)

    [Header("원거리 공격 설정")]
    [SerializeField] private GameObject projectilePrefab; // 발사할 프리팹
    [SerializeField] private Transform shootPoint;          // 발사 위치 (입 등)
    [SerializeField] private float shootInterval = 4f;      // 발사 주기
    [SerializeField] private int projectileCount = 3;       // 발사할 투사체 개수
    [SerializeField] private float spreadAngle = 45f;       // 부채꼴로 퍼지는 총 각도
    
    [SerializeField] private Color normalColor = Color.white; // 평상시 색상
    [SerializeField] private Color rushColor = Color.red;   // 돌진 시 색상

    [Header("사운드 설정")]
    [SerializeField] private AudioClip rushSound;       // 돌진 시 재생되는 효과음
    [Range(0f, 1f)]
    [SerializeField] private float rushVolume = 0.5f;   // 돌진 사운드 볼륨

    private float currentSpeed;
    private bool isStopped = false;
    private bool isRushing = false;                     // 현재 돌진 중인지 체크용
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Coroutine haltCoroutine;
    private AudioSource audioSource;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        currentSpeed = baseSpeed;
        
        // 초기 색상 설정
        if (spriteRenderer != null) spriteRenderer.color = normalColor;
        
        // 반복 루틴들 시작
        StartCoroutine(RushRoutine());
        StartCoroutine(ShootRoutine());
    }

    private void Update()
    {
        // 애니메이터 상태 업데이트
        if (anim != null)
        {
            anim.SetBool("isStopped", isStopped);
            
            // 기본 속도(baseSpeed)일 때 애니메이션 재생 속도를 1배속으로 보정
            float speedMultiplier = 0f;
            if (!isStopped && baseSpeed > 0)
            {
                speedMultiplier = currentSpeed / baseSpeed;
            }
            anim.SetFloat("Speed", speedMultiplier);
        }

        if (isStopped) return;

        // ── [핵심] 지형지물을 완전히 무시하고 전진 ────────────────
        transform.position += Vector3.right * currentSpeed * Time.deltaTime;

        UpdateRushSound();
    }

    private void UpdateRushSound()
    {
        if (audioSource == null || rushSound == null || !isRushing) return;

        bool isVisible = IsVisibleToCamera();
        
        // 화면에 보이면 재생, 안 보이면 정지 (돌진 중일 때만)
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
        // z > 0 (카메라 앞), x/y -0.3~1.3 (거대 몬스터이므로 여유 범위를 넓게 설정)
        return viewportPos.z > 0 && viewportPos.x >= -0.3f && viewportPos.x <= 1.3f && viewportPos.y >= -0.3f && viewportPos.y <= 1.3f;
    }

    // ── 주기적으로 투사체를 발사하는 루틴 ──────────────
    private IEnumerator ShootRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(shootInterval);

            // 멈춰 있지 않을 때만 발사 가능
            if (!isStopped && projectilePrefab != null && shootPoint != null)
            {
                // 공격 애니메이션 트리거
                if (anim != null) anim.SetTrigger("Attack");

                // 먼저 플레이어가 있는 곳의 각도를 계산하여 뼈대(기준)로 삼음
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                Vector3 dirToPlayer = (player != null) ? (player.transform.position - shootPoint.position).normalized : Vector3.right;
                float baseAngle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
                Quaternion baseRotation = Quaternion.Euler(0, 0, baseAngle);

                if (projectileCount <= 1)
                {
                    Instantiate(projectilePrefab, shootPoint.position, baseRotation);
                }
                else
                {
                    // 부채꼴 여러 개 발사
                    float angleStep = spreadAngle / (projectileCount - 1);
                    float startAngle = -spreadAngle / 2f;

                    for (int i = 0; i < projectileCount; i++)
                    {
                        float currentAngle = startAngle + (angleStep * i);
                        // 플레이어를 바라보는 기준 각도를 바탕으로 위/아래로 spread 회전 적용
                        Quaternion spreadRot = baseRotation * Quaternion.Euler(0, 0, currentAngle);
                        Instantiate(projectilePrefab, shootPoint.position, spreadRot);
                    }
                }
                
                Debug.Log($"<color=orange>[Stalker] 부채꼴 에너지파 발사! (총 {projectileCount}발)</color>");
            }
        }
    }

    // ── 주기적으로 빠르게 전진하는 루틴 ──────────────
    private IEnumerator RushRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(rushInterval);
            
            if (!isStopped)
            {
                Debug.Log("<color=red><b>[Stalker]</b> 돌진 시작! 빠르게 따라붙습니다!</color>");
                currentSpeed = rushSpeed;
                isRushing = true;

                // 돌진 효과음 설정
                if (rushSound != null)
                {
                    if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.clip = rushSound;
                    audioSource.loop = true;
                    audioSource.volume = rushVolume;
                    
                    // 화면에 보일 때만 즉시 재생 시작
                    if (IsVisibleToCamera()) audioSource.Play();
                }
                
                // 돌진 색상 적용
                if (spriteRenderer != null) spriteRenderer.color = rushColor;

                yield return new WaitForSeconds(rushDuration);
                
                currentSpeed = baseSpeed;
                isRushing = false;

                // 돌진 효과음 정지
                if (audioSource != null) audioSource.Stop();

                // 원래 색상으로 복구
                if (spriteRenderer != null) spriteRenderer.color = normalColor;
                
                Debug.Log("<color=white>[Stalker] 돌진 종료. 다시 천천히 전진합니다.</color>");
            }
        }
    }

    // ── 아군 희생 등을 통해 잠시 멈추는 기능 ─────────
    public void Halt(float duration)
    {
        if (haltCoroutine != null) StopCoroutine(haltCoroutine);
        haltCoroutine = StartCoroutine(HaltRoutine(duration));
    }

    private IEnumerator HaltRoutine(float duration)
    {
        isStopped = true;
        Debug.Log($"<color=cyan><b>[Stalker]</b> 저지당했습니다! {duration}초간 정지합니다.</color>");
        
        yield return new WaitForSeconds(duration);
        
        isStopped = false;
    }

    // ── 플레이어 접촉 시 패배 처리 ──────────────────
    // 지형을 뚫고 지나가기 위해 Collider의 Is Trigger가 켜져 있어야 합니다.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("<color=red><b>[Game Over]</b> 추격자에게 따라잡혔습니다!</color>");

            // PlayerHealth.TakeDamage를 통해 정상적인 사망 흐름 (Die → FailStage)
            PlayerHealth hp = other.GetComponentInParent<PlayerHealth>()
                           ?? other.GetComponent<PlayerHealth>();
            if (hp != null)
            {
                hp.InstantKill(); // 무적 시간/대쉬 무시하고 즉사
            }
            else
            {
                // PlayerHealth가 없으면 StageManager로 직접 fallback
                if (StageManager.Instance != null)
                    StageManager.Instance.FailStage();
            }
        }
    }
}
