using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using System;



public static class SceneChangeSignal
{
    public static event Action OnPlay;

    public static void RequestPlay()
    {
        OnPlay?.Invoke();
    }
}

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance;
    // 场景栈：只存叠加弹窗（设置、帮助等）
    private Stack<string> overlayStack = new Stack<string>();

    [Header("基础配置")]
    public string mainMenuSceneName = "StartScene";

    // 记录当前底层场景（可以是主菜单 / Level1 / Level2...）
    private string currentBaseScene;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        // 初始化底层场景
        currentBaseScene = SceneManager.GetActiveScene().name;
    }

    // 监听场景加载，更新底层记录
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Single加载 = 底层切换，更新底层标记
        if (mode == LoadSceneMode.Single)
        {
            currentBaseScene = scene.name;
            overlayStack.Clear(); // 切底层，清空弹窗栈
        }
    }

    /// <summary>
    /// 加载关卡/切换底层（Single模式）
    /// </summary>
    public void LoadLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

    /// <summary>
    /// 打开叠加弹窗菜单（压栈）
    /// </summary>
    public void OpenOverlay(string sceneName)
    {
        StartCoroutine(OpenOverlayRoutine(sceneName));
    }

    private IEnumerator OpenOverlayRoutine(string sceneName)
    {
        
        

        
        overlayStack.Push(sceneName);
        SceneChangeSignal.RequestPlay();       // 全局通知
        yield return new WaitForSeconds(2f);   // 等 2 秒
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }

    /// <summary>
    /// 通用返回按钮，统一调用这个
    /// 栈有弹窗：关掉弹窗，回到当前底层（菜单/任意关卡）
    /// 栈空：
    ///     如果底层是关卡 → 回到主菜单
    ///     如果底层已经是主菜单 → 可退出游戏
    /// </summary>
    public void Back()
    {
        if (overlayStack.Count > 0)
        {
            string top = overlayStack.Pop();
            SceneManager.UnloadSceneAsync(top);
        }
        else
        {
            if (currentBaseScene != mainMenuSceneName)
            {
                // 当前底层是任意关卡，栈空点返回 → 回到主菜单
                LoadLevel(mainMenuSceneName);
            }
            else
            {
                // 在主菜单，栈空，点返回：退出游戏
                Application.Quit();
            }
        }
    }

    /// <summary>
    /// 直接回到主菜单（强制，不管弹窗）
    /// </summary>
    public void BackToMainMenu()
    {
        overlayStack.Clear();
        LoadLevel(mainMenuSceneName);
    }
}