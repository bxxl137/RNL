using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PauseMenuButton : MonoBehaviour
{
    public enum ActionType
    {
        Resume,          // 继续游戏（关闭 PauseScene，恢复 timeScale）
        BackToMainMenu,  // 返回 StartScene
        OpenSettings,    // 打开设置页面
        QuitGame         // 退出游戏
    }

    [Tooltip("选择这个按钮要执行的功能")]
    public ActionType action = ActionType.Resume;

    [Tooltip("OpenSettings 时用的场景名")]
    public string settingsSceneName = "SettingsScene";

    private void Start()
    {
        // PauseScene 是 Additive 加载的，Start 执行时 GameSceneManager 一定已经存在
        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();          // 清掉 Inspector 里可能的空绑定
        btn.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (GameSceneManager.Instance == null)
        {
            Debug.LogWarning($"[PauseMenuButton] 找不到 GameSceneManager，无法执行 {action}。" +
                             " 请从 StartScene 正常启动游戏。", this);
            return;
        }

        switch (action)
        {
            case ActionType.Resume:
                GameSceneManager.Instance.ClosePause();
                break;

            case ActionType.BackToMainMenu:
                GameSceneManager.Instance.BackToMainMenu();
                break;

            case ActionType.OpenSettings:
                GameSceneManager.Instance.OpenOverlay(settingsSceneName);
                break;

            case ActionType.QuitGame:
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }
}