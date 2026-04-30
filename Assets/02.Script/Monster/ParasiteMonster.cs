using UnityEngine;
using System.Collections; // 코루틴을 사용하기 위해 필요한 네임스페이스입니다.

public class ParasiteMonster : MonoBehaviour
{
    [Header("--- 이동 설정 ---")]
    [SerializeField] private float speed = 2f;         // 이동 속도
    [SerializeField] private float patrolDistance = 3f; // 시작 지점으로부터 이동할 거리
    
    [Header("--- 기생(Debuff) 설정 ---")]
    [SerializeField] [Tooltip("머리에 붙어있는 시간 (초)")] 
    private float attachDuration = 3f;
    
    [SerializeField] [Tooltip("이동 속도 감소 비율 (예: 0.5면 속도 절반)")] 
    private float speedMultiplier = 0.5f;
    
    [SerializeField] [Tooltip("점프력 감소 비율 (예: 0.5면 점프력 절반)")] 
    private float jumpMultiplier = 0.5f;

    [SerializeField] [Tooltip("플레이어 머리 위 오프셋 (얼마나 위에 붙을지)")] 
    private Vector3 headOffset = new Vector3(0, 0.5f, 0);

    [Header("--- 외형 설정 ---")]
    [SerializeField] [Tooltip("머리에 붙었을 때 변할 새로운 스프라이트 (안넣으면 원본 유지)")] 
    private Sprite attachedSprite;

    [SerializeField] [Tooltip("별도의 부착용 오브젝트가 있다면 여기 할당 (없으면 스프라이트만 교체)")]
    private GameObject attachedVisual;
    
    [SerializeField] [Tooltip("머리에 붙었을 때의 고정 크기 (기본값 1,1,1)")] 
    private Vector3 attachedScale = Vector3.one;

    private Vector2 startPosition;                     // 초기 생성 위치
    private int direction = 1;                         // 이동 방향 (1: 오른쪽, -1: 왼쪽)
    private float originalScaleX;                      // 원래 X 크기
    private bool isAttached = false;                   // 현재 플레이어에게 붙어 있는지 여부

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;

    void Start()
    {
        startPosition = transform.position;
        originalScaleX = transform.localScale.x;

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        
        if (rb != null) rb.freezeRotation = true;

        // [자동화] 파라사이트는 데미지를 주지 않으므로 관련 기능들을 모두 끔
        if (TryGetComponent(out Monster monster))
        {
            monster.CanDealContactDamage = false;
        }
        if (TryGetComponent(out MonsterHealth mHealth))
        {
            mHealth.CanDealContactDamage = false;
        }
    }


    void Update()
    {
        // 플레이어에게 부착된 상태라면 순찰(Patrol)을 하지 않습니다!
        if (isAttached) return;

        // --- 순찰 로직 (기존 몬스터와 동일) ---
        if (transform.rotation.z != 0)
        {
            transform.rotation = Quaternion.identity;
        }

        float currentDistance = Vector2.Distance(startPosition, transform.position);

        if (currentDistance >= patrolDistance)
        {
            direction *= -1;
            Vector3 newScale = transform.localScale;
            newScale.x = originalScaleX * direction;
            transform.localScale = newScale;

            float offset = (currentDistance - patrolDistance) + 0.01f;
            transform.Translate(Vector3.right * direction * offset, Space.World);
        }

        transform.Translate(Vector3.right * direction * speed * Time.deltaTime, Space.World);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 이미 찰싹 붙은 상태라면 또 작동하지 않도록 막습니다.
        if (isAttached) return;

        // 플레이어인지 확인합니다. (PlayerMove 스크립트가 있다면 플레이어로 판단)
        PlayerMove playerMove = collision.gameObject.GetComponent<PlayerMove>();

        if (playerMove != null)
        {
            // [수정] 대쉬 공격 중일 때는 달라붙지 못하게 막아서, 공격으로 정상적으로 죽일 수 있게 합니다.
            PlayerDashAttack dashAttack = playerMove.GetComponent<PlayerDashAttack>();
            if (dashAttack != null && dashAttack.IsInvincible) return;

            isAttached = true; // 붙었다고 상태를 변경합니다.

            // 물리적 충돌과 중력을 꺼줍니다 (안 그러면 플레이어가 무거워지거나 버그 발생!)
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic; 
            }
            if (col != null)
            {
                col.enabled = false; // 충돌체를 완전히 끄거나 Trigger로 변경
            }

            // == 로직 및 애니메이션 정지 ==
            // Monster 스크립트가 있다면 이동 로직이 겹치지 않게 끕니다.
            if (TryGetComponent(out Monster monster)) monster.enabled = false;
            // Animator 가 있다면 스프라이트 교체를 방해하므로 정지시킵니다.
            if (TryGetComponent(out Animator anim)) anim.enabled = false;

            // 트랜스폼 상속: 몬스터를 플레이어의 자식 오브젝트로 만듭니다.
            transform.SetParent(playerMove.transform);
            
            // 위치를 머리 위쪽(headOffset)으로 옮깁니다.
            transform.localPosition = headOffset;
            transform.localScale = attachedScale;

            // == 외형 교체 및 보이기 처리 ==
            if (attachedVisual != null)
            {
                // 별도 비주얼 오브젝트가 있다면 기존 렌더러를 끄고 해당 오브젝트를 활성화
                if (sr != null) sr.enabled = false;
                attachedVisual.SetActive(true);
            }
            else if (sr != null)
            {
                // 설정해둔 이미지가 있다면 교체
                if (attachedSprite != null)
                {
                    sr.sprite = attachedSprite;
                }
                
                // [핵심] 스프라이트가 안보이는 현상 픽스: 플레이어보다 무조건 위에 그려지도록 순서를 확 올립니다.
                sr.sortingOrder = 100;
            }

            // 디버프(능력치 감소)를 적용합니다.
            playerMove.AddSlowStack(speedMultiplier);
            playerMove.AddJumpDebuff(jumpMultiplier);

            // [코루틴 실행 시작!]
            StartCoroutine(DetachAfterTime(playerMove));
        }
    }

    // ---------------------------------------------------------
    // [코루틴(Coroutine) 이란?]
    // 일반적인 함수는 콜을 하면 한 번에 내용이 끝까지 주욱 실행됩니다.
    // 하지만 코루틴은 'yield return' 이라는 예약어를 사용할 수 있는데,
    // "함수 실행을 잠깐 여기서 멈췄다가(대기), 약속한 시간이 지나면 이어서 실행해!" 라고 명령할 수 있습니다.
    // 유니티에서 'n초 뒤에 무언가를 한다'는 타이머 로직을 짤 때 아주 핵심적이고 자주 쓰이는 기능입니다.
    // ---------------------------------------------------------
    private IEnumerator DetachAfterTime(PlayerMove playerMove)
    {
        // 입력받은 attachDuration(n초) 만큼 코루틴을 대기(일시정지)시킵니다.
        yield return new WaitForSeconds(attachDuration);

        // --- n초 뒤에 이어서 실행되는 부분 ---

        // 도중에 플레이어가 죽어 파괴되었을 수 있으니 안전하게 확인합니다
        if (playerMove != null) 
        {
            // 빼앗아간 이동 속도 및 점프력을 원래대로 복구시킵니다.
            playerMove.RemoveSlowStack(speedMultiplier);
            playerMove.RemoveJumpDebuff(jumpMultiplier);
        }

        // 마지막으로 몬스터 자신을 삭제하여 플레이어의 머리에서 없앱니다.
        Destroy(gameObject);
    }
}
