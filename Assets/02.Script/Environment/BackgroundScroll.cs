using UnityEngine;

// ──────────────────────────────────────────────
//  BackgroundScroll
//  카메라 위치 기반으로 두 장의 배경을 반복시켜,
//  스크롤하거나 플레이어가 멀리 이동해도 빈틈이 생기지 않도록 수정했습니다.
// ──────────────────────────────────────────────
public class BackgroundScroll : MonoBehaviour
{
    [Header("스크롤 설정")]
    [SerializeField] private float scrollSpeed = 0.3f;
    [SerializeField] private bool autoCreateSecondary = true;

    [Header("두 번째 배경 (비워두면 자동 생성)")]
    [SerializeField] private SpriteRenderer secondBackground;

    private SpriteRenderer primaryRenderer;
    private float spriteWidth;
    private Camera mainCam;

    private void Start()
    {
        primaryRenderer = GetComponent<SpriteRenderer>();
        mainCam = Camera.main;

        if (primaryRenderer == null) return;

        spriteWidth = primaryRenderer.bounds.size.x;

        // 두 번째 배경이 비어있으면 자동 생성
        if (secondBackground == null && autoCreateSecondary)
        {
            GameObject copy = new GameObject(gameObject.name + "_Copy");
            copy.transform.SetParent(transform.parent);
            copy.transform.localScale = transform.localScale;

            SpriteRenderer copyRenderer = copy.AddComponent<SpriteRenderer>();
            copyRenderer.sprite = primaryRenderer.sprite;
            copyRenderer.sortingLayerName = primaryRenderer.sortingLayerName;
            copyRenderer.sortingOrder = primaryRenderer.sortingOrder;
            copyRenderer.color = primaryRenderer.color;

            secondBackground = copyRenderer;
        }

        // 스크롤 방향에 맞춰 두 번째 배경 초기 배치
        if (secondBackground != null)
        {
            float offset = (scrollSpeed >= 0) ? -spriteWidth : spriteWidth;
            secondBackground.transform.position = transform.position + new Vector3(offset, 0, 0);
        }
    }

    private void Update()
    {
        if (primaryRenderer == null || secondBackground == null) return;

        // 1. 매 프레임 한 방향으로 이동
        float delta = scrollSpeed * Time.deltaTime;
        transform.position += new Vector3(delta, 0, 0);
        secondBackground.transform.position += new Vector3(delta, 0, 0);

        // 2. 카메라 기준으로 너무 멀어진 배경을 반대편으로 재배치 (무한 스크롤 완성)
        float camX = mainCam != null ? mainCam.transform.position.x : 0f;

        if (transform.position.x - camX > spriteWidth)
        {
            transform.position -= new Vector3(spriteWidth * 2f, 0, 0);
        }
        else if (transform.position.x - camX < -spriteWidth)
        {
            transform.position += new Vector3(spriteWidth * 2f, 0, 0);
        }

        if (secondBackground.transform.position.x - camX > spriteWidth)
        {
            secondBackground.transform.position -= new Vector3(spriteWidth * 2f, 0, 0);
        }
        else if (secondBackground.transform.position.x - camX < -spriteWidth)
        {
            secondBackground.transform.position += new Vector3(spriteWidth * 2f, 0, 0);
        }
    }
}
