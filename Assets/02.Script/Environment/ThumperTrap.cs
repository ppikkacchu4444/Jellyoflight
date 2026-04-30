using UnityEngine;
using System.Collections;

// ──────────────────────────────────────────────
//  ThumperTrap
//  천장에서 바닥으로 급강하하여 플레이어를 압박하는 트랩입니다.
//  바닥 충돌 시 화면 진동과 먼지 이펙트를 발생시킵니다.
// ──────────────────────────────────────────────
public class ThumperTrap : MonoBehaviour
{
    [Header("거리 및 속도 설정")]
    [SerializeField] private float slamDistance = 5f;    // 내려갈 거리
    [SerializeField] private float slamSpeed = 18f;      // 내려가는 속도 (빨라야 함)
    [SerializeField] private float returnSpeed = 3f;      // 돌아오는 속도 (천천히)

    [Header("시간 설정")]
    [SerializeField] private float idleDuration = 2f;    // 천장에서 대기 시간
    [SerializeField] private float slamWaitTime = 0.6f;  // 바닥에서 누르고 있는 시간

    [Header("효과 설정")]
    [SerializeField] private float shakeIntensity = 0.2f; // 진동 세기
    [SerializeField] private float shakeDuration = 0.15f; // 진동 시간

    [SerializeField] private GameObject dustPrefab;       // 바닥 먼지 파티클 프리팹
    [SerializeField] private Transform effectPoint;        // 이펙트 생성 위치 (바닥면)
    [SerializeField] private AudioClip slamSound;         // 바닥 충돌 시 효과음

    private Vector3 startPos;
    private Vector3 targetPos;
    private Camera mainCam;
    private bool isSlamming = false; // [추가] 하강 중인지 체크하는 플래그

    private void Start()
    {
        // 시작 위치와 목표 위치(바닥) 계산
        startPos = transform.position;
        targetPos = startPos + Vector3.down * slamDistance;
        
        mainCam = Camera.main;

        // 무한 반복 루틴 시작
        StartCoroutine(ThumperRoutine());
    }

    private IEnumerator ThumperRoutine()
    {
        while (true)
        {
            // 1. 천장에서 다음 공격까지 대기
            yield return new WaitForSeconds(idleDuration);

            // 2. 쿵! 하고 빠른 속도로 하강 시작
            isSlamming = true; 
            while (Vector3.Distance(transform.position, targetPos) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, slamSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetPos; // 위치 보정
            isSlamming = false; // [추가] 바닥에 닿으면 하강 상태 종료

            // ──────────────────────────────────────────────────────────
            // [연출] 바닥에 닿는 순간 화면 진동 및 먼지 이펙트 발생
            // ──────────────────────────────────────────────────────────
            if (mainCam != null) StartCoroutine(CameraShake());
            
            if (dustPrefab != null && effectPoint != null)
            {
                Instantiate(dustPrefab, effectPoint.position, Quaternion.identity);
            }

            // 바닥 충돌 효과음 재생 (화면에 보일 때만)
            if (slamSound != null && SoundManager.Instance != null && IsVisibleToCamera())
                SoundManager.Instance.PlaySFX(slamSound);
            
            Debug.Log("<color=red><b>[Thumper]</b> 쿵! 바닥을 찍었습니다.</color>");
            // ──────────────────────────────────────────────────────────

            // 3. 바닥에서 잠시 머무름 (플레이어를 압박!)
            yield return new WaitForSeconds(slamWaitTime);

            // 4. 천천히 원래 위치로 복귀
            while (Vector3.Distance(transform.position, startPos) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, startPos, returnSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = startPos; // 위치 보정
        }
    }

    private bool IsVisibleToCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Vector3 viewportPos = cam.WorldToViewportPoint(transform.position);
        return viewportPos.z > 0 && viewportPos.x >= -0.2f && viewportPos.x <= 1.2f && viewportPos.y >= -0.2f && viewportPos.y <= 1.2f;
    }

    // 카메라를 짧게 진동시키는 코루틴
    private IEnumerator CameraShake()
    {
        Vector3 originalPos = mainCam.transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            // 무작위 방향으로 흔듦
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;

            mainCam.transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 진동 종료 후 위치 복구
        mainCam.transform.localPosition = originalPos;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckSquish(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckSquish(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckSquish(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        CheckSquish(other);
    }

    private void CheckSquish(Collider2D other)
    {
        // 플레이어 접촉 시
        if (other.CompareTag("Player"))
        {
            // [핵심] 하강 중일 때 부딪히면 '찌부(압착)' 판정!
            if (isSlamming)
            {
                // 옆에 스치기만 해도 죽는 억까 방지: 플레이어의 위치가 트랩 바닥면보다 명확히 아래쪽에 있을 때만 압살 판정
                Collider2D myCol = GetComponent<Collider2D>();
                float trapBottom = myCol != null ? myCol.bounds.min.y : transform.position.y;

                // 트랩 바닥보다 약간(0.3f) 위쪽 여유까지를 허용해서 아래에 깔렸을 때만 죽게 설정
                if (other.transform.position.y < trapBottom + 0.3f)
                {
                    Debug.Log("<color=red><b>[Thumper]</b> 플레이어가 깔렸습니다!</color>");

                    // 바닥 뚫림 방지: 플레이어 물리 속도 즉시 제거 및 위치 고정
                    Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        playerRb.linearVelocity = Vector2.zero;
                        playerRb.bodyType = RigidbodyType2D.Kinematic;
                    }

                    // Die 애니메이션이 재생되도록 PlayerHealth를 통해 사망 처리
                    PlayerHealth hp = other.GetComponent<PlayerHealth>();
                    if (hp != null)
                    {
                        hp.InstantKill();
                    }
                    else if (StageManager.Instance != null)
                    {
                        StageManager.Instance.FailStage();
                    }
                }
            }
        }
    }
}
