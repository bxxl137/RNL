using UnityEngine;

/// <summary>
/// 2D 横板摄像机跟随：只跟随目标 X 轴，Y 恒为 0。
/// 支持按背景边界钳制，避免镜头越界露出底色（黑边）。
/// </summary>
public class camerafollow : MonoBehaviour
{
    [Header("目标")]
    [SerializeField] private Transform target;

    [Header("自动查找（运行时生成玩家时使用）")]
    [SerializeField] private bool autoFindTarget = true;
    [SerializeField] private float findInterval = 0.5f;

    [Header("跟随")]
    [SerializeField] private float offsetX = 0f;
    [SerializeField] private float smoothTime = 0.15f;

    [Header("边界限制")]
    [Tooltip("勾选后相机会被钳制在 [minX, maxX] 内，避免露出背景外的黑边")]
    [SerializeField] private bool clampX = false;
    [SerializeField] private float minX = -100f;
    [SerializeField] private float maxX = 100f;

    private Vector3 velocity = Vector3.zero;
    private float nextFindTime = 0f;

    private void LateUpdate()
    {
        if (target == null)
        {
            if (!autoFindTarget) return;
            if (Time.unscaledTime >= nextFindTime)
            {
                nextFindTime = Time.unscaledTime + findInterval;
                TryAutoFindTarget();
            }
            if (target == null) return;
        }

        float targetX = target.position.x + offsetX;

        // 关键：只在边界有效时钳制。minX >= maxX 视为无效边界，跳过钳制
        if (clampX && maxX > minX)
            targetX = Mathf.Clamp(targetX, minX, maxX);

        Vector3 desired = new Vector3(targetX, 0f, transform.position.z);

        if (smoothTime <= 0f)
            transform.position = desired;
        else
            transform.position = Vector3.SmoothDamp(
                transform.position, desired, ref velocity, smoothTime,
                Mathf.Infinity, Time.deltaTime);
    }

    private void TryAutoFindTarget()
    {
        var player = FindObjectOfType<Level2PlayerController>();
        if (player != null)
        {
            target = player.transform;
            velocity = Vector3.zero;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        velocity = Vector3.zero;
    }

    public void SetBounds(float left, float right)
    {
        // 防御：如果传进来的范围无效（左 >= 右），视为不限制，而不是锁死相机
        if (right <= left)
        {
            clampX = false;
            return;
        }

        minX = left;
        maxX = right;
        clampX = true;
    }
}
