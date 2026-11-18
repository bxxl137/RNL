using UnityEngine;

/// <summary>
/// 交互提示“！”的轻微上下浮动。
/// </summary>
public class Level2PromptBob : MonoBehaviour
{
    public float amplitude = 0.13f;
    public float speed = 2.6f;

    private float _baseY;

    public void Init(Vector3 localPosition)
    {
        _baseY = localPosition.y;
        transform.localPosition = localPosition;
    }

    private void Update()
    {
        Vector3 p = transform.localPosition;
        p.y = _baseY + Mathf.Sin(Time.time * speed) * amplitude;
        transform.localPosition = p;
    }
}
