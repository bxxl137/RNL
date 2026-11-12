using System;

[Serializable]
public class InventoryItem
{
    public string itemId;       // 物品唯一 ID
    public string itemName;     // 物品显示名称
    public string iconName;     // 新增：图片的名字（比如 "Potion_Red"）
    public int amount;          // 数量

    public InventoryItem(string id, string name, string icon, int count)
    {
        itemId = id;
        itemName = name;
        iconName = icon;
        amount = count;
    }
}