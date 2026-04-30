using UnityEngine;

// ──────────────────────────────────────────────
//  아이템 종류 열거형
// ──────────────────────────────────────────────
public enum ItemType
{
    HealthPotion,   // 체력 회복약 → 즉시 사용
    GaugePotion,    // 게이지 회복약 → 즉시 사용
    KeyItem,        // 퀘스트 키 아이템 → 인벤토리 보관
    AOE_Skill,      // 전체 공격 스킬 → 습득 시 즉시 발동
    Invincible_Skill, // 10초 무적 모드 → 습득 시 즉시 발동
    SpeedPotion,
    TimeStop_Skill  // 시간 정지 스킬 → 몬스터 이동 n초 정지
}

// ──────────────────────────────────────────────
//  ItemData : ScriptableObject
//  Project 창에서 우클릭 → Create → Items → Item Data 로 생성
// ──────────────────────────────────────────────
[CreateAssetMenu(fileName = "NewItem", menuName = "Items/Item Data", order = 0)]
public class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    public string itemName = "아이템";
    [TextArea] public string description = "";
    public Sprite icon;         // 인벤토리 UI에 표시될 아이콘
    public ItemType itemType;

    [Header("회복량 (회복약 전용)")]
    public int healAmount = 1;  // HealthPotion: HP 회복량 / GaugePotion: 게이지 회복량

    [Header("지속 효과 설정 (물약/스킬 전용)")]
    public float duration = 5f;     // 지속 시간
    public float multiplier = 1.5f;  // 효과 배율 (속도 등)

    [Header("범위 설정 (AOE 스킬 전용)")]
    public float aoeRange = 0f;      // 0이면 무제한(화면 전체), 0보다 크면 캐릭터 중심 반경 폭발
}
