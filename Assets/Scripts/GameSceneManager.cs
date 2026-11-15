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

    // ★ 影子标志：场景重新加载产生的新实例会被标记为影子
    private bool isShadow = false;

    private Stack<string> overlayStack = new Stack<string>();

    [Header("基础配置")]
    public string mainMenuSceneName = "StartScene";

    [Header("暂停场景")]
    public string pauseSceneName = "PauseScene";

    [Header("ESC 快捷键")]
    public bool enableEscForPause = true;

    [Header("暂停时冻结时间")]
    public bool freezeTimeOnPause = true;

    [Header("转场冷却")]
    public float transitionCooldown = 0.3f;

    private string currentBaseScene;
    private float lastTransitionTime = -999f;

    // ===== 对外查询接口 =====
    // 影子也转发到真正实例，保证 InventoryUI 等外部调用拿到正确状态
    public bool IsInMainMenu => isShadow ? Instance.IsInMainMenu
                                         : (currentBaseScene == mainMenuSceneName);

    public bool IsPauseOpen => isShadow ? Instance.IsPauseOpen : CheckPauseOpen();

    private bool CheckPauseOpen()
    {
        if (string.IsNullOrEmpty(pauseSceneName)) return false;
        Scene s = SceneManager.GetSceneByName(pauseSceneName);
        return s.IsValid() && s.isLoaded;
    }

    private void Awake()
    {
        currentBaseScene = SceneManager.GetActiveScene().name;

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            // ★ 场景重新加载时产生的新实例：不销毁自己，退化为"影子"
            //   - Inspector 拖拽的按钮引用会指向这个影子，点击时转发给 Instance
            //   - 影子不 DontDestroyOnLoad，会随 StartScene 一起正常卸载
            isShadow = true;
            // 注意：这里不要 Destroy(gameObject)，也不要 DontDestroyOnLoad
        }
    }

    private void OnEnable()
    {
        if (isShadow) return;   // 影子不订阅场景事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (isShadow) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isShadow) return;
        if (mode == LoadSceneMode.Single)
        {
            currentBaseScene = scene.name;
            overlayStack.Clear();
            if (freezeTimeOnPause) Time.timeScale = 1f;
        }
    }

    private void Update()
    {
        if (isShadow) return;   // 影子不处理 ESC，避免重复
        if (enableEscForPause && Input.GetKeyDown(KeyCode.Escape))
            HandleEscape();
    }

    // ===================== ESC 统一入口 =====================
    private void HandleEscape()
    {
        if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
        {
            InventoryUI.Instance.CloseInventory();
            if (!IsInMainMenu && IsCooldownReady())
                OpenPause();
            return;
        }

        if (!IsCooldownReady()) return;

        if (overlayStack.Count > 0)
        {
            string top = overlayStack.Peek();
            if (top == pauseSceneName) ClosePause();
            else Back();
            return;
        }

        if (IsInMainMenu) return;
        OpenPause();
    }

    // ===================== 打开 / 关闭 PauseScene =====================
    public void OpenPause()
    {
        if (isShadow) { Instance.OpenPause(); return; }

        if (!IsCooldownReady()) return;
        if (CheckPauseOpen()) return;
        if (IsInMainMenu) return;
        if (overlayStack.Contains(pauseSceneName)) return;

        PlayWithTransition(() =>
        {
            overlayStack.Push(pauseSceneName);
            SceneManager.LoadScene(pauseSceneName, LoadSceneMode.Additive);
            if (freezeTimeOnPause) Time.timeScale = 0f;
        });
    }

    public void ClosePause()
    {
        if (isShadow) { Instance.ClosePause(); return; }

        if (!CheckPauseOpen()) return;
        if (!IsCooldownReady()) return;

        PlayWithTransition(() =>
        {
            string top = overlayStack.Pop();
            SceneManager.UnloadSceneAsync(top);
            if (freezeTimeOnPause) Time.timeScale = 1f;
        });
    }

    private bool IsCooldownReady()
        => Time.unscaledTime - lastTransitionTime >= transitionCooldown;

    // ===================== 场景切换 =====================
    public void LoadLevel(string levelName)
    {
        if (isShadow) { Instance.LoadLevel(levelName); return; }

        if (!IsCooldownReady()) return;
        PlayWithTransition(() => SceneManager.LoadScene(levelName));
    }

    public void OpenOverlay(string sceneName)
    {
        if (isShadow) { Instance.OpenOverlay(sceneName); return; }

        if (overlayStack.Contains(sceneName)) return;
        if (!IsCooldownReady()) return;

        PlayWithTransition(() =>
        {
            if (overlayStack.Contains(sceneName)) return;
            overlayStack.Push(sceneName);
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        });
    }

    public void Back()
    {
        if (isShadow) { Instance.Back(); return; }

        if (overlayStack.Count > 0)
        {
            if (!IsCooldownReady()) return;
            PlayWithTransition(() =>
            {
                string top = overlayStack.Pop();
                SceneManager.UnloadSceneAsync(top);
                if (top == pauseSceneName && freezeTimeOnPause)
                    Time.timeScale = 1f;
            });
        }
        else if (!IsInMainMenu)
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
        if (isShadow) { Instance.BackToMainMenu(); return; }

        if (!IsCooldownReady()) return;
        PlayWithTransition(() =>
        {
            overlayStack.Clear();
            SceneManager.LoadScene(mainMenuSceneName);
            if (freezeTimeOnPause) Time.timeScale = 1f;
        });
    }

    private void PlayWithTransition(Action onBlacked)
    {
        lastTransitionTime = Time.unscaledTime;
        if (SceneChangeView.Instance != null)
            SceneChangeView.Instance.Play(onBlacked);
        else
            onBlacked?.Invoke();
    }
}