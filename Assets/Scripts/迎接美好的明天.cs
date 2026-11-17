using UnityEngine;

/// <summary>
/// 挂在 tiggerplace5 上（Collider2D 需勾 Is Trigger）。
/// 玩家在触发区域内按 Return：noon 失活，day 激活。
/// </summary>
public class 迎接美好的明天 : MonoBehaviour
{
    [Header("切换目标")]
    [SerializeField] private GameObject noonObject;   // 拖 secondfloor/noon
    [SerializeField] private GameObject dayObject;    // 拖 secondfloor/day

    [Header("按键")]
    [SerializeField] private KeyCode confirmKey = KeyCode.Return;
    [SerializeField] private KeyCode confirmKeyAlt = KeyCode.KeypadEnter;  // 小键盘回车

    [Header("触发限制")]
    [SerializeField] private bool onlyOnce = true;    // 只切一次
    [Header("掉落物品数据（对接背包）")]
    [SerializeField] private string itemId = "item_03";
    [SerializeField] private string itemName = "微光碎片3";
    [SerializeField] private string iconName = "Glow_Shard";
    [SerializeField] private int itemAmount = 1;

    private bool _playerInside;
    private bool _switched;

    private void Start()
    {
        // 场景开始时确保状态正确：day 关、noon 开
        if (noonObject != null) noonObject.SetActive(true);
        if (dayObject != null) dayObject.SetActive(false);
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

    private void Update()
    {
        if (!_playerInside) return;
        if (onlyOnce && _switched) return;

        if (Input.GetKeyDown(confirmKey) || Input.GetKeyDown(confirmKeyAlt))
        {
            Switch();
            confirm();
        }
    }

    private void Switch()
    {
        if (noonObject == null || dayObject == null)
        {
            Debug.LogWarning("[DayNoonSwitcher] noon 或 day 没有拖到引用里");
            return;
        }

        noonObject.SetActive(false);
        dayObject.SetActive(true);

        _switched = true;
        Debug.Log("[切换] noon 已关闭，day 已开启");
    }
    private void confirm()
    {
        InventoryManager.Instance.AddItem(itemId, itemName, iconName, itemAmount);
    }
}

