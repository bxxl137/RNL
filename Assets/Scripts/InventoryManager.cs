using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System;



public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public static event Action<string> OnItemAdded;
    public static event Action<string, int> OnItemRemoved;   // 【新增】物品id, 消耗数量


    [Header("UI 设置")]
    public Transform contentParent;       // 拖入 ScrollView 的 Content
    public GameObject itemRowPrefab;      // 拖入包含图标和文字的 ItemRow 预制体
    public ScrollRect scrollRect;         // 拖入 ScrollView


    [Header("启动设置")]
    [Tooltip("勾选后，仅在第一次启动时清空一次背包，之后不再清空")]
    [SerializeField] private bool clearInventoryOnceOnFirstRun = true;


    private List<InventoryItem> inventoryItems = new List<InventoryItem>();
    private string saveFilePath;
    private string clearFlagPath;          // 清空标记文件路径


    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);


        saveFilePath = Path.Combine(Application.persistentDataPath, "inventory.json");
        clearFlagPath = Path.Combine(Application.persistentDataPath, "inventory_cleared.flag");
    }


    void Start()
    {
        // 只在第一次启动时清空一次
        if (clearInventoryOnceOnFirstRun && !HasClearedBefore())
        {
            ClearInventoryData();
            MarkCleared();
        }

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
        OnItemAdded?.Invoke($"获得 {itemName} x{amount}");
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


        // 【新增】扣除成功后广播消耗事件
        OnItemRemoved?.Invoke(itemId, amount);


        return true;
    }


    // ================== 清空背包接口 ==================


    /// <summary>
    /// 清空内存中的物品列表、刷新 UI，并删除磁盘上的存档文件。
    /// 完全清空，不可恢复。
    /// </summary>
    public void ClearInventoryData()
    {
        inventoryItems.Clear();
        RefreshInventoryUI();

        try
        {
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[背包] 删除存档失败：{e.Message}");
        }

        Debug.Log("[背包] 已清空所有物品与存档");
    }


    /// <summary>
    /// 只清空内存中的物品并刷新 UI，不删除存档文件。
    /// </summary>
    public void ClearInventoryInMemory()
    {
        inventoryItems.Clear();
        RefreshInventoryUI();
        Debug.Log("[背包] 已清空内存中的物品（未删除存档）");
    }


    // ================== 一次性清空标记 ==================


    /// <summary>是否已经清空过（标记文件存在即表示清空过）</summary>
    private bool HasClearedBefore()
    {
        return File.Exists(clearFlagPath);
    }


    /// <summary>写入清空标记，之后启动不再清空</summary>
    private void MarkCleared()
    {
        try
        {
            File.WriteAllText(clearFlagPath, "cleared");
            Debug.Log("[背包] 已写入一次性清空标记");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[背包] 写入清空标记失败：{e.Message}");
        }
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
                    iconImage.enabled = false;
                }
            }


            // ================== 【新增：绑定点击选中事件】 ==================
            Button rowBtn = row.GetComponent<Button>();
            if (rowBtn == null)
            {
                rowBtn = row.AddComponent<Button>();
                Image targetImg = row.GetComponent<Image>();
                if (targetImg == null)
                {
                    targetImg = row.AddComponent<Image>();
                    targetImg.color = new Color(0, 0, 0, 0); // 全透明
                }
                rowBtn.targetGraphic = targetImg;
            }


            rowBtn.onClick.RemoveAllListeners();


            string currentId = item.itemId;
            rowBtn.onClick.AddListener(() =>
            {
                if (ItemSelectionManager.Instance != null)
                {
                    ItemSelectionManager.Instance.SelectItem(currentId, row);
                }
            });
            // ================================================================
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
