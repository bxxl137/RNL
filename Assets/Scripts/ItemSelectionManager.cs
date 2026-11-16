using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 这是一个用于在 Inspector 中填写的结构体
[System.Serializable]
public class ItemDescriptionEntry
{
    public string itemId;           // 物品 ID (必须和添加物品时的 ID 一致)
    [TextArea(2, 5)]                // 让文本框大一点，方便填多行描述
    public string descriptionText;  // 物品描述文本
}

public class ItemSelectionManager : MonoBehaviour
{
    public static ItemSelectionManager Instance;

    [Header("UI 绑定")]
    public Button showDescriptionButton;     // 背包里的那个“查看详情”按钮
    public TextMeshProUGUI descriptionTextUI;// 背包下方用来显示描述的 TextMeshPro

    [Header("物品描述数据配置")]
    public List<ItemDescriptionEntry> itemDescriptions = new List<ItemDescriptionEntry>();

    // 记录当前选中的物品
    private string currentSelectedItemId = "";
    private GameObject currentSelectedRow = null;

    // 记录上一次放大的物体，用于恢复
    private GameObject lastSelectedRow = null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 绑定按钮点击事件
        if (showDescriptionButton != null)
        {
            showDescriptionButton.onClick.AddListener(OnShowDescriptionClicked);
        }

        // 初始状态清空描述
        if (descriptionTextUI != null)
        {
            descriptionTextUI.text = "请选择一个物品查看详情。";
        }
    }

    /// <summary>
    /// 当某个物品被点击时调用（由 InventoryManager 生成 UI 时自动绑定）
    /// </summary>
    /// <param name="itemId">被点击物品的ID</param>
    /// <param name="rowObject">被点击的那一行UI实体</param>
    public void SelectItem(string itemId, GameObject rowObject)
    {
        currentSelectedItemId = itemId;

        // 1. 如果上一次有选中的，恢复它原来的大小
        if (lastSelectedRow != null)
        {
            lastSelectedRow.transform.localScale = Vector3.one;
        }

        // 2. 将当前选中的物体稍微放大 (比如放大 1.1 倍)
        currentSelectedRow = rowObject;
        currentSelectedRow.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
        lastSelectedRow = currentSelectedRow;
    }

    /// <summary>
    /// 当点击“查看详情”按钮时调用
    /// </summary>
    private void OnShowDescriptionClicked()
    {
        if (string.IsNullOrEmpty(currentSelectedItemId))
        {
            descriptionTextUI.text = "<color=red>请先选中一个物品！</color>";
            return;
        }

        // 在列表中查找对应的描述
        ItemDescriptionEntry entry = itemDescriptions.Find(x => x.itemId == currentSelectedItemId);

        if (entry != null)
        {
            descriptionTextUI.text = entry.descriptionText;
        }
        else
        {
            descriptionTextUI.text = "该物品暂无描述信息。";
            Debug.LogWarning($"在列表中找不到物品 ID: {currentSelectedItemId} 的描述，请检查是否漏填了。");
        }
    }
}