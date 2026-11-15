using UnityEngine;
using UnityEngine.UI;

public class QuickMenuTest : MonoBehaviour
{
    void Start()
    {
        // 找到 Canvas 里的四个按钮
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            // 先清空之前的烂绑定
            btn.onClick.RemoveAllListeners();

            string btnName = btn.gameObject.name;
            Debug.Log("找到按钮: " + btnName);

            // 根据名字绑定最简单的方法
            if (btnName.Contains("开始") || btnName.Contains("Start"))
            {
                btn.onClick.AddListener(() => {
                    Debug.Log("点击了开始游戏！");
                    if (GameSceneManager.Instance != null)
                        GameSceneManager.Instance.LoadLevel("GameScene"); // 换成你的游戏场景名
                });
            }
            else if (btnName.Contains("设置") || btnName.Contains("Setting"))
            {
                btn.onClick.AddListener(() => {
                    Debug.Log("点击了设置！");
                    if (GameSceneManager.Instance != null)
                        GameSceneManager.Instance.OpenOverlay("SettingsScene");
                });
            }
            else if (btnName.Contains("退出") || btnName.Contains("Quit") || btnName.Contains("Exit"))
            {
                btn.onClick.AddListener(() => {
                    Debug.Log("点击了退出！");
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                });
            }
        }
    }
}