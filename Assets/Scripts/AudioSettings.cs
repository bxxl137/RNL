using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI; // 必须引入UI命名空间

public class AudioSettings : MonoBehaviour
{
    [Header("音频混合器")]
    public AudioMixer audioMixer;

    // 初始化滑条：每次打开设置界面时，由 SettingsUI 调用
    public void InitSliders(Slider bgmSlider, Slider sfxSlider)
    {
        // 1. 从本地读取之前存的音量（如果没有记录，默认给 1.0 满音量）
        float bgmValue = PlayerPrefs.GetFloat("BGM_Volume", 1.0f);
        float sfxValue = PlayerPrefs.GetFloat("SFX_Volume", 1.0f);

        // 2. 把读取到的值赋给滑条
        // (为了防止赋值时触发事件，先移除所有监听)
        bgmSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();

        bgmSlider.value = bgmValue;
        sfxSlider.value = sfxValue;

        // 3. 把值应用到 Mixer 上（让游戏声音立刻变成上次保存的大小）
        ApplyBGMVolume(bgmValue);
        ApplySFXVolume(sfxValue);

        // 4. 重新绑定事件（这样玩家拖动滑条时才会触发）
        bgmSlider.onValueChanged.AddListener(ApplyBGMVolume);
        sfxSlider.onValueChanged.AddListener(ApplySFXVolume);
    }

    // 应用 BGM 音量（对应 Slider 的 OnValueChanged 事件）
    public void ApplyBGMVolume(float volume)
    {
        // 防止 log10(0) 报错，给个极小值
        if (volume <= 0.0001f) volume = 0.0001f;

        // 核心公式：把 0~1 的线性值转为 -80~0 的分贝值
        audioMixer.SetFloat("BGMVolume", Mathf.Log10(volume) * 20);

        // 每次玩家拖动，都把最新的值存到电脑里
        PlayerPrefs.SetFloat("BGM_Volume", volume);
    }

    // 应用 SFX 音量（对应 Slider 的 OnValueChanged 事件）
    public void ApplySFXVolume(float volume)
    {
        if (volume <= 0.0001f) volume = 0.0001f;
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("SFX_Volume", volume);
    }
}