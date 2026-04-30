using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 간 이동, 타임어택 및 스테이지 전체 상태를 관리하는 싱글톤 클래스.
/// </summary>
public class StageManager : MonoBehaviour
{
    private static StageManager instance;
    public static StageManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<StageManager>();
            }
            return instance;
        }
    }

    [Header("스테이지 설정")]
    [SerializeField] private string nextSceneName = "Stage2";
    [SerializeField] private int requiredKeyCount = 3;

    [Header("타임어택 설정")]
    [SerializeField] private bool isTimeAttack = false;
    [SerializeField] private float timeLimit = 60f;
    [SerializeField] private string failSceneName = "Result";

    private float currentTime;
    private bool isStageCleared = false;

    // 외부 참조용 프로퍼티
    public int RequiredKeyCount => requiredKeyCount;
    public float CurrentTime => currentTime;
    public bool IsTimeAttack => isTimeAttack;

    // 대리자 정의
    public event System.Action OnStageClear;
    public event System.Action OnStageFail;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        if (isTimeAttack)
        {
            currentTime = timeLimit;
        }
    }

    private void Update()
    {
        if (isTimeAttack && !isStageCleared)
        {
            HandleTimeAttack();
        }
    }

    /// <summary>
    /// 남은 시간을 계산하고 0이 될 경우 실패 처리를 수행함.
    /// </summary>
    private void HandleTimeAttack()
    {
        currentTime = Mathf.Max(0, currentTime - Time.deltaTime);

        if (currentTime <= 0f)
        {
            FailStage();
        }
    }

    /// <summary>
    /// 씬 전환 없이 스테이지를 클리어 상태로만 변경함 (영상 재생 등 준비 시 사용)
    /// </summary>
    public void MarkAsCleared()
    {
        if (isStageCleared) return;
        isStageCleared = true;
        Debug.Log("<color=cyan>[StageManager] 스테이지 클리어 상태 확정 (실패 처리 차단)</color>");
    }

    /// <summary>
    /// 성공 상태로 변경하고 다음 씬으로 전환을 시작함.
    /// </summary>
    public void ClearStage()
    {
        MarkAsCleared();

        Debug.Log($"<color=yellow>=== 스테이지 클리어! (다음 씬: {nextSceneName}) ===</color>");
        OnStageClear?.Invoke();

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("[StageManager] 다음 씬 이름이 설정되지 않았습니다! (nextSceneName 빈값)");
            return;
        }

        // 전환 관리자를 통한 씬 이동
        if (SceneTransitionManager.Instance != null)
        {
            Debug.Log($"[StageManager] SceneTransitionManager를 통해 {nextSceneName}으로 이동합니다.");
            SceneTransitionManager.Instance.TransitionToScene(nextSceneName);
        }
        else
        {
            Debug.Log($"[StageManager] SceneManager를 통해 {nextSceneName}으로 직접 이동합니다.");
            SceneManager.LoadScene(nextSceneName);
        }
    }

    /// <summary>
    /// 실패 상태로 변경하고 결과 씬으로 전환함.
    /// </summary>
    public void FailStage()
    {
        if (isStageCleared) return;
        
        Debug.Log($"<color=red>=== 스테이지 실패! ===</color>");
        OnStageFail?.Invoke();

        // [추가] 재시도를 위해 현재 씬의 이름을 저장함
        PlayerPrefs.SetString("LastPlayedStage", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();

        if (string.IsNullOrEmpty(failSceneName))
        {
            Debug.LogWarning("[StageManager] 실패 결과 씬 이름이 설정되지 않음");
            return;
        }

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(failSceneName);
        }
        else
        {
            SceneManager.LoadScene(failSceneName);
        }
    }
}

