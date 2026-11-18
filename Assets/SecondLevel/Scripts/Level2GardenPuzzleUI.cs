using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 花园谜题：参考上方六朵活花，把下方六朵枯花按顺序放回花坛。
/// 枯花顺序：星形、暗红、浅黄、干红、花苞、灰紫。
/// 对应答案（槽位 1→6 放置的枯花编号）：3、4、1、5、2、6。
/// </summary>
public class Level2GardenPuzzleUI : MonoBehaviour
{
    private TMP_FontAsset _font;
    private Sprite _panelSprite;
    private Sprite _buttonSprite;
    private Sprite _slotSprite;

    private GameObject _root;
    private RectTransform _panelRt;
    private Image[] _slots = new Image[6];
    private TextMeshProUGUI[] _slotNumbers = new TextMeshProUGUI[6];
    private Image[] _slotFlowers = new Image[6];
    private Image[] _trayFrames = new Image[6];
    private Image[] _trayFlowers = new Image[6];
    private TextMeshProUGUI _message;
    private Button _confirmButton;

    private Sprite[] _alive;
    private Sprite[] _withered;

    private readonly int[] _slotItem = { -1, -1, -1, -1, -1, -1 };
    private readonly bool[] _itemPlaced = new bool[6];
    private readonly int[] _correctItemForSlot = { 2, 3, 0, 4, 1, 5 };
    private int _selected = -1;
    private bool _solved;
    private bool _busy;
    private Action _onSolved;
    private Action _onClosed;

    public bool IsOpen { get { return _root != null && _root.activeSelf; } }

    public void Build(Transform canvas, TMP_FontAsset font, Sprite panel, Sprite button, Sprite slot,
                      Sprite[] alive, Sprite[] withered)
    {
        _font = font;
        _panelSprite = panel;
        _buttonSprite = button;
        _slotSprite = slot;
        _alive = alive;
        _withered = withered;

        _root = Level2UI.Create("GardenPuzzle", canvas);
        RectTransform rrt = _root.GetComponent<RectTransform>();
        rrt.anchorMin = Vector2.zero;
        rrt.anchorMax = Vector2.one;
        rrt.offsetMin = Vector2.zero;
        rrt.offsetMax = Vector2.zero;

        Image scrim = Level2UI.MakeImage(_root.transform, "Scrim", null, new Vector2(1920f, 1080f), Vector2.zero,
                                         new Color(0.02f, 0.03f, 0.05f, 0.76f), false, true);
        Level2UI.Stretch(scrim.rectTransform, Vector2.zero, Vector2.zero);

        Image panelImg = Level2UI.MakeImage(_root.transform, "Panel", panel, new Vector2(1400f, 900f), Vector2.zero,
                                            Color.white, false, true);
        _panelRt = panelImg.rectTransform;

        Level2UI.MakeText(panelImg.transform, "Title", "花园 · 枯萎的花", _font, 38f, Level2UI.BodyBrown,
                          new Vector2(700f, 60f), Vector2.zero, TextAlignmentOptions.Left);
        RectTransform titleRt = panelImg.transform.Find("Title").GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(0f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(46f, -26f);
        titleRt.sizeDelta = new Vector2(700f, 60f);

        TextMeshProUGUI page = Level2UI.MakeText(panelImg.transform, "Page", "1 / 1", _font, 22f,
                                                 new Color(0.42f, 0.31f, 0.20f, 0.8f),
                                                 new Vector2(120f, 40f), Vector2.zero, TextAlignmentOptions.Right);
        Level2UI.Anchor(page.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-186f, -34f), new Vector2(120f, 40f));

        Level2UI.MakeButton(panelImg.transform, "Exit", "退出", _font, 24f, new Vector2(116f, 56f), Vector2.zero,
                            Close, _buttonSprite, Color.white, Level2UI.BodyBrown).GetComponent<RectTransform>()
            .anchoredPosition = new Vector2(0f, 0f);
        RectTransform exitRt = panelImg.transform.Find("Exit").GetComponent<RectTransform>();
        Level2UI.Anchor(exitRt, new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-54f, -30f), new Vector2(116f, 56f));

        Level2UI.MakeText(panelImg.transform, "RefLabel", "参考：花坛里原本开着的六朵花", _font, 24f,
                          new Color(0.32f, 0.23f, 0.15f, 0.95f), new Vector2(900f, 40f), Vector2.zero,
                          TextAlignmentOptions.Left);
        RectTransform refRt = panelImg.transform.Find("RefLabel").GetComponent<RectTransform>();
        Level2UI.Anchor(refRt, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                        new Vector2(-630f, 340f), new Vector2(900f, 40f));

