using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;

/// <summary>
/// 모든 특수 효과 조절 및 카메라 위치를 배경 경계선 안에 '물리적으로' 가두는 클래스.
/// </summary>
public class EffectManager : MonoBehaviour
{
    private static EffectManager instance;
    public static EffectManager Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<EffectManager>();
            return instance;
        }
    }

    [Header("1. 광역 공격(AOE) 설정")]
    [SerializeField] private int aoeDamage = 100;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private float viewportMargin = 0.2f;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private UnityEngine.UI.Image screenFlashUI;
    [SerializeField] private AudioClip aoeExplosionSound;          // AOE 폭발 시 재생되는 효과음

    [Header("2. 무적(Invincibility) 설정")]
    [SerializeField] private float invincPushForce = 15f;
    [SerializeField] private float invincPushRadius = 5f;
    [SerializeField] private AudioClip invincLoopSound;             // 무적 지속 시간 동안 재생되는 루프 효과음
    [SerializeField] private AudioClip invincKillSound;             // 무적 중 몬스터를 날려보낼 때 재생되는 효과음
    [Range(0f, 1f)] [SerializeField] private float invincLoopVolume = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float invincKillVolume = 0.4f;

    [Header("3. 시네머신 카메라 줌 & 경계 보호 설정")]
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private float skillZoomSize = 8.0f; 
    [SerializeField] private float zoomDuration = 0.4f; 
    [Space]
    [SerializeField] private bool useHardLimit = true; 
    [SerializeField] private Collider2D backgroundBoundary; 

    [Header("4. 시간 정지(TimeStop) 사운드")]
    [SerializeField] private AudioClip timeStopLoopSound;           // 시간 정지 지속 시간 동안 재생되는 루프 효과음
    [Range(0f, 1f)] [SerializeField] private float timeStopVolume = 0.5f;

    [Header("5. 속도 버프 사운드")]
    [SerializeField] private AudioClip speedBoostLoopSound;         // 속도 버프 지속 중 재생되는 효과음
    [Range(0f, 1f)] [SerializeField] private float speedBoostVolume = 0.4f;

    private float originalZoomSize;
    private Coroutine zoomCoroutine;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        
        if (virtualCamera == null) virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        if (virtualCamera != null) originalZoomSize = virtualCamera.Lens.OrthographicSize;
        
        if (backgroundBoundary == null)
        {
            GameObject bg = GameObject.Find("CameraBoundary");
            if (bg != null) backgroundBoundary = bg.GetComponent<Collider2D>();
        }

        if (screenFlashUI != null) screenFlashUI.gameObject.SetActive(false);
    }

    // [강력 필살기] 매 프레임 시네머신 위치를 배경 안으로 강제 고정
    private void LateUpdate()
    {
        if (virtualCamera == null || backgroundBoundary == null || !useHardLimit) return;

        // 현재 렌즈 사이즈 기준 화면 너비/높이 계산
        float camHeight = virtualCamera.Lens.OrthographicSize;
        float camWidth = camHeight * ((float)Screen.width / Screen.height);

        Bounds b = backgroundBoundary.bounds;

        // 카메라 위치가 가질 수 있는 최소/최대값 제한
        float minX = b.min.x + camWidth;
        float maxX = b.max.x - camWidth;
        float minY = b.min.y + camHeight;
        float maxY = b.max.y - camHeight;

        // 만약 줌 사이즈가 배경보다 커졌을 경우를 대비한 방어 로직
        if (minX > maxX) { minX = maxX = (b.min.x + b.max.x) / 2f; }
        if (minY > maxY) { minY = maxY = (b.min.y + b.max.y) / 2f; }

        Vector3 curPos = virtualCamera.transform.position;
        float clampedX = Mathf.Clamp(curPos.x, minX, maxX);
        float clampedY = Mathf.Clamp(curPos.y, minY, maxY);

        // 시네머신 시스템에 강제 위치 적용
        virtualCamera.ForceCameraPosition(new Vector3(clampedX, clampedY, curPos.z), virtualCamera.transform.rotation);
    }

    public void ActivateAOE(Vector3 originPos, float range = 0f)
    {
        if (screenFlashUI != null) StartCoroutine(FlashRoutine());
        StartSmoothZoom(skillZoomSize, 1.5f);

        // AOE 폭발 효과음 재생
        if (aoeExplosionSound != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(aoeExplosionSound);

        bool IsInRange(Vector3 targetPos)
        {
            if (range <= 0f) return IsInExtendedView(targetPos);
            return Vector2.Distance(originPos, targetPos) <= range;
        }

        MonsterHealth[] targets = FindObjectsByType<MonsterHealth>(FindObjectsSortMode.None);
        foreach (var monster in targets)
        {
            if (monster != null && IsInRange(monster.transform.position))
            {
                monster.TakeDamage(aoeDamage);
                if (explosionPrefab != null) Instantiate(explosionPrefab, monster.transform.position, Quaternion.identity);
            }
        }
    }

    public void ActivateInvincibility(GameObject player, float duration, Sprite icon = null)
    {
        if (player == null) return;
        if (!player.TryGetComponent(out PlayerInvincibility invinc)) invinc = player.AddComponent<PlayerInvincibility>();
        invinc.StartInvincibility(duration, icon);
        PushBackEnemies(player, invincPushRadius, invincPushForce);

        // 무적 루프 효과음 재생 (지속 시간 동안)
        if (invincLoopSound != null && SoundManager.Instance != null)
            StartCoroutine(PlayLoopSoundRoutine(invincLoopSound, duration, invincLoopVolume));
    }

    private void PushBackEnemies(GameObject player, float radius, float force)
    {
        Vector2 origin = player.transform.position;
        Collider2D[] observers = Physics2D.OverlapCircleAll(origin, radius);
        foreach (var col in observers)
        {
            if (col.gameObject == player || col.transform.IsChildOf(player.transform)) continue;
            Rigidbody2D rb = col.GetComponentInParent<Rigidbody2D>();
            if (rb != null)
            {
                MonoBehaviour[] scripts = rb.GetComponentsInChildren<MonoBehaviour>();
                foreach (var s in scripts) if (s != null && !s.GetType().Name.Contains("Health")) StartCoroutine(DisableScriptRoutine(s, 0.6f));
                RigidbodyConstraints2D originalConstraints = rb.constraints;
                bool wasKinematic = (rb.bodyType == RigidbodyType2D.Kinematic);
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.bodyType = RigidbodyType2D.Dynamic;
                Vector2 dir = (rb.position - origin).normalized; if (dir == Vector2.zero) dir = Vector2.up;
                rb.linearVelocity = dir.normalized * force;
                StartCoroutine(RestoreMonsterStateRoutine(rb, originalConstraints, wasKinematic, 0.6f));
            }
        }
    }

    private IEnumerator DisableScriptRoutine(MonoBehaviour script, float delay)
    {
        if (script == null) yield break;
        script.enabled = false; yield return new WaitForSeconds(delay);
        if (script != null) script.enabled = true;
    }

    private IEnumerator RestoreMonsterStateRoutine(Rigidbody2D rb, RigidbodyConstraints2D originalConstraints, bool wasKinematic, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.constraints = originalConstraints; if (wasKinematic) rb.bodyType = RigidbodyType2D.Kinematic; }
    }

    public void ActivateSpeedBoost(GameObject player, float duration, float multiplier, Sprite icon = null)
    {
        if (player == null) return;
        if (!player.TryGetComponent(out PlayerSpeedEffect speed)) speed = player.AddComponent<PlayerSpeedEffect>();
        speed.StartSpeedBoost(duration, multiplier, icon);

        // 속도 버프 지속 효과음 재생
        if (speedBoostLoopSound != null && SoundManager.Instance != null)
            StartCoroutine(PlayLoopSoundRoutine(speedBoostLoopSound, duration, speedBoostVolume));
    }

    public void ActivateTimeStop(float duration, Sprite icon = null)
    {
        StartSmoothZoom(skillZoomSize, duration);
        StartCoroutine(TimeStopRoutine(duration));

        // 시간 정지 루프 효과음 재생 (지속 시간 동안)
        if (timeStopLoopSound != null && SoundManager.Instance != null)
            StartCoroutine(PlayLoopSoundRoutine(timeStopLoopSound, duration, timeStopVolume));
    }

    private IEnumerator TimeStopRoutine(float duration)
    {
        MonsterHealth[] healths = FindObjectsByType<MonsterHealth>(FindObjectsSortMode.None);
        BossMonster boss = FindFirstObjectByType<BossMonster>();
        List<Rigidbody2D> rbs = new List<Rigidbody2D>();
        List<Vector2> rbVelocities = new List<Vector2>();
        List<Animator> animators = new List<Animator>();
        List<RigidbodyType2D> originalBodyTypes = new List<RigidbodyType2D>();
        List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();

        void ProcessTarget(GameObject go)
        {
            if (go == null) return;
            MonoBehaviour[] allScripts = go.GetComponentsInChildren<MonoBehaviour>();
            foreach (var s in allScripts)
            {
                if (s == null || s is MonsterHealth || s is Animator || s is Renderer || s is Collider2D) continue;
                if (s.enabled) { s.enabled = false; disabledScripts.Add(s); }
            }
            if (go.TryGetComponent(out Rigidbody2D rb)) { rbs.Add(rb); rbVelocities.Add(rb.linearVelocity); originalBodyTypes.Add(rb.bodyType); rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Kinematic; }
            Animator anim = go.GetComponentInChildren<Animator>(); if (anim != null) { animators.Add(anim); anim.speed = 1.0f; anim.Play("Stun", 0, 0f); }
        }

        foreach (var h in healths) ProcessTarget(h.gameObject);
        if (boss != null) ProcessTarget(boss.gameObject);
        yield return new WaitForSeconds(duration);
        foreach (var s in disabledScripts) if (s != null) s.enabled = true;
        for (int i = 0; i < rbs.Count; i++) if (rbs[i] != null) { rbs[i].bodyType = originalBodyTypes[i]; rbs[i].linearVelocity = rbVelocities[i]; }
        foreach (var anim in animators) if (anim != null) { anim.speed = 1f; anim.Play("Idle"); }
    }

    public void StartSmoothZoom(float targetSize, float maintainDuration)
    {
        if (virtualCamera == null) return;
        if (useHardLimit && backgroundBoundary != null)
        {
            float maxSafeSize = backgroundBoundary.bounds.size.y / 2f;
            targetSize = Mathf.Min(targetSize, maxSafeSize * 0.98f); 
        }
        if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
        zoomCoroutine = StartCoroutine(SmoothZoomRoutine(targetSize, maintainDuration));
    }

    private IEnumerator SmoothZoomRoutine(float targetSize, float maintainDuration)
    {
        float startSize = virtualCamera.Lens.OrthographicSize;
        float elapsed = 0;
        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            virtualCamera.Lens.OrthographicSize = Mathf.Lerp(startSize, targetSize, elapsed / zoomDuration);
            yield return null;
        }
        virtualCamera.Lens.OrthographicSize = targetSize;
        yield return new WaitForSeconds(maintainDuration);
        elapsed = 0;
        float restoreDur = zoomDuration * 1.5f;
        while (elapsed < restoreDur)
        {
            elapsed += Time.deltaTime;
            virtualCamera.Lens.OrthographicSize = Mathf.Lerp(targetSize, originalZoomSize, elapsed / restoreDur);
            yield return null;
        }
        virtualCamera.Lens.OrthographicSize = originalZoomSize;
    }

    private bool IsInExtendedView(Vector3 position)
    {
        Camera cam = Camera.main; if (cam == null) return false;
        Vector3 viewportPos = cam.WorldToViewportPoint(position);
        return viewportPos.z > 0 && viewportPos.x > -viewportMargin && viewportPos.x < 1 + viewportMargin && viewportPos.y > -viewportMargin && viewportPos.y < 1 + viewportMargin;
    }

    private IEnumerator FlashRoutine()
    {
        if (screenFlashUI == null) yield break;
        screenFlashUI.gameObject.SetActive(true);
        float elapsed = 0;
        while (elapsed < flashDuration) { elapsed += Time.deltaTime; float alpha = Mathf.Lerp(1, 0, elapsed / flashDuration); screenFlashUI.color = new Color(1, 1, 1, alpha); yield return null; }
        screenFlashUI.gameObject.SetActive(false);
    }

    /// 지정된 시간 동안 AudioSource를 생성하여 루프 사운드를 재생하고, 시간 종료 시 페이드 아웃 후 파괴한다.
    /// </summary>
    private IEnumerator PlayLoopSoundRoutine(AudioClip clip, float duration, float volume = 0.7f)
    {
        if (clip == null || SoundManager.Instance == null) yield break;

        // 임시 AudioSource 생성
        GameObject sfxObj = new GameObject("LoopSFX_" + clip.name);
        sfxObj.transform.SetParent(transform);
        AudioSource source = sfxObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.volume = volume;
        source.Play();

        yield return new WaitForSeconds(duration - 0.5f);

        // 페이드 아웃 (0.5초)
        float fadeTime = 0.5f;
        float startVol = source.volume;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            if (source != null) source.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeTime);
            yield return null;
        }

        if (sfxObj != null) Destroy(sfxObj);
    }

    /// <summary>
    /// 무적 중 몬스터를 날려보낼 때 호출하는 킬 효과음 재생.
    /// PlayerInvincibility에서 호출한다.
    /// </summary>
    public void PlayInvincKillSound()
    {
        if (invincKillSound != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(invincKillSound, invincKillVolume);
    }
}
