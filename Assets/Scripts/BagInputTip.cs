using System.Collections;
using System.Collections.Generic;

using TMPro;
using UnityEngine;

/// <summary>
/// 挂在玩家头顶的 TMP 文字上（World Space Canvas 或 3D Text）。
/// 订阅 InventoryManager.OnItemAdded，显示文字 → 跟随玩家 → 延迟淡出。
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class BagInputTip : MonoBehaviour
{
    [Header("跟随目标")]
    [SerializeField] private Transform followTarget;      // 拖 Player
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, 0f); // 头顶偏移

    [Header("时间参数")]
    [SerializeField] private float stayDuration = 1.2f;   // 完全不透明停留时长
    [SerializeField] private float fadeDuration = 0.8f;   // 淡出时长

    private TextMeshProUGUI _text;
    private Coroutine _routine;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
        SetAlpha(0f);
    }

    private void OnEnable()
    {
        InventoryManager.OnItemAdded += HandleItemAdded;
    }

    private void OnDisable()
    {
        InventoryManager.OnItemAdded -= HandleItemAdded;
    }

    private void LateUpdate()
    {
        if (followTarget == null) return;
        // LateUpdate 保证在玩家移动之后才更新位置，避免抖动
        transform.position = followTarget.position + offset;
    }

    private void HandleItemAdded(string message)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ShowRoutine(message));
    }

    private IEnumerator ShowRoutine(string message)
    {
        _text.text = message;
        SetAlpha(1f);

        // 完全显示，等待
        yield return new WaitForSeconds(stayDuration);

        // 线性淡出
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetAlpha(1f - t / fadeDuration);
            yield return null;
        }

        SetAlpha(0f);
        _routine = null;
    }

    private void SetAlpha(float a)
    {
        Color c = _text.color;
        c.a = Mathf.Clamp01(a);
        _text.color = c;
    }
}
