using System.Collections;
using UnityEngine;

public class SceneChangeView : MonoBehaviour
{
    [Header("状态机")]
    [SerializeField] private Animator animator;

    [Header("黑屏 Canvas (必须拖入!)")]
    [Tooltip("把黑屏遮罩所在的 Canvas 拖进来")]
    [SerializeField] private Canvas fadeCanvas;

    [Header("时长")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float blackHoldTime = 0f;

    private bool isPlaying;
    public static SceneChangeView Instance { get; private set; }
    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isPlaying = false;
    }

    public void Play(System.Action onBlacked = null)
    {
        if (isPlaying) return;
        StartCoroutine(PlayRoutine(onBlacked));
    }

    private IEnumerator PlayRoutine(System.Action onBlacked)
    {
        isPlaying = true;

        // 1. 开始时强制置顶
        ForceCanvasOnTop();

        Debug.Log($"[时间:{Time.time:F2}] 1. 开始播放变黑动画，强制播放 FadeIn");
        if (animator != null) animator.Play("FadeIn", 0, 0f);

        // 等变黑结束
        yield return new WaitForSeconds(fadeInDuration);

        if (blackHoldTime > 0f)
            yield return new WaitForSeconds(blackHoldTime);

        // 2. 此时全黑，执行叠加加载
        Debug.Log($"[时间:{Time.time:F2}] 2. 此时全黑！正在执行 onBlacked 叠加新场景");
        onBlacked?.Invoke();

        // ★ 核心修复：因为叠加加载是瞬间完成的，新场景的 UI 脚本需要 1 帧去初始化（Awake/Start）
        // 我们必须等这一帧过去，新场景的 UI 才会稳定，然后再把黑屏抢回最顶层！
        yield return null;

        Debug.Log($"[时间:{Time.time:F2}] 2.5 新场景初始化完毕，重新把黑屏抢回最顶层");
        ForceCanvasOnTop();

        // 3. 开始淡出
        Debug.Log($"[时间:{Time.time:F2}] 3. 开始播放淡出动画，强制播放 FadeOut");
        if (animator != null) animator.Play("FadeOut", 0, 0f);

        // 等淡出结束
        yield return new WaitForSeconds(fadeOutDuration);

        Debug.Log($"[时间:{Time.time:F2}] 4. 动画全部结束，回到 WaitForFade 待机");
        if (animator != null) animator.Play("WaitForFade", 0, 0f);

        isPlaying = false;
    }

    private void ForceCanvasOnTop()
    {
        if (fadeCanvas == null) return;

        // 把黑屏放在最底下的子节点（渲染在最前面）
        fadeCanvas.transform.SetAsLastSibling();

        // 强制覆盖层级
        fadeCanvas.overrideSorting = true;
        fadeCanvas.sortingOrder = 32767; // 拉到 short 的最大值，确保没有任何东西能盖住它

        // 如果是 Camera 模式，强制绑定到主相机
        if (fadeCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                fadeCanvas.worldCamera = mainCam;
                fadeCanvas.planeDistance = 1f; // 放在相机脸上，绝对挡住 3D 物体
            }
        }
    }
}