using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTipSequence : MonoBehaviour
{
    [Header("触发的场景名")]
    [SerializeField] private string targetSceneName = "MainScene";

    [Header("提示组件（可不拖，运行时自动查找）")]
    [SerializeField] private BagInputTip tip;

    [Header("要依次显示的文本")]
    [SerializeField]
    private List<string> messages = new List<string>()
    {
        "欢迎回来",
        "按 WASD 移动",
        "按 E 打开背包",
    };

    [Header("每条之间的间隔（秒）")]
    [SerializeField] private float interval = 3f;

    private Coroutine _routine;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);   // 本脚本必须常驻
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != targetSceneName) return;
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(BindAndPlay());
    }

    private IEnumerator BindAndPlay()
    {
        // 关键：每次进目标场景，都重新绑定一次
        tip = null;
        float timeout = 5f;                 // 最多等 5 秒（应对延迟生成的 Player）
        while (tip == null && timeout > 0f)
        {
            tip = FindObjectOfType<BagInputTip>();   // Unity 2023+ 用 FindFirstObjectByType
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (tip == null)
        {
            Debug.LogWarning("[SceneTipSequence] 目标场景里没找到 BagInputTip。");
            _routine = null;
            yield break;
        }

        for (int i = 0; i < messages.Count; i++)
        {
            tip.ShowMessage(messages[i]);
            if (i < messages.Count - 1)
                yield return new WaitForSeconds(interval);
        }
        _routine = null;
    }
}