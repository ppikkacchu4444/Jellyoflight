using UnityEngine;
using System.Collections;

/// <summary>
/// 아이템 효과로 발동되는 이동 속도 버프를 관리하는 클래스.
/// PlayerMove의 스택형 버프 API(AddSpeedBuff/RemoveSpeedBuff)를 사용하여
/// 슬로우 스택과 충돌하지 않도록 설계되었다.
/// </summary>
public class PlayerSpeedEffect : MonoBehaviour
{
    private PlayerMove playerMove;                 // 속도 버프 적용/해제 대상
    private bool isSpeedBoostActive = false;       // 현재 속도 버프 활성 여부

    private void Awake()
    {
        playerMove = GetComponent<PlayerMove>();
    }

    public void StartSpeedBoost(float duration, float multiplier, Sprite icon = null)
    {
        // UI 표시
        if (BuffUIManager.Instance != null && icon != null)
            BuffUIManager.Instance.AddBuff(icon, duration);

        if (isSpeedBoostActive) StopAllCoroutines();
        StartCoroutine(SpeedBoostRoutine(duration, multiplier));
    }

    private IEnumerator SpeedBoostRoutine(float duration, float multiplier)
    {
        isSpeedBoostActive = true;
        
        if (playerMove != null) 
        {
            // ★ moveSpeed 직접 수정 대신 버프 API 사용 (슬로우 스택과 충돌 방지)
            playerMove.AddSpeedBuff(multiplier);
            
            Debug.Log($"<color=green>[Speed] 속도 {multiplier}배 증가!</color>");
            
            yield return new WaitForSeconds(duration);

            playerMove.RemoveSpeedBuff(multiplier);
            Debug.Log("<color=white>[Speed] 원래 속도로 복구</color>");
        }

        isSpeedBoostActive = false;
    }
}
