using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class ButtonAudio : MonoBehaviour,
    IPointerEnterHandler, ISelectHandler,   // 高亮进入
    IPointerExitHandler, IDeselectHandler,  // 高亮退出
    IPointerDownHandler, ISubmitHandler     // 按下
{
    [Header("音效")]
    [Tooltip("鼠标悬停、或键盘/手柄选中时播放")]
    public AudioClip highlightClip;
    [Range(0f, 1f)] public float highlightVolume = 0.5f;

    [Tooltip("按下瞬间播放")]
    public AudioClip pressedClip;
    [Range(0f, 1f)] public float pressedVolume = 1f;

    [Header("按下时切换的 BGM（只对名字匹配的按钮生效）")]
    [Tooltip("留空则不切 BGM，交给 GameSceneManager 按目标场景自动播")]
    public AudioClip pressedBGM;
    public float bgmFadeTime = 0.8f;

    [Header("名字匹配（大小写不敏感，包含即可）")]
    public string[] namesThatPlayBGM = { "start", "continue" };

    private Button button;
    private float lastHighlightTime = -999f;
    private bool isHighlighted = false; // 高亮状态锁

    void Awake()
    {
        button = GetComponent<Button>();
    }

    // 物体禁用时重置状态，避免状态残留
    private void OnDisable()
    {
        ResetHighlightState();
    }

    // 每帧检测：按钮不可交互时强制重置高亮状态
    private void Update()
    {
        if (!button.interactable && isHighlighted)
        {
            ResetHighlightState();
        }
    }

    // ================= 高亮进入 =================
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.interactable) return;
        PlayHighlight();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!button.interactable) return;
        PlayHighlight();
    }

    private void PlayHighlight()
    {
        if (highlightClip == null) return;

        // 核心：已经处于高亮状态，直接拦截，不重复播放
        if (isHighlighted) return;

        // 保留原时间去重，作为极端同帧触发的双保险
        if (Time.unscaledTime - lastHighlightTime < 0.05f) return;
        lastHighlightTime = Time.unscaledTime;

        isHighlighted = true;
        AudioManager.Instance.PlaySFX(highlightClip, highlightVolume);
    }

    // ================= 高亮退出 =================
    public void OnPointerExit(PointerEventData eventData)
    {
        ResetHighlightState();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        ResetHighlightState();
    }

    // 重置高亮状态，允许下次进入时重新播放
    private void ResetHighlightState()
    {
        isHighlighted = false;
    }

    // ================= 按下逻辑 =================
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.interactable) return;
        PlayPressed();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (!button.interactable) return;
        PlayPressed();
    }

    private void PlayPressed()
    {
        if (pressedClip != null)
            AudioManager.Instance.PlaySFX(pressedClip, pressedVolume);

        if (pressedBGM != null && ShouldPlayBGM())
            AudioManager.Instance.PlayBGM(pressedBGM, fadeTime: bgmFadeTime);
    }

    private bool ShouldPlayBGM()
    {
        string n = gameObject.name.ToLowerInvariant();
        foreach (var s in namesThatPlayBGM)
        {
            if (!string.IsNullOrEmpty(s) && n.Contains(s.ToLowerInvariant()))
                return true;
        }
        return false;
    }
}
