using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 挂在触发区域物体上（Collider2D 需勾 Is Trigger）。
/// 第一次进入：播放台词。
/// 再次进入且 day 已激活：把"微光碎片"加入背包。
/// </summary>
public class 老比灯 : MonoBehaviour
{
    [Header("文字组件")]
    [SerializeField] private TMP_Text targetText;

    [Header("台词")]
    [SerializeField] private string line = "楼下的灯也是这样的……";

    [Header("显示节奏")]
    [SerializeField] private bool typewriter = true;
    [SerializeField] private float charsPerSecond = 25f;
    [SerializeField] private float holdTime = 2f;
    [SerializeField] private bool fadeOut = true;

    [Header("场景状态")]
    [Tooltip("拖 secondfloor/day，用来判断场景是否已切换")]
    [SerializeField] private GameObject dayObject;

    [Header("背包发放")]
    [SerializeField] private string itemId = "shard_glow";
    [SerializeField] private string itemName = "微光碎片";
    [SerializeField] private string iconName = "shard_glow";
    [SerializeField] private int addCount = 1;

    [Header("触发限制")]
    [SerializeField] private bool onlyOnce = true;      // 台词只播一次
    [SerializeField] private bool grantOnlyOnce = true; // 碎片只发一次

    private Coroutine _routine;
    private bool _played;          // 台词是否播过
    private bool _enteredBefore;   // 是否已经进过区域
    private bool _granted;         // 碎片是否已发放

    private void Awake()
    {
        if (targetText != null) SetAlpha(targetText, 0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // —— 台词分支：第一次进入播放 ——
        if (!(onlyOnce && _played) && targetText != null)
        {
            _played = true;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(PlayRoutine());
        }

        // —— 碎片分支：第二次及以后进入，且 day 已激活 ——
        if (grantOnlyOnce && _granted)
        {
            _enteredBefore = true;
            return;
        }

        if (!_enteredBefore)
        {
            // 第一次进入，只记录，不发碎片
            _enteredBefore = true;
            return;
        }

        // 第二次及以后
        bool dayActive = dayObject != null && dayObject.activeInHierarchy;
        if (!dayActive)
        {
            Debug.Log("[微光碎片] day 尚未激活，本次不发");
            return;
        }

        GrantShard();
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

        yield return new WaitForSeconds(holdTime);

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

    private void GrantShard()
    {
        _granted = true;

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[微光碎片] InventoryManager.Instance 为空，检查背包管理器是否已加载");
            return;
        }

        bool ok = InventoryManager.Instance.AddItem(
            itemId,
            itemName,
            iconName,
            addCount
        );

        if (ok)
            Debug.Log($"[微光碎片] 已加入背包：{itemName} ×{addCount}");
        else
            Debug.LogWarning("[微光碎片] 添加失败");
    }

    private void SetAlpha(TMP_Text txt, float a)
    {
        Color c = txt.color;
        c.a = Mathf.Clamp01(a);
        txt.color = c;
    }
}
