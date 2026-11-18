using UnityEngine;

/// <summary>
/// 第二关的横向移动控制。沿用第一关的 A/D、左右方向键操作。
/// </summary>
public class Level2PlayerController : MonoBehaviour
{
    public float speed = 4.2f;
    public bool canMove;
    public float minX = -8f;
    public float maxX = 8f;
    public bool IsWalking { get; private set; }

    private SpriteRenderer _renderer;
    private Sprite _idle;
    private Sprite[] _walk;
    private float _animTime;
    private float _baseY;

    public void Setup(SpriteRenderer renderer, Sprite idle, Sprite[] walk, float moveSpeed, float baseY)
    {
        _renderer = renderer;
        _idle = idle;
        _walk = walk;
        speed = moveSpeed;
        _baseY = baseY;
        if (_renderer != null) _renderer.sprite = idle;
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
            Vector3 p = transform.localPosition;
            p.x = Mathf.Clamp(p.x + dir * speed * Time.deltaTime, minX, maxX);
            p.y = _baseY;
            transform.localPosition = p;
            if (_renderer != null) _renderer.flipX = dir < 0f;

            _animTime += Time.deltaTime * 9f;
            if (_walk != null && _walk.Length > 0)
                _renderer.sprite = _walk[Mathf.Abs((int)_animTime) % _walk.Length];
        }
        else
        {
            _animTime = 0f;
            if (_renderer != null) _renderer.sprite = _idle;
        }
    }
}
