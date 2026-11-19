using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


/// <summary>
/// 第二关底部对话框。点击/Enter/空格推进，逐字显示。
/// </summary>
public class Level2DialogueUI : MonoBehaviour
{
    private TMP_FontAsset _font;
    private GameObject _root;
    private TextMeshProUGUI _body;
    private TextMeshProUGUI _speaker;
    private TextMeshProUGUI _arrow;


    private List<Level2Line> _lines;
    private int _index;
    private bool _typing;
    private bool _waiting;
    private float _ignoreInputUntil;
    private Coroutine _routine;
    private Action _onDone;


    public bool IsOpen { get; private set; }


    public void Build(Transform canvas, TMP_FontAsset font, Sprite boxSprite)
    {
        _font = font;


        _root = Level2UI.Create("DialogueRoot", canvas);
        RectTransform rt = _root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 24f);
        rt.sizeDelta = new Vector2(1340f, 290f);


        Level2UI.MakeImage(_root.transform, "Box", boxSprite, new Vector2(1340f, 290f), Vector2.zero,
                           Color.white, false, false);


        GameObject speakerGo = Level2UI.Create("Speaker", _root.transform);
        _speaker = speakerGo.AddComponent<TextMeshProUGUI>();
        _speaker.font = font;
        _speaker.fontSize = 27f;
        _speaker.fontStyle = FontStyles.Bold;
        _speaker.alignment = TextAlignmentOptions.Left;
        _speaker.raycastTarget = false;
        RectTransform srt = speakerGo.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 1f);
        srt.anchorMax = new Vector2(0f, 1f);
        srt.pivot = new Vector2(0f, 1f);
        srt.anchoredPosition = new Vector2(42f, -15f);
        srt.sizeDelta = new Vector2(280f, 56f);


        GameObject bodyGo = Level2UI.Create("Body", _root.transform);
        _body = bodyGo.AddComponent<TextMeshProUGUI>();
        _body.font = font;
        _body.fontSize = 33f;
        _body.lineSpacing = 8f;
        _body.color = Level2UI.BodyBrown;
        _body.alignment = TextAlignmentOptions.TopLeft;
        _body.enableWordWrapping = true;
        _body.overflowMode = TextOverflowModes.Overflow;
        _body.raycastTarget = false;
        RectTransform brt = bodyGo.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(120f, 46f);
        brt.offsetMax = new Vector2(-120f, -76f);


        _arrow = Level2UI.MakeText(_root.transform, "Arrow", "▼", font, 24f,
                                  new Color32(118, 80, 42, 255), new Vector2(40f, 36f), Vector2.zero,
                                  TextAlignmentOptions.Center);
        RectTransform art = _arrow.rectTransform;
        art.anchorMin = new Vector2(1f, 0f);
        art.anchorMax = new Vector2(1f, 0f);
        art.pivot = new Vector2(1f, 0f);
        art.anchoredPosition = new Vector2(-40f, 24f);
        art.sizeDelta = new Vector2(40f, 36f);
        _arrow.raycastTarget = false;


        _root.SetActive(false);
    }


    public void Show(List<Level2Line> lines, Action onDone)
    {
        if (IsOpen)
        {
            Debug.LogWarning("[Level2] 对话已经打开，忽略新的对话请求。");
            return;
        }
        if (lines == null || lines.Count == 0)
        {
            if (onDone != null) onDone();
            return;
        }


        _lines = lines;
        _index = 0;
        _onDone = onDone;
        IsOpen = true;
        _root.SetActive(true);
        _ignoreInputUntil = Time.unscaledTime + 0.16f;
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PlayRoutine());
    }


    private IEnumerator PlayRoutine()
    {
        for (_index = 0; _index < _lines.Count; _index++)
        {
            Level2Line line = _lines[_index];
            if (line == null || string.IsNullOrEmpty(line.text)) continue;


            _speaker.text = line.speaker ?? "";
            _speaker.gameObject.SetActive(!string.IsNullOrEmpty(line.speaker));
            _speaker.color = SpeakerColor(line.speaker);

            // ★ 新增：每行对话开始，按说话人播一次语音
            PlaySpeakerVoice(line.speaker);


            _body.text = line.text;
            _body.maxVisibleCharacters = 0;
            _body.ForceMeshUpdate(true);
            int total = _body.textInfo.characterCount;


            _typing = true;
            _waiting = false;
            _arrow.gameObject.SetActive(false);


            float shown = 0f;
            const float charsPerSecond = 30f;
            while (_typing && _body.maxVisibleCharacters < total)
            {
                shown += Time.deltaTime * charsPerSecond;
                _body.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
                yield return null;
            }
            _typing = false;
            _body.maxVisibleCharacters = total;


            _waiting = true;
            while (_waiting) yield return null;
            _arrow.gameObject.SetActive(false);
            yield return null;
        }


        IsOpen = false;
        _routine = null;
        Action cb = _onDone;
        _onDone = null;
        if (cb != null) cb();
        if (!IsOpen) _root.SetActive(false);
    }


    // ★ 新增：按说话人分派语音
    private static void PlaySpeakerVoice(string speaker)
{
    string clip;

    if (string.IsNullOrEmpty(speaker))
    {
        clip = Level2SFX.Narration;
    }
    else
    {
        string s = speaker.ToLowerInvariant();

        if (s.Contains("园丁"))
            clip = Level2SFX.GardenerTalk;
        else if (s.Contains("阿婆") || s.Contains("奶奶"))
            clip = Level2SFX.OldLadyTalk;
        else if (s.Contains("老板") || s.Contains("早餐"))
            clip = Level2SFX.BossTalk;
        else if (s.Contains("裁缝"))
            clip = Level2SFX.TailorTalk;
        else if (s.Contains("工人"))
            clip = Level2SFX.WorkerTalk;
        else
            clip = Level2SFX.PlayerTalk;
    }

    Level2SFX.Play(clip, 1f);   // ← 从 AudioManager.Instance.PlaySFXByName 改成这个
}



    private void Update()
    {
        if (!IsOpen) return;


        if (_waiting)
        {
            _arrow.gameObject.SetActive(true);
            float a = 0.25f + 0.75f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4.2f));
            Color c = _arrow.color;
            c.a = a;
            _arrow.color = c;
        }
        else
        {
            _arrow.gameObject.SetActive(false);
        }


        if (Time.unscaledTime < _ignoreInputUntil) return;


        bool advance = Input.GetKeyDown(KeyCode.Return)
                       || Input.GetKeyDown(KeyCode.KeypadEnter)
                       || Input.GetKeyDown(KeyCode.Space)
                       || Input.GetMouseButtonDown(0);
        if (!advance) return;


        if (_typing)
        {
            _typing = false;
            _body.maxVisibleCharacters = 99999;
        }
        else if (_waiting)
        {
            _waiting = false;
        }
    }


    private static Color SpeakerColor(string speaker)
    {
        switch (speaker)
        {
            case "阿婆": return new Color32(146, 62, 45, 255);
            case "园丁": return new Color32(72, 104, 54, 255);
            case "裁缝": return new Color32(52, 78, 110, 255);
            default: return new Color32(86, 62, 38, 255);
        }
    }
}
