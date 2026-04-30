using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal; // Light2D 제어
using TMPro; // UI 제어
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class LightSwitchObject : MonoBehaviour
{
    [Header("조명 설정")]
    public Light2D targetAreaLight; 
    [SerializeField] private float fadeSpeed = 1.5f;     // 밝아지는 속도
    [SerializeField] private float targetIntensity = 1.0f; // 최종 목표 밝기

    [Header("UI — TextMeshPro 연결")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Image promptIcon;              // 프롬프트 옆에 표시할 아이콘 (Image 컴포넌트 연결)
    [SerializeField] private string interactMessage = "[ F ]  장치 가동";
    [SerializeField] private Color promptColor = Color.white;

    [Header("순서 설정")]
    [SerializeField] private LightSwitchObject requiredPreviousSwitch; // 이 장치를 켜기 위해 먼저 켜져야 하는 장치
    [SerializeField] private string lockedMessage = "이전 장치를 먼저 가동해야 합니다!";
    [SerializeField] private Color lockedColor = new Color(1f, 0.35f, 0.35f);

    [Header("기믹 설정 (옵션)")]
    [SerializeField] private GameObject[] barriersToDestroy; // 스위치를 켜면 파괴될 배리어들
    [SerializeField] private GameObject[] monstersToSpawn;   // 스위치를 켜면 나타날 몬스터들 (맵에 미리 배치)
    [SerializeField] private GameObject[] platformsToSpawn;  // 스위치를 켜면 나타날 발판들 (맵에 미리 배치)

    [Header("비주얼 설정")]
    [SerializeField] private Sprite switchOffSprite; // 꺼진 상태 이미지
    [SerializeField] private Sprite switchOnSprite;  // 켜진 상태 이미지

    [Header("사운드 설정")]
    [SerializeField] private AudioClip switchOnSound;
    [SerializeField] private AudioClip lockedSound;

    private static LightSwitchObject _currentNearbyObject;
    private bool isActivated = false;
    private SpriteRenderer sr;
    
    public bool IsActivated => isActivated;

    private void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null && switchOffSprite != null)
        {
            sr.sprite = switchOffSprite;
        }

        HidePrompt();
        
        // 조명 초기 상태 설정: 꺼진 상태로 시작
        if (targetAreaLight != null)
        {
            targetAreaLight.intensity = 0f;
            targetAreaLight.gameObject.SetActive(false);
        }

        // 스위치에 연결된 몬스터와 발판들은 시작할 때 자동으로 숨김 처리
        if (monstersToSpawn != null)
        {
            foreach (var monster in monstersToSpawn)
            {
                if (monster != null) monster.SetActive(false);
            }
        }

        if (platformsToSpawn != null)
        {
            foreach (var platform in platformsToSpawn)
            {
                if (platform != null) platform.SetActive(false);
            }
        }
    }

    void Update()
    {
        // 상호작용 범위를 체크하는 로직 (ExitPoint와 동일한 방식)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && !isActivated)
        {
            float dist = Vector2.Distance(transform.position, player.transform.position);
            
            // Gizmos로 설정한 detectionRadius 거리 내에 있는지 확인
            if (dist <= 2.0f) // 거리 값은 필요에 따라 조절하세요
            {
                if (_currentNearbyObject != this)
                {
                    _currentNearbyObject = this;
                    ShowPrompt();
                }
            }
            else if (_currentNearbyObject == this) 
            {
                _currentNearbyObject = null;
                HidePrompt();
            }
        }
    }

    public static LightSwitchObject GetNearbyObject() => _currentNearbyObject;

    // 플레이어가 F키를 눌렀을 때 실행
    public void Interact()
    {
        if (isActivated || targetAreaLight == null) return;

        // 1. 이전 스위치가 켜졌는지 확인 (순서 제어 로직)
        if (requiredPreviousSwitch != null && !requiredPreviousSwitch.IsActivated)
        {
            ShowPromptMessage(lockedMessage, lockedColor);
            CancelInvoke(nameof(RestoreReadyPrompt));
            Invoke(nameof(RestoreReadyPrompt), 1.5f);
            
            // 잠김 효과음 재생
            if (lockedSound != null) SoundManager.Instance.PlaySFX(lockedSound);

            Debug.Log("<color=red>[LightSwitchObject] 순서를 지키지 않았습니다! 이전 장치를 켜주세요.</color>");
            return;
        }

        // 2. 조건 만족시 가동
        isActivated = true;
        
        // 가동 효과음 재생
        if (switchOnSound != null) SoundManager.Instance.PlaySFX(switchOnSound);
        
        // [추가] 스프라이트 교체
        if (sr != null && switchOnSprite != null)
        {
            sr.sprite = switchOnSprite;
        }

        _currentNearbyObject = null;
        HidePrompt();

        // 3. 기믹 발동: 배리어 파괴 및 몬스터 소환
        if (barriersToDestroy != null)
        {
            foreach (var barrier in barriersToDestroy)
            {
                if (barrier != null) Destroy(barrier);
            }
        }

        if (monstersToSpawn != null)
        {
            foreach (var monster in monstersToSpawn)
            {
                // 소환 이펙트 등이 있다면 여기서 추가 가능
                if (monster != null) monster.SetActive(true);
            }
        }

        if (platformsToSpawn != null)
        {
            foreach (var platform in platformsToSpawn)
            {
                if (platform != null) platform.SetActive(true);
            }
        }

        // 조명 스르륵 켜기 시작
        StartCoroutine(FadeInLight());

        Debug.Log("<color=lime>[LightSwitchObject] 구역 조명 페이드 인 시작!</color>");
        
        // 상호작용 후 트리거 비활성화
        if (TryGetComponent<Collider2D>(out var col)) col.enabled = false;
    }

    // ── 조명 페이드 인 코루틴 ──────────────────────
    private IEnumerator FadeInLight()
    {
        targetAreaLight.gameObject.SetActive(true);
        float current = 0;

        while (current < targetIntensity)
        {
            current += Time.deltaTime * fadeSpeed;
            targetAreaLight.intensity = current;
            yield return null;
        }

        targetAreaLight.intensity = targetIntensity;
        this.enabled = false; // 업데이트 중지
    }

    // ── UI 프롬프트 로직 (ExitPoint 방식 이식) ────
    private void ShowPromptMessage(string message, Color color)
    {
        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = message;
            promptText.color = color;
        }
        if (promptIcon != null) promptIcon.gameObject.SetActive(true);
    }

    private void ShowPrompt()
    {
        ShowPromptMessage(interactMessage, promptColor);
    }

    private void RestoreReadyPrompt()
    {
        if (_currentNearbyObject == this)
            ShowPrompt();
    }

    private void HidePrompt()
    {
        if (promptText != null) promptText.gameObject.SetActive(false);
        if (promptIcon != null) promptIcon.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (_currentNearbyObject == this)
        {
            _currentNearbyObject = null;
            HidePrompt();
        }
    }
}