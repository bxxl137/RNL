using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 第二关 HUD：微光节点进度、微光碎片、区域标题、物品提示和随身物品面板。
/// </summary>
public class Level2HUD : MonoBehaviour
{
    private TMP_FontAsset _font;
    private Sprite _dotSprite;
    private Sprite _glowSprite;
    private Sprite _fragmentSprite;
    private Sprite _panelBagSprite;

    private GameObject _root;
    private GameObject _bagRoot;
    private RectTransform _bagContent;
    private GameObject _toastRoot;
    private CanvasGroup _toastGroup;
    private CanvasGroup _zoneGroup;
    private TextMeshProUGUI _toastText;
    private TextMeshProUGUI _zoneText;
    private TextMeshProUGUI _hintText;
    private TextMeshProUGUI _fragmentText;
    private TextMeshProUGUI _nodeTitle;

    private Image[] _nodeDots = new Image[4];
    private Image[] _nodeGlows = new Image[4];
    private Image[] _segments = new Image[3];
    private bool[] _nodeLit = new bool[4];
    private readonly string[] _nodeNames = { "棋盘", "早餐店", "花园", "裁缝铺" };

    private readonly Queue<string> _toastQueue = new Queue<string>();
    private bool _toastBusy;
    private Coroutine _zoneRoutine;
    private bool _bagOpen;

    public bool BagOpen { get { return _bagOpen; } }

    public void Build(Transform canvas, TMP_FontAsset font, Sprite dot, Sprite glow, Sprite fragment,
                      Sprite panelBag, Sprite toastSprite, Sprite hudPanel)
    {
        _font = font;
        _dotSprite = dot;
        _glowSprite = glow;
        _fragmentSprite = fragment;
        _panelBagSprite = panelBag;

        _root = Level2UI.Create("HUD", canvas);
        RectTransform rootRt = _root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        BuildNodePanel(hudPanel);
        BuildFragmentChip();
        BuildHint();
        BuildZoneCaption();
        BuildToast(toastSprite);
        BuildBagPanel();
    }

    private void BuildNodePanel(Sprite hudPanel)
    {
        Image panel = Level2UI.MakeImage(_root.transform, "NodePanel", hudPanel, new Vector2(400f, 124f), Vector2.zero);
        Level2UI.Anchor(panel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-22f, -22f), new Vector2(400f, 124f));

        _nodeTitle = Level2UI.MakeText(panel.transform, "Title", "微光节点  0 / 4", _font, 22f,
                                       Level2UI.Cream, new Vector2(260f, 34f), Vector2.zero,
                                       TextAlignmentOptions.Left);
        RectTransform trt = _nodeTitle.rectTransform;
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(0f, 1f);
        trt.pivot = new Vector2(0f, 1f);
        trt.anchoredPosition = new Vector2(20f, -8f);
        trt.sizeDelta = new Vector2(260f, 30f);

        float[] xs = { -126f, -42f, 42f, 126f };
        for (int i = 0; i < 4; i++)
        {
            Image glow = Level2UI.MakeImage(panel.transform, "NodeGlow" + i, _glowSprite, new Vector2(72f, 72f),
                                            new Vector2(xs[i], -8f), new Color(1f, 0.82f, 0.52f, 0.10f));
            _nodeGlows[i] = glow;

            Image dot = Level2UI.MakeImage(panel.transform, "NodeDot" + i, _dotSprite, new Vector2(28f, 28f),
                                           new Vector2(xs[i], -8f), Level2UI.Dim);
            _nodeDots[i] = dot;

            TextMeshProUGUI label = Level2UI.MakeText(panel.transform, "NodeLabel" + i, _nodeNames[i], _font,
                                                      16f, new Color(0.86f, 0.82f, 0.72f, 0.85f),
                                                      new Vector2(70f, 24f), new Vector2(xs[i], -38f));
            label.raycastTarget = false;
        }

