using UnityEngine;
using System.Collections;

// ──────────────────────────────────────────────
//  DelayedItemSpawn (Item Spawner 업그레이드 버전)
//  빈 게임오브젝트에 달아서 아이템을 일정 시간 뒤에 소환하거나, 계속해서 리스폰 시킬 수 있습니다.
// ──────────────────────────────────────────────
public class DelayedItemSpawn : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private GameObject itemPrefab;       // 소환할 원래 아이템 프리팹
    [SerializeField] private float firstSpawnDelay = 3f;  // 게임 시작 후 처음 스폰될 때까지의 대기 시간
    
    [Header("반복(리스폰) 설정")]
    [SerializeField] private bool repeatSpawn = false;    // 아이템을 먹으면 다시 나타나게 할지 여부
    [SerializeField] private float repeatInterval = 10f;  // 플레이어가 아이템을 먹고난 뒤 다음 스폰까지의 대기 시간(쿨타임)

    [Header("연출")]
    [SerializeField] private GameObject spawnEffect;      // 나타날 때 이펙트
    [SerializeField] private float spawnEffectDestroyDelay = 2.0f;

    private GameObject currentItem;

    private void Start()
    {
        // 씬 배치 시 에디터 확인용으로만 쓰인 스프라이트가 있다면 게임 땐 안보이게 숨김
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        if (itemPrefab != null)
        {
            StartCoroutine(SpawnRoutine());
        }
        else
        {
            Debug.LogWarning($"[ItemSpawner] {gameObject.name}에 Item Prefab이 설정되지 않았습니다!");
        }
    }

    private IEnumerator SpawnRoutine()
    {
        // 1. 처음 약속된 시간만큼 대기
        yield return new WaitForSeconds(firstSpawnDelay);

        while (true)
        {
            // 2. 아이템 소환
            SpawnItem();

            // 3. 플레이어가 아이템을 먹어서 없어질 때까지 대기
            while (currentItem != null)
            {
                yield return null;
            }

            if (!repeatSpawn) break;

            // 4. 아이템을 먹었다면 설정된 쿨타임(repeatInterval)만큼 대기 후 루프 다시 시작
            yield return new WaitForSeconds(repeatInterval);
        }
    }

    private void SpawnItem()
    {
        // 아이템 소환 위치 설정 (Z축을 0으로 고정하여 레이어 겹침 방지)
        Vector3 spawnPos = new Vector3(transform.position.x, transform.position.y, 0f);
        
        currentItem = Instantiate(itemPrefab, spawnPos, Quaternion.identity);

        if (spawnEffect != null)
        {
            // 이펙트는 아이템보다 확실히 앞(Z = -0.5)에 생성
            Vector3 effectPos = spawnPos + new Vector3(0, 0, -0.5f);
            GameObject effect = Instantiate(spawnEffect, effectPos, Quaternion.identity);
            
            // 프리팹 자체가 꺼져있을 경우를 대비해 명시적 활성화
            effect.SetActive(true);
            
            // 모든 렌더러(Sprite, Particle 등)를 강제로 맨 앞으로(Order 100) 설정
            Renderer[] rs = effect.GetComponentsInChildren<Renderer>(true);
            foreach (var r in rs)
            {
                r.sortingOrder = 100;
            }
            
            // 스케일이 0인 경우를 대비한 최소 크기 보장
            if (effect.transform.localScale.sqrMagnitude < 0.001f)
                effect.transform.localScale = Vector3.one;

            // 파티클 시스템인 경우 강제 재생 시도
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                Debug.Log($"<color=yellow>[ItemSpawner] 파티클 시스템(ps) 재생됨: {ps.gameObject.name}</color>");
            }

            Destroy(effect, spawnEffectDestroyDelay);
            Debug.Log($"<color=white>[ItemSpawner] '{spawnEffect.name}' 이펙트 생성 완료 (SortingOrder: 100)</color>");
        }
        else
        {
            Debug.LogWarning($"<color=red>[ItemSpawner] {gameObject.name}에 spawnEffect 필드가 비어있습니다!</color>");
        }
            
        Debug.Log($"<color=cyan>[ItemSpawner] {itemPrefab.name} 소환 완료!</color>");
    }
}
