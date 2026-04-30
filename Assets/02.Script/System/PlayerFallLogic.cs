using UnityEngine;

// ──────────────────────────────────────────────
//  PlayerFallLogic
//  1. 카메라가 플레이어를 쫓아가되, 절벽 아래(cameraLimitY)로는 더 이상 내려가지 않게 하는 '가짜 타겟' 역할
//  2. 플레이어가 그보다 더 아래(deathLimitY)로 떨어지면 즉사(InstantKill) 처리
// ──────────────────────────────────────────────
public class PlayerFallLogic : MonoBehaviour
{
    [Header("추적할 대상 (비워두면 자동 찾음)")]
    [SerializeField] private Transform playerTransform;

    [Header("높이 제한 설정")]
    [Tooltip("이 높이 이하로는 카메라가 쫓아가지 않습니다.")]
    [SerializeField] private float cameraLimitY = -5f;
    
    [Tooltip("이 높이까지 떨어지면 플레이어가 즉사합니다.")]
    [SerializeField] private float deathLimitY = -12f;

    private PlayerHealth playerHealth;
    private bool isDeadTriggered = false;

    private void Start()
    {
        // 플레이어 찾기
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (playerTransform != null)
        {
            playerHealth = playerTransform.GetComponent<PlayerHealth>();
        }
        
        // 이 가짜 타겟이 플레이어의 자식으로 묶여있다면 해제 (독립적으로 움직여야 함)
        transform.SetParent(null);
    }

    private void LateUpdate()
    {
        if (playerTransform == null) return;

        // 1. 가짜 타겟의 위치 업데이트 (카메라가 이 오브젝트를 쫓아옴)
        Vector3 targetPos = playerTransform.position;
        if (targetPos.y < cameraLimitY)
        {
            // 카메라 마지노선 밑으로는 내려가지 않음
            targetPos.y = cameraLimitY;
        }
        transform.position = targetPos;

        // 2. 즉사 높이 밑으로 떨어졌는지 검사
        if (!isDeadTriggered && playerTransform.position.y <= deathLimitY)
        {
            if (playerHealth != null)
            {
                isDeadTriggered = true;
                Debug.Log("<color=red>[PlayerFallLogic] 추락사 범위 도달! 사망 처리합니다.</color>");
                playerHealth.InstantKill();
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        // 씬 뷰에서 한계선을 그어주어 에디터 작업 시 편하게 조절할 수 있도록 합니다.
        Gizmos.color = Color.yellow;
        Vector3 camLeft = new Vector3(transform.position.x - 50, cameraLimitY, 0);
        Vector3 camRight = new Vector3(transform.position.x + 50, cameraLimitY, 0);
        Gizmos.DrawLine(camLeft, camRight);
        
        Gizmos.color = Color.red;
        Vector3 deathLeft = new Vector3(transform.position.x - 50, deathLimitY, 0);
        Vector3 deathRight = new Vector3(transform.position.x + 50, deathLimitY, 0);
        Gizmos.DrawLine(deathLeft, deathRight);
    }
}
