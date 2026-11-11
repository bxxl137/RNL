using UnityEngine;
using UnityEngine.UI;

public class SettingBackButton : MonoBehaviour
{
    private void Awake()
    {
        Button btn = GetComponent<Button>();
        if (btn == null)
        {
            Debug.LogError("这个物体身上没有 Button 组件！", this);
            return;
        }

        btn.onClick.AddListener(OnClickBack);
    }

    void OnClickBack()
    {
        if (GameSceneManager.Instance == null)
        {
            Debug.LogWarning("GameSceneManager 不存在！请从主菜单 StartScene 启动游戏");
            return;
        }
        GameSceneManager.Instance.Back();
    }
}
