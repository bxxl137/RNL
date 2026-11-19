using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 挂在 tiggerplace7 上（Collider2D 需勾 Is Trigger）。
/// 玩家在触发区域内按 Return：检查是否有微光碎片4，满足才切换场景。
/// </summary>
public class Tofirst : MonoBehaviour
{
    public AudioClip clip;
    [Header("目标场景")]
    [Tooltip("场景名，必须与 Build Settings 里完全一致（大小写敏感）")]
    [SerializeField] private string targetSceneName = "NeibourScene-firstfloor";

    [Header("通关条件")]
    [Tooltip("必须和 AddItem 时的 itemId 完全一致（微光碎片4）")]
    [SerializeField] private string requiredItemId = "shard_glow_4";
    [Tooltip("仅用于日志显示")]
    [SerializeField] private string requiredItemName = "微光碎片4";
    [Tooltip("需要几个才能切换")]
    [SerializeField] private int requiredAmount = 1;

    [Header("按键")]
    [SerializeField] private KeyCode confirmKey = KeyCode.Return;
    [SerializeField] private KeyCode confirmKeyAlt = KeyCode.KeypadEnter;

    [Header("触发限制")]
    [Tooltip("勾选后只切一次，防止连按重复加载")]
    [SerializeField] private bool onlyOnce = true;

    private bool _playerInside;
    private bool _switching;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInside = true;
        Debug.Log("www");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInside = false;
    }

    private void Update()
    {
        if (!_playerInside) return;
        if (onlyOnce && _switching) return;

        if (Input.GetKeyDown(confirmKey) || Input.GetKeyDown(confirmKeyAlt))
        {
            SwitchScene();
        }
    }

    private void SwitchScene()
    {
        if (_switching) return;

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("[ReturnToScene] targetSceneName 为空，无法切换场景");
            return;
        }

        // 【新增】碎片4 门槛检查
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[ReturnToScene] InventoryManager.Instance 为空，无法检测道具");
            return;
        }

        if (!InventoryManager.Instance.HasItem(requiredItemId, requiredAmount))
        {
            Debug.Log($"[ReturnToScene] 缺少 {requiredItemName} ×{requiredAmount}，无法切换场景");
            return;
        }

        _switching = true;
        Debug.Log($"[ReturnToScene] 已确认拥有 {requiredItemName}，正在切换到场景：{targetSceneName}");

        // 用名字加载，比 buildIndex 更稳（buildIndex 会随 Build Settings 顺序变）
        AudioManager.Instance.PlaySFX(clip);
        SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
    }
}
