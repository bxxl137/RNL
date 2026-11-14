using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂在 letterbox/tiggerplace2 上。
/// 玩家进入触发器后按 Enter，若背包中“微光碎片”数量 >= 1：
///   打开失活、关闭激活，并发通知（供下游触发器解锁场景切换）。
/// </summary>
public class LetterboxLock : MonoBehaviour
{
    [Header("视觉状态")]
    [SerializeField] private GameObject openedVisual;   // 拖 letterbox/opened
    [SerializeField] private GameObject closedVisual;   // 拖 letterbox/closed

    [Header("背包条件")]
    [Tooltip("必须填物品 ID（itemId），不是显示名。对应 AddItem 的第一个参数")]
    [SerializeField] private string requiredItemId = "item_01";
    [SerializeField] private int requiredCount = 1;
    [SerializeField] private bool consumeItem = true;

    [Header("交互按键")]
    [SerializeField] private KeyCode confirmKey = KeyCode.Return;
    [SerializeField] private KeyCode confirmKeyAlt = KeyCode.KeypadEnter;

    [Header("通知参数")]
    [SerializeField] private string noticeKey = "letterbox_closed";

    private bool _playerInside;
    private bool _done;

    private void Awake()
    {
        if (openedVisual != null) openedVisual.SetActive(true);
        if (closedVisual != null) closedVisual.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_done) return;
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
        if (_done || !_playerInside) return;

        if (Input.GetKeyDown(confirmKey) || Input.GetKeyDown(confirmKeyAlt))
        {
            TryClose();
        }
    }

    private void TryClose()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[LetterboxLockOpener] InventoryManager 未初始化");
            return;
        }

        // ★ 关键修复：用 itemId 查询，不是 itemName
        if (!InventoryManager.Instance.HasItem(requiredItemId, requiredCount))
        {
            Debug.Log($"[信箱] 需要 {requiredCount} 个物品（ID = {requiredItemId}）才能关闭");
            return;
        }

        if (consumeItem)
        {
            InventoryManager.Instance.RemoveItem(requiredItemId, requiredCount);
        }

        ApplyClosedState();
    }

    private void ApplyClosedState()
    {
        _done = true;
        _playerInside = false;

        if (openedVisual != null) openedVisual.SetActive(false);
        if (closedVisual != null) closedVisual.SetActive(true);

        TouchNoticeBus.Publish(new TouchNotice(noticeKey, gameObject, null));
    }
}
