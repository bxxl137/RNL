using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 挂在 tiggerplace7 上（Collider2D 需勾 Is Trigger）。
/// 玩家在触发区域内按 Return：切换到指定场景。
/// </summary>
public class 故地重游: MonoBehaviour
{
    [Header("目标场景")]
    [Tooltip("场景名，必须与 Build Settings 里完全一致（大小写敏感）")]
    [SerializeField] private string targetSceneName = "NeibourScene-firstfloor";

    [Header("按键")]
    [SerializeField] private KeyCode confirmKey = KeyCode.Return;
    [SerializeField] private KeyCode confirmKeyAlt = KeyCode.KeypadEnter;

    [Header("触发限制")]
    [Tooltip("勾选后只切一次，防止连按重复加载")]
    [SerializeField] private bool onlyOnce = true;

    private bool _playerInside;
    private bool _switching;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInside = true;
        Debug.Log("www");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInside = false;
    }

    private void Update()
    {
        if (!_playerInside) return;
        if (onlyOnce && _switching) return;

        if (Input.GetKeyDown(confirmKey) || Input.GetKeyDown(confirmKeyAlt))
        {
            SwitchScene();
        }
    }

    private void SwitchScene()
    {
        if (_switching) return;

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("[ReturnToScene] targetSceneName 为空，无法切换场景");
            return;
        }

        _switching = true;
        Debug.Log($"[ReturnToScene] 正在切换到场景：{targetSceneName}");

        // 用名字加载，比 buildIndex 更稳（buildIndex 会随 Build Settings 顺序变）
        SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
    }
}