        float[] xs = { -500f, -300f, -100f, 100f, 300f, 500f };
        for (int i = 0; i < 6; i++)
        {
            Level2UI.MakeImage(panelImg.transform, "Alive" + i, _alive[i], new Vector2(150f, 190f),
                               new Vector2(xs[i], 215f), Color.white, true);
            Level2UI.MakeSolid(panelImg.transform, "Stand" + i, new Color(0.64f, 0.50f, 0.31f, 0.35f),
                               new Vector2(130f, 6f), new Vector2(xs[i], 112f));
        }

        Level2UI.MakeSolid(panelImg.transform, "Divider1", new Color(0.62f, 0.50f, 0.33f, 0.55f),
                           new Vector2(1300f, 2f), new Vector2(0f, 92f));

        Level2UI.MakeText(panelImg.transform, "SlotLabel", "花坛：按上方的顺序把枯花放回（1 → 6）", _font, 24f,
                          new Color(0.32f, 0.23f, 0.15f, 0.95f), new Vector2(1000f, 40f), Vector2.zero,
                          TextAlignmentOptions.Left);
        RectTransform slotLabelRt = panelImg.transform.Find("SlotLabel").GetComponent<RectTransform>();
        Level2UI.Anchor(slotLabelRt, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                        new Vector2(-630f, 65f), new Vector2(1000f, 40f));

        for (int i = 0; i < 6; i++)
        {
            Image slotImg = Level2UI.MakeImage(panelImg.transform, "Slot" + i, _slotSprite, new Vector2(180f, 150f),
                                               new Vector2(xs[i], -35f), Color.white, false, true);
            _slots[i] = slotImg;
            int index = i;
            Button btn = slotImg.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.onClick.AddListener(() => OnSlotClicked(index));

            TextMeshProUGUI num = Level2UI.MakeText(slotImg.transform, "Num", (i + 1).ToString(), _font, 30f,
                                                    new Color(0.93f, 0.85f, 0.68f, 0.9f),
                                                    new Vector2(160f, 130f), Vector2.zero);
            _slotNumbers[i] = num;

            Image flower = Level2UI.MakeImage(slotImg.transform, "Flower", null, new Vector2(150f, 130f),
                                              Vector2.zero, Color.white, true);
            flower.gameObject.SetActive(false);
            _slotFlowers[i] = flower;
        }

        Level2UI.MakeSolid(panelImg.transform, "Divider2", new Color(0.62f, 0.50f, 0.33f, 0.55f),
                           new Vector2(1300f, 2f), new Vector2(0f, -130f));

        Level2UI.MakeText(panelImg.transform, "TrayLabel", "枯花：掉了几片花瓣，只剩一处特征", _font, 24f,
                          new Color(0.32f, 0.23f, 0.15f, 0.95f), new Vector2(1000f, 40f), Vector2.zero,
                          TextAlignmentOptions.Left);
        RectTransform trayLabelRt = panelImg.transform.Find("TrayLabel").GetComponent<RectTransform>();
        Level2UI.Anchor(trayLabelRt, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                        new Vector2(-630f, -155f), new Vector2(1000f, 40f));

        float[] trayXs = { -500f, -300f, -100f, 100f, 300f, 500f };
        for (int i = 0; i < 6; i++)
        {
            Image frame = Level2UI.MakeImage(panelImg.transform, "Tray" + i, null, new Vector2(180f, 190f),
                                             new Vector2(trayXs[i], -280f), new Color(0.40f, 0.31f, 0.21f, 0.22f), false, true);
            frame.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            Button btn = frame.GetComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            int index = i;
            btn.onClick.AddListener(() => OnTrayClicked(index));
            _trayFrames[i] = frame;

            Image flower = Level2UI.MakeImage(frame.transform, "Flower", _withered[i], new Vector2(150f, 170f),
                                              Vector2.zero, Color.white, true);
            _trayFlowers[i] = flower;
        }

        _message = Level2UI.MakeText(panelImg.transform, "Message", "", _font, 22f,
                                     new Color(0.55f, 0.20f, 0.13f, 0.95f), new Vector2(900f, 46f), Vector2.zero,
                                     TextAlignmentOptions.Left);
        RectTransform msgRt = _message.rectTransform;
        Level2UI.Anchor(msgRt, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                        new Vector2(-630f, -405f), new Vector2(900f, 46f));

        Level2UI.MakeButton(panelImg.transform, "Hint", "提示", _font, 24f, new Vector2(150f, 58f), Vector2.zero,
                            OnHint, _buttonSprite, Color.white, Level2UI.BodyBrown);
        RectTransform hintRt = panelImg.transform.Find("Hint").GetComponent<RectTransform>();
        Level2UI.Anchor(hintRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(438f, -405f), new Vector2(150f, 58f));

