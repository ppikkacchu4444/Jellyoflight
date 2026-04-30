using System.Collections.Generic;
using UnityEngine;
using TMPro;

// ──────────────────────────────────────────────
//  ExitKeyUI
//  "열쇠 n / m" 형태로 키 아이템 수집 현황을 표시합니다.
//
//  [에디터 설정]
//  Canvas 아래 빈 GameObject에 붙이고,
//  keyCountText : "열쇠 0 / 3" 을 표시할 TextMeshPro 오브젝트 연결
// ──────────────────────────────────────────────
public class ExitKeyUI : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private TextMeshProUGUI keyCountText;

    [Header("플레이어 (비워두면 자동 탐색)")]
    [SerializeField] private PlayerInventory playerInventory;

    private int requiredCount = 0;

    private void Start()
    {
        // StageManager에서 필요 수량 가져오기
        if (StageManager.Instance != null)
            requiredCount = StageManager.Instance.RequiredKeyCount;

        // PlayerInventory 자동 탐색
        if (playerInventory == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerInventory = player.GetComponent<PlayerInventory>();
        }

        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged += OnInventoryChanged;
            RefreshText(playerInventory.GetKeyItems().Count);
        }
        else
        {
            Debug.LogWarning("[ExitKeyUI] PlayerInventory를 찾을 수 없습니다.");
            RefreshText(0);
        }
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
            playerInventory.OnInventoryChanged -= OnInventoryChanged;
    }

    // ── 인벤토리 변경 시 호출 ─────────────────
    private void OnInventoryChanged(List<ItemData> items)
    {
        // 키 아이템만 카운트
        int keyCount = 0;
        foreach (var item in items)
            if (item.itemType == ItemType.KeyItem)
                keyCount++;

        RefreshText(keyCount);
    }

    // ── 텍스트 갱신 ───────────────────────────
    private void RefreshText(int current)
    {
        if (keyCountText == null) return;

        keyCountText.text = $"{current} / {requiredCount}";

        // 다 모았으면 강조 색 변경
        if (current >= requiredCount && requiredCount > 0)
            keyCountText.color = Color.yellow;
        else
            keyCountText.color = Color.white;
    }
}
