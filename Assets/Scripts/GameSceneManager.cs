using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneChangeSignal
{
    public static event Action OnPlay;
    public static void RequestPlay() => OnPlay?.Invoke();
}

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance;

    private Stack<string> overlayStack = new Stack<string>();

    [Header("基础配置")]
    public string mainMenuSceneName = "StartScene";

    [Header("弹窗场景名")]
    public string settingsSceneName = "SettingsScene";

    [Header("快捷键")]
    public bool enableEscForSettings = true;

    [Header("转场冷却")]
    [Tooltip("两次转场之间最短间隔，防止连按导致状态错乱")]
    public float transitionCooldown = 0.3f;

    private string currentBaseScene;
    private float lastTransitionTime = -999f;

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
            return;
        }
        currentBaseScene = SceneManager.GetActiveScene().name;
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
        {
            currentBaseScene = scene.name;
            overlayStack.Clear();
        }
    }

    private void Update()
    {
        if (enableEscForSettings && Input.GetKeyDown(KeyCode.Escape))
            ToggleSettings();
    }

    // ===================== 设置页开关 =====================

    public void ToggleSettings()
    {
        if (!IsCooldownReady()) return;

        if (IsSettingsOpen())
            CloseSettings();
        else
            OpenSettings();
    }

    public void OpenSettings()
    {
        if (!IsCooldownReady()) return;
        if (IsSettingsOpen()) return;
        OpenOverlay(settingsSceneName);
    }

    public void CloseSettings()
    {
        if (!IsSettingsOpen()) return;

        if (overlayStack.Count > 0 && overlayStack.Peek() == settingsSceneName)
        {
            Back();
        }
        else
        {
            // 兜底：栈里没有但场景还在，直接卸载并把栈里残留删掉
            if (!IsCooldownReady()) return;
            PlayWithTransition(() =>
            {
                RemoveFromOverlayStack(settingsSceneName);
                SceneManager.UnloadSceneAsync(settingsSceneName);
            });
        }
    }

    private bool IsSettingsOpen()
    {
        if (string.IsNullOrEmpty(settingsSceneName)) return false;
        Scene s = SceneManager.GetSceneByName(settingsSceneName);
        return s.IsValid() && s.isLoaded;
    }

    private bool IsCooldownReady()
    {
        return Time.unscaledTime - lastTransitionTime >= transitionCooldown;
    }

    private void RemoveFromOverlayStack(string sceneName)
    {
        if (!overlayStack.Contains(sceneName)) return;
        var list = new List<string>(overlayStack); // list[0] 是栈顶
        list.Remove(sceneName);
        overlayStack.Clear();
        for (int i = list.Count - 1; i >= 0; i--)
            overlayStack.Push(list[i]);
    }

    // ===================== 场景切换 =====================

    public void LoadLevel(string levelName)
    {
        if (!IsCooldownReady()) return;
        PlayWithTransition(() => SceneManager.LoadScene(levelName));
    }

    public void OpenOverlay(string sceneName)
    {
        // 用栈判断去重，而不是 isLoaded
        if (overlayStack.Contains(sceneName)) return;
        if (!IsCooldownReady()) return;

        PlayWithTransition(() =>
        {
            // 回调内部再 push，保证“push + Load”是原子的
            if (overlayStack.Contains(sceneName)) return;
            overlayStack.Push(sceneName);
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        });
    }

    public void Back()
    {
        if (overlayStack.Count > 0)
        {
            if (!IsCooldownReady()) return;
            PlayWithTransition(() =>
            {
                // ★ 关键修复：pop 放进回调内，和 Unload 一起执行
                string top = overlayStack.Pop();
                SceneManager.UnloadSceneAsync(top);
            });
        }
        else if (currentBaseScene != mainMenuSceneName)
        {
            if (!IsCooldownReady()) return;
            PlayWithTransition(() => SceneManager.LoadScene(mainMenuSceneName));
        }
        else
        {
            Application.Quit();
        }
    }

    public void BackToMainMenu()
    {
        if (!IsCooldownReady()) return;
        PlayWithTransition(() =>
        {
            overlayStack.Clear();
            SceneManager.LoadScene(mainMenuSceneName);
        });
    }

    /// <summary>
    /// 统一入口：只做“冷却 + 转发”，不再用 isTransitioning 拦截。
    /// 即使 SceneChangeView 内部把请求丢弃，也不会污染栈状态。
    /// </summary>
    private void PlayWithTransition(Action onBlacked)
    {
        lastTransitionTime = Time.unscaledTime;

        if (SceneChangeView.Instance != null)
            SceneChangeView.Instance.Play(onBlacked);
        else
            onBlacked?.Invoke();
    }
}