using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 획득한 키 아이템을 보관하고 관리하는 클래스.
/// ExitPoint 상호작용 시 보유 아이템을 확인하는 데 사용되며,
/// OnInventoryChanged 이벤트를 통해 UI와 실시간으로 동기화된다.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    // 보유 중인 키 아이템 목록
    private List<ItemData> keyItems = new List<ItemData>();

    // 인벤토리 변경 시 호출되는 이벤트 (UI 갱신용)
    public event System.Action<List<ItemData>> OnInventoryChanged;

    /// <summary>
    /// 새로운 키 아이템을 목록에 추가함.
    /// </summary>
    /// <param name="item">추가할 아이템 데이터</param>
    public void AddKeyItem(ItemData item)
    {
        if (item == null) return;

        keyItems.Add(item);
        Debug.Log($"<color=green>[인벤토리] '{item.itemName}' 추가 (보유: {keyItems.Count})</color>");
        OnInventoryChanged?.Invoke(keyItems);
    }

    /// <summary>
    /// 특정 키 아이템을 목록에서 제거함.
    /// </summary>
    /// <param name="item">제거할 아이템 데이터</param>
    /// <returns>제거 성공 여부</returns>
    public bool RemoveKeyItem(ItemData item)
    {
        if (item == null) return false;

        bool removed = keyItems.Remove(item);
        if (removed)
        {
            Debug.Log($"<color=orange>[인벤토리] '{item.itemName}' 제거</color>");
            OnInventoryChanged?.Invoke(keyItems);
        }
        return removed;
    }

    /// <summary>
    /// 특정 아이템의 보유 여부를 반환함.
    /// </summary>
    public bool HasItem(ItemData item) => item != null && keyItems.Contains(item);

    /// <summary>
    /// 현재 보유한 모든 키 아이템 목록을 읽기 전용으로 반환함.
    /// </summary>
    public IReadOnlyList<ItemData> GetKeyItems() => keyItems.AsReadOnly();
}

