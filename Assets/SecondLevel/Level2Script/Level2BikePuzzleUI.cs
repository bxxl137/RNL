using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


/// <summary>
/// 自行车棚谜题：检查三辆倒下的自行车，找出断掉的车脚架和车篮里的包裹。
/// 贴纸是彩蛋，不影响解谜。
/// </summary>
public class Level2BikePuzzleUI : MonoBehaviour
{
    private TMP_FontAsset _font;
    private GameObject _root;
    private RectTransform _panelRt;
    private TextMeshProUGUI _message;
    private GameObject _takeButton;
    private TextMeshProUGUI[] _checks = new TextMeshProUGUI[3];


    private bool _foundWheel;
    private bool _foundPackage;
    private bool _foundSticker;
    private bool _busy;
    private Action _onSolved;
    private Action _onClosed;


    public bool IsOpen { get { return _root != null && _root.activeSelf; } }
    public bool FoundWheel { get { return _foundWheel; } }
    public bool FoundPackage { get { return _foundPackage; } }
    public bool FoundSticker { get { return _foundSticker; } }


    public void Build(Transform canvas, TMP_FontAsset font, Sprite panel, Sprite button, Sprite slot,
                      Sprite[] bikes)
    {
        _font = font;


        _root = Level2UI.Create("BikePuzzle", canvas);
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


        TextMeshProUGUI title = Level2UI.MakeText(panelImg.transform, "Title", "自行车棚 · 倒下的车", _font, 38f,
                                                  Level2UI.BodyBrown, new Vector2(900f, 60f), Vector2.zero,
                                                  TextAlignmentOptions.Left);
        Level2UI.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(46f, -26f), new Vector2(900f, 60f));


        Level2UI.MakeButton(panelImg.transform, "Exit", "退出", _font, 24f, new Vector2(116f, 56f), Vector2.zero,
                            Close, button, Color.white, Level2UI.BodyBrown);
        RectTransform exitRt = panelImg.transform.Find("Exit").GetComponent<RectTransform>();
        Level2UI.Anchor(exitRt, new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-54f, -30f), new Vector2(116f, 56f));


        TextMeshProUGUI hint = Level2UI.MakeText(panelImg.transform, "Hint", "点击自行车检查（可随时离开）",
                                                 _font, 24f, new Color(0.32f, 0.23f, 0.15f, 0.95f),
                                                 new Vector2(900f, 40f), Vector2.zero, TextAlignmentOptions.Left);
        Level2UI.Anchor(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                        new Vector2(-630f, 330f), new Vector2(900f, 40f));


        string[] names = { "白色自行车", "粉色自行车", "蓝色自行车" };
        float[] xs = { -420f, 0f, 420f };
        for (int i = 0; i < 3; i++)
        {
            Image hit = Level2UI.MakeImage(panelImg.transform, "Bike" + i, null, new Vector2(380f, 320f),
                                           new Vector2(xs[i], 100f), new Color(1f, 1f, 1f, 0f), false, true);
            Button btn = hit.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            int index = i;
            btn.onClick.AddListener(() => Inspect(index));


            if (i < bikes.Length && bikes[i] != null)
            {
                Image img = Level2UI.MakeImage(hit.transform, "Sprite", bikes[i], new Vector2(370f, 310f),
                                               Vector2.zero, Color.white, true);
                img.raycastTarget = false;
            }


            TextMeshProUGUI label = Level2UI.MakeText(panelImg.transform, "Name" + i, names[i], _font, 24f,
                                                      new Color(0.32f, 0.23f, 0.15f, 0.95f),
                                                      new Vector2(280f, 36f), new Vector2(xs[i], -80f));
            label.raycastTarget = false;


            _checks[i] = Level2UI.MakeText(panelImg.transform, "Check" + i, "已检查", _font, 20f,
                                           new Color(0.24f, 0.44f, 0.20f, 1f),
                                           new Vector2(200f, 30f), new Vector2(xs[i], -112f));
            _checks[i].gameObject.SetActive(false);
        }


        Image msgBox = Level2UI.MakeImage(panelImg.transform, "MessageBox", slot, new Vector2(1200f, 180f),
                                          new Vector2(0f, -250f), Color.white, false, false);
        _message = Level2UI.MakeText(msgBox.transform, "Message",
                                     "三辆自行车倒在一起，先看看它们的情况吧。",
                                     _font, 25f, new Color(0.94f, 0.86f, 0.70f, 0.95f),
                                     new Vector2(1120f, 150f), Vector2.zero, TextAlignmentOptions.Left);


        _takeButton = Level2UI.MakeButton(panelImg.transform, "Take", "拿走包裹", _font, 24f,
                                          new Vector2(180f, 60f), new Vector2(570f, -402f),
                                          TakePackage, button, Color.white, Level2UI.BodyBrown).gameObject;
        _takeButton.SetActive(false);


        _root.SetActive(false);
    }


    public void Open(Action onSolved, Action onClosed)
    {
        _onSolved = onSolved;
        _onClosed = onClosed;
        _foundWheel = false;
        _foundPackage = false;
        _foundSticker = false;
        _busy = false;
        _takeButton.SetActive(false);
        for (int i = 0; i < 3; i++) _checks[i].gameObject.SetActive(false);
        _message.text = "三辆自行车倒在一起，先看看它们的情况吧。";
        _root.SetActive(true);
    }


    private void Inspect(int index)
    {
        if (_busy) return;

        bool isNew;     // ★ 是否是新发现（用于只在"第一次查到"时播提示音）

        if (index == 0)
        {
            isNew = !_foundWheel;
            _foundWheel = true;
            _message.text = "车脚架断了，车身歪倒在地上。";
        }
        else if (index == 1)
        {
            isNew = !_foundPackage;
            _foundPackage = true;
            _message.text = "车篮里有一个用麻绳捆着的包裹，上面别着一张写着「阿婆早餐店」的纸条。";
        }
        else
        {
            isNew = !_foundSticker;
            _foundSticker = true;
            _message.text = "车座下有一张贴纸翘起了一角，隐约写着「邻光重连」。";
        }
        _checks[index].gameObject.SetActive(true);

        // ★ 新增：首次发现线索 → 播"节点点亮"音效
        

        if (_foundWheel && _foundPackage)
        {
            _message.text = "你找到了断掉的车脚架，也找到了车篮里的包裹。";
            _takeButton.SetActive(true);
        }
    }


    private void TakePackage()
    {
        if (_busy) return;
        _busy = true;

        // ★ 新增：拿走包裹 = 解开这个谜题 → 播放解密成功音效
        Level2SFX.PlayForce(Level2SFX.PuzzleSolved, 1f);

        Finish(true);
    }


    private void Close()
    {
        if (_busy) return;
        _busy = true;
        Finish(_foundWheel && _foundPackage);
    }


    private void Finish(bool solved)
    {
        _root.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        Action solvedCb = _onSolved;
        Action closedCb = _onClosed;
        _onSolved = null;
        _onClosed = null;
        if (solved)
        {
            if (solvedCb != null) solvedCb();
        }
        else
        {
            if (closedCb != null) closedCb();
        }
    }
}
