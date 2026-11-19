using UnityEngine;

/// <summary>
/// 挂在 Canvas/Panel/独白 上。
/// 独白显示时锁玩家移动，隐藏时解锁。
/// 支持 SetActive 控制（OnEnable/OnDisable）与手动调用两种方式。
/// </summary>
public class PanelMoveLocker : MonoBehaviour
{
    [Tooltip("留空则自动查找场景里的 PlayerManager")]
    [SerializeField] private PlayerManager player;

    [Tooltip("锁原因，独白建议用 Story 或 UI")]
    [SerializeField] private MoveLockReason reason = MoveLockReason.Story;

    [Tooltip("关闭时勾选：由代码手动调用 SetVisible，不依赖 OnEnable/OnDisable")]
    [SerializeField] private bool manualControl = false;

    private bool _locked;

    private void Awake()
    {
        if (player == null)
            player = FindObjectOfType<PlayerManager>();
    }

    private void OnEnable()
    {
        if (!manualControl) Lock();
    }

    private void OnDisable()
    {
        if (!manualControl) Unlock();
    }

    /// <summary>手动控制时由外部调用</summary>
    public void SetVisible(bool visible)
    {
        if (!manualControl) return;
        if (visible) Lock();
        else Unlock();
    }

    private void Lock()
    {
        if (_locked) return;

        if (player == null)
        {
            player = FindObjectOfType<PlayerManager>();
            if (player == null)
            {
                Debug.LogWarning("[PanelMoveLocker] 找不到 PlayerManager，无法加锁", this);
                return;
            }
        }

        player.AddMoveLock(reason);
        _locked = true;
    }

    private void Unlock()
    {
        if (!_locked) return;
        if (player != null)
            player.RemoveMoveLock(reason);
        _locked = false;
    }
}
