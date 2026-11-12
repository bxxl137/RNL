using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance;

    [Header("背包预制体")]
    public GameObject inventoryPrefab; // 把你的背包预制体拖到这里

    private GameObject inventoryInstance; // 运行时实例化的背包对象
    private bool isOpen = false; // 背包是否打开

    private void Awake()
    {
        // 单例模式，防止场景切换时重复生成
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (inventoryPrefab != null)
        {
            // 1. 实例化背包预制体
            inventoryInstance = Instantiate(inventoryPrefab);

            // 【修改点一：设为子物体】让背包跟着 GameManager 跨场景保留
            inventoryInstance.transform.SetParent(this.transform, false);

            // 【修改点四：重置位置】强制归零位置和缩放，防止 UI 飘到屏幕外
            RectTransform rect = inventoryInstance.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localPosition = Vector3.zero;
                rect.localScale = Vector3.one;
                rect.anchoredPosition = Vector2.zero;
            }

            inventoryInstance.SetActive(false); // 初始状态隐藏

            // 2. 精准查找名字叫 "cancel" 的按钮（支持多层级查找）
            Transform cancelBtnTransform = FindChildByName(inventoryInstance.transform, "cancel");

            if (cancelBtnTransform != null)
            {
                Button closeBtn = cancelBtnTransform.GetComponent<Button>();
                if (closeBtn != null)
                {
                    closeBtn.onClick.AddListener(CloseInventory);
                }
                else
                {
                    Debug.LogError("找到了名为 'cancel' 的物体，但它身上没有挂载 Button 组件！");
                }
            }
            else
            {
                Debug.LogError("在背包预制体中找不到名字叫 'cancel' 的物体！请检查名字大小写是否一致。");
            }
        }
        else
        {
            Debug.LogError("背包预制体没有赋值！请在 Inspector 面板拖入 inventoryPrefab");
        }
    }

    private void Update()
    {
        if (inventoryInstance == null) return;

        // 监听 E 键
        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        if (isOpen) CloseInventory();
        else OpenInventory();
    }

    public void OpenInventory()
    {
        if (isOpen || inventoryInstance == null) return;

        isOpen = true;
        inventoryInstance.SetActive(true);
        Time.timeScale = 0f; // 暂停游戏时间

        // 每次打开背包时，强制刷新一次数据，确保显示最新物品
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RefreshInventoryUI();
        }
    }

    public void CloseInventory()
    {
        if (!isOpen || inventoryInstance == null) return;

        isOpen = false;
        inventoryInstance.SetActive(false);
        Time.timeScale = 1f; // 恢复游戏时间
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f; // 安全机制：防止编辑器卡死
    }

    /// <summary>
    /// 递归查找子物体（无论嵌套多深都能找到）
    /// </summary>
    private Transform FindChildByName(Transform parent, string childName)
    {
        // 遍历所有子物体
        foreach (Transform child in parent)
        {
            // 如果名字匹配，直接返回
            if (child.name == childName)
            {
                return child;
            }

            // 如果没有匹配，去它的子物体里继续找
            Transform result = FindChildByName(child, childName);
            if (result != null)
            {
                return result;
            }
        }
        return null; // 没找到返回空
    }
}