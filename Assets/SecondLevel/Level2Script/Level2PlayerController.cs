using UnityEngine;

/// <summary>
/// 第二关横向移动控制。A/D、左右方向键。
/// 动画帧：0~1 向左，2~3 向右。
/// </summary>
public class Level2PlayerController : MonoBehaviour
{
    public float speed = 4.2f;
    public bool canMove;
    public float minX = -8f;
    public float maxX = 8f;
    public bool IsWalking { get; private set; }

    [Header("动画")]
    [Tooltip("每秒播放多少帧")]
    [SerializeField] private float walkFPS = 8f;

    [Header("帧区间索引")]
    [Tooltip("向左动画的起始帧索引")]
    [SerializeField] private int leftStartIndex = 0;
    [Tooltip("向左动画帧数")]
    [SerializeField] private int leftFrameCount = 2;
    [Tooltip("向右动画的起始帧索引")]
    [SerializeField] private int rightStartIndex = 2;
    [Tooltip("向右动画帧数")]
    [SerializeField] private int rightFrameCount = 2;

    private SpriteRenderer _renderer;
    private Sprite _idle;
    private Sprite[] _walk;
    private float _animTime;
    private int _lastFrame = -1;
    private float _baseY;

    public void Setup(SpriteRenderer renderer, Sprite idle, Sprite[] walk, float moveSpeed, float baseY)
    {
        _renderer = renderer;
        _idle = idle;
        _walk = walk;
        speed = moveSpeed;
        _baseY = baseY;
        _animTime = 0f;
        _lastFrame = -1;

        if (_renderer != null)
        {
            _renderer.sprite = idle;
            _renderer.flipX = false;   // 双向素材，禁用翻转
        }
    }

    private void Update()
    {
        if (_renderer == null) return;

        float dir = 0f;
        if (canMove)
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dir = -1f;
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dir = 1f;
        }

        IsWalking = dir != 0f;

        if (IsWalking)
        {
            // ---- 移动：原逻辑不动 ----
            Vector3 p = transform.localPosition;
            p.x = Mathf.Clamp(p.x + dir * speed * Time.deltaTime, minX, maxX);
            p.y = _baseY;
            transform.localPosition = p;

            // ---- 动画 ----
            if (_walk != null && _walk.Length > 0)
            {
                GetFrameRange(dir, out int start, out int count);
                if (count <= 0) return;

                // 方向切换则重置
                if (_lastFrame < start || _lastFrame >= start + count)
                {
                    _lastFrame = start;
                    _animTime = 0f;
                }

                _animTime += Time.deltaTime * walkFPS;

                int offset = Mathf.FloorToInt(_animTime);
                if (offset >= count) offset %= count;   // 循环

                int frame = start + offset;
                if (frame != _lastFrame)
                {
                    _lastFrame = frame;
                    _renderer.sprite = _walk[frame];
                }
            }
        }
        else
        {
            _animTime = 0f;
            _lastFrame = -1;
            _renderer.sprite = _idle;
        }
    }

    private void GetFrameRange(float dir, out int start, out int count)
    {
        if (dir < 0f)
        {
            start = leftStartIndex;
            count = leftFrameCount;
        }
        else
        {
            start = rightStartIndex;
            count = rightFrameCount;
        }

        // 越界保护
        if (_walk == null || _walk.Length == 0)
        {
            start = 0; count = 0; return;
        }
        if (start < 0) start = 0;
        if (start >= _walk.Length) { count = 0; return; }
        if (start + count > _walk.Length) count = _walk.Length - start;
    }
}
