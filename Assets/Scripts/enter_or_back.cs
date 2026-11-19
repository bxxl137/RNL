using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 挂在 upfloor 上（Collider2D 需勾 Is Trigger）。
/// 玩家在区域内按 Enter：
///   - 信箱未关闭：发放微光碎片（仅一次，永久），飘字“获得了一张告示”
///   - 信箱已关闭：进入 secondfloor
/// </summary>
public class UpFloorInteractor : MonoBehaviour
{
    public AudioClip clip;
    [Header("引用")]
    [SerializeField] private LetterboxLock letterboxLock;  // letterbox/tiggerplace2
    [SerializeField] private BagInputTip toastUI;              // 玩家头顶的飘字脚本
    [SerializeField] private wellpaperrepair wellpaperRepair;

    [Header("奖励设置")]
    [SerializeField] private string rewardItemId = "item_01";
    [SerializeField] private string rewardItemName = "微光碎片 1";
    [SerializeField] private string rewardIconName = "Glow_Shard";
    [SerializeField] private int rewardAmount = 1;

    [Header("飘字内容")]
    [SerializeField] private string toastMessage = "获得了一张告示";

    [Header("一次性标记（PlayerPrefs Key，改动会让老存档失效）")]
    [SerializeField] private string rewardGivenKey = "upfloor_reward_given";

    [Header("场景切换")]
    [SerializeField] private string nextSceneName = "secondfloor";

    [Header("交互按键")]
    [SerializeField] private KeyCode confirmKey = KeyCode.Return;
    [SerializeField] private KeyCode confirmKeyAlt = KeyCode.KeypadEnter;

    private bool _playerInside;
    private bool _switching;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInside = false;
    }

    private void Awake()
    {
        PlayerPrefs.SetInt(rewardGivenKey, 0);//好痛苦啊啊啊，废了我的唯一性存档5555555555
    }
    private void Update()
    {
        if (!_playerInside || _switching) return;

        if (Input.GetKeyDown(confirmKey) || Input.GetKeyDown(confirmKeyAlt))
        {
            OnConfirm();
        }
    }

    private void OnConfirm()
    {
        if (letterboxLock == null)
        {
            Debug.LogWarning("[UpFloorInteractor] 没有引用 LetterboxLockOpener");
            return;
        }

        if (!letterboxLock.IsClosed)
            GiveRewardOnce();
        else
            EnterNextScene();
    }

    private void GiveRewardOnce()
    {
        // 1. 已领过：只给反馈，不再加物品
        if (PlayerPrefs.GetInt(rewardGivenKey, 0) == 1)
        {
            Debug.Log("[告示] 奖励已经领取过了");
            if (toastUI != null) toastUI.ShowMessage("奖励已经领取过了");
            
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[UpFloorInteractor] InventoryManager 未初始化");
            return;
        }

        bool ok = InventoryManager.Instance.AddItem(
            rewardItemId, rewardItemName, rewardIconName, rewardAmount);

        if (!ok) return;

        // 2. 写入永久标记
        PlayerPrefs.SetInt(rewardGivenKey, 0);//一个存档只能领一次，如果要更改成每个存档都能领一次，可以改成 PlayerPrefs.SetInt(rewardGivenKey, 0)。
        PlayerPrefs.Save();

        // 3. 用方案 B 的飘字，覆盖掉 AddItem 自动触发的“获得 XXX x2”
        if (toastUI != null)
            toastUI.ShowMessage(toastMessage);
        else
            Debug.Log($"[告示] {toastMessage}（获得 {rewardItemName} x{rewardAmount}）");
    }

    private void EnterNextScene()
    {
        _switching = true;
        AudioManager.Instance.PlaySFX(clip);
        SceneManager.LoadScene(nextSceneName);
    }
}
