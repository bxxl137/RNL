using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// 挂在带 Sprite + Collider2D 的静态物体上（Collider 不勾 Is Trigger）。
/// 玩家碰撞到它 → 标记可交互 → 按 Enter → 物体消失并发全局通知。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TouchDisappearNotifier : MonoBehaviour
{
    public static event Action<TouchDisappearNotifier, GameObject> OnTouched;

    [Header("触发条件")]
    [SerializeField] private string toucherTag = "Player";

    [Header("交互按键")]
    [SerializeField] private KeyCode confirmKey = KeyCode.Return;   // 主键盘 Enter
    [SerializeField] private KeyCode confirmKeyAlt = KeyCode.KeypadEnter; // 小键盘 Enter

    [Header("消失设置")]
    [SerializeField] private bool destroySelf = true;
    [SerializeField] private float destroyDelay = 0f;

    [Header("通知参数")]
    [SerializeField] private string noticeKey = "object_touched";

    // 当前是否有玩家贴着
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

        // 全局通知
        OnTouched?.Invoke(this, _toucher);
        TouchNoticeBus.Publish(new TouchNotice(noticeKey, gameObject, _toucher));

        if (destroySelf)
        {
            if (destroyDelay > 0f) Destroy(gameObject, destroyDelay);
            else Destroy(gameObject);
        }
    }

    // ================= 提示对接区（留空，对接你搭档的系统）=================
    private void ShowPrompt(bool show)
    {
        // TODO: 调用搭档的提示 UI，例如：
        // if (show) PromptUI.Instance.Show("按 Enter 拾取");
        // else      PromptUI.Instance.Hide();
    }
    // =======================================================================
}
