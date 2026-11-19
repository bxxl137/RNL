using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 挂在 tiggerplace6 上（Collider2D 需勾 Is Trigger）。
/// 1. day 被激活后，自动激活 worker/humannpc
/// 2. humannpc 已激活 + 背包有"微光碎片3" ×1 的前提下，玩家在区域内按 Return：
///    扣除 1 个微光碎片3，并播放 Canvas/Panel/独白 的台词序列
/// </summary>
public class lightrepair : MonoBehaviour
{
    [System.Serializable]
    public class LineConfig
    {
        [TextArea(2, 4)] public string text = "";
        [Tooltip("文字颜色。Alpha 为 0 时自动按白色处理")]
        public Color color = Color.white;
        [Tooltip("这句说出口后，等多久再说下一句")]
        public float gapAfter = 1f;
        [Tooltip("是否逐字显示")]
        public bool typewriter = true;
        [Tooltip("每秒字数，0 用全局设置")]
        public float charsPerSecond = 0f;

        public Color SafeColor
        {
            get
            {
                if (color.a <= 0.01f) return Color.white;
                return new Color(color.r, color.g, color.b, 1f);
            }
        }
    }

    [Header("场景状态")]
    [Tooltip("拖 secondfloor/day，作为激活条件")]
    [SerializeField] private GameObject dayObject;

    [Header("NPC")]
    [Tooltip("拖 worker/humannpc，day 激活后会被 SetActive(true)")]
    [SerializeField] private GameObject humanNpc;

    [Header("交互道具要求")]
    [Tooltip("需要消耗的道具 Id，必须和背包里 AddItem 时的 itemId 完全一致")]
    [SerializeField] private string requiredItemId = "shard_glow_3";
    [Tooltip("道具显示名，仅用于提示文字")]
    [SerializeField] private string requiredItemName = "微光碎片3";
    [SerializeField] private int requiredAmount = 1;

    [Header("按键")]
    [SerializeField] private KeyCode confirmKey = KeyCode.Return;
    [SerializeField] private KeyCode confirmKeyAlt = KeyCode.KeypadEnter;

    [Header("遮罩")]
    [Tooltip("拖 Canvas/Panel")]
    [SerializeField] private GameObject maskPanel;
    [SerializeField] private bool hideMaskOnExit = true;

    [Header("文字组件")]
    [Tooltip("拖 Canvas/Panel/独白")]
    [SerializeField] private TMP_Text targetText;

    [Header("台词序列")]
    [SerializeField] private List<LineConfig> lines = new List<LineConfig>();

    [Header("全局节奏")]
    [SerializeField] private float defaultCharsPerSecond = 22f;
    [SerializeField] private float holdTime = 1.5f;

    [Header("触发限制")]
    [SerializeField] private bool onlyOnce = true;

    private Coroutine _routine;
    private bool _played;
    private bool _playerInside;
    private bool _npcActivated;
    private bool _dayChecked;

    private void Awake()
    {
        if (maskPanel != null) maskPanel.SetActive(false);

        if (targetText != null)
        {
            targetText.text = "";
            targetText.maxVisibleCharacters = 99999;
            SetAlpha(targetText, 0f);
        }
        else
        {
            Debug.LogWarning("[工人对话] targetText 未赋值，文字不会显示");
        }
    }

    private void Start()
    {
        if (humanNpc != null) humanNpc.SetActive(false);
    }

    private void Update()
    {
        // ① 持续检测 day，激活后开启 NPC（只做一次）
        if (!_npcActivated)
        {
            if (dayObject != null && dayObject.activeInHierarchy)
            {
                if (humanNpc != null) humanNpc.SetActive(true);
                _npcActivated = true;
                Debug.Log("[工人对话] day 已激活，humannpc 已开启");
            }
            else if (!_dayChecked)
            {
                Debug.Log($"[工人对话] 等待 day 激活，当前 dayObject = {(dayObject == null ? "null" : dayObject.name)}");
                _dayChecked = true;
            }
        }

        // ② 区域内 + NPC 已显示 + 按下 Return → 尝试交互
        if (!_playerInside) return;
        if (!_npcActivated) return;
        if (onlyOnce && _played) return;
        if (_routine != null) return;

        if (Input.GetKeyDown(confirmKey) || Input.GetKeyDown(confirmKeyAlt))
        {
            TryInteract();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInside = false;
    }

    /// <summary>尝试交互：先检查道具，够才扣除并播台词。</summary>
    private void TryInteract()
    {
        if (targetText == null)
        {
            Debug.LogWarning("[工人对话] 没有指定 TMP 文字组件");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[工人对话] InventoryManager.Instance 为空，无法检测道具");
            return;
        }

        // 先查有没有
        if (!InventoryManager.Instance.HasItem(requiredItemId, requiredAmount))
        {
            Debug.Log($"[工人对话] 缺少道具：{requiredItemName} ×{requiredAmount}，无法交互");
            return;   // 不播台词、不扣东西
        }

        // 再扣
        bool removed = InventoryManager.Instance.RemoveItem(requiredItemId, requiredAmount);
        if (!removed)
        {
            Debug.LogWarning("[工人对话] 道具扣除失败（可能刚好被其他逻辑消耗）");
            return;
        }

        Debug.Log($"[工人对话] 已消耗 {requiredItemName} ×{requiredAmount}，开始播放独白");

        _played = true;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        if (maskPanel != null) maskPanel.SetActive(true);

        if (targetText != null)
        {
            targetText.maxVisibleCharacters = 99999;
            SetAlpha(targetText, 1f);
        }

        if (lines == null || lines.Count == 0)
        {
            Debug.LogWarning("[工人对话] lines 列表为空，没有内容可播放");
        }

        foreach (var cfg in lines)
        {
            if (targetText == null) break;
            if (cfg == null) continue;
            if (string.IsNullOrEmpty(cfg.text)) continue;

            targetText.color = cfg.SafeColor;

            if (cfg.typewriter)
            {
                targetText.text = cfg.text;
                targetText.maxVisibleCharacters = 0;
                targetText.ForceMeshUpdate(true);
                int total = targetText.textInfo.characterCount;

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

            yield return new WaitForSeconds(holdTime);
            yield return new WaitForSeconds(cfg.gapAfter);

            targetText.text = "";
            yield return null;
        }

        if (targetText != null)
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
