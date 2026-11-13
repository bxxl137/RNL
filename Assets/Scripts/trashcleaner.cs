using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TouchDisappearNotifier : MonoBehaviour
{
    public static event Action<TouchDisappearNotifier, GameObject> OnTouched;

    [Header("触发条件")]
    [SerializeField] private string toucherTag = "Player";

    [Header("交互按键")]
    [SerializeField] private KeyCode confirmKey = KeyCode.Return;
    [SerializeField] private KeyCode confirmKeyAlt = KeyCode.KeypadEnter;

    [Header("消失设置")]
    [SerializeField] private bool destroySelf = true;
    [SerializeField] private float destroyDelay = 0f;

    [Header("通知参数")]
    [SerializeField] private string noticeKey = "object_touched";

    // ================= 【新增：物品数据】 =================
    [Header("掉落物品数据（对接背包）")]
    [SerializeField] private string itemId = "item_01";
    [SerializeField] private string itemName = "神秘物品";
    [SerializeField] private string iconName = "DefaultIcon";
    [SerializeField] private int itemAmount = 1;
    // =======================================================

    private GameObject _toucher;
    private bool _fired;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_fired) return;
        if (!MatchTag(collision.collider)) return;

        _toucher = collision.gameObject;
        ShowPrompt(true);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (_fired) return;
        if (collision.gameObject != _toucher) return;

        _toucher = null;
        ShowPrompt(false);
    }

    private void Update()
    {
        if (_fired || _toucher == null) return;

        if (Input.GetKeyDown(confirmKey) || Input.GetKeyDown(confirmKeyAlt))
        {
            Confirm();
        }
    }

    private bool MatchTag(Component other)
    {
        if (string.IsNullOrEmpty(toucherTag)) return true;
        return other.CompareTag(toucherTag);
    }

    private void Confirm()
    {
        if (_fired) return;
        _fired = true;

        ShowPrompt(false);

        // ================= 【对接背包】 =================
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(itemId, itemName, iconName, itemAmount);
        }
        else
        {
            Debug.LogWarning("背包系统未初始化，找不到 InventoryManager！");
        }
        // ================================================

        OnTouched?.Invoke(this, _toucher);
        TouchNoticeBus.Publish(new TouchNotice(noticeKey, gameObject, _toucher));

        if (destroySelf)
        {
            if (destroyDelay > 0f) Destroy(gameObject, destroyDelay);
            else Destroy(gameObject);
        }
    }

    private void ShowPrompt(bool show)
    {
        if (show) Debug.Log("按 Enter 拾取物品");
        else Debug.Log("离开了物品");
    }
}