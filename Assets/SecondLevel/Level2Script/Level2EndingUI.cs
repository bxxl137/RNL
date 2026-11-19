using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 结局：四个节点由暖光连成一体，渐暗，进入收尾独白，最后回到标题。
/// </summary>
public class Level2EndingUI : MonoBehaviour
{
    private TMP_FontAsset _font;
    private Sprite _glowSprite;
    private Sprite _dotSprite;
    private Sprite _buttonSprite;

    private GameObject _backdropRoot;
    private CanvasGroup _backdropGroup;
    private Image[] _nodeGlows = new Image[4];
    private GameObject _titleRoot;
    private TextMeshProUGUI _promptText;
    private bool _loading;
    private float _titleShownAt;
    private Action _onReturnTitle;

    public void Build(Transform canvas, TMP_FontAsset font, Sprite glow, Sprite dot, Sprite button)
    {
        _font = font;
        _glowSprite = glow;
        _dotSprite = dot;
        _buttonSprite = button;

        BuildBackdrop(canvas);
        BuildTitle(canvas);
    }

    private void BuildBackdrop(Transform canvas)
    {
        _backdropRoot = Level2UI.Create("EndingBackdrop", canvas);
        RectTransform rrt = _backdropRoot.GetComponent<RectTransform>();
        rrt.anchorMin = Vector2.zero;
        rrt.anchorMax = Vector2.one;
        rrt.offsetMin = Vector2.zero;
        rrt.offsetMax = Vector2.zero;
        _backdropGroup = _backdropRoot.AddComponent<CanvasGroup>();
        _backdropGroup.alpha = 0f;

        Image black = Level2UI.MakeImage(_backdropRoot.transform, "Black", null, new Vector2(1920f, 1080f),
                                         Vector2.zero, new Color(0.015f, 0.02f, 0.035f, 0.94f), false, false);
        Level2UI.Stretch(black.rectTransform, Vector2.zero, Vector2.zero);

        GameObject constellation = Level2UI.Create("Constellation", _backdropRoot.transform);
        RectTransform crt = constellation.GetComponent<RectTransform>();
        Level2UI.Anchor(crt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f),
                        new Vector2(1100f, 650f));

        Vector2[] pts =
        {
            new Vector2(0f, 190f),
            new Vector2(360f, 10f),
            new Vector2(0f, -190f),
            new Vector2(-360f, 10f),
        };

        GameObject lineGo = Level2UI.Create("Lines", constellation.transform);
        RectTransform lrt = lineGo.GetComponent<RectTransform>();
        Level2UI.Anchor(lrt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                        new Vector2(1100f, 650f));
        Level2LineGraphic line = lineGo.AddComponent<Level2LineGraphic>();
        line.color = new Color(1f, 0.76f, 0.40f, 0.72f);
        line.thickness = 3.2f;
        line.raycastTarget = false;
        line.SetPoints(pts, true);

        string[] labels = { "棋盘", "早餐店", "花园", "裁缝铺" };
        Vector2[] labelPos =
        {
            new Vector2(0f, 128f),
            new Vector2(360f, -42f),
            new Vector2(0f, -258f),
            new Vector2(-360f, -42f),
        };

        for (int i = 0; i < 4; i++)
        {
            Level2UI.MakeImage(constellation.transform, "GlowOuter" + i, _glowSprite, new Vector2(240f, 240f),
                               pts[i], new Color(1f, 0.82f, 0.52f, 0.16f));
            _nodeGlows[i] = Level2UI.MakeImage(constellation.transform, "Glow" + i, _glowSprite,
                                               new Vector2(110f, 110f), pts[i],
                                               new Color(1f, 0.84f, 0.56f, 0.62f));
            Level2UI.MakeImage(constellation.transform, "Dot" + i, _dotSprite, new Vector2(28f, 28f),
                               pts[i], Level2UI.Warm);
            Level2UI.MakeText(constellation.transform, "Label" + i, labels[i], _font, 23f,
                              new Color(0.95f, 0.89f, 0.76f, 0.9f), new Vector2(160f, 34f), labelPos[i]);
        }

        Level2UI.MakeText(_backdropRoot.transform, "Subtitle", "微光连接着每一个善意的瞬间", _font, 24f,
                          new Color(0.94f, 0.88f, 0.76f, 0.78f), new Vector2(1200f, 40f), Vector2.zero);
        RectTransform srt = _backdropRoot.transform.Find("Subtitle").GetComponent<RectTransform>();
        Level2UI.Anchor(srt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(0f, 78f), new Vector2(1200f, 40f));

        _backdropRoot.SetActive(false);
    }

    private void BuildTitle(Transform canvas)
    {
        _titleRoot = Level2UI.Create("EndingTitle", canvas);
        RectTransform rrt = _titleRoot.GetComponent<RectTransform>();
        rrt.anchorMin = Vector2.zero;
        rrt.anchorMax = Vector2.one;
        rrt.offsetMin = Vector2.zero;
        rrt.offsetMax = Vector2.zero;

        Image black = Level2UI.MakeImage(_titleRoot.transform, "Black", null, new Vector2(1920f, 1080f),
                                         Vector2.zero, new Color(0.01f, 0.015f, 0.025f, 1f), false, false);
        Level2UI.Stretch(black.rectTransform, Vector2.zero, Vector2.zero);

        for (int i = 0; i < 3; i++)
        {
            Level2UI.MakeImage(_titleRoot.transform, "Star" + i, _glowSprite,
                               new Vector2(90f + i * 30f, 90f + i * 30f),
                               new Vector2(-420f + i * 420f, 180f - i * 90f),
                               new Color(1f, 0.84f, 0.55f, 0.18f));
        }

        TextMeshProUGUI title = Level2UI.MakeText(_titleRoot.transform, "Title", "第二关 · 完", _font, 76f,
                                                  Level2UI.Cream, new Vector2(1400f, 110f),
                                                  new Vector2(0f, 100f), TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        title.outlineWidth = 0.18f;
        title.outlineColor = new Color(0.35f, 0.22f, 0.10f, 0.8f);

        Level2UI.MakeText(_titleRoot.transform, "Thanks", "谢谢游玩", _font, 32f,
                          new Color(0.90f, 0.83f, 0.68f, 0.92f), new Vector2(900f, 60f),
                          new Vector2(0f, 10f));

        _promptText = Level2UI.MakeText(_titleRoot.transform, "Prompt", "按 Enter 返回标题", _font, 24f,
                                        new Color(0.86f, 0.79f, 0.64f, 0.85f), new Vector2(900f, 50f),
                                        new Vector2(0f, -110f));

        Level2UI.MakeButton(_titleRoot.transform, "Back", "返回标题", _font, 24f, new Vector2(200f, 62f),
                            new Vector2(0f, -200f), ReturnToTitle, _buttonSprite, Color.white, Level2UI.BodyBrown);

        Level2UI.MakeText(_titleRoot.transform, "Hint", "感谢你连接起这个充满真善美的世界。", _font, 20f,
                          new Color(0.80f, 0.74f, 0.62f, 0.55f), new Vector2(1200f, 36f),
                          new Vector2(0f, -286f));

        _titleRoot.SetActive(false);
    }

    public void Play(List<Level2Line> lines, Level2DialogueUI dialogue, Action onReturnTitle)
    {
        _onReturnTitle = onReturnTitle;
        _backdropRoot.SetActive(true);
        StartCoroutine(PlayRoutine(lines, dialogue));
    }

    private IEnumerator PlayRoutine(List<Level2Line> lines, Level2DialogueUI dialogue)
    {
        _backdropGroup.alpha = 0f;
        float t = 0f;
        while (t < 1.15f)
        {
            t += Time.unscaledDeltaTime;
            _backdropGroup.alpha = Mathf.Clamp01(t / 1.15f);
            yield return null;
        }
        _backdropGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(1.35f);

        if (dialogue != null)
            dialogue.Show(lines, ShowTitle);
        else
            ShowTitle();
    }

    private void ShowTitle()
    {
        _titleRoot.SetActive(true);
        _titleShownAt = Time.unscaledTime;
        _loading = false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void Update()
    {
        if (_backdropRoot != null && _backdropRoot.activeSelf)
        {
            for (int i = 0; i < 4; i++)
            {
                if (_nodeGlows[i] == null) continue;
                float a = 0.55f + 0.18f * Mathf.Sin(Time.unscaledTime * 1.7f + i);
                _nodeGlows[i].color = new Color(1f, 0.84f, 0.56f, a);
            }
        }

        if (_titleRoot != null && _titleRoot.activeSelf)
        {
            float blink = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f));
            Color c = _promptText.color;
            c.a = blink;
            _promptText.color = c;

            if (Time.unscaledTime - _titleShownAt > 0.5f &&
                (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                ReturnToTitle();
            }
        }
    }

    private void ReturnToTitle()
    {
        if (_loading) return;
        _loading = true;
        if (_onReturnTitle != null) _onReturnTitle();
        else SceneManager.LoadScene("StartScene");
    }
}
