using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuUIManager : MonoBehaviour
{
    [Header("把主菜单的四个按钮拖进来")]
    public Button startBtn;
    public Button continueBtn;
    public Button settingsBtn;
    public Button quitBtn;

    [Header("场景配置")]
    public string gameSceneName = "GameScene";       // 换成你的游戏场景名
    public string settingsSceneName = "SettingsScene"; // 换成你的设置场景名

    void Start()
    {
        // 每次进入这个场景，都用代码重新绑定一次，彻底告别 Missing！
        if (startBtn != null)
        {
            startBtn.onClick.RemoveAllListeners();
            startBtn.onClick.AddListener(OnClickStart);
        }

        if (continueBtn != null)
        {
            continueBtn.onClick.RemoveAllListeners();
            continueBtn.onClick.AddListener(OnClickContinue);
        }

        if (settingsBtn != null)
        {
            settingsBtn.onClick.RemoveAllListeners();
            settingsBtn.onClick.AddListener(OnClickSettings);
        }

        if (quitBtn != null)
        {
            quitBtn.onClick.RemoveAllListeners();
            quitBtn.onClick.AddListener(OnClickQuit);
        }
    }

    private void OnClickStart()
    {
        Debug.Log("点击了开始游戏！");
        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.LoadLevel(gameSceneName);
        else
            Debug.LogError("GameSceneManager.Instance 为空！");
    }

    private void OnClickContinue()
    {
        Debug.Log("点击了继续游戏！");
        // 你的读档逻辑
    }

    private void OnClickSettings()
    {
        Debug.Log("点击了设置！");
        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.OpenOverlay(settingsSceneName);
    }

    private void OnClickQuit()
    {
        Debug.Log("点击了退出游戏！");
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}