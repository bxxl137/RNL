using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shack : MonoBehaviour
{
    [Tooltip("两个灯交替的时间间隔（秒）")]
    public float interval = 1f;

    [Header("碎片切换")]
    [Tooltip("必须和 AddItem 时的 itemId 完全一致（微光碎片3）")]
    [SerializeField] private string unlockItemId = "shard_glow_3";
    [Tooltip("仅用于日志显示")]
    [SerializeField] private string unlockItemName = "微光碎片3";

    // 两个子物体
    private GameObject anDeng;
    private GameObject haoDeng;

    private float timer = 0f;
    private bool isAnTurn = true; // true = an_deng 亮, false = hao_deng 亮

    void Start()
    {
        // 按名字查找子物体（必须是 dengdengdeng 的直接子级）
        Transform t = transform.Find("an_deng");
        if (t != null) anDeng = t.gameObject;
        else Debug.LogError("没找到子物体 an_deng");

        t = transform.Find("hao_deng");
        if (t != null) haoDeng = t.gameObject;
        else Debug.LogError("没找到子物体 hao_deng");

        // 初始状态：an_deng 亮，hao_deng 灭
        ApplyState();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= interval)
        {
            timer -= interval;   // 用减法而不是清零，避免长时间运行累积误差
            isAnTurn = !isAnTurn;
            ApplyState();
        }
    }

    void ApplyState()
    {
        if (anDeng != null) anDeng.SetActive(isAnTurn);
        if (haoDeng != null) haoDeng.SetActive(!isAnTurn);
    }

    // ================== 碎片3 消耗后：锁定 hao_deng 常亮，本脚本失活 ==================

    private void OnEnable()
    {
        InventoryManager.OnItemRemoved += HandleItemRemoved;
    }

    private void OnDisable()
    {
        InventoryManager.OnItemRemoved -= HandleItemRemoved;
    }

    private void HandleItemRemoved(string itemId, int amount)
    {
        if (itemId != unlockItemId) return;

        // 定住状态：hao_deng 亮，an_deng 灭（不受 isAnTurn 影响，直接强制写死）
        if (anDeng != null) anDeng.SetActive(false);
        if (haoDeng != null) haoDeng.SetActive(true);

        Debug.Log($"[闪灯] {unlockItemName} 被消耗 ×{amount}，hao_deng 常亮，本脚本失活");

        // 本脚本失活：Update 不再执行、OnDisable 自动解绑事件
        enabled = false;
    }
}
