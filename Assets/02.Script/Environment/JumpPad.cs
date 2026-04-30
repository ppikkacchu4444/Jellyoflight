using UnityEngine;

/// <summary>
/// 플레이어가 딛고 점프할 수 있는 물리적인 점프 패드 클래스.
/// </summary>
public class JumpPad : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private Animator anim;

    /// <summary>
    /// 물리 충돌 발생 시 플레이어가 위에서 밟았는지 판정함.
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 플레이어의 위치가 패드의 중심보다 위에 있을 때만 작동 (확실한 판정)
            if (collision.transform.position.y > transform.position.y)
            {
                ExecuteJump(collision.gameObject);
            }
        }
    }

    /// <summary>
    /// 점프 힘을 가하고 애니메이션을 재생함.
    /// </summary>
    private void ExecuteJump(GameObject player)
    {
        if (player.TryGetComponent(out PlayerMove playerMove))
        {
            playerMove.LaunchPlayer(jumpForce);
        }
        else if (player.TryGetComponent(out Rigidbody2D rb))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        if (anim != null)
        {
            anim.SetTrigger("Jump");
        }

        Debug.Log($"<color=lime>[JumpPad] 점프 패드 발동! 힘: {jumpForce}</color>");
    }
}


