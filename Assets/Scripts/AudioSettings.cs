using UnityEngine;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    /// <summary>由 SettingsUI 在打开设置面板时调用</summary>
    public void InitSliders(Slider bgmSlider, Slider sfxSlider)
    {
        float bgm = PlayerPrefs.GetFloat("BGM_Volume", 1f);
        float sfx = PlayerPrefs.GetFloat("SFX_Volume", 1f);

        // 先摘监听，避免赋值时触发事件
        bgmSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();

        bgmSlider.value = bgm;
        sfxSlider.value = sfx;

        // 立刻应用一次
        AudioManager.Instance.ApplyBGMVolume(bgm);
        AudioManager.Instance.ApplySFXVolume(sfx);

        // 重新绑定
        bgmSlider.onValueChanged.AddListener(v => AudioManager.Instance.ApplyBGMVolume(v));
        sfxSlider.onValueChanged.AddListener(v => AudioManager.Instance.ApplySFXVolume(v));
    }
}