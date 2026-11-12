using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneChangeView : MonoBehaviour
{
    [Header("拖入你新建的黑图组件")]
    [SerializeField] private Canvas fadeCanvas;
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private Image fadeImage;

    [Header("时长")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    private bool isPlaying;
    public static SceneChangeView Instance { get; private set; }
    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 初始必须是透明的，防止一进游戏就黑屏
        SetAlpha(0f);
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

    private void SetAlpha(float a)
    {
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = a;
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = a;
            fadeImage.color = c;
        }
    }

    private IEnumerator PlayRoutine(System.Action onBlacked)
    {
        isPlaying = true;

        // 确保它永远在 Hierarchy 最下方（渲染在最前面）
        if (fadeCanvas != null) fadeCanvas.transform.SetAsLastSibling();

        // 1. 变黑
        float timer = 0;
        while (timer < fadeInDuration)
        {
            timer += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(0, 1, timer / fadeInDuration));
            yield return null;
        }
        SetAlpha(1f); // 确保全黑

        // 2. 切场景
        onBlacked?.Invoke();

        // 给新场景 1 帧时间初始化，防止它卡顿导致下一帧时间太长
        yield return null;

        // 3. 亮屏
        timer = 0;
        while (timer < fadeOutDuration)
        {
            timer += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(1, 0, timer / fadeOutDuration));
            yield return null;
        }
        SetAlpha(0f); // 确保全透明

        isPlaying = false;
    }
}