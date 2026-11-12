using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("UI 设置")]
    public TextMeshProUGUI logText;
    public ScrollRect scrollRect;

    private string currentText = "";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        logText.text = "";
        currentText = "";
    }

    public void AddItem(string itemName, int amount = 1)
    {
        string newLine = $"获得： {itemName} x{amount}\n";
        currentText += newLine;
        logText.text = currentText;

        // --- 强制刷新布局 ---
        // 1. 刷新 LogText 的文本网格，算出正确的高度
        logText.ForceMeshUpdate();

        // 2. 强制 Unity 立即更新所有 Canvas 布局（让 Content Size Fitter 生效，把 Content 撑大）
        LayoutRebuilder.ForceRebuildLayoutImmediate(Content);

        // 3. 将滚动条拉到底部 (0表示最底部)
        scrollRect.verticalNormalizedPosition = 0f;
    }

    // 为了调用 LayoutRebuilder，我们需要拿到 Content 的 RectTransform
    public RectTransform Content;
}