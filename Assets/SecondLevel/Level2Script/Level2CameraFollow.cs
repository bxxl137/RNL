using UnityEngine;

/// <summary>
/// 2D 横板摄像机跟随：只跟随目标 X 轴，Y 恒为 0。
/// 自动根据视口宽高比计算可视宽度，并按区域边界钳制，防止露黑边。
/// 区域比视野窄时自动锁定在区域中心。
/// </summary>
public class Level2CameraFollow : MonoBehaviour
{
    [Header("目标")]
    [SerializeField] private Transform target;

    [Header("自动查找")]
    [SerializeField] private bool autoFindTarget = true;
    [SerializeField] private float findInterval = 0.5f;

    [Header("跟随")]
    [SerializeField] private float offsetX = 0f;
    [SerializeField] private float smoothTime = 0.15f;

    [Header("边界（由 Bootstrap 或手动设置）")]
    [Tooltip("是否启用边界钳制")]
    [SerializeField] private bool clampX = false;
    [Tooltip("区域世界坐标的左边缘（不是相机中心）")]
    [SerializeField] private float zoneLeft = -100f;
    [Tooltip("区域世界坐标的右边缘")]
    [SerializeField] private float zoneRight = 100f;

    private Vector3 velocity = Vector3.zero;
    private float nextFindTime = 0f;
    private Camera _cam;


    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;
    }


    private void LateUpdate()
    {
        clampX = false;
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

        if (clampX && zoneRight > zoneLeft && _cam != null && _cam.orthographic)
        {
            // 相机可视半宽
            float halfWidth = _cam.orthographicSize * _cam.aspect;
            float camMin = zoneLeft + halfWidth;
            float camMax = zoneRight - halfWidth;

            if (camMin <= camMax)
            {
                // 区域比视野宽：正常钳制
                targetX = Mathf.Clamp(targetX, camMin, camMax);
            }
            else
            {
                // 区域比视野窄：锁定在区域中心
                targetX = (zoneLeft + zoneRight) * 0.5f;
            }
        }

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


    /// <summary>设置跟随目标</summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        velocity = Vector3.zero;
    }


    /// <summary>设置区域边界（传区域左右边缘，不是相机中心）</summary>
    public void SetZone(float left, float right)
    {
        if (right <= left)
        {
            clampX = false;
            return;
        }
        zoneLeft = left;
        zoneRight = right;
        clampX = true;
    }


    // 兼容你原来的调用（如果你别处用了 SetBounds）
    public void SetBounds(float left, float right) => SetZone(left, right);
}
