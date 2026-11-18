using UnityEngine;

/// <summary>
/// 挂在 tiggerplace 上（Collider2D 需勾 Is Trigger）。
/// 玩家在区域内按 Return：
///   背包有"微光碎片4" → 扣除 1 个，dark 失活、warm 激活，并永久锁定。
/// </summary>
public class Shard4SwitchToWarm : MonoBehaviour
{
    [Header("背包条件")]
    [Tooltip("必须和 AddItem 时的 itemId 完全一致")]
    [SerializeField] private string requiredItemId = "shard_glow_4";
    [Tooltip("仅用于日志显示")]
    [SerializeField] private string requiredItemName = "微光碎片4";
    [SerializeField] private int requiredAmount = 1;

    [Header("灯光物体")]
    [Tooltip("拖 Dark_warm_lamp/dark")]
    [SerializeField] private GameObject darkObject;
    [Tooltip("拖 Dark_warm_lamp/warm")]
    [SerializeField] private GameObject warmObject;

    [Header("按键")]
    [SerializeField] private KeyCode confirmKey = KeyCode.Return;
    [SerializeField] private KeyCode confirmKeyAlt = KeyCode.KeypadEnter;

    private bool _playerInside;
    private bool _done;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInside = false;
    }

    private void Update()
    {
        if (!_playerInside) return;
        if (_done) return;                 // 已经切换过，彻底不再响应

        if (Input.GetKeyDown(confirmKey) || Input.GetKeyDown(confirmKeyAlt))
        {
            TrySwitch();
        }
    }

    private void TrySwitch()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[碎片4] InventoryManager.Instance 为空，无法检测道具");
            return;
        }

        // 先查
        if (!InventoryManager.Instance.HasItem(requiredItemId, requiredAmount))
        {
            Debug.Log($"[碎片4] 缺少 {requiredItemName} ×{requiredAmount}，无法交互");
            return;
        }

        // 再扣
        bool removed = InventoryManager.Instance.RemoveItem(requiredItemId, requiredAmount);
        if (!removed)
        {
            Debug.LogWarning("[碎片4] 道具扣除失败");
            return;
        }

        // 扣成功后才切状态，且只切一次
        _done = true;

        if (darkObject != null) darkObject.SetActive(false);
        if (warmObject != null) warmObject.SetActive(true);

        Debug.Log($"[碎片4] 已消耗 {requiredItemName} ×{requiredAmount}，dark 关闭、warm 开启");
    }
}
