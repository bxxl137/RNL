using System;

[Serializable]
public class InventoryItem
{
    public string itemId;       // 物品唯一 ID
    public string itemName;     // 物品显示名称
    public string iconName;     // 图片名字（在 Resources/ItemIcons 文件夹里）
    public int amount;          // 数量

    public InventoryItem(string id, string name, string icon, int count)
    {
        itemId = id;
        itemName = name;
        iconName = icon;
        amount = count;
    }
}