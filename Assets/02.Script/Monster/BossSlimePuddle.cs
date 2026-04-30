using UnityEngine;
using System.Collections;

/// <summary>
/// 보스의 점액 사격이 바닥에 닿았을 때 생성되는 장판 오브젝트 스크립트.
/// </summary>
public class BossSlimePuddle : MonoBehaviour
{
    [Header("점액 장판 설정")]
    [SerializeField] private float slowMultiplier = 0.5f; // 플레이어 이동 속도 배율
    [SerializeField] private float duration = 5f;        // 장판 지속 시간

    [Header("사운드 설정")]
    [SerializeField] private AudioClip splashSound;     // 생성 시 재생되는 효과음

    private SpriteRenderer spriteRenderer;
    private PlayerMove currentPlayer; // 현재 장판 위에 있는 플레이어 참조

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 생성 시 효과음 재생 (화면에 보일 때만)
        if (splashSound != null && SoundManager.Instance != null && IsVisibleToCamera())
            SoundManager.Instance.PlaySFX(splashSound);

        StartCoroutine(LifeRoutine());
    }

    private IEnumerator LifeRoutine()
    {
        float fadeOutTime = 1f;
        yield return new WaitForSeconds(Mathf.Max(0, duration - fadeOutTime));

        if (spriteRenderer != null)
        {
            float elapsed = 0;
            Color startColor = spriteRenderer.color;
            while (elapsed < fadeOutTime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutTime);
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
        }

        // 장판 제거 전, 플레이어가 아직 위에 있다면 슬로우 해제
        if (currentPlayer != null)
        {
            currentPlayer.RemoveSlowStack(slowMultiplier);
            currentPlayer = null;
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMove player = other.GetComponent<PlayerMove>();
            if (player != null)
            {
                currentPlayer = player;
                player.AddSlowStack(slowMultiplier);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMove player = other.GetComponent<PlayerMove>();
            if (player != null && currentPlayer == player)
            {
                player.RemoveSlowStack(slowMultiplier);
                currentPlayer = null;
            }
        }
    }

    private bool IsVisibleToCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Vector3 viewportPos = cam.WorldToViewportPoint(transform.position);
        return viewportPos.z > 0 && viewportPos.x >= -0.2f && viewportPos.x <= 1.2f && viewportPos.y >= -0.2f && viewportPos.y <= 1.2f;
    }
}
