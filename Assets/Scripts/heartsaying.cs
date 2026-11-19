using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 挂在触发区域物体上（Collider2D 需勾 Is Trigger）。
/// 玩家进入区域时：激活遮罩，按顺序逐句播放台词。
/// 每句可单独设置颜色、逐字速度与播放后间隔。
/// </summary>
public class heartsaying : MonoBehaviour
{
    [System.Serializable]
    public class LineConfig
    {
        [TextArea(2, 4)] public string text = "";
        [Tooltip("文字颜色。Alpha 为 0 时会被自动按白色处理，避免 Inspector 新建元素时透明不显示。")]
        public Color color = Color.white;
        [Tooltip("这句说出口后，等多久再说下一句")]
        public float gapAfter = 1f;
        [Tooltip("是否逐字显示")]
        public bool typewriter = true;
        [Tooltip("每秒字数，0 或其他值时用全局设置")]
        public float charsPerSecond = 0f;

        /// <summary>取得安全的显示颜色：透明度为 0 时回退到白色。</summary>
        public Color SafeColor
        {
            get
            {
                if (color.a <= 0.01f) return Color.white;
                return new Color(color.r, color.g, color.b, 1f);
            }
        }
    }

    [Header("遮罩")]
    [SerializeField] private GameObject maskPanel;
    [SerializeField] private bool hideMaskOnExit = true;

    [Header("文字组件")]
    [SerializeField] private TMP_Text targetText;

    [Header("台词序列")]
    [SerializeField] private List<LineConfig> lines = new List<LineConfig>();

    [Header("全局节奏")]
    [SerializeField] private float defaultCharsPerSecond = 22f;
    [SerializeField] private float holdTime = 1.5f;     // 每句说完后停留

    [Header("触发限制")]
    [SerializeField] private bool onlyOnce = true;

    public AudioClip clip1;
    public AudioClip clip2;

    private Coroutine _routine;
    private bool _played;

    private void Awake()
    {
        if (maskPanel != null) maskPanel.SetActive(false);

        if (targetText != null)
        {
            targetText.text = "";
            // 顺手把可能残留的可见字数恢复，避免上一次中途停止后文字被卡在隐藏状态
            targetText.maxVisibleCharacters = 99999;
            SetAlpha(targetText, 0f);
        }
        else
        {
            Debug.LogWarning("[台词] targetText 未赋值，文字不会显示");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (onlyOnce && _played) return;

        _played = true;
        if (_routine != null) StopCoroutine(_routine);
        AudioManager.Instance.PlayBGM(clip1, fadeTime: 1.5f);
        _routine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        if (maskPanel != null) maskPanel.SetActive(true);
        if (targetText != null) SetAlpha(targetText, 1f);

        if (lines == null || lines.Count == 0)
        {
            Debug.LogWarning("[台词] lines 列表为空，没有内容可播放");
        }

        foreach (var cfg in lines)
        {
            if (targetText == null) break;
            if (cfg == null) continue;
            if (string.IsNullOrEmpty(cfg.text)) continue;

            // 用 SafeColor：Alpha 为 0 的条目按白色显示，避免整体透明看不见
            targetText.color = cfg.SafeColor;

            if (cfg.typewriter)
            {
                targetText.text = cfg.text;
                targetText.maxVisibleCharacters = 0;
                targetText.ForceMeshUpdate(true);
                int total = targetText.textInfo.characterCount;

                // 极少数情况下 characterCount 为 0（字体缺字 / 富文本异常），直接整句显示兜底
                if (total <= 0)
                {
                    targetText.maxVisibleCharacters = 99999;
                }
                else
                {
                    float cps = cfg.charsPerSecond > 0f ? cfg.charsPerSecond : defaultCharsPerSecond;
                    float interval = 1f / Mathf.Max(1f, cps);

                    for (int i = 1; i <= total; i++)
                    {
                        targetText.maxVisibleCharacters = i;
                        yield return new WaitForSeconds(interval);
                    }
                    targetText.maxVisibleCharacters = total;
                }
            }
            else
            {
                targetText.text = cfg.text;
                targetText.maxVisibleCharacters = 99999;
                yield return null;
            }

            // 说完停顿，再等句后间隔
            yield return new WaitForSeconds(holdTime);
            yield return new WaitForSeconds(cfg.gapAfter);

            // 这句说完先清空，避免下一句接在旧文字后面
            targetText.text = "";

            yield return null;
        }

        // 全部说完，淡出收尾
        if (targetText != null)
        {
            AudioManager.Instance.PlayBGM(clip2, fadeTime: 1.5f);
            float t = 0f, dur = 0.6f;
            while (t < dur)
            {
                t += Time.deltaTime;
                SetAlpha(targetText, 1f - t / dur);
                yield return null;
            }
            SetAlpha(targetText, 0f);
            targetText.text = "";
        }

        if (hideMaskOnExit && maskPanel != null)
            maskPanel.SetActive(false);

        _routine = null;
    }

    private void SetAlpha(TMP_Text txt, float a)
    {
        Color c = txt.color;
        c.a = Mathf.Clamp01(a);
        txt.color = c;
    }
}
