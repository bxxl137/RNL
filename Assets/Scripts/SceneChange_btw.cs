using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场景切换动画：收到全局通知后，先 fakein = true 变黑，再 fakeout = true 淡出。
/// 视觉表现由 Animator 状态机驱动，脚本只负责翻转 bool。
/// </summary>
public class SceneChangeView : MonoBehaviour
{
    [Header("状态机")]
    [SerializeField] private Animator animator;

    [Header("时长")]
    [SerializeField] private float fadeInDuration = 0.3f;    // 变黑用时（与动画时长一致）
    [SerializeField] private float fadeOutDuration = 0.5f;   // 淡出用时（与动画时长一致）
    [SerializeField] private float blackHoldTime = 0f;       // 全黑保持时长

    private static readonly int FakeInHash  = Animator.StringToHash("FakeIn");
    private static readonly int FakeOutHash = Animator.StringToHash("FakeOut");

    private bool isPlaying;

    public static SceneChangeView Instance { get; private set; }

    private void Awake()
    {
        
        animator.SetBool(FakeInHash, false);
        animator.SetBool(FakeOutHash, false);
        
    }

    private void OnEnable()  { SceneChangeSignal.OnPlay += HandlePlay; }
    private void OnDisable() { SceneChangeSignal.OnPlay -= HandlePlay; }

    private void HandlePlay()
    {
        if (isPlaying) return;
        StartCoroutine(PlayRoutine());
    }

    private System.Collections.IEnumerator PlayRoutine()
    {
        isPlaying = true;

        // ① 变黑
        if (animator != null)
        {
            animator.SetBool(FakeOutHash, true);
            animator.SetBool(FakeInHash, true);
        }
        yield return new WaitForSeconds(fadeInDuration);

        if (blackHoldTime > 0f)
            yield return new WaitForSeconds(blackHoldTime);

        // ② 淡出
        if (animator != null)
        {
            animator.SetBool(FakeInHash, false);
            animator.SetBool(FakeOutHash, true);
        }
        yield return new WaitForSeconds(fadeOutDuration);

        // ③ 回落初始状态
        if (animator != null)
        {
            animator.SetBool(FakeInHash, false);
            animator.SetBool(FakeOutHash, false);
        }

        isPlaying = false;
    }
}
