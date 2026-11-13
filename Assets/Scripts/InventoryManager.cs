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

    // ================== 接口区（供外部调用） ==================

    /// <summary>
    /// 1. 给背包添加物品
    /// </summary>
    /// <returns>返回 true 表示添加成功</returns>
    public bool AddItem(string itemId, string itemName, string iconName, int amount = 1)
    {
        if (amount <= 0) return false;

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

        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
        return true;
    }

    /// <summary>
    /// 2. 检测背包是否有足够数量的某物品（只查不删）
    /// </summary>
    public bool HasItem(string itemId, int amount = 1)
    {
        InventoryItem existingItem = inventoryItems.Find(item => item.itemId == itemId);
        return existingItem != null && existingItem.amount >= amount;
    }

    /// <summary>
    /// 3. 检测并删除（消耗）背包里的物品
    /// </summary>
    /// <returns>返回 true 表示扣除成功；返回 false 表示物品不够或没有该物品</returns>
    public bool RemoveItem(string itemId, int amount = 1)
    {
        if (amount <= 0) return false;

        InventoryItem existingItem = inventoryItems.Find(item => item.itemId == itemId);

        if (existingItem == null) return false;
        if (existingItem.amount < amount) return false;

        existingItem.amount -= amount;

        // 如果数量扣到 0 以下，直接从列表移除
        if (existingItem.amount <= 0)
        {
            inventoryItems.Remove(existingItem);
        }

        RefreshInventoryUI();
        SaveData();

        return true;
    }

    // =========================================================

    /// <summary>
    /// 刷新背包 UI
    /// </summary>
    public void RefreshInventoryUI()
    {
        if (contentParent == null || itemRowPrefab == null) return;

        // 清空旧列表
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // 重新生成
        foreach (var item in inventoryItems)
        {
            GameObject row = Instantiate(itemRowPrefab, contentParent);

            Transform iconTransform = row.transform.Find("IconImage");
            Transform textTransform = row.transform.Find("ItemText");

            if (textTransform != null)
            {
                TextMeshProUGUI rowText = textTransform.GetComponent<TextMeshProUGUI>();
                rowText.text = $"{item.itemName}  x{item.amount}";
            }

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
                    iconImage.enabled = false;
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