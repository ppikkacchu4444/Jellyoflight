using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MonsterPortal : MonoBehaviour
{
    [Header("소환 설정")]
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int maxMonsterCount = 3;         // 동시에 유지할 수 있는 몬스터 최대 수
    [SerializeField] private int totalSpawnLimit = 5;         // 포탈이 수명이 다할 때까지 뽑아낼 총 몬스터 수
    [SerializeField] private GameObject spawnEffect;

    [Header("끼임 방지 설정")]
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 0.5f); // 몬스터가 약간 위에서 나오게 설정
    [SerializeField] private float spawnRadius = 1.0f;               // 여러 마리가 겹치지 않게 분산 소환

    [Header("파괴 및 상호작용 설정")]
    [SerializeField] private int hp = 3;                       // 부수기 위해 필요한 타격 수
    [SerializeField] private string interactMessage = "[ E ]  포탈 파괴";
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Image promptIcon;              // 프롬프트 옆에 표시할 아이콘 (Image 컴포넌트 연결)
    [SerializeField] private Color promptColor = Color.white;
    [SerializeField] private GameObject hiddenItemObject;      // 포탈 파괴 시 활성화될 아이템
    [SerializeField] private GameObject breakEffect;           // 포탈 파괴 이펙트
    [SerializeField] private AudioClip destroySound;           // 포탈 파괴 시 재생되는 효과음
    [SerializeField] private GameObject[] hiddenLevelObjects;  // 파괴 시 활성화될 숨겨진 오브젝트

    private List<GameObject> activeMonsters = new List<GameObject>();
    private int spawnedCount = 0;
    private bool isDestructible = false;
    private Animator anim;

    // 모든 포탈을 관리하는 정적 리스트 (상호작용용)
    private static List<MonsterPortal> allPortals = new List<MonsterPortal>();
    public static MonsterPortal GetNearby(Vector3 position, float radius = 2f)
    {
        foreach (var p in allPortals)
        {
            if (p != null && p.isDestructible && Vector3.Distance(p.transform.position, position) <= radius)
                return p;
        }
        return null;
    }

    private void Awake()
    {
        allPortals.Add(this);
        HidePrompt();
        anim = GetComponentInChildren<Animator>();
    }

    private void OnDestroy()
    {
        allPortals.Remove(this);
    }

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (spawnedCount < totalSpawnLimit)
        {
            activeMonsters.RemoveAll(monster => monster == null);

            // 현재 활동 중인 몬스터가 maxMonsterCount 미만일 때만 소환
            if (activeMonsters.Count < maxMonsterCount)
            {
                SpawnMonster();
                spawnedCount++;
            }

            // 총 한도에 도달하면 소환 루프 종료
            if (spawnedCount >= totalSpawnLimit)
            {
                isDestructible = true;
                Debug.Log($"<color=white>[Portal] 몬스터 소환 종료. 포탈을 부술 수 있습니다.</color>");
                
                // 애니메이션 정지 (Idle 애니메이션 중단)
                if (anim != null) anim.enabled = false;

                // 시각적으로 부술 수 있게 변한 것을 보여줌
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(0.6f, 1f, 0.6f); 
                
                Debug.Log($"<color=white>[Portal] 이제 근처에 가면 상호작용 메시지가 뜹니다.</color>");
                
                break;
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnMonster()
    {
        if (monsterPrefab == null) return;

        // 1. 기본 위치 + 오프셋(위로 띄우기) + 랜덤 반경 적용
        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position + (Vector3)spawnOffset + new Vector3(randomOffset.x, randomOffset.y, 0);

        // 2. 소환 효과
        if (spawnEffect != null)
            Instantiate(spawnEffect, spawnPosition, Quaternion.identity);

        // 3. 몬스터 생성
        GameObject monster = Instantiate(monsterPrefab, spawnPosition, Quaternion.identity);
        activeMonsters.Add(monster);

        Debug.Log($"<color=red>[Portal] 몬스터 소환 ({spawnedCount + 1}/{totalSpawnLimit})</color>");
    }

    public void TakeDamage(int damage = 1)
    {
        // 아직 소환 중이면 부서지지 않음
        if (!isDestructible) return;

        hp -= damage;
        Debug.Log($"<color=orange>[Portal] 데미지! 남은 HP: {hp}</color>");

        if (hp <= 0)
        {
            if (breakEffect != null) Instantiate(breakEffect, transform.position, Quaternion.identity);

            // 파괴 효과음 재생
            if (destroySound != null && SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(destroySound);
            
            // 미리 배치해둔 아이템 등장(활성화)
            if (hiddenItemObject != null)
            {
                hiddenItemObject.SetActive(true);
            }
            
            // 숨겨진 레벨 오브젝트 활성화 (발판, 문 등)
            if (hiddenLevelObjects != null)
            {
                foreach (var obj in hiddenLevelObjects)
                {
                    if (obj != null) obj.SetActive(true);
                }
            }
            
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 플레이어 상호작용(E키)을 통한 즉시 파괴 처리.
    /// </summary>
    public void Interact()
    {
        if (!isDestructible) return;
        
        Debug.Log("<color=cyan>[Portal] 상호작용으로 포탈을 즉시 파괴합니다.</color>");
        HidePrompt(); 
        hp = 0; 
        TakeDamage(0); 
    }

    private void ShowPrompt()
    {
        if (promptText != null && isDestructible)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = interactMessage;
            promptText.color = promptColor;
        }
        if (promptIcon != null && isDestructible) promptIcon.gameObject.SetActive(true);
    }

    private void HidePrompt()
    {
        if (promptText != null) promptText.gameObject.SetActive(false);
        if (promptIcon != null) promptIcon.gameObject.SetActive(false);
    }

    // ── 플레이어 접근 감지 ────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isDestructible)
        {
            ShowPrompt();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            HidePrompt();
        }
    }

    private void OnDrawGizmos()
    {
        // 씬 뷰에서 소환 가능한 영역을 표시
        Gizmos.color = Color.magenta;
        Vector3 center = transform.position + (Vector3)spawnOffset;
        Gizmos.DrawWireSphere(center, spawnRadius);
    }
}
