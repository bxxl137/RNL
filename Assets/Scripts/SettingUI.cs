using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("设置界面的滑条")]
    public Slider bgmSlider;
    public Slider sfxSlider;

    void OnEnable()   // ← Start 改成 OnEnable
    {
        // Unity 2023+ 用 FindFirstObjectByType，旧版本用 FindObjectOfType
        AudioSettings audioSettings = FindFirstObjectByType<AudioSettings>();

        if (audioSettings != null)
            audioSettings.InitSliders(bgmSlider, sfxSlider);
        else
            Debug.LogError("找不到 AudioSettings！确认 _AudioManager 场景已加载且挂了这个脚本。");
    }
}