using UnityEngine;

// ──────────────────────────────────────────────
//  BuffUIManager (Singleton)
//  버프 UI 프리팹을 생성하고 관리합니다.
// ──────────────────────────────────────────────
public class BuffUIManager : MonoBehaviour
{
    public static BuffUIManager Instance { get; private set; }

    [SerializeField] private GameObject buffPrefab; // BuffTimerUI 프리팹
    [SerializeField] private Transform container;    // UI들이 배치될 부모(LayoutGroup 권장)

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddBuff(Sprite icon, float duration)
    {
        if (buffPrefab == null || container == null) return;

        GameObject newBuff = Instantiate(buffPrefab, container);
        BuffTimerUI timer = newBuff.GetComponent<BuffTimerUI>();
        
        if (timer != null)
        {
            timer.Setup(icon, duration);
        }
    }
}
