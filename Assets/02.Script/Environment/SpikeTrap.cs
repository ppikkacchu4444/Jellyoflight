using UnityEngine;
using System.Collections;

public class SpikeTrap : MonoBehaviour
{
    [Header("전투 설정")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private float damageInterval = 0.6f; 

    [Header("작동 설정")]
    [SerializeField] private bool isTimed = false;
    [SerializeField] private float activeTime = 2f;
    [SerializeField] private float inactiveTime = 2f;
    [SerializeField] private Animator anim;

    [Header("사운드 설정")]
    [SerializeField] private AudioClip activateSound;      // 스파이크가 활성화될 때 재생되는 효과음

    private bool isActive = true;
    private float lastDamageTime;

    private void Start()
    {
        if (isTimed) StartCoroutine(TrapCycle());
        else isActive = true;
    }

    private IEnumerator TrapCycle()
    {
        while (true)
        {
            isActive = true;
            if (anim != null) anim.SetBool("IsActive", true);
            ToggleVisibility(true);

            // 활성화 효과음 재생 (화면에 보일 때만)
            if (activateSound != null && SoundManager.Instance != null && IsVisibleToCamera())
                SoundManager.Instance.PlaySFX(activateSound);

            yield return new WaitForSeconds(activeTime);

            isActive = false;
            if (anim != null) anim.SetBool("IsActive", false);
            ToggleVisibility(false);
            yield return new WaitForSeconds(inactiveTime);
        }
    }

    private void ToggleVisibility(bool visible)
    {
        // 자식 오브젝트까지 포함하여 모든 SpriteRenderer를 켜고 끕니다.
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in renderers)
        {
            sr.enabled = visible;
        }
    }

    // ── 충돌 즉시 대미지 ──────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        ProcessDamage(other);
    }

    // ── 머물러 있을 때 주기적 대미지 ──────────
    private void OnTriggerStay2D(Collider2D other)
    {
        ProcessDamage(other);
    }

    private void ProcessDamage(Collider2D other)
    {
        if (!isActive) return;

        // 플레이어 태그 확인 (대소문자 및 태그 설정 확인 필수)
        if (other.CompareTag("Player"))
        {
            if (Time.time < lastDamageTime + damageInterval) return;

            // attachedRigidbody를 사용해 플레이어 본체의 health를 더 정확히 찾음
            PlayerHealth hp = null;
            if (other.attachedRigidbody != null)
            {
                hp = other.attachedRigidbody.GetComponent<PlayerHealth>();
            }
            
            // 못 찾았다면 부모 오브젝트에서 재시도
            if (hp == null) hp = other.GetComponentInParent<PlayerHealth>();

            if (hp != null)
            {
                hp.TakeDamage(damage);
                lastDamageTime = Time.time;

                Rigidbody2D rb = other.attachedRigidbody;
                if (rb != null)
                {
                    // 넉백 (위로 뿅 튕겨내기)
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, knockbackForce);
                }
                Debug.Log("<color=red>[SpikeTrap] 플레이어 찔림!</color>");
            }
            else
            {
                Debug.LogWarning("[SpikeTrap] 플레이어를 감지했지만 PlayerHealth를 찾을 수 없습니다.");
            }
        }
    }

    private bool IsVisibleToCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        
        Vector3 viewportPos = cam.WorldToViewportPoint(transform.position);
        // z > 0 (카메라 앞), x/y 0~1 (화면 범위 내)
        // 약간의 여유(0.1)를 주어 화면 끝에 걸쳐있을 때도 소리가 나게 함
        return viewportPos.z > 0 && viewportPos.x >= -0.1f && viewportPos.x <= 1.1f && viewportPos.y >= -0.1f && viewportPos.y <= 1.1f;
    }
}
