using UnityEngine;
using UnityEngine.UI;

// ──────────────────────────────────────────────
//  BossHpUI  —  LateUpdate 폴링 방식으로 보스 HP를 자동 반영
//  1) 캔버스에 직접 UI를 만들고, 이 스크립트에서 참조합니다.
// ──────────────────────────────────────────────
public class BossHpUI : MonoBehaviour
{
    [Header("UI 연결 (직접 캔버스에 만든 UI들 연결)")]
    [Tooltip("캔버스에 미리 배치해 둔 하트 이미지들을 순서대로 넣어주세요. (Player 방식과 동일)")]
    [SerializeField] private Image[] heartImages;

    [Header("하트 상태 스프라이트")]
    [SerializeField] private Sprite filledHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;

    [Header("보스가 프리팹이라면 자동 탐색용 (체크 시 씬에서 찾음)")]
    [SerializeField] private bool searchBossInScene = true;

    private BossMonster bossMonster;
    private int maxHp;
    private int lastDisplayedHp = -1;

    private void Start()
    {
        // 1. 보스 몬스터 찾기
        bossMonster = GetComponent<BossMonster>();
        if (bossMonster == null && searchBossInScene)
        {
            // 이 스크립트를 보스가 아닌 '캔버스의 UI' 자체에 붙였을 경우를 대비한 자동 탐색
            bossMonster = FindFirstObjectByType<BossMonster>();
        }

        if (bossMonster != null)
        {
            maxHp = bossMonster.MaxHp;
            lastDisplayedHp = -1;
        }

        // 초기 시작 전 모든 하트를 일단 끕니다. (보스가 활성화될 때 켜지게)
        if (heartImages != null)
        {
            foreach (var img in heartImages)
            {
                if (img != null) img.gameObject.SetActive(false);
            }
        }
    }

    private void LateUpdate()
    {
        // 보스가 없거나, 파괴되었으면 UI 숨김 처리
        if (bossMonster == null)
        {
            if (searchBossInScene) 
            {
                bossMonster = FindFirstObjectByType<BossMonster>(); // 주기적으로 탐색 (보스가 나중에 스폰되는 경우)
            }
            
            if (bossMonster == null) return;
        }

        if (heartImages == null || heartImages.Length == 0) return;

        // HP가 변경됐을 때만 UI 갱신 (폴링)
        int current = bossMonster.CurrentHp;
        if (current != lastDisplayedHp)
        {
            RefreshHearts(current);
            lastDisplayedHp = current;
        }
    }

    private void RefreshHearts(int currentHp)
    {
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null) continue;
            
            bool isFilled = (i < currentHp);
            
            // 보스의 최대 체력보다 인덱스가 크면 아예 꺼버림 (UI 이미지가 10개인데 보스 체력이 5개인 경우 방지)
            if (i >= bossMonster.MaxHp)
            {
                heartImages[i].gameObject.SetActive(false);
                continue;
            }

            // 하트 UI 이미지가 있다면 표시 처리
            if (emptyHeartSprite != null)
            {
                heartImages[i].sprite = isFilled ? filledHeartSprite : emptyHeartSprite;
                heartImages[i].gameObject.SetActive(true);
            }
            else
            {
                heartImages[i].gameObject.SetActive(isFilled);
            }
        }
    }

    // 보스 사망 시
    public void OnBossDestroyed()
    {
        bossMonster = null; 
        
        // 보스가 죽으면 모든 하트 UI 끄기
        if (heartImages != null)
        {
            foreach (var img in heartImages)
            {
                if (img != null) img.gameObject.SetActive(false);
            }
        }
        Debug.Log("<color=lime>[BossHpUI] 보스 사망 — 수동 배치 UI 숨김 처리 완료</color>");
    }

    // Initialize 호환성 유지용 (LateUpdate로 자동화되어 있으므로 구현 불필요)
    public void Initialize(int totalHp) { }
    public void UpdateDisplay(int currentHp) { }
}
