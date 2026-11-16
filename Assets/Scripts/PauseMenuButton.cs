using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

    [Header("返回主菜单专属设置")]
    [Tooltip("点击后切换的图片（留空则不切换）")]
    public Sprite clickedSprite;
    [Tooltip("延迟多少秒后执行返回（默认0.5秒）")]
    public float backDelay = 0.5f;

    private Button btn;
    private Image btnImage;

    private void Start()
    {
        btn = GetComponent<Button>();
        btnImage = GetComponent<Image>(); // 获取按钮上的图片组件

        // PauseScene 是 Additive 加载的，Start 执行时 GameSceneManager 一定已经存在
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
                // 触发返回主菜单的特殊逻辑：切图 + 延迟
                StartCoroutine(BackToMainMenuRoutine());
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

    private IEnumerator BackToMainMenuRoutine()
    {
        // 1. 防止玩家在 0.5 秒内狂点按钮
        btn.interactable = false;

        // 2. 切换图片
        if (btnImage != null && clickedSprite != null)
        {
            btnImage.sprite = clickedSprite;
        }

        // 3. 等待 0.5 秒（注意：必须用 Realtime，因为当前 timeScale 是 0）
        yield return new WaitForSecondsRealtime(backDelay);

        // 4. 执行返回主菜单
        GameSceneManager.Instance.BackToMainMenu();
    }
}