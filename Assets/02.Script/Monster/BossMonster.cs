using UnityEngine;
using System.Collections;

/// <summary>
/// 고정 위치에서 3가지 스킬(슬라임 사격, 낙석, 슬라임 파도)과 기본 공격(촉수)을 사용하는 보스 몬스터 클래스.
/// 기믹 오브젝트(BossSacrificeObject)에 의해 그로기 상태에 빠져야만 피격이 가능하며,
/// maxHp만큼 타격받으면 사망 연출 후 스테이지 클리어가 진행된다.
/// </summary>
public class BossMonster : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  체력 및 상태
    // ─────────────────────────────────────────────

    [Header("보스 설정")]
    [SerializeField] private int maxHp = 3;               // 보스 최대 체력. 그로기 상태에서 대쉬 공격으로 타격할 때마다 1씩 감소한다.
    private int currentHp;                                 // 현재 남은 체력 (런타임 전용)
    private bool isVulnerable = false;                     // 그로기(피격 가능) 상태 여부
    private bool isDead = false;                           // 사망 여부 — true가 되면 모든 루틴이 중단된다

    // 대쉬 공격에서 상태 확인용으로 참조
    public bool IsGroggy => isVulnerable;

    [Header("보스 HP UI")]
    [SerializeField] private BossHpUI bossHpUI;            // 보스 체력을 시각적으로 표시하는 UI 컴포넌트. 없으면 자동으로 같은 오브젝트에서 탐색한다.

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;

    // ─────────────────────────────────────────────
    //  패턴 주기 및 접촉 데미지
    // ─────────────────────────────────────────────

    [Header("패턴 설정")]
    [SerializeField] private float actionInterval = 4f;           // 각 스킬/기본 공격 사이의 대기 시간(초). 값이 작을수록 보스가 공격적이 된다.
    [SerializeField] private float minionGroupInterval = 12f;     // 미니언 웨이브 소환 주기(초). BossRoutine과 독립적으로 동작한다.
    [SerializeField] private int contactDamage = 1;               // 보스 몸체에 플레이어가 닿았을 때 주는 데미지 (그로기 상태에서는 발생하지 않음)
    [SerializeField] private float contactKnockback = 1.5f;       // 몸체 접촉 시 플레이어에게 가해지는 넉백 강도
    [SerializeField] private Transform skillSpawnPoint;            // 스킬(슬라임·미니언·파도) 생성 기준 위치. 보스 근처의 빈 Transform을 할당한다.

    // ─────────────────────────────────────────────
    //  스킬별 데미지 · 미니언 수량
    // ─────────────────────────────────────────────

    [Header("데미지 설정")]
    [SerializeField] private int basicAttackDamage = 1;           // 촉수 기본 공격이 플레이어에게 주는 데미지
    [SerializeField] private int minionDamage = 1;                // 소환된 미니언이 플레이어에게 주는 데미지
    [SerializeField] private int minionSpawnCount = 3;            // 한 웨이브당 소환되는 미니언 수
    [SerializeField] private float minionSpawnInterval = 0.5f;    // 미니언 개체 간 소환 간격(초)
    [SerializeField] private int slimeProjectileDamage = 1;       // 슬라임 사격(Skill_SlimeSplatter) 투사체가 주는 데미지
    [SerializeField] private int stoneRainDamage = 1;             // 낙석(Skill_StoneRain) 한 개당 플레이어에게 주는 데미지

    // ─────────────────────────────────────────────
    //  기본 공격(촉수) 타이밍
    // ─────────────────────────────────────────────

    [Header("기본 공격 타이밍 설정")]
    [SerializeField] private float basicAttackStartupDelay = 1.0f;    // 공격 애니메이션 시작 후 실제 히트박스가 활성화되기까지의 선딜레이(초)
    [SerializeField] private float basicAttackActiveDuration = 0.5f;  // 촉수 히트박스가 활성화되어 있는 지속 시간(초). 이 시간이 지나면 비활성화된다.

    // ─────────────────────────────────────────────
    //  프리팹 / 히트박스 참조
    // ─────────────────────────────────────────────

    [Header("프리팹 연결")]
    [SerializeField] private GameObject tentacleHitbox;               // 기본 공격용 촉수 히트박스 오브젝트. 보스 자식 오브젝트에 미리 배치해 두고, 활성/비활성으로 판정을 제어한다.
    [SerializeField] private GameObject minionPrefab;                 // 보스가 주기적으로 소환하는 미니언 프리팹
    [SerializeField] private GameObject slimeProjectilePrefab;        // 슬라임 사격 스킬에서 발사되는 투사체 프리팹 (BossSlimeProjectile 컴포넌트 필요)
    [SerializeField] private GameObject stonePrefab;                  // 낙석 스킬에서 하늘에서 떨어지는 돌 프리팹 (BossStone 컴포넌트 필요)
    [SerializeField] private GameObject wavePrefab;                   // 슬라임 파도 스킬에서 사용하는 파도 프리팹 (BossWave 컴포넌트 필요)

    [Header("사운드 설정")]
    [SerializeField] private AudioClip basicAttackSound;     // 기본 공격 사운드
    [SerializeField] private AudioClip minionSpawnSound;     // 미니언 소환 사운드
    [SerializeField] private AudioClip slimeSplatterSound;   // 슬라임 발사 사운드
    [SerializeField] private AudioClip slimeWaveSound;       // 파도 발사 사운드
    [SerializeField] private AudioClip stoneRainHitSound;    // 낙석 바닥 충돌 사운드 (프리팹 전달용)

    // ─────────────────────────────────────────────
    //  낙석 스킬(Skill_StoneRain) 파라미터
    // ─────────────────────────────────────────────

    [Header("낙석(Stone Rain) 설정")]
    [SerializeField] private float stoneRainDuration = 3f;           // 낙석이 떨어지는 총 지속 시간(초)
    [SerializeField] private float stoneRainInterval = 0.5f;         // 낙석 하나가 생성되는 간격(초). 값이 작을수록 빽빽하게 떨어진다.
    [SerializeField] private float stoneSpawnHeight = 10f;           // 보스 위치(Y) 기준으로 낙석이 생성되는 높이 오프셋
    [SerializeField] private float stoneSpawnRangeX = 8f;            // 플레이어 위치(X) 기준 좌우 낙석 생성 범위. ±이 값 범위 내에서 랜덤 생성된다.

    // ─────────────────────────────────────────────
    //  슬라임 파도 스킬(Skill_SlimeWave) 파라미터
    // ─────────────────────────────────────────────

    [Header("슬라임 파도(Wave) 설정")]
    [SerializeField] private int waveDamage = 1;                     // 파도 한 번에 플레이어에게 주는 데미지
    [SerializeField] private float waveSpeed = 8f;                   // 파도 오브젝트의 이동 속도 (현재 교차 전진 패턴이므로 Initialize에서 0으로 덮어씌워짐)
    [SerializeField] private float waveSpacing = 5.5f;               // 파도가 한 번 점프(FadeLeap)할 때 전진하는 거리(X). 값이 클수록 넓은 범위를 커버한다.
    [SerializeField] private int waveCount = 3;                      // 파도 A·B가 교차 전진하는 총 반복 횟수. 실제 파도 출현은 waveCount × 2 회.
    [SerializeField] private float waveInterval = 0.8f;              // 파도 간 교차 전진 대기 시간(초). 전체 리듬감을 결정한다.

    // ─────────────────────────────────────────────
    //  내부 상태 (런타임 전용)
    // ─────────────────────────────────────────────

    private int skillIndex = 0;                            // 현재 사용할 스킬 인덱스 (0:사격, 1:낙석, 2:파도 순환)
    private Collider2D[] bossColliders;                     // 보스 자신 및 자식의 모든 콜라이더 캐싱 (충돌 무시·활성 제어용)
    private MonsterHitEffect hitEffect;                     // 피격 시 시각 효과(흰색 플래시 등)를 담당하는 컴포넌트
    private Animator anim;                                  // 자식(Visual) 오브젝트에 위치한 애니메이터 — 공격·스킬·그로기·사망 애니메이션 제어
    private BossSacrificeObject currentGroggyObject;        // 현재 보스를 그로기 상태로 만든 기믹 오브젝트 참조 (타격 후 아군 복귀에 사용)

    private void Start()
    {
        // 수치 유효성 검사
        basicAttackDamage = Mathf.Max(1, basicAttackDamage);
        minionDamage = Mathf.Max(1, minionDamage);
        slimeProjectileDamage = Mathf.Max(1, slimeProjectileDamage);
        stoneRainDamage = Mathf.Max(1, stoneRainDamage);
        waveDamage = Mathf.Max(1, waveDamage);

        currentHp = maxHp;
        bossColliders = GetComponentsInChildren<Collider2D>(true);
        hitEffect = GetComponent<MonsterHitEffect>();
        anim = GetComponentInChildren<Animator>(); // 자식(Visual) 오브젝트에 있는 애니메이터까지 포함해서 찾음
        
        // 촉수 히트박스 초기화
        if (tentacleHitbox != null)
        {
            IgnoreBossCollisions(tentacleHitbox);
            
            if (!tentacleHitbox.TryGetComponent(out BossAttackHitbox hitboxScript))
            {
                hitboxScript = tentacleHitbox.AddComponent<BossAttackHitbox>();
            }
            hitboxScript.damage = basicAttackDamage;

            // 판정 신뢰성을 위해 트리거로 설정
            foreach (var col in tentacleHitbox.GetComponentsInChildren<Collider2D>(true))
            {
                col.isTrigger = true;
            }

            tentacleHitbox.SetActive(false);
        }

        // UI 연동
        if (bossHpUI == null) bossHpUI = GetComponent<BossHpUI>();
        if (bossHpUI != null) bossHpUI.Initialize(maxHp);

        StartCoroutine(BossRoutine());
        StartCoroutine(MinionIndependentRoutine());
    }


    /// <summary>
    /// 보스 자신과 생성물 간의 물리 충돌을 무시하도록 설정함.
    /// </summary>
    private void IgnoreBossCollisions(GameObject targetObj)
    {
        if (targetObj == null || bossColliders == null) return;

        Collider2D[] targetColliders = targetObj.GetComponentsInChildren<Collider2D>(true);
        foreach (var bc in bossColliders)
        {
            if (bc == null) continue;
            foreach (var tc in targetColliders)
            {
                if (tc != null && bc != tc)
                {
                    Physics2D.IgnoreCollision(bc, tc, true);
                }
            }
        }
    }

    /// <summary>
    /// 전체적인 보스 공격 패턴을 순환시키는 루틴.
    /// </summary>
    private IEnumerator BossRoutine()
    {
        yield return new WaitForSeconds(actionInterval);

        while (!isDead)
        {
            if (isVulnerable)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // 기본 공격 시전
            yield return StartCoroutine(BasicAttackQueue());

            if (isDead) break;
            yield return new WaitForSeconds(1f);

            if (isVulnerable) continue;

            // 스킬 교차 사용 (0:사격, 1:낙석, 2:파도)
            switch (skillIndex)
            {
                case 0:
                    yield return StartCoroutine(Skill_SlimeSplatter());
                    break;
                case 1:
                    yield return StartCoroutine(Skill_StoneRain());
                    break;
                case 2:
                    yield return StartCoroutine(Skill_SlimeWave());
                    break;
            }
            
            skillIndex = (skillIndex + 1) % 3;
            yield return new WaitForSeconds(actionInterval);
        }
    }

    private IEnumerator BasicAttackQueue()
    {
        // 애니메이션 트리거
        if (anim != null) anim.SetTrigger("Attack");

        // 기본 공격 효과음 재생
        if (basicAttackSound != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(basicAttackSound);

        yield return new WaitForSeconds(basicAttackStartupDelay);

        if (tentacleHitbox != null && !isVulnerable)
        {
            tentacleHitbox.SetActive(true);
            yield return new WaitForSeconds(basicAttackActiveDuration);
            tentacleHitbox.SetActive(false);
        }
    }

    /// <summary>
    /// 패턴 주기와 별개로 주기적으로 미니언을 생성하는 루틴.
    /// </summary>
    private IEnumerator MinionIndependentRoutine()
    {
        yield return new WaitForSeconds(minionGroupInterval * 0.5f);

        while (!isDead)
        {
            if (isVulnerable)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            for (int i = 0; i < minionSpawnCount; i++)
            {
                if (isVulnerable || isDead) break;

                if (minionPrefab != null && skillSpawnPoint != null)
                {
                    GameObject minion = Instantiate(minionPrefab, skillSpawnPoint.position, Quaternion.identity);
                    
                    // 미니언 소환 효과음 재생
                    if (minionSpawnSound != null && SoundManager.Instance != null)
                        SoundManager.Instance.PlaySFX(minionSpawnSound);

                    IgnoreBossCollisions(minion);
                    
                    if (!minion.TryGetComponent(out BossAttackHitbox minionHit))
                    {
                        minionHit = minion.AddComponent<BossAttackHitbox>();
                    }
                    minionHit.damage = minionDamage;

                    // 미니언과 플레이어 간의 물리적 밀림 방지
                    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                    if (playerObj != null)
                    {
                        Collider2D[] playerCols = playerObj.GetComponentsInChildren<Collider2D>(true);
                        Collider2D[] minionCols = minion.GetComponentsInChildren<Collider2D>(true);
                        foreach (var mc in minionCols)
                            foreach (var pc in playerCols)
                                if (mc != null && pc != null) Physics2D.IgnoreCollision(mc, pc, true);
                    }
                }
                yield return new WaitForSeconds(minionSpawnInterval);
            }
            yield return new WaitForSeconds(minionGroupInterval);
        }
    }

    private IEnumerator Skill_SlimeSplatter()
    {
        if (slimeProjectilePrefab == null || skillSpawnPoint == null) yield break;

        // 애니메이션 트리거
        if (anim != null) anim.SetTrigger("Skill_Slime");

        // 슬라임 발사 효과음 재생
        if (slimeSplatterSound != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(slimeSplatterSound);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        bool isPlayerLeft = player != null && player.transform.position.x < transform.position.x;

        int slimeCount = Random.Range(5, 8);
        for (int i = 0; i < slimeCount; i++)
        {
            float angle = isPlayerLeft ? Random.Range(100f, 170f) : Random.Range(10f, 80f);
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            
            GameObject slime = Instantiate(slimeProjectilePrefab, skillSpawnPoint.position, Quaternion.identity);
            IgnoreBossCollisions(slime);
            
            if (slime.TryGetComponent(out BossSlimeProjectile slimeScript))
            {
                slimeScript.damage = slimeProjectileDamage;
            }

            foreach (var col in slime.GetComponentsInChildren<Collider2D>(true))
            {
                col.isTrigger = true;
            }

            if (slime.TryGetComponent(out Rigidbody2D rb))
            {
                rb.linearVelocity = dir * Random.Range(7f, 14f);
            }
        }
        yield return null;
    }

    private IEnumerator Skill_StoneRain()
    {
        float elapsed = 0;

        // 애니메이션 트리거
        if (anim != null) anim.SetTrigger("Skill_Stone");

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 centerPos = (player != null) ? player.transform.position : transform.position;

        while (elapsed < stoneRainDuration)
        {
            float randomX = Random.Range(-stoneSpawnRangeX, stoneSpawnRangeX);
            Vector3 spawnPos = new Vector3(centerPos.x + randomX, transform.position.y + stoneSpawnHeight, 0);

            if (stonePrefab != null)
            {
                GameObject stoneObj = Instantiate(stonePrefab, spawnPos, Quaternion.identity);
                IgnoreBossCollisions(stoneObj);
                
                if (!stoneObj.TryGetComponent(out BossStone stoneScript))
                {
                    stoneScript = stoneObj.AddComponent<BossStone>();
                }
                stoneScript.damage = stoneRainDamage;
                stoneScript.hitSound = stoneRainHitSound; // 충돌 사운드 전달
            }

            elapsed += stoneRainInterval;
            yield return new WaitForSeconds(stoneRainInterval);
        }
    }

    /// <summary>
    /// 2개의 파도가 번갈아가며 투명하게 사라졌다가 앞쪽에서 나타나는 교차 전진 패턴.
    /// </summary>
    private IEnumerator Skill_SlimeWave()
    {
        if (wavePrefab == null || skillSpawnPoint == null) yield break;

        // 애니메이션 트리거
        if (anim != null) anim.SetTrigger("Skill_Wave");

        // 파도 발사 효과음 재생
        if (slimeWaveSound != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(slimeWaveSound);

        Debug.Log("<color=cyan>[Boss] 폭풍 파도 교차 전진 시작!</color>");

        float spacing = waveSpacing;
        float fadeTime = 0.15f; 
        Vector3 basePos = skillSpawnPoint.position;
        basePos.y = transform.position.y - 1.5f;

        // 1. 초기 파도 2개 생성 (처음엔 투명하게)
        GameObject waveA = Instantiate(wavePrefab, basePos, Quaternion.identity);
        GameObject waveB = Instantiate(wavePrefab, basePos + Vector3.left * (spacing * 0.5f), Quaternion.identity);
        
        IgnoreBossCollisions(waveA);
        IgnoreBossCollisions(waveB);
        
        // 중요: 스스로 이동하지 않도록 속도를 0으로 초기화
        if (waveA.TryGetComponent(out BossWave wA)) wA.Initialize(waveDamage, 0);
        if (waveB.TryGetComponent(out BossWave wB)) wB.Initialize(waveDamage, 0);

        // 처음 소환 시 투명하게 설정
        SetAlpha(waveA, 0f);
        SetAlpha(waveB, 0f);

        float currentX = basePos.x;

        // 2. 순차적 교차 전진 루프
        for (int i = 0; i < waveCount; i++)
        {
            if (isVulnerable || isDead) break;

            // 파도 A 전진 (나타남 -> 유지 -> 사라짐의 리듬)
            currentX -= spacing;
            yield return StartCoroutine(FadeLeap(waveA, new Vector3(currentX, basePos.y, 0), fadeTime));
            yield return new WaitForSeconds(waveInterval * 0.5f);

            if (isVulnerable || isDead) break;

            // 파도 B 전진
            currentX -= spacing;
            yield return StartCoroutine(FadeLeap(waveB, new Vector3(currentX, basePos.y, 0), fadeTime));
            yield return new WaitForSeconds(waveInterval * 0.5f);
        }

        // 패턴 종료 시 소멸
        StartCoroutine(FadeOutAndDestroy(waveA, 0.5f));
        StartCoroutine(FadeOutAndDestroy(waveB, 0.5f));
    }

    private void SetAlpha(GameObject obj, float alpha)
    {
        if (obj == null) return;
        SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    /// <summary>
    /// 파도를 투명하게 만들며 목적지로 이동시킨 후 다시 나타나게 함.
    /// 투명한 상태에서는 충돌 판정을 비활성화하여 억울한 피격을 방지함.
    /// </summary>
    private IEnumerator FadeLeap(GameObject wave, Vector3 targetPos, float duration)
    {
        if (wave == null) yield break;

        SpriteRenderer sr = wave.GetComponentInChildren<SpriteRenderer>();
        Collider2D col = wave.GetComponent<Collider2D>();
        if (col == null) col = wave.GetComponentInChildren<Collider2D>();
        
        // 1. 페이드 아웃 (사라지기 시작하면 콜라이더 바로 끔)
        if (col != null) col.enabled = false;

        float elapsed = 0;
        while (elapsed < duration)
        {
            if (wave == null) yield break;
            elapsed += Time.deltaTime;
            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(1f, 0f, elapsed / duration);
                sr.color = c;
            }
            yield return null;
        }

        // 2. 위치 이동
        if (wave != null)
        {
            wave.transform.position = targetPos;
        }
        else yield break;

        // 3. 페이드 인 (완전히 나타나면 다시 콜라이더 켬)
        elapsed = 0;
        while (elapsed < duration)
        {
            if (wave == null) yield break;
            elapsed += Time.deltaTime;
            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(0f, 1f, elapsed / duration);
                sr.color = c;
            }
            yield return null;
        }

        // 목적지에 도착해서 다시 선명해졌을 때만 판정 활성화
        if (col != null) col.enabled = true;
    }



    private IEnumerator FadeOutAndDestroy(GameObject target, float duration)
    {
        if (target == null) yield break;
        SpriteRenderer sr = target.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                Color c = sr.color;
                c.a = Mathf.Lerp(1f, 0f, elapsed / duration);
                sr.color = c;
                yield return null;
            }
        }
        Destroy(target);
    }




    /// <summary>
    /// 아군 기믹 발동 시 보스를 그로기 상태로 전환함.
    /// </summary>
    public void TriggerGroggy(float duration, BossSacrificeObject groggyObj)
    {
        currentGroggyObject = groggyObj;
        StartCoroutine(VulnerabilityRoutine(duration, groggyObj));
    }

    private IEnumerator VulnerabilityRoutine(float duration, BossSacrificeObject groggyObj)
    {
        isVulnerable = true;
        if (anim != null) anim.SetBool("isGroggy", true); // 그로기 애니메이션 시작
        SetBossCollidersEnabled(false);
        Debug.Log("<color=yellow>[Boss] 그로기 상태 진입!</color>");
        
        yield return new WaitForSeconds(duration);
        
        // 타격받지 않고 시간이 종료되었을 때만 여기서 수동 해제
        if (isVulnerable)
        {
            isVulnerable = false;
            if (anim != null) anim.SetBool("isGroggy", false); // 그로기 해제
            SetBossCollidersEnabled(true);
            if (currentGroggyObject != null)
            {
                currentGroggyObject.ClearAllies();
                currentGroggyObject = null;
            }
        }
    }

    private void SetBossCollidersEnabled(bool isEnabled)
    {
        if (bossColliders == null) return;
        foreach (var col in bossColliders)
        {
            if (col != null) col.enabled = isEnabled;
        }
    }

    /// <summary>
    /// 외부 타격 시 체력을 소모하고 피격 연출을 수행함.
    /// </summary>
    public void TakeHit()
    {
        if (!isVulnerable || isDead) return;

        currentHp--;
        Debug.Log($"<color=orange>[Boss] 피격 성공! 남은 체력: {currentHp}/{maxHp}</color>");

        if (hitEffect != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Vector3 sourcePos = player != null ? player.transform.position : Vector3.zero;
            hitEffect.PlayHitEffect(sourcePos);
        }

        // 체력 소모 후 사망 체크
        if (currentHp <= 0)
        {
            Die();
        }
        else
        {
            // 아직 살아있다면 그로기 해제 및 충돌 판정 복구
            isVulnerable = false;
            if (anim != null) anim.SetBool("isGroggy", false); // 타격 시 즉시 그로기 해제
            SetBossCollidersEnabled(true);
            UpdateHpUI();

            // [중요] 타격 성공 시 아군들을 고향으로 돌려보냄 (기믹 순환 복구)
            if (currentGroggyObject != null)
            {
                currentGroggyObject.ClearAllies();
                currentGroggyObject = null;
            }
        }
    }



    private void UpdateHpUI()
    {
        if (bossHpUI != null) bossHpUI.UpdateDisplay(currentHp);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        isVulnerable = false;

        // 사망 애니메이션 실행
        if (anim != null) anim.SetTrigger("Die");

        // 모든 소환물 및 공격 루틴 즉시 정지
        StopAllCoroutines();
        SetBossCollidersEnabled(true);
        UpdateHpUI();

        if (bossHpUI != null) bossHpUI.OnBossDestroyed();

        // [연출을 위한 지연 실행] 보스가 죽는 모습을 잠시 보여준 뒤 클리어 처리
        StartCoroutine(DieSequenceRoutine());
    }

    private IEnumerator DieSequenceRoutine()
    {
        Debug.Log("<color=cyan>[Boss] 사망 연출 중... 2초 후 스테이지 클리어</color>");
        
        // 보스가 사라지기 전 잠깐의 연출 시간 (2.0초)
        yield return new WaitForSeconds(1.0f);

        // 1. 즉시 스테이지 클리어 상태로 확정 (타이머 정지 및 실패 판정 차단)
        if (StageManager.Instance != null)
        {
            StageManager.Instance.MarkAsCleared();
        }

        // 2. 플레이어 보호 및 고정 (영상 도중 사망 및 추락 방지)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // 무적 설정 (데미지 차단)
            if (player.TryGetComponent(out PlayerHealth pHealth))
                pHealth.SetInvincible(true);

            // 이동 스크립트 비활성화
            if (player.TryGetComponent(out PlayerMove pMove))
                pMove.enabled = false;

            // 중력 및 물리 영향을 받지 않도록 Static으로 변경 (바닥 아래로 꺼짐 방지)
            if (player.TryGetComponent(out Rigidbody2D pRb))
            {
                pRb.linearVelocity = Vector2.zero;
                pRb.bodyType = RigidbodyType2D.Static;
            }
        }

        // 3. 5스테이지(보스전) 영상 시퀀스가 있다면 영상 재생에 맡김
        if (BossDefeatedSequence.Instance != null)
        {
            Debug.Log("<color=yellow>[Boss] 사망 연출 완료. 영상을 시작합니다.</color>");
            BossDefeatedSequence.Instance.TriggerVictorySequence();
            
            // 물리 판정만 제거하여 플레이어와의 충돌을 막음
            SetBossCollidersEnabled(false);
        }
        else
        {
            Debug.Log("<color=yellow>[Boss] 일반 클리어 처리를 시작합니다. (BossDefeatedSequence 없음)</color>");
            if (StageManager.Instance != null)
            {
                StageManager.Instance.ClearStage();
            }
            Destroy(gameObject);
        }
    }



    // 보스 몸체 충돌 판정 (Stay에서 Enter로 변경하여 중복 데미지 방지)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandlePlayerCollision(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandlePlayerCollision(other.gameObject);
    }

    private void HandlePlayerCollision(GameObject other)
    {
        // 사망했거나 그로기 상태, 또는 플레이어가 아니면 데미지 없음
        if (isDead || isVulnerable || other == null || !other.CompareTag("Player")) return;

        // 대쉬 공격 중 무적 상태라면 무적 무시
        if (other.TryGetComponent(out PlayerDashAttack dash) && dash.IsInvincible) return;

        if (other.TryGetComponent(out PlayerHealth hp))
        {
            hp.TakeDamage(contactDamage, transform.position, contactKnockback);
            Debug.Log("<color=white>[Boss] 플레이어 몸체 접촉 데미지!</color>");
        }
    }

    private void OnDrawGizmos()
    {
        if (tentacleHitbox != null)
        {
            // 활성화 시 빨간색, 비활성화 시 회색
            Gizmos.color = tentacleHitbox.activeInHierarchy ? Color.red : Color.gray;
            
            Collider2D col = tentacleHitbox.GetComponent<Collider2D>();
            if (col != null)
            {
                // 로컬 좌표계 행렬 적용하여 회전/크기 반영
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = tentacleHitbox.transform.localToWorldMatrix;

                if (col is BoxCollider2D box)
                {
                    Gizmos.DrawWireCube(box.offset, box.size);
                }
                else if (col is CircleCollider2D circle)
                {
                    Gizmos.DrawWireSphere(circle.offset, circle.radius);
                }
                else
                {
                    // 기타 콜라이더는 월드 Bounds 기반 (회전 미반영될 수 있음)
                    Gizmos.matrix = oldMatrix;
                    Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
                }
                
                Gizmos.matrix = oldMatrix;
            }
        }
    }
}


