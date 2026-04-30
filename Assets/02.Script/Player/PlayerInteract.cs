using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어의 입력(F키 등)을 수신하여 주변 오브젝트와 상호작용을 수행하는 클래스.
/// 우선순위: 탈출구(ExitPoint) → 아군 희생(AllySacrifice) → 조명 스위치(LightSwitch)
/// → 보스 기믹(BossSacrifice) → 몬스터 포탈(MonsterPortal)
/// </summary>
public class PlayerInteract : MonoBehaviour
{
    /// <summary>
    /// 입력 시스템으로부터 상호작용 액션을 수신함.
    /// </summary>
    /// <param name="value">입력 값 데이터</param>
    public void OnInteract(InputValue value)
    {
        if (value == null || !value.isPressed) return;

        Debug.Log("<color=cyan>[PlayerInteract] 상호작용 입력 수신</color>");

        // 1. 탈출구(ExitPoint) 상호작용 체크
        ExitPoint exit = ExitPoint.GetNearbyExit();
        if (exit != null)
        {
            if (TryGetComponent(out PlayerInventory inventory))
            {
                exit.Interact(inventory);
            }
            return; 
        }

        // 2. 아군 희생(AllySacrifice) 상호작용 체크
        AllySacrifice ally = AllySacrifice.GetNearby();
        if (ally != null)
        {
            ally.Interact();
            return;
        }

        // 3. 조명 스위치 상호작용 체크
        LightSwitchObject lightSwitch = LightSwitchObject.GetNearbyObject();
        if (lightSwitch != null)
        {
            lightSwitch.Interact();
            return;
        }

        // 4. 보스 기믹 상호작용 체크
        BossSacrificeObject bossSacrifice = BossSacrificeObject.GetNearby();
        if (bossSacrifice != null)
        {
            bossSacrifice.Interact();
            return;
        }

        // 5. 몬스터 포탈 (소환 종료 후 파괴) 체크
        MonsterPortal portal = MonsterPortal.GetNearby(transform.position);
        if (portal != null)
        {
            portal.Interact();
        }
    }
}

