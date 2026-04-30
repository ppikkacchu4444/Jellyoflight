using UnityEngine;

// ──────────────────────────────────────────────
//  ItemPickup
//  씬에 배치할 아이템 오브젝트에 붙이는 컴포넌트.
//
//  [에디터 설정]
//  1. 이 컴포넌트와 함께 SpriteRenderer를 추가하고
//     Sprite 필드에 아이콘을 직접 할당하세요.
//  2. Collider2D를 추가하고 Is Trigger에 체크하세요.
//  3. Item Data에 ScriptableObject를 연결하세요.
// ──────────────────────────────────────────────
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    [Header("아이템 설정")]
    [SerializeField] private ItemData itemData;

    [Header("둥둥 뜨는 연출")]
    [SerializeField] private float floatAmplitude = 0.15f;
    [SerializeField] private float floatSpeed = 2f;

    [Header("획득 연출")]
    [SerializeField] private GameObject pickupEffect;
    [SerializeField] private float effectDestroyDelay = 2.0f;
    [SerializeField] private AudioClip pickupSound;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (itemData == null)
        {
            Debug.LogWarning($"[ItemPickup] {gameObject.name} 에 ItemData가 없습니다!");
            return;
        }

        switch (itemData.itemType)
        {
            case ItemType.HealthPotion:
                UseHealthPotion(other);
                break;

            case ItemType.GaugePotion:
                UseGaugePotion(other);
                break;

            case ItemType.KeyItem:
                AddToInventory(other);
                break;

            case ItemType.AOE_Skill:
                if (EffectManager.Instance != null)
                    EffectManager.Instance.ActivateAOE(other.transform.position, itemData.aoeRange);
                break;

            case ItemType.Invincible_Skill:
                if (EffectManager.Instance != null) 
                    EffectManager.Instance.ActivateInvincibility(other.gameObject, itemData.duration, itemData.icon);
                break;
                
            case ItemType.SpeedPotion:
                if (EffectManager.Instance != null) 
                    EffectManager.Instance.ActivateSpeedBoost(other.gameObject, itemData.duration, itemData.multiplier, itemData.icon);
                break;

            case ItemType.TimeStop_Skill:
                if (EffectManager.Instance != null)
                    EffectManager.Instance.ActivateTimeStop(itemData.duration, itemData.icon);
                break;
        }

        if (pickupEffect != null)
        {
            // Z축을 -0.5로 설정하여 플레이어나 배경보다 앞에 보이게 함
            Vector3 effectPos = new Vector3(transform.position.x, transform.position.y, -0.5f);
            GameObject effect = Instantiate(pickupEffect, effectPos, Quaternion.identity);
            
            // 모든 렌더러를 맨 앞으로 (SortingOrder 110)
            Renderer[] rs = effect.GetComponentsInChildren<Renderer>(true);
            foreach (var r in rs)
            {
                r.sortingOrder = 110;
            }

            Destroy(effect, effectDestroyDelay);
            Debug.Log($"<color=white>[ItemPickup] '{itemData.itemName}' 획득 이펙트 생성 완료!</color>");
        }

        Debug.Log($"<color=yellow>[아이템 획득] {itemData.itemName}</color>");

        // 아이템 획득 효과음 재생
        if (pickupSound != null)
        {
            SoundManager.Instance.PlaySFX(pickupSound);
        }

        Destroy(gameObject);
    }

    private void UseHealthPotion(Collider2D playerCollider)
    {
        PlayerHealth hp = playerCollider.GetComponent<PlayerHealth>();
        if (hp != null)
            hp.Heal(itemData.healAmount);
    }

    private void UseGaugePotion(Collider2D playerCollider)
    {
        Debug.Log($"<color=cyan>[게이지 회복] {itemData.healAmount} 회복!</color>");

        // PlayerGauge gauge = playerCollider.GetComponent<PlayerGauge>();
        // if (gauge != null) gauge.RecoverGauge(itemData.healAmount);
    }

    private void AddToInventory(Collider2D playerCollider)
    {
        PlayerInventory inventory = playerCollider.GetComponent<PlayerInventory>();
        if (inventory != null)
            inventory.AddKeyItem(itemData);
        else
            Debug.LogWarning("[ItemPickup] 플레이어에 PlayerInventory 컴포넌트가 없습니다.");
    }
}
