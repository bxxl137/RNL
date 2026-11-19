using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer 引用")]
    public AudioMixer mixer;
    public AudioMixerGroup bgmGroup;
    public AudioMixerGroup sfxGroup;

    [Header("BGM")]
    public AudioClip defaultBGM;
    public bool playBGMOnStart = true;
    public float defaultFadeTime = 0.8f;   // 默认交叉淡入淡出时长

    [Header("SFX")]
    public int sfxChannelCount = 4;

    private AudioSource bgmA;
    private AudioSource bgmB;
    private AudioSource activeBGM;
    private Coroutine fadeRoutine;

    private AudioSource[] sfxChannels;
    private int sfxIndex;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 两个 BGM 源用于交叉淡化
        bgmA = CreateBGMSource();
        bgmB = CreateBGMSource();
        activeBGM = bgmA;

        // SFX 池
        sfxChannels = new AudioSource[Mathf.Max(1, sfxChannelCount)];
        for (int i = 0; i < sfxChannels.Length; i++)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.outputAudioMixerGroup = sfxGroup;
            sfxChannels[i] = s;
        }

        ApplyBGMVolume(PlayerPrefs.GetFloat("BGM_Volume", 1f));
        ApplySFXVolume(PlayerPrefs.GetFloat("SFX_Volume", 1f));

        if (playBGMOnStart && defaultBGM != null)
            PlayBGM(defaultBGM, fadeTime: 0f);
    }

    AudioSource CreateBGMSource()
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.loop = true;
        s.playOnAwake = false;
        s.spatialBlend = 0f;
        s.outputAudioMixerGroup = bgmGroup;
        return s;
    }

    // ================= 音量 =================
    public void ApplyBGMVolume(float v)
    {
        if (v <= 0.0001f) v = 0.0001f;
        mixer.SetFloat("BGMVolume", Mathf.Log10(v) * 20f);
        PlayerPrefs.SetFloat("BGM_Volume", v);
    }
    public void ApplySFXVolume(float v)
    {
        if (v <= 0.0001f) v = 0.0001f;
        mixer.SetFloat("SFXVolume", Mathf.Log10(v) * 20f);
        PlayerPrefs.SetFloat("SFX_Volume", v);
    }

    // ================= BGM =================
    /// <summary>
    /// 播放/切换 BGM。同一首且正在播时默认不重启（跨场景连续关卡用得到）。
    /// </summary>
    public void PlayBGM(AudioClip clip, bool restartIfSame = false, float fadeTime = -1f)
    {
        if (clip == null) return;
        if (!restartIfSame && activeBGM.clip == clip && activeBGM.isPlaying) return;

        if (fadeTime < 0f) fadeTime = defaultFadeTime;

        if (fadeRoutine != null) { StopCoroutine(fadeRoutine); fadeRoutine = null; }

        if (fadeTime <= 0.001f)
        {
            activeBGM.Stop();
            activeBGM.clip = clip;
            activeBGM.volume = 1f;
            activeBGM.Play();
            return;
        }
        fadeRoutine = StartCoroutine(CrossFade(clip, fadeTime));
    }

    IEnumerator CrossFade(AudioClip newClip, float duration)
    {
        var from = activeBGM;
        var to = (activeBGM == bgmA) ? bgmB : bgmA;

        to.clip = newClip;
        to.volume = 0f;
        to.Play();

        float t = 0f;
        float fromStart = from.volume;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;          // 暂停游戏时淡变照样进行
            float k = Mathf.Clamp01(t / duration);
            to.volume = k;
            from.volume = fromStart * (1f - k);
            yield return null;
        }

        to.volume = 1f;
        from.Stop();
        from.volume = 1f;                         // 重置给下次用
        activeBGM = to;
        fadeRoutine = null;
    }

    public void StopBGM(float fadeTime = -1f)
    {
        if (fadeTime < 0f) fadeTime = defaultFadeTime;
        if (fadeTime <= 0.001f) { activeBGM.Stop(); return; }
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeOutAndStop(activeBGM, fadeTime));
    }

    IEnumerator FadeOutAndStop(AudioSource src, float duration)
    {
        float t = 0f, start = src.volume;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            src.volume = start * (1f - Mathf.Clamp01(t / duration));
            yield return null;
        }
        src.Stop();
        src.volume = 1f;
        fadeRoutine = null;
    }

    // ================= SFX（不变） =================
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        var src = sfxChannels[sfxIndex];
        sfxIndex = (sfxIndex + 1) % sfxChannels.Length;
        src.pitch = 1f;
        src.panStereo = 0f;
        src.PlayOneShot(clip, volumeScale);
    }

    public void PlaySFXAt(AudioClip clip, Vector3 worldPos, float volumeScale = 1f)
    {
        if (clip == null) return;
        var cam = Camera.main;
        if (cam == null) { PlaySFX(clip, volumeScale); return; }

        Vector3 vp = cam.WorldToViewportPoint(worldPos);
        float pan = Mathf.Clamp((vp.x - 0.5f) * 2f, -1f, 1f);

        var go = new GameObject("SFX2D");
        go.transform.position = worldPos;
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volumeScale;
        src.spatialBlend = 0f;
        src.panStereo = pan;
        src.outputAudioMixerGroup = sfxGroup;
        src.Play();
        Destroy(go, clip.length + 0.1f);
    }
}