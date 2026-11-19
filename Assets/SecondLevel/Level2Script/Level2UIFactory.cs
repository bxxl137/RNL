using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 第二关运行时 UI 的小工具。所有界面都在代码里搭建，不依赖额外预制体。
/// </summary>
public static class Level2UI
{
    public static readonly Color BodyBrown = new Color32(76, 52, 32, 255);
    public static readonly Color Cream = new Color32(244, 231, 195, 255);
    public static readonly Color Gold = new Color32(203, 161, 82, 255);
    public static readonly Color Warm = new Color32(255, 214, 150, 255);
    public static readonly Color Dim = new Color32(96, 106, 126, 255);
    public static readonly Color DarkPanel = new Color32(20, 24, 34, 190);

    public static GameObject Create(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    public static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    public static void Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    public static Image MakeImage(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 pos,
                                  Color? color = null, bool preserveAspect = false, bool raycast = false)
    {
        GameObject go = Create(name, parent);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = preserveAspect;
        img.raycastTarget = raycast;
        img.color = color ?? Color.white;
        Anchor(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        return img;
    }

    public static Image MakeSolid(Transform parent, string name, Color color, Vector2 size, Vector2 pos, bool raycast = false)
    {
        return MakeImage(parent, name, null, size, pos, color, false, raycast);
    }

    public static TextMeshProUGUI MakeText(Transform parent, string name, string content, TMP_FontAsset font,
                                           float fontSize, Color color, Vector2 size, Vector2 pos,
                                           TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        GameObject go = Create(name, parent);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = align;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        Anchor(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        return text;
    }

    public static Button MakeButton(Transform parent, string name, string label, TMP_FontAsset font, float fontSize,
                                Vector2 size, Vector2 pos, Action onClick, Sprite sprite = null,
                                Color? backgroundColor = null, Color? textColor = null)
{
    GameObject go = Create(name, parent);
    Image img = go.AddComponent<Image>();
    img.sprite = sprite;
    img.preserveAspect = false;
    img.raycastTarget = true;
    img.color = backgroundColor ?? Color.white;
    Anchor(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);

    Button btn = go.AddComponent<Button>();
    btn.targetGraphic = img;
    btn.navigation = new Navigation { mode = Navigation.Mode.None };
    ColorBlock colors = btn.colors;
    colors.normalColor = Color.white;
    colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
    colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    colors.selectedColor = Color.white;
    colors.disabledColor = new Color(1f, 1f, 1f, 0.5f);
    btn.colors = colors;

    // ★ 新增：点击音效（先播，再执行 onClick，保证按钮隐藏了也能响）
    btn.onClick.AddListener(() => Level2SFX.Play(Level2SFX.SelectClick, 1f));

    if (onClick != null) btn.onClick.AddListener(() => onClick());

    if (!string.IsNullOrEmpty(label))
    {
        TextMeshProUGUI text = MakeText(go.transform, "Label", label, font, fontSize,
                                        textColor ?? BodyBrown, size, Vector2.zero, TextAlignmentOptions.Center);
        text.raycastTarget = false;
    }
    return btn;
}

    public static T AddIfMissing<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }
}
