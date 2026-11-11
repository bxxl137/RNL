using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 全局转场广播（保留，但不再负责驱动动画）
/// </summary>
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

    private string currentBaseScene;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 必须把 SceneChangeView 挂在这个对象（或其子物体）下
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        currentBaseScene = SceneManager.GetActiveScene().name;
    }

    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
        {
            currentBaseScene = scene.name;
            overlayStack.Clear();
        }
    }

    /// <summary>加载关卡 / 切换底层（Single 模式）</summary>
    public void LoadLevel(string levelName)
    {
        PlayWithTransition(() => SceneManager.LoadScene(levelName));
    }

    /// <summary>打开叠加弹窗（压栈）</summary>
    public void OpenOverlay(string sceneName)
    {
        PlayWithTransition(() =>
        {
            overlayStack.Push(sceneName);
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        });
    }

    /// <summary>
    /// 通用返回
    /// </summary>
    public void Back()
    {
        if (overlayStack.Count > 0)
        {
            string top = overlayStack.Pop();
            PlayWithTransition(() => SceneManager.UnloadSceneAsync(top));
        }
        else if (currentBaseScene != mainMenuSceneName)
        {
            PlayWithTransition(() => SceneManager.LoadScene(mainMenuSceneName));
        }
        else
        {
            Application.Quit();
        }
    }

    /// <summary>强制回主菜单</summary>
    public void BackToMainMenu()
    {
        PlayWithTransition(() =>
        {
            overlayStack.Clear();
            SceneManager.LoadScene(mainMenuSceneName);
        });
    }

    /// <summary>
    /// 统一入口：黑屏时加载，如果转场组件没挂则直接加载。
    /// </summary>
    private void PlayWithTransition(Action onBlacked)
    {
        if (SceneChangeView.Instance != null)
        {
            SceneChangeView.Instance.Play(onBlacked);
        }
        else
        {
            // 极端情况下如果没有转场UI，也要保证场景能切
            onBlacked?.Invoke();
        }
    }
}