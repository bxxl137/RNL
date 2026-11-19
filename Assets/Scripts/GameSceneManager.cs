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

    // ★ 新增：场景 BGM 配置
    [System.Serializable]
    public class SceneBGMEntry
    {
        [Tooltip("场景名，要和 Build Settings 里的场景名完全一致")]
        public string sceneName;
        public AudioClip bgm;
        [Tooltip("淡变时长，0 = 硬切")]
        public float fadeTime = 0.8f;
    }

    [Header("场景 BGM 配置")]
    [Tooltip("进入某个场景时自动播放对应 BGM（只对 LoadSceneMode.Single 生效）")]
    public SceneBGMEntry[] sceneBGMs;

    [Tooltip("场景没在列表里时，用这首 BGM；留空 = 不切 BGM")]
    public AudioClip defaultBGM;

    private string currentBaseScene;
    private float lastTransitionTime = -999f;

    // ===== 对外查询接口 =====
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
            isShadow = true;
        }
    }

    // ★ 新增：初始场景不会触发 sceneLoaded 事件，手动补一次
    private void Start()
    {
        if (isShadow) return;
        PlayBGMForScene(currentBaseScene);
    }

    private void OnEnable()
    {
        if (isShadow) return;
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
            PlayBGMForScene(scene.name);   // ★ 新增
        }
        // Additive（PauseScene 等）不切 BGM，原 BGM 继续
    }

    // ★ 新增：根据场景名切 BGM
    private void PlayBGMForScene(string sceneName)
    {
        if (AudioManager.Instance == null) return;

        AudioClip clip = null;
        float fade = 0.8f;

        if (sceneBGMs != null)
        {
            foreach (var entry in sceneBGMs)
            {
                if (entry != null && entry.sceneName == sceneName)
                {
                    clip = entry.bgm;
                    fade = entry.fadeTime;
                    break;
                }
            }
        }

        if (clip == null) clip = defaultBGM;
        if (clip == null) return;   // 没配就保持当前 BGM 不变

        AudioManager.Instance.PlayBGM(clip, fadeTime: fade);
    }

    private void Update()
    {
        if (isShadow) return;
        if (enableEscForPause && Input.GetKeyDown(KeyCode.Escape))
            HandleEscape();
    }

    // ===================== ESC =====================
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

    // ===================== Pause =====================
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