using System.Collections;
using UnityEngine;


/// <summary>
/// 监听 Dark_warm_lamp 是否进入 warm 状态：
/// 一旦 warm 激活，就激活 endchat 并淡出消失，然后把 ma 整体失活。
/// 只触发一次。
/// </summary>
public class MaEndChatOnWarm : MonoBehaviour
{
    [Header("灯状态源")]
    [Tooltip("挂有 warm / dark 两个子物体的灯物体")]
    [SerializeField] private GameObject darkWarmLamp;
    [Tooltip("warm 子物体，用它是否激活来判断是否进入 warm 状态")]
    [SerializeField] private GameObject warmChild;

    [Header("要淡出的对话")]
    [Tooltip("endchat 物体，初始应处于失活状态")]
    [SerializeField] private GameObject endchat;

    [Header("淡出参数")]
    [SerializeField] private float fadeDuration = 1.2f;

    private bool _triggered;


    private void Awake()
    {
        // 兜底：Inspector 没拖就按名字找
        if (darkWarmLamp == null) darkWarmLamp = GameObject.Find("Dark_warm_lamp");
        if (warmChild == null && darkWarmLamp != null)
            warmChild = darkWarmLamp.transform.Find("warm")?.gameObject;
        if (endchat == null)
        {
            Transform t = transform.Find("Canvas/endchat");
            if (t != null) endchat = t.gameObject;
        }//666

        if (endchat != null) endchat.SetActive(false);
    }


    private void Update()
    {
        if (_triggered) return;
        if (warmChild == null || endchat == null) return;

        if (!warmChild.activeSelf)
        {
            _triggered = true;
            StartCoroutine(PlayEndSequence());
        }
    }


    private IEnumerator PlayEndSequence()
    {
        // 1. 激活 endchat
        endchat.SetActive(true);

        // 2. 逐渐变透明
        CanvasGroup cg = endchat.GetComponent<CanvasGroup>();
        if (cg == null) cg = endchat.AddComponent<CanvasGroup>();

        float t = 0f;
        float startAlpha = cg.alpha;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, 0f, t / fadeDuration);
            yield return null;
        }
        cg.alpha = 0f;

        // 3. endchat 淡出后关掉自己，再让 ma 失活
        endchat.SetActive(false);
        gameObject.SetActive(false);
    }
}
