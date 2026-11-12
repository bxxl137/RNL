using UnityEngine;

public class TestInventory : MonoBehaviour
{
    void Update()
    {
        // 按下数字键 1，添加红药水
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            InventoryManager.Instance.AddItem("item_01", "微光碎片1", "Glow_Shard",  1);
        }

        // 按下数字键 2，添加木剑
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            InventoryManager.Instance.AddItem("item_02", "微光碎片2", "Glow_Shard", 1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            InventoryManager.Instance.AddItem("item_03", "微光碎片3", "Glow_Shard", 1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            InventoryManager.Instance.AddItem("item_04", "微光碎片4", "Glow_Shard", 1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            InventoryManager.Instance.AddItem("item_05", "微光碎片5", "Glow_Shard", 1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            InventoryManager.Instance.AddItem("item_06", "微光碎片6", "Glow_Shard", 1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            InventoryManager.Instance.AddItem("item_07", "微光碎片7", "Glow_Shard", 1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            InventoryManager.Instance.AddItem("item_08", "微光碎片8", "Glow_Shard", 1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            InventoryManager.Instance.AddItem("item_09", "微光碎片9", "Glow_Shard", 1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            InventoryManager.Instance.AddItem("item_10", "微光碎片10", "Glow_Shard", 1);
        }
        // 按下数字键 3，清空存档（用于重置测试）
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayerPrefs.DeleteAll();
            string path = System.IO.Path.Combine(Application.persistentDataPath, "inventory.json");
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            Debug.LogWarning("存档已删除，请重新运行游戏。");
        }
    }
}