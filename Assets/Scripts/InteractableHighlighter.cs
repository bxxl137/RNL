using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 全局常驻：给所有 Tag = waitforinteraction 的物体添加忽明忽暗的呼吸效果。
/// 直接调制 SpriteRenderer 的 alpha，无需贴图 Read/Write、无需预制体。
/// 挂在 StartScene 一次即可，所有场景自动生效。
/// </summary>
public class InteractableHighlighter : MonoBehaviour
{
    public static InteractableHighlighter Instance { get; private set; }

    [Header("配置")]
    [Tooltip("要处理的 Tag（须在 Tag Manager 注册）")]
    [SerializeField] private string targetTag = "waitforinteraction";

    [Header("呼吸参数")]
    [Tooltip("呼吸速度（越大越快）")]
    [SerializeField] private float pulseSpeed = 2f;

    [Range(0f, 1f)]
    [Tooltip("最低不透明度（1=不变，0=完全消失）")]
    [SerializeField] private float minAlpha = 0.35f;

    [Tooltip("是否同时做轻微的缩放呼吸（0=不缩放）")]
    [SerializeField] private float scalePulseAmount = 0f;

    [Header("扫描")]
    [Tooltip("轮询间隔（秒）")]
    [SerializeField] private float scanInterval = 0.5f;

    [Tooltip("是否用 unscaledTime（暂停游戏时也会呼吸）")]
    [SerializeField] private bool useUnscaledTime = true;

    // 目标 → 绑定信息
    private class Binding
    {
        public Transform Target;
        public SpriteRenderer[] Renderers;
        public Color[] BaseColors;
        public Vector3 BaseScale;
    }

    private readonly Dictionary<GameObject, Binding> _bindings = new Dictionary<GameObject, Binding>();
    private float _timer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // 清掉当前场景里可能已存在的目标，再扫描
        RescanAll();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            // 恢复所有物体原状
            foreach (var b in _bindings.Values) RestoreBinding(b);
            _bindings.Clear();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 场景切换时旧绑定随物体一起销毁，这里只清字典
        _bindings.Clear();
        RescanAll();
    }

    private float _rescanTimer;

private void Update()
{
    _timer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    _rescanTimer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    CleanupInvalid();
    ApplyPulse(_timer);

    if (_rescanTimer >= scanInterval)
    {
        _rescanTimer = 0f;
        RescanAll();
    }
}

    /// <summary>扫描一次，把新出现的 Tag 物体加入绑定</summary>
    public void RescanAll()
    {
        CleanupInvalid();

        GameObject[] targets;
        try { targets = GameObject.FindGameObjectsWithTag(targetTag); }
        catch (UnityException) { return; }

        foreach (var go in targets)
        {
            if (go == null) continue;
            if (_bindings.ContainsKey(go))
            {
                if (_bindings[go].Target != null) continue;
                _bindings.Remove(go);
            }
            Bind(go);
        }
    }

    private void Bind(GameObject target)
    {
        SpriteRenderer[] srs = target.GetComponentsInChildren<SpriteRenderer>(true);
        if (srs == null || srs.Length == 0) return;

        Binding b = new Binding
        {
            Target = target.transform,
            Renderers = srs,
            BaseColors = new Color[srs.Length],
            BaseScale = target.transform.localScale
        };

        for (int i = 0; i < srs.Length; i++)
            b.BaseColors[i] = srs[i].color;

        _bindings[target] = b;
    }

    private void ApplyPulse(float t)
    {
        // 0~1 的呼吸权重
        float w = (Mathf.Sin(t * pulseSpeed) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(minAlpha, 1f, w);

        foreach (var b in _bindings.Values)
        {
            if (b.Target == null) continue;

            for (int i = 0; i < b.Renderers.Length; i++)
            {
                var sr = b.Renderers[i];
                if (sr == null) continue;
                Color c = b.BaseColors[i];
                c.a *= alpha;
                sr.color = c;
            }

            if (Mathf.Abs(scalePulseAmount) > 0.0001f)
            {
                float s = 1f + (w - 0.5f) * 2f * scalePulseAmount;
                b.Target.localScale = b.BaseScale * s;
            }
        }
    }

    private void RestoreBinding(Binding b)
    {
        if (b == null) return;
        if (b.Renderers != null && b.BaseColors != null)
        {
            for (int i = 0; i < b.Renderers.Length; i++)
            {
                if (b.Renderers[i] != null) b.Renderers[i].color = b.BaseColors[i];
            }
        }
        if (b.Target != null) b.Target.localScale = b.BaseScale;
    }

    private void CleanupInvalid()
    {
        List<GameObject> dead = null;
        foreach (var kv in _bindings)
        {
            if (kv.Key == null || kv.Value.Target == null)
            {
                if (dead == null) dead = new List<GameObject>();
                dead.Add(kv.Key);
            }
        }
        if (dead != null)
            foreach (var k in dead) _bindings.Remove(k);

        // 定期补扫（应对运行时新生成的物体）
        // 这里用计时器控制补扫，避免每帧 FindGameObjectsWithTag
    }
}
