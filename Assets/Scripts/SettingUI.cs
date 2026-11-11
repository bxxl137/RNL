using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("设置界面的滑条")]
    public Slider bgmSlider;
    public Slider sfxSlider;

    void Start()
    {
        // 跨场景寻找那个不死鸟 AudioSettings 脚本
        AudioSettings audioSettings = FindObjectOfType<AudioSettings>();

        if (audioSettings != null)
        {
            // 找到之后，把本场景的两个滑条传给它去初始化
            audioSettings.InitSliders(bgmSlider, sfxSlider);
        }
        else
        {
            Debug.LogError("设置场景里找不到 AudioSettings 脚本！请检查主场景里有没有 AudioManager。");
        }
    }
}