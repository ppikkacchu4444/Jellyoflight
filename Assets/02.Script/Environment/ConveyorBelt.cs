using UnityEngine;

// ──────────────────────────────────────────────
//  ConveyorBelt
//  발판 위에 있는 플레이어를 한쪽 방향으로 계속 밀어냅니다.
// ──────────────────────────────────────────────
public class ConveyorBelt : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private float speed = 3f;      // 밀어내는 속도
    [SerializeField] private Vector2 direction = Vector2.right; // 밀어내는 방향

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerMove pm = collision.gameObject.GetComponent<PlayerMove>();
            if (pm != null)
            {
                // 위치를 직접 바꾸는 대신 PlayerMove에게 "나 지금 이만큼 방향으로 밀고 있어!" 라고 알려줌
                // PlayerMove 스크립트가 물리 업데이트인 FixedUpdate에서 합산해 깔끔하게 이동시킵니다.
                pm.externalVelocity += direction.normalized * speed;
            }
        }
    }
}
