using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ──────────────────────────────────────────────
//  StageKeyUI
//  탈출에 필요한 키 아이템 아이콘 하나와 
//  현재 수집 현황(n / m)을 표시합니다.
// ──────────────────────────────────────────────
public class StageKeyUI : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private Image keyIconDisplay;       // 키 아이콘 이미지
    [SerializeField] private TextMeshProUGUI countText;   // 수량 텍스트 (예: 0 / 3)

    [Header("설정")]
    [SerializeField] private ItemData keyItemData;       // 아이콘을 가져올 키 아이템 데이터
    [SerializeField] private Color activeColor = Color.yellow; // 다 모았을 때 색상
    
    private PlayerInventory playerInventory;
    private int requiredCount = 0;

    private void Start()
    {
        // 1. 필요 수량 가져오기 (StageManager)
        if (StageManager.Instance != null)
            requiredCount = StageManager.Instance.RequiredKeyCount;

        // 2. 아이콘 기본 설정
        if (keyIconDisplay != null && keyItemData != null)
            keyIconDisplay.sprite = keyItemData.icon;

        // 3. 인벤토리 연결
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerInventory = player.GetComponent<PlayerInventory>();
            playerInventory.OnInventoryChanged += UpdateUI;
        }

        // 초기 상태 표시
        RefreshDisplay(0);
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
            playerInventory.OnInventoryChanged -= UpdateUI;
    }

    // 인벤토리 변경 시 호출되는 콜백
    private void UpdateUI(List<ItemData> items)
    {
        int currentCount = 0;
        foreach (var item in items)
        {
            if (item.itemType == ItemType.KeyItem)
                currentCount++;
        }

        RefreshDisplay(currentCount);
    }

    // 실제 화면 갱신
    private void RefreshDisplay(int current)
    {
        if (countText != null)
        {
            countText.text = $"{current}/{requiredCount}";
            
            // 다 모았으면 노란색으로 강조
            countText.color = (current >= requiredCount && requiredCount > 0) ? activeColor : Color.white;
        }
    }
}
