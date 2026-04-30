using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ──────────────────────────────────────────────
//  KeyItemUI
//  보유 중인 키 아이템을 화면에 표시하는 UI 스크립트.
//  Canvas 아래의 빈 GameObject에 붙이세요.
//
//  [Inspector 설정]
//  ├ playerInventory : 플레이어의 PlayerInventory 컴포넌트
//  ├ itemSlotPrefab  : 아이템 슬롯 프리팹 (Image + Text로 구성)
//  └ slotParent      : 슬롯들이 들어갈 부모 Transform (HorizontalLayoutGroup 권장)
// ──────────────────────────────────────────────
public class KeyItemUI : MonoBehaviour
{
    [Header("연결 대상")]
    [SerializeField] private PlayerInventory playerInventory;

    [Header("UI 구성")]
    [SerializeField] private GameObject itemSlotPrefab; // 아이템 슬롯 프리팹
    [SerializeField] private Transform slotParent;      // 슬롯들의 부모 (HorizontalLayoutGroup 등)

    // 현재 생성된 슬롯 목록 (갱신 시 재활용)
    private List<GameObject> activeSlots = new List<GameObject>();

    void Start()
    {
        if (playerInventory == null)
        {
            // Player 태그로 자동 탐색
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerInventory = player.GetComponent<PlayerInventory>();
        }

        if (playerInventory != null)
        {
            // 인벤토리가 바뀔 때마다 UI 갱신
            playerInventory.OnInventoryChanged += RefreshUI;
            // 시작 시 한 번 갱신
            RefreshUI(new List<ItemData>(playerInventory.GetKeyItems()));
        }
        else
        {
            Debug.LogWarning("[KeyItemUI] PlayerInventory를 찾을 수 없습니다.");
        }
    }

    void OnDestroy()
    {
        if (playerInventory != null)
            playerInventory.OnInventoryChanged -= RefreshUI;
    }

    // ── UI 전체 갱신 ──────────────────────────
    private void RefreshUI(List<ItemData> items)
    {
        // 기존 슬롯 제거
        foreach (var slot in activeSlots)
            Destroy(slot);
        activeSlots.Clear();

        // 보유 아이템 슬롯 생성
        foreach (var item in items)
        {
            if (itemSlotPrefab == null || slotParent == null) break;

            GameObject slot = Instantiate(itemSlotPrefab, slotParent);
            activeSlots.Add(slot);

            // 아이콘 설정
            Image icon = slot.GetComponentInChildren<Image>();
            if (icon != null && item.icon != null)
                icon.sprite = item.icon;

            // 이름 텍스트 설정 (TextMeshPro)
            TextMeshProUGUI label = slot.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = item.itemName;
        }
    }

    // ── 외부에서 직접 갱신 트리거 (선택적) ───
    public void ForceRefresh()
    {
        if (playerInventory != null)
            RefreshUI(new List<ItemData>(playerInventory.GetKeyItems()));
    }
}
