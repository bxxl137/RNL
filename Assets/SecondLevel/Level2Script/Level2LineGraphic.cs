using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 用 UI 顶点画一条有厚度的折线，用于结局的暖光连接。
/// </summary>
public class Level2LineGraphic : MaskableGraphic
{
    public float thickness = 3.5f;
    public bool closed;
    private readonly List<Vector2> _points = new List<Vector2>();

    public void SetPoints(IList<Vector2> points, bool isClosed)
    {
        _points.Clear();
        if (points != null) _points.AddRange(points);
        closed = isClosed;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (_points.Count < 2) return;

        int count = closed ? _points.Count : _points.Count - 1;
        for (int i = 0; i < count; i++)
        {
            Vector2 a = _points[i];
            Vector2 b = _points[(i + 1) % _points.Count];
            Vector2 dir = (b - a).sqrMagnitude > 0.0001f ? (b - a).normalized : Vector2.right;
            Vector2 n = new Vector2(-dir.y, dir.x) * (thickness * 0.5f);
            AddQuad(vh, a - n, a + n, b + n, b - n);
        }

        for (int i = 0; i < _points.Count; i++)
        {
            Vector2 p = _points[i];
            float r = thickness * 0.55f;
            AddQuad(vh, p + new Vector2(-r, -r), p + new Vector2(-r, r),
                    p + new Vector2(r, r), p + new Vector2(r, -r));
        }
    }

    private void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        int start = vh.currentVertCount;
        UIVertex v = UIVertex.simpleVert;
        v.color = color;
        v.position = a; vh.AddVert(v);
        v.position = b; vh.AddVert(v);
        v.position = c; vh.AddVert(v);
        v.position = d; vh.AddVert(v);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }
}
