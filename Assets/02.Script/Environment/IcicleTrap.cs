using UnityEngine;
using System.Collections;

/// <summary>
/// 천장에 배치되어 주기적으로 고드름을 생성하고 낙하시키는 트랩 클래스.
/// </summary>
public class IcicleTrap : MonoBehaviour
{
    [Header("--- 고드름 소환 설정 ---")]
    [SerializeField] private GameObject iciclePrefab;
    [SerializeField] private float dropInterval = 3.5f;
    [SerializeField] private float fallSpeed = 7f;
    [SerializeField] private int damage = 1;

    [Header("--- 바닥 판정 설정 ---")]
    [Tooltip("고드름이 부딪혔을 때 파괴될 지형 레이어들을 선택하세요.")]
    [SerializeField] private LayerMask groundLayers;

    [Header("--- 사운드 설정 ---")]
    [SerializeField] private AudioClip hitSound;          // 바닥 충돌 시 효과음

    private void Start()
    {
        if (iciclePrefab != null)
        {
            StartCoroutine(DropRoutine());
        }
        else
        {
            Debug.LogWarning($"[IcicleTrap] {gameObject.name}에 프리팹이 없습니다.");
        }
    }

    private IEnumerator DropRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(dropInterval);
            
            if (iciclePrefab != null)
            {
                GameObject icicle = Instantiate(iciclePrefab, transform.position, Quaternion.identity);
                
                // 생성된 고드름에 설정값 전달
                if (!icicle.TryGetComponent(out IcicleObject icicleScript))
                {
                    icicleScript = icicle.AddComponent<IcicleObject>();
                }
                
                // 본체에 설정된 레이어마스크와 수치를 그대로 전달함
                icicleScript.Initialize(fallSpeed, damage, groundLayers, hitSound);
            }
        }
    }
}

/// <summary>
/// 낙하하는 고드름의 실질적인 이동과 충돌을 처리하는 클래스.
/// </summary>
public class IcicleObject : MonoBehaviour
{
    private float speed;
    private int damage;
    private LayerMask terrainLayers;
    private float spawnTime;
    private Animator anim;
    private AudioClip hitSound;
    private bool isHit = false;

    /// <summary>
    /// 고드름 소환 시 필요한 정보를 초기화함.
    /// </summary>
    public void Initialize(float s, int d, LayerMask layers, AudioClip sound)
    {
        speed = s;
        damage = d;
        terrainLayers = layers;
        hitSound = sound;
    }

    private void Start()
    {
        spawnTime = Time.time;
        anim = GetComponentInChildren<Animator>();

        // 물리 연산을 위한 최소 설정
        if (!TryGetComponent(out Rigidbody2D rb))
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
    }

    private void Update()
    {
        if (isHit) return;

        transform.Translate(Vector3.down * speed * Time.deltaTime);

        // 맵 아웃 방지용 자동 제거
        if (transform.position.y < -50f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || isHit) return;

        // 1. 플레이어 피격 처리
        if (other.CompareTag("Player") || other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            if (other.TryGetComponent(out PlayerHealth hp) || 
                (other.transform.parent != null && other.transform.parent.TryGetComponent(out hp)))
            {
                hp.TakeDamage(damage, transform.position);
            }
            TriggerHit();
            return;
        }

        // 2. 지형 충돌 처리 (비트 연산으로 레이어 체크)
        if (Time.time > spawnTime + 0.15f)
        {
            if (((1 << other.gameObject.layer) & terrainLayers) != 0)
            {
                TriggerHit();
            }
        }
    }

    private void TriggerHit()
    {
        isHit = true;

        // 바닥 충돌 효과음 재생 (화면에 보일 때만)
        if (hitSound != null && SoundManager.Instance != null && IsVisibleToCamera())
            SoundManager.Instance.PlaySFX(hitSound);

        // 즉시 이동 정지 및 콜라이더 비활성화
        speed = 0;
        if (TryGetComponent(out Collider2D col)) col.enabled = false;

        if (anim != null)
        {
            anim.SetTrigger("Hit");
            // 원래 설정했던 0.4초로 복구
            Destroy(gameObject, 0.4f);
        }
        else
        {
            Destroy(gameObject);
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


