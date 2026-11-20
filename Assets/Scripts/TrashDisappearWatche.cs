using System.Collections;
using UnityEngine;


/// <summary>
/// 挂在独立空物体上，监听 trash_noon 是否被销毁。
/// 一旦 trash_noon 消失，就显示 disappear 并淡出。
/// </summary>
public class TrashDisappearWatcher : MonoBehaviour
{
    [Header("监听目标")]
    [Tooltip("要监听是否被销毁的物体，通常是 trash_noon")]
    [SerializeField] private GameObject target;

    [Header("要显示的提示")]
    [Tooltip("提示物体，初始未激活")]
    [SerializeField] private GameObject disappear;

    [Header("时间参数")]
    [Tooltip("显示多久后开始淡出（秒）")]
    [SerializeField] private float showDuration = 2f;
    [Tooltip("淡出时长（秒）")]
    [SerializeField] private float fadeDuration = 0.8f;

    [Header("兜底：直接找 GameObject")]
    [SerializeField] private string targetNameFallback = "trash_noon";


    private bool _triggered;


    private void Awake()
    {
        if (target == null)
            target = GameObject.Find(targetNameFallback);

        if (disappear != null)
            disappear.SetActive(false);
    }


    private void Update()
    {
        if (_triggered) return;

        // target 没找到，或者 target 已经被销毁 → 触发
        if (target == null)
        {
            _triggered = true;
            Debug.Log("[TrashDisappearWatcher] 触发：target 已销毁或未找到，显示 disappear");
            StartCoroutine(ShowAndFade());
        }
    }


    private IEnumerator ShowAndFade()
    {
        if (disappear == null)
        {
            Debug.LogWarning("[TrashDisappearWatcher] disappear 未赋值");
            yield break;
        }

        // 1. 显示
        disappear.SetActive(true);
        disappear.transform.SetAsLastSibling();

        // 保证 alpha 从 1 开始
        CanvasGroup cg = disappear.GetComponent<CanvasGroup>();
        if (cg == null) cg = disappear.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        // 2. 停留
        if (showDuration > 0f)
            yield return new WaitForSeconds(showDuration);

        // 3. 淡出
        if (fadeDuration > 0f)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
                yield return null;
            }
        }
        cg.alpha = 0f;

        // 4. 关闭
        disappear.SetActive(false);
    }
}
