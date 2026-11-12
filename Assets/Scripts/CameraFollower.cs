using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2D 横板摄像机跟随：只跟随目标 X 轴，Y 恒为 0。
/// 挂在 Main Camera 上。
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    [Header("目标")]
    [SerializeField] private Transform target;

    [Header("跟随")]
    [Tooltip("水平偏移，正值镜头偏右")]
    [SerializeField] private float offsetX = 0f;

    [Tooltip("0 表示硬跟随，越大越慢。横板游戏常用 0.1 ~ 0.2")]
    [SerializeField] private float smoothTime = 0.15f;

    [Header("边界限制（可选）")]
    [SerializeField] private bool clampX = false;
    [SerializeField] private float minX = -100f;
    [SerializeField] private float maxX = 100f;

    private Vector3 velocity = Vector3.zero;

    private void LateUpdate()
    {
        if (target == null) return;

        float targetX = target.position.x + offsetX;
        if (clampX) targetX = Mathf.Clamp(targetX, minX, maxX);

        Vector3 desired = new Vector3(targetX, 0f, transform.position.z);

        if (smoothTime <= 0f)
            transform.position = desired;
        else
            transform.position = Vector3.SmoothDamp(
                transform.position, desired, ref velocity, smoothTime,
                Mathf.Infinity, Time.deltaTime);
    }

    /// <summary>运行中换跟随目标。</summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}

    

