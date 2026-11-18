using UnityEngine;

/// <summary>
/// 挂在 dayscenereloading 上。
/// 检测到背包里有"微光碎片3"时，一次性把本场景的昼夜背景 / 信箱 / 墙纸 / 垃圾桶切到指定状态。
/// 切换后锁定，不再响应背景变化。
/// </summary>
public class dayscenereloading : MonoBehaviour
{
    [Header("背包条件")]
    [Tooltip("必须和 AddItem 时的 itemId 完全一致")]
    [SerializeField] private string requiredItemId = "shard_glow_3";
    [Tooltip("仅用于日志显示")]
    [SerializeField] private string requiredItemName = "微光碎片3";
    [SerializeField] private int requiredAmount = 1;

    [Header("检测设置")]
    [Tooltip("每帧检测一次，还是只在 Start 检测一次。建议保持每帧，防止背包数据延迟加载")]
    [SerializeField] private bool checkEveryFrame = true;

    [Header("Day_night_back")]
    [Tooltip("拖 Day_night_back/day")]
    [SerializeField] private GameObject dayBackground;
    [Tooltip("拖 Day_night_back/night")]
    [SerializeField] private GameObject nightBackground;

    [Header("letterbox")]
    [Tooltip("拖 letterbox/closed")]
    [SerializeField] private GameObject letterboxClosed;
    [Tooltip("拖 letterbox/opened")]
    [SerializeField] private GameObject letterboxOpened;
    [SerializeField] private GameObject letterboxOpened2;

    [Header("wallpaper")]
    [Tooltip("拖 wallpaper/well")]
    [SerializeField] private GameObject wallpaperWell;
    [Tooltip("拖 wallpaper/broken")]
    [SerializeField] private GameObject wallpaperBroken;

    [Header("trash")]
    [Tooltip("拖 trash（整个父物体）")]
    [SerializeField] private GameObject trashRoot;

    private bool _applied;

    private void Start()
    {
        // 如果不需要每帧检测，Start 里试一次就够
        if (!checkEveryFrame)
        {
            TryApply();
        }
    }

    private void Update()
    {
        if (_applied) return;
        if (!checkEveryFrame) return;

        TryApply();
    }

    private void TryApply()
    {
        if (InventoryManager.Instance == null) return;

        // 没有碎片3，什么都不做
        if (!InventoryManager.Instance.HasItem(requiredItemId, requiredAmount)) return;

        ApplyState();
    }

    private void ApplyState()
    {
        _applied = true;   // 先锁住，避免 Update 里重复进入

        // ① 背景：day 开，night 关
        if (dayBackground != null) dayBackground.SetActive(true);
        if (nightBackground != null) nightBackground.SetActive(false);

        // ② letterbox：closed 开，opened 关
        if (letterboxClosed != null) letterboxClosed.SetActive(true);
        if (letterboxOpened != null) letterboxOpened.SetActive(false);
        if (letterboxOpened2 != null) letterboxOpened2.SetActive(false);


        // ③ wallpaper：well 开，broken 关
        if (wallpaperWell != null) wallpaperWell.SetActive(true);
        if (wallpaperBroken != null) wallpaperBroken.SetActive(false);

        // ④ trash 整个父物体失活
        if (trashRoot != null) trashRoot.SetActive(false);

        Debug.Log($"[DaySceneStateApplier] 检测到 {requiredItemName}，场景状态已切换并锁定");
    }
}
