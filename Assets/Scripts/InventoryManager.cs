using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("UI 设置")]
    public Transform contentParent;       // 拖入 ScrollView 的 Content
    public GameObject itemRowPrefab;      // 拖入包含图标和文字的 ItemRow 预制体
    public ScrollRect scrollRect;         // 拖入 ScrollView

    private List<InventoryItem> inventoryItems = new List<InventoryItem>();
    private string saveFilePath;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        saveFilePath = Path.Combine(Application.persistentDataPath, "inventory.json");
    }

    void Start()
    {
        LoadData();
    }

    /// <summary>
    /// 添加物品（带图片名称和叠加逻辑）
    /// </summary>
    public void AddItem(string itemId, string itemName, string iconName, int amount = 1)
    {
        InventoryItem existingItem = inventoryItems.Find(item => item.itemId == itemId);

        if (existingItem != null)
        {
            existingItem.amount += amount;
        }
        else
        {
            inventoryItems.Add(new InventoryItem(itemId, itemName, iconName, amount));
        }

        RefreshInventoryUI();
        SaveData();

        // 如果背包开着，自动滚到底部
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// 刷新背包 UI (改成 public 供外部调用了)
    /// </summary>
    public void RefreshInventoryUI()
    {
        if (contentParent == null || itemRowPrefab == null) return;

        // 1. 清空旧列表
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // 2. 遍历数据，重新生成每一行
        foreach (var item in inventoryItems)
        {
            GameObject row = Instantiate(itemRowPrefab, contentParent);

            // 获取子物体上的图片和文本组件 (名字必须与预制体一致)
            Transform iconTransform = row.transform.Find("IconImage");
            Transform textTransform = row.transform.Find("ItemText");

            // 设置文字
            if (textTransform != null)
            {
                TextMeshProUGUI rowText = textTransform.GetComponent<TextMeshProUGUI>();
                rowText.text = $"{item.itemName}  x{item.amount}";
            }

            // 设置图片
            if (iconTransform != null)
            {
                Image iconImage = iconTransform.GetComponent<Image>();
                Sprite loadedSprite = Resources.Load<Sprite>("ItemIcons/" + item.iconName);

                if (loadedSprite != null)
                {
                    iconImage.sprite = loadedSprite;
                    iconImage.enabled = true;
                }
                else
                {
                    iconImage.enabled = false; // 找不到图就隐藏
                    Debug.LogWarning($"找不到图片: {item.iconName}");
                }
            }
        }
    }

    private void SaveData()
    {
        SaveData data = new SaveData();
        data.items = inventoryItems;
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, json);
    }

    private void LoadData()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            inventoryItems = data.items;
            RefreshInventoryUI();
        }
    }
}