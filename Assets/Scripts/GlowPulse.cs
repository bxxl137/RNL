using UnityEngine;

/// <summary>给 LineRenderer 加呼吸闪烁。</summary>
public class GlowPulse : MonoBehaviour
{
    private LineRenderer _lr;
    private Color _baseColor;
    private float _speed;
    private float _minAlpha;

    public void Init(LineRenderer lr, Color color, float speed, float minAlpha)
    {
        _lr = lr;
        _baseColor = color;
        _speed = speed;
        _minAlpha = Mathf.Clamp01(minAlpha);
        Apply(0f);
    }

    public void SetColor(Color c) => _baseColor = c;

    private void Update()
    {
        if (_lr == null) return;
        float t = (Mathf.Sin(Time.unscaledTime * _speed) + 1f) * 0.5f;
        Apply(t);
    }

    private void Apply(float t)
    {
        if (_lr == null) return;
        float a = Mathf.Lerp(_minAlpha, 1f, t);
        Color c = _baseColor;
        c.a *= a;
        _lr.startColor = c;
        _lr.endColor = c;
    }
}