        _confirmButton = Level2UI.MakeButton(panelImg.transform, "Confirm", "确认", _font, 24f,
                                             new Vector2(150f, 58f), Vector2.zero, OnConfirm,
                                             _buttonSprite, Color.white, Level2UI.BodyBrown);
        Level2UI.Anchor(_confirmButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f), new Vector2(608f, -405f), new Vector2(150f, 58f));

        _root.SetActive(false);
    }

    public void Open(Action onSolved, Action onClosed)
    {
        _onSolved = onSolved;
        _onClosed = onClosed;
        _busy = false;
        _solved = false;
        _selected = -1;
        _message.text = "";
        _root.SetActive(true);
        UpdateVisuals();
    }

    private void OnTrayClicked(int item)
    {
        if (_busy) return;
        if (_itemPlaced[item])
        {
            int slot = IndexOfSlot(item);
            if (slot >= 0) _slotItem[slot] = -1;
            _itemPlaced[item] = false;
        }
        _selected = _selected == item ? -1 : item;
        UpdateVisuals();
    }

    private void OnSlotClicked(int slot)
    {
        if (_busy) return;
        if (_slotItem[slot] >= 0)
        {
            int item = _slotItem[slot];
            _slotItem[slot] = -1;
            _itemPlaced[item] = false;
            _selected = item;
            UpdateVisuals();
            return;
        }
        if (_selected < 0) return;
        _slotItem[slot] = _selected;
        _itemPlaced[_selected] = true;
        _selected = -1;
        UpdateVisuals();
    }

    private int IndexOfSlot(int item)
    {
        for (int i = 0; i < 6; i++) if (_slotItem[i] == item) return i;
        return -1;
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < 6; i++)
        {
            bool filled = _slotItem[i] >= 0;
            _slotNumbers[i].gameObject.SetActive(!filled);
            _slotFlowers[i].gameObject.SetActive(filled);
            if (filled) _slotFlowers[i].sprite = _withered[_slotItem[i]];
        }

        for (int i = 0; i < 6; i++)
        {
            bool placed = _itemPlaced[i];
            _trayFlowers[i].gameObject.SetActive(!placed);
            if (_selected == i)
                _trayFrames[i].color = new Color(0.86f, 0.68f, 0.32f, 0.62f);
            else
                _trayFrames[i].color = placed ? new Color(0.40f, 0.31f, 0.21f, 0.08f)
                                              : new Color(0.40f, 0.31f, 0.21f, 0.22f);
        }
    }

    private void OnHint()
    {
        if (_busy) return;
        _message.color = new Color(0.30f, 0.22f, 0.14f, 0.95f);
        _message.text = "留意花瓣的数量、形状，还有残留的那一点颜色。";
    }

    private void OnConfirm()
    {
        if (_busy || _solved) return;

        for (int i = 0; i < 6; i++)
        {
            if (_slotItem[i] < 0)
            {
                _message.color = new Color(0.55f, 0.20f, 0.13f, 0.95f);
                _message.text = "先把所有枯花都放回花坛吧。";
                return;
            }
        }

        bool correct = true;
        for (int i = 0; i < 6; i++)
        {
            if (_slotItem[i] != _correctItemForSlot[i]) { correct = false; break; }
        }

        if (correct)
        {
            _busy = true;
            _solved = true;
            _message.color = new Color(0.24f, 0.44f, 0.20f, 1f);
            _message.text = "花坛重新焕发了生机。";
            StartCoroutine(SuccessRoutine());
        }
        else
        {
            _message.color = new Color(0.55f, 0.20f, 0.13f, 0.95f);
            _message.text = "有些花放错了位置……再仔细看看花瓣的形状。";
            if (_busy) return;
            StartCoroutine(ShakeRoutine());
        }
    }

    private IEnumerator SuccessRoutine()
    {
        yield return new WaitForSecondsRealtime(0.95f);
        _root.SetActive(false);
        SelectDeselect();
        Action cb = _onSolved;
        _onSolved = null;
        _onClosed = null;
        if (cb != null) cb();
    }

    private IEnumerator ShakeRoutine()
    {
        _busy = true;
        Vector2 basePos = _panelRt.anchoredPosition;
        float t = 0f;
        while (t < 0.26f)
        {
            t += Time.unscaledDeltaTime;
            float amp = Mathf.Lerp(12f, 0f, t / 0.26f);
            _panelRt.anchoredPosition = basePos + new Vector2(Mathf.Sin(t * 70f) * amp, 0f);
            yield return null;
        }
        _panelRt.anchoredPosition = basePos;
        _busy = false;
    }

    private void Close()
    {
        if (_busy) return;
        _root.SetActive(false);
        SelectDeselect();
        Action cb = _onClosed;
        _onClosed = null;
        _onSolved = null;
        if (cb != null) cb();
    }

    private void SelectDeselect()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }
}
