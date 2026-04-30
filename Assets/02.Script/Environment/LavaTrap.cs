using UnityEngine;
using System.Collections;

// ──────────────────────────────────────────────
//  LavaTrap
//  바닥에서 주기적으로 솟구쳤다가 가라앉는 용암 트랩입니다.
// ──────────────────────────────────────────────
public class LavaTrap : MonoBehaviour
{
    [Header("움직임 설정")]
    [SerializeField] private float moveDistance = 3.5f; // 솟구치는 최대 높이
    [SerializeField] private float moveSpeed = 8.0f;    // 솟구칠 때의 속도
    [SerializeField] private float gravity = 25.0f;     // 가라앉을 때 적용될 중력 가속도
    [SerializeField] private float waitInterval = 2.5f; // 다음 점프까지 대기 시간

    [Header("사운드 설정")]
    [SerializeField] private AudioClip eruptSound;      // 솟구칠 때 재생되는 효과음

    private Vector3 startPos;

    private IEnumerator Start()
    {
        // 시작 위치 저장
        startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * moveDistance;

        while (true)
        {
            // 솟구치기 시작 효과음 (화면에 보일 때만)
            if (eruptSound != null && SoundManager.Instance != null && IsVisibleToCamera())
                SoundManager.Instance.PlaySFX(eruptSound);

            // 1. 솟구치기 (위로 빠르게 뿜어져 나옴)
            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }

            // 최상단에서 아주 잠깐 정지 (더 자연스러운 느낌 추구 시)
            yield return new WaitForSeconds(0.1f);

            // 2. 가라앉기 (중력을 받아 점점 빠르게 떨어짐)
            float currentFallSpeed = 0f;
            while (transform.position.y > startPos.y)
            {
                currentFallSpeed += gravity * Time.deltaTime; // 중력 가속도 누적
                transform.position += Vector3.down * currentFallSpeed * Time.deltaTime;

                // 바닥을 뚫고 내려가면 원래 위치로 보정
                if (transform.position.y <= startPos.y)
                {
                    transform.position = startPos;
                    break;
                }
                yield return null;
            }

            // 3. 바닥에서 대기 시간
            yield return new WaitForSeconds(waitInterval);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어 접촉 시 데미지 -1
        if (other.CompareTag("Player"))
        {
            PlayerHealth hp = other.GetComponent<PlayerHealth>();
            if (hp != null)
            {
                hp.TakeDamage(1);
                Debug.Log("<color=red>[Lava] 플레이어가 용암에 닿아 데미지를 입었습니다!</color>");
            }
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
}
