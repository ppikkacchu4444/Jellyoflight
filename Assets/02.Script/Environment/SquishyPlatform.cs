using UnityEngine;
using System.Collections;

/// <summary>
/// 밟으면 말랑하게 눌렸다가 복구되는 발판 연출 스크립트.
/// 물리 안정성을 위해 실제 스프라이트가 있는 자식 오브젝트의 스케일만 조절합니다.
/// </summary>
public class SquishyPlatform : MonoBehaviour
{
    [Header("말랑함 설정")]
    [Range(0.1f, 1.0f)]
    [SerializeField] private float squishScaleY = 0.8f;   // 얼마나 눌릴지 (1.0 기준)
    [Range(1.0f, 2.0f)]
    [SerializeField] private float stretchScaleX = 1.15f; // 눌릴 때 옆으로 퍼지는 정도
    [SerializeField] private float squishDuration = 0.08f; // 눌리는 속도 (빠를수록 탄성 있음)
    [SerializeField] private float restoreDuration = 0.25f;// 복구되는 속도 (천천히 젤리처럼 복구)

    [Header("구조 설정")]
    [SerializeField] private Transform visualTransform;  // 실제 SpriteRenderer가 있는 자식 오브젝트 추천

    private Vector3 originalScale;
    private bool isSquishing = false;

    private void Start()
    {
        // visualTransform이 지정되지 않았다면 현재 오브젝트를 사용
        if (visualTransform == null) visualTransform = transform;
        originalScale = visualTransform.localScale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 이미 눌리고 있는 중이라면 추가 충돌 판정은 무시하여 들들거림 방지
        if (isSquishing) return;

        // 플레이어가 위쪽에서 밟았을 때만 발동
        if (collision.gameObject.CompareTag("Player"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f)
                {
                    StartSquish();
                    break;
                }
            }
        }
    }

    public void StartSquish()
    {
        // 중복 실행을 막아 물리 연산 오류와 시각적 떨림을 방지
        if (!isSquishing)
        {
            StartCoroutine(SquishRoutine());
        }
    }

    private IEnumerator SquishRoutine()
    {
        isSquishing = true;

        Vector3 targetScale = new Vector3(originalScale.x * stretchScaleX, originalScale.y * squishScaleY, originalScale.z);

        // 1. 눌리기 (Squish)
        float elapsed = 0;
        while (elapsed < squishDuration)
        {
            elapsed += Time.deltaTime;
            visualTransform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / squishDuration);
            yield return null;
        }

        // 2. 원래대로 복구 (Restore)
        // 약간의 탄성을 위해 복구 시 Lerp를 부드럽게 적용
        elapsed = 0;
        while (elapsed < restoreDuration)
        {
            elapsed += Time.deltaTime;
            // 복구 효과를 위해 점진적인 보간
            float t = elapsed / restoreDuration;
            // 튕기는 느낌을 더하고 싶다면 t 대신 커스텀 커브를 쓸 수 있으나 Lerp로도 충분히 말랑함
            visualTransform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        visualTransform.localScale = originalScale;
        isSquishing = false;
    }
}
