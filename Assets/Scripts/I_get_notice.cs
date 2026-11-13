using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂在会发出通知的物体的子物体上。
/// 收到该子物体发出的触摸通知后，让父物体失活。
/// </summary>
public class I_get_notice : MonoBehaviour
{
    private void OnEnable()
    {
        TouchDisappearNotifier.OnTouched += HandleTouched;
        Debug.Log($"[ParentDeactivateOnTouch] {name} 已订阅通知", this);
    }

    private void OnDisable()
    {
        TouchDisappearNotifier.OnTouched -= HandleTouched;
    }

    private void HandleTouched(TouchDisappearNotifier sender, GameObject toucher)
    {
        // 只响应“我自己这个子物体”发出的通知，避免别的物体消失时误伤
        
        Transform parent = transform.parent;
        if (parent == null)
        {
            Debug.LogWarning($"[ParentDeactivateOnTouch] {name} 没有父物体", this);
            return;
        }

        parent.gameObject.SetActive(false);
    }
}

