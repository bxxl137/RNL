using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 挂在触发区域物体上（Collider2D 需勾 Is Trigger）。
/// 玩家进入区域时，向指定的 TMP 文字组件输出一句话。
/// </summary>
public class 老比灯 : MonoBehaviour
{
    [Header("文字组件")]
    [SerializeField] private TMP_Text targetText;   // 拖场景里的 TMP 文字（UGUI 或 3D 都可以）

    [Header("台词")]
    [SerializeField] private string line = "楼下的灯也是这样的……";

    [Header("显示节奏")]
    [SerializeField] private bool typewriter = true;      // 是否逐字显示
    [SerializeField] private float charsPerSecond = 25f;  // 每秒字数
    [SerializeField] private float holdTime = 2f;         // 全部显示后停留几秒
    [SerializeField] private bool fadeOut = true;         // 是否淡出

    [Header("触发限制")]
    [SerializeField] private bool onlyOnce = true;        // 只播一次

    private Coroutine _routine;
    private bool _played;

    private void Awake()
    {
        // 初始隐藏，避免场景里露出一坨默认文字
        if (targetText != null) SetAlpha(targetText, 0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (onlyOnce && _played) return;
        if (targetText == null)
        {
            Debug.LogWarning("[TriggerLineSpeaker] 没有指定 TMP 文字组件");
            return;
        }

        _played = true;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        SetAlpha(targetText, 1f);

        if (typewriter)
        {
            targetText.text = line;
            targetText.maxVisibleCharacters = 0;
            targetText.ForceMeshUpdate();
            int total = targetText.textInfo.characterCount;

            float interval = 1f / Mathf.Max(1f, charsPerSecond);
            for (int i = 1; i <= total; i++)
            {
                targetText.maxVisibleCharacters = i;
                yield return new WaitForSeconds(interval);
            }
            targetText.maxVisibleCharacters = total;
        }
        else
        {
            targetText.text = line;
        }

        // 停留
        yield return new WaitForSeconds(holdTime);

        // 淡出
        if (fadeOut)
        {
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

        _routine = null;
    }

    private void SetAlpha(TMP_Text txt, float a)
    {
        Color c = txt.color;
        c.a = Mathf.Clamp01(a);
        txt.color = c;
    }
}

