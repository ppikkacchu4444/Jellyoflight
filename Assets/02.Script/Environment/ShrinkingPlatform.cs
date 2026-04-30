using UnityEngine;

public class ShrinkingPlatform : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private float shrinkSpeed = 1f;    // 작아지는 속도
    [SerializeField] private float minWidthPercent = 0.0f; // 최소 가로 비율 (0 = 완전히 사라짐)
    [SerializeField] private bool regrow = true;        // 발을 떼면 다시 커질지 여부
    [SerializeField] private float respawnDelay = 1f;   // 0으로 사라졌을 때 다시 생겨날 때까지의 대기 시간

    private Vector3 originalScale;
    private bool isSteppedOn = false;
    private bool isWaitingToRespawn = false;

    private void Start()
    {
        originalScale = transform.localScale;
    }

    private float lastSteppedTime;

    private void Update()
    {
        if (isWaitingToRespawn) return;

        // OnCollisionStay2D가 호출되지 않은지 0.1초가 지났다면 발판에서 떨어진 것으로 간주합니다.
        // 가장자리에서 떨어질 듯 말 듯 할 때 트랩이 커졌다 작아졌다를 반복하며 덜덜거리는 것을 방지!
        if (Time.time - lastSteppedTime > 0.1f)
        {
            isSteppedOn = false;
        }

        float targetWidth = isSteppedOn ? originalScale.x * minWidthPercent : originalScale.x;
        
        if (isSteppedOn || regrow)
        {
            float newX = Mathf.MoveTowards(transform.localScale.x, targetWidth, shrinkSpeed * Time.deltaTime);
            transform.localScale = new Vector3(newX, originalScale.y, originalScale.z);

            // 거의 완전히 사라졌을 때 (minWidthPercent가 0일 때)
            if (isSteppedOn && newX <= 0.05f)
            {
                transform.localScale = new Vector3(0, originalScale.y, originalScale.z);
                StartCoroutine(RespawnRoutine());
            }
        }
    }

    private System.Collections.IEnumerator RespawnRoutine()
    {
        isWaitingToRespawn = true;
        isSteppedOn = false; 
        
        // 1. 사라지는 순간 버벅이는 물리 버그를 막기 위해 컴포넌트를 완전히 꺼버림
        Collider2D col = GetComponent<Collider2D>();
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (col != null) col.enabled = false;
        if (sr != null) sr.enabled = false;
        
        // 플레이어가 밑으로 확실히 떨어질수 있게 잠시 대기
        yield return new WaitForSeconds(respawnDelay);

        // 2. 대기 완료 후 다시 활성화 (이후 Update에서 서서히 커짐)
        if (col != null) col.enabled = true;
        if (sr != null) sr.enabled = true;

        isWaitingToRespawn = false;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // 최상단, 즉 플레이어가 발판을 '밟고 있을 때'만 갱신 (선택사항이지만 충돌 안정성을 높임)
        if (!isWaitingToRespawn && collision.gameObject.CompareTag("Player")) 
        {
            isSteppedOn = true;
            lastSteppedTime = Time.time;
        }
    }
}