        float[] segX = { -84f, 0f, 84f };
        for (int i = 0; i < 3; i++)
        {
            Image seg = Level2UI.MakeSolid(panel.transform, "Seg" + i, new Color(0.55f, 0.58f, 0.66f, 0.35f),
                                           new Vector2(44f, 3f), new Vector2(segX[i], -8f));
            _segments[i] = seg;
            seg.transform.SetSiblingIndex(0);
        }
    }

    private void BuildFragmentChip()
    {
        Image chip = Level2UI.MakeSolid(_root.transform, "FragmentChip", Level2UI.DarkPanel,
                                        new Vector2(252f, 58f), Vector2.zero);
        Level2UI.Anchor(chip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(22f, -22f), new Vector2(252f, 58f));

        Level2UI.MakeImage(chip.transform, "Icon", _fragmentSprite, new Vector2(38f, 38f),
                           new Vector2(-98f, 0f), Color.white, true);
        _fragmentText = Level2UI.MakeText(chip.transform, "Label", "微光碎片 ×0", _font, 23f,
                                          Level2UI.Cream, new Vector2(172f, 44f), new Vector2(16f, 0f),
                                          TextAlignmentOptions.Left);
    }

    private void BuildHint()
    {
        _hintText = Level2UI.MakeText(_root.transform, "Hint", "Enter 交互    E 随身物品", _font, 19f,
                                      new Color(0.94f, 0.89f, 0.78f, 0.62f), new Vector2(480f, 30f), Vector2.zero,
                                      TextAlignmentOptions.Right);
        Level2UI.Anchor(_hintText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-24f, 20f), new Vector2(480f, 30f));
    }

    private void BuildZoneCaption()
    {
        _zoneText = Level2UI.MakeText(_root.transform, "ZoneCaption", "", _font, 46f,
                                      Level2UI.Cream, new Vector2(1000f, 80f), Vector2.zero,
                                      TextAlignmentOptions.Center);
        _zoneText.fontStyle = FontStyles.Bold;
        _zoneText.outlineWidth = 0.18f;
        _zoneText.outlineColor = new Color(0.05f, 0.04f, 0.03f, 0.85f);
        RectTransform rt = _zoneText.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -44f);
        rt.sizeDelta = new Vector2(1000f, 80f);
        _zoneGroup = _zoneText.gameObject.AddComponent<CanvasGroup>();
        _zoneGroup.alpha = 0f;
    }

    private void BuildToast(Sprite toastSprite)
    {
        _toastRoot = Level2UI.Create("Toast", _root.transform);
        RectTransform rt = _toastRoot.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -132f);
        rt.sizeDelta = new Vector2(760f, 86f);
        _toastGroup = _toastRoot.AddComponent<CanvasGroup>();
        _toastGroup.alpha = 0f;
        Level2UI.MakeImage(_toastRoot.transform, "Bg", toastSprite, new Vector2(760f, 86f), Vector2.zero,
                           Color.white, false, false);
        _toastText = Level2UI.MakeText(_toastRoot.transform, "Text", "", _font, 27f, Level2UI.Cream,
                                       new Vector2(710f, 70f), Vector2.zero, TextAlignmentOptions.Center);
        _toastRoot.SetActive(false);
    }

    private void BuildBagPanel()
    {
        _bagRoot = Level2UI.Create("BagRoot", _root.transform);
        RectTransform brt = _bagRoot.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;

        Image scrim = Level2UI.MakeImage(_bagRoot.transform, "Scrim", null,
                                         new Vector2(1920f, 1080f), Vector2.zero,
                                         new Color(0.02f, 0.03f, 0.05f, 0.66f), false, true);
        Level2UI.Stretch(scrim.rectTransform, Vector2.zero, Vector2.zero);
        Button scrimBtn = scrim.gameObject.AddComponent<Button>();
        scrimBtn.transition = Selectable.Transition.None;
        scrimBtn.onClick.AddListener(HideBag);

        Image panel = Level2UI.MakeImage(_bagRoot.transform, "Panel", _panelBagSprite,
                                         new Vector2(1000f, 680f), Vector2.zero, Color.white, false, true);
        Level2UI.Anchor(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(1000f, 680f));

        TextMeshProUGUI title = Level2UI.MakeText(panel.transform, "Title", "随身物品", _font, 38f,
                                                  Level2UI.BodyBrown, new Vector2(420f, 58f), Vector2.zero,
                                                  TextAlignmentOptions.Left);
        Level2UI.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(44f, -26f), new Vector2(420f, 58f));

        TextMeshProUGUI close = Level2UI.MakeText(panel.transform, "CloseHint", "E / Esc 关闭", _font, 20f,
                                                  new Color(0.40f, 0.29f, 0.18f, 0.8f), new Vector2(240f, 40f),
                                                  Vector2.zero, TextAlignmentOptions.Right);
        Level2UI.Anchor(close.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-40f, -30f), new Vector2(240f, 40f));

        GameObject content = Level2UI.Create("Content", panel.transform);
        RectTransform crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = new Vector2(0f, -34f);
        crt.sizeDelta = new Vector2(900f, 480f);
        _bagContent = crt;

        _bagRoot.SetActive(false);
    }

    public void SetFragments(int count)
    {
        if (_fragmentText != null) _fragmentText.text = "微光碎片 ×" + count;
    }

    public void SetNode(int index, bool lit)
    {
        if (index < 0 || index >= 4) return;
        _nodeLit[index] = lit;
        _nodeDots[index].color = lit ? Level2UI.Warm : Level2UI.Dim;
        _nodeGlows[index].color = new Color(1f, 0.82f, 0.52f, lit ? 0.5f : 0.08f);

        int count = 0;
        for (int i = 0; i < 4; i++) if (_nodeLit[i]) count++;
        if (_nodeTitle != null) _nodeTitle.text = "微光节点  " + count + " / 4";

        for (int i = 0; i < 3; i++)
        {
            bool on = _nodeLit[i] && _nodeLit[i + 1];
            _segments[i].color = on ? new Color(1f, 0.78f, 0.42f, 0.9f)
                                    : new Color(0.55f, 0.58f, 0.66f, 0.35f);
        }
    }

    public void ShowToast(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        _toastQueue.Enqueue(message);
        if (!_toastBusy && gameObject.activeInHierarchy)
            StartCoroutine(ToastRoutine());
    }

    private IEnumerator ToastRoutine()
    {
        _toastBusy = true;
        while (_toastQueue.Count > 0)
        {
            string msg = _toastQueue.Dequeue();
            _toastText.text = msg;
            _toastRoot.SetActive(true);
            _toastGroup.alpha = 0f;
            float t = 0f;
            while (t < 0.16f)
            {
                t += Time.unscaledDeltaTime;
                _toastGroup.alpha = Mathf.Clamp01(t / 0.16f);
                yield return null;
            }
            _toastGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(1.65f);
            t = 0f;
            while (t < 0.3f)
            {
                t += Time.unscaledDeltaTime;
                _toastGroup.alpha = 1f - Mathf.Clamp01(t / 0.3f);
                yield return null;
            }
            _toastGroup.alpha = 0f;
            _toastRoot.SetActive(false);
            yield return new WaitForSecondsRealtime(0.08f);
        }
        _toastBusy = false;
    }

    public void ShowZoneCaption(string title)
    {
        if (_zoneText == null) return;
        _zoneText.text = title;
        if (_zoneRoutine != null) StopCoroutine(_zoneRoutine);
        _zoneRoutine = StartCoroutine(ZoneRoutine());
    }

    private IEnumerator ZoneRoutine()
    {
        _zoneGroup.alpha = 0f;
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            _zoneGroup.alpha = Mathf.Clamp01(t / 0.3f);
            yield return null;
        }
        _zoneGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(1.8f);
        t = 0f;
        while (t < 0.55f)
        {
            t += Time.unscaledDeltaTime;
            _zoneGroup.alpha = 1f - Mathf.Clamp01(t / 0.55f);
            yield return null;
        }
        _zoneGroup.alpha = 0f;
        _zoneRoutine = null;
    }

    public void SetHint(string message)
    {
        if (_hintText != null) _hintText.text = message;
    }

    public void ShowBag(List<Level2BagEntry> entries)
    {
        _bagRoot.SetActive(true);
        _bagOpen = true;
        RefreshBag(entries);
    }

    public void HideBag()
    {
        if (_bagRoot == null) return;
        _bagRoot.SetActive(false);
        _bagOpen = false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void RefreshBag(List<Level2BagEntry> entries)
    {
        for (int i = _bagContent.childCount - 1; i >= 0; i--)
        {
            GameObject child = _bagContent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }

        if (entries == null || entries.Count == 0)
        {
            Level2UI.MakeText(_bagContent, "Empty", "还没有收集到物品。", _font, 28f,
                              new Color(0.42f, 0.31f, 0.20f, 0.85f), new Vector2(600f, 80f), Vector2.zero);
            return;
        }

        for (int i = 0; i < entries.Count && i < 6; i++)
        {
            Level2BagEntry e = entries[i];
            float y = 190f - i * 82f;

            Level2UI.MakeSolid(_bagContent, "Line" + i, new Color(0.62f, 0.50f, 0.33f, 0.28f),
                               new Vector2(850f, 1f), new Vector2(0f, y - 38f));
            if (e.icon != null)
            {
                Level2UI.MakeImage(_bagContent, "Icon" + i, e.icon, new Vector2(58f, 58f),
                                   new Vector2(-386f, y), Color.white, true);
            }
            TextMeshProUGUI title = Level2UI.MakeText(_bagContent, "Title" + i, e.title, _font, 27f,
                                                      Level2UI.BodyBrown, new Vector2(280f, 54f),
                                                      new Vector2(-320f, y), TextAlignmentOptions.Left);
            title.raycastTarget = false;
            TextMeshProUGUI desc = Level2UI.MakeText(_bagContent, "Desc" + i, e.description, _font, 18f,
                                                     new Color(0.44f, 0.34f, 0.23f, 0.9f), new Vector2(470f, 54f),
                                                     new Vector2(-20f, y), TextAlignmentOptions.Left);
            desc.raycastTarget = false;
            if (!string.IsNullOrEmpty(e.count))
            {
                TextMeshProUGUI count = Level2UI.MakeText(_bagContent, "Count" + i, e.count, _font, 24f,
                                                          Level2UI.BodyBrown, new Vector2(120f, 54f),
                                                          new Vector2(400f, y), TextAlignmentOptions.Right);
                count.raycastTarget = false;
            }
        }
    }

    private void Update()
    {
        for (int i = 0; i < 4; i++)
        {
            if (!_nodeLit[i] || _nodeGlows[i] == null) continue;
            float a = 0.42f + 0.16f * Mathf.Sin(Time.unscaledTime * 2f + i * 1.3f);
            _nodeGlows[i].color = new Color(1f, 0.82f, 0.52f, a);
        }
    }

    public void HideAll()
    {
        if (_root != null) _root.SetActive(false);
    }
}
