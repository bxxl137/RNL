using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 挂在 LightNetworkManager 同一个物体上。
/// 当场景内光点数量达到 targetPointCount（4）时，切换到指定场景。
/// </summary>
public class LightPointSceneSwitcher : MonoBehaviour
{
    [Header("目标场景")]
    [Tooltip("场景名，必须与 Build Settings 里完全一致（大小写敏感）")]
    [SerializeField] private string targetSceneName = "NeibourScene-secondlevel";

    [Header("触发条件")]
    [Tooltip("光点数量达到这个值就切场景")]
    [SerializeField] private int targetPointCount = 4;

    [Header("延时")]
    [Tooltip("达到数量后延迟多久切场景（秒），留时间看光点连线")]
    [SerializeField] private float delayBeforeSwitch = 0.5f;

    [Header("触发限制")]
    [Tooltip("勾选后只切一次")]
    [SerializeField] private bool onlyOnce = true;

    private bool _triggered;
    private bool _switching;

    private void Update()
    {
        if (_switching) return;
        if (onlyOnce && _triggered) return;

        var manager = LightNetworkManager.Instance;
        if (manager == null) return;

        if (manager.PointCount >= targetPointCount)
        {
            _triggered = true;
            _switching = true;

            Debug.Log($"[光点切场景] 光点数量达到 {manager.PointCount}（目标 {targetPointCount}），{delayBeforeSwitch}s 后切换到 {targetSceneName}");

            if (delayBeforeSwitch > 0f)
                Invoke(nameof(DoSwitch), delayBeforeSwitch);
            else
                DoSwitch();
        }
    }

    private void DoSwitch()
{
    if (string.IsNullOrEmpty(targetSceneName))
    {
        Debug.LogWarning("[光点切场景] targetSceneName 为空，无法切换");
        return;
    }

    var manager = LightNetworkManager.Instance;

    // ★ 只有满足条件才清空
    if (manager != null && manager.PointCount >= targetPointCount)
    {
        Debug.Log($"[光点切场景] 确认达到 {targetPointCount} 个，清空所有光点后切场景");
        manager.ClearAllPoints();
    }
    Invoke(nameof(LoadScene), 3f); // 延迟一点点，确保光点清空后再切场景
    
}
    private void LoadScene()
    {SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);}
        

}
