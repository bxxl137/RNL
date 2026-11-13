using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance;

    [Header("背包预制体")]
    public GameObject inventoryPrefab;

    private GameObject inventoryInstance;
    private bool isOpen = false;

    private void Awake()
    {
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
            // 1. 实例化
            inventoryInstance = Instantiate(inventoryPrefab);

            // 2. 跨场景保留（设为子物体）
            inventoryInstance.transform.SetParent(this.transform, false);

            // 3. 重置位置
            RectTransform rect = inventoryInstance.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localPosition = Vector3.zero;
                rect.localScale = Vector3.one;
                rect.anchoredPosition = Vector2.zero;
            }

            inventoryInstance.SetActive(false); // 初始隐藏

            // 4. 自动找 cancel 按钮绑定退出
            Transform cancelBtnTransform = FindChildByName(inventoryInstance.transform, "cancel");
            if (cancelBtnTransform != null)
            {
                Button closeBtn = cancelBtnTransform.GetComponent<Button>();
                if (closeBtn != null) closeBtn.onClick.AddListener(CloseInventory);
            }
        }
    }

    private void Update()
    {
        if (inventoryInstance == null) return;

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
        Time.timeScale = 0f;

        // 打开时刷新数据
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
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    // 递归查找子物体
    private Transform FindChildByName(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;

            Transform result = FindChildByName(child, childName);
            if (result != null) return result;
        }
        return null;
    }
}