using UnityEngine;

/// <summary>
/// 第二关音效集中入口。
/// 人声互斥：任意人声在播时，新的人声请求会被拦截；短音效不受限。
/// </summary>
public static class Level2SFX
{
    // ============ 文件名常量 ============
    public const string PlayerTalk   = "主角说话音效";
    public const string GardenerTalk = "园丁说话音效";
    public const string WorkerTalk   = "工人说话音效";
    public const string Narration    = "旁白说话音效";
    public const string BossTalk     = "早餐店老板说话音效";
    public const string OldLadyTalk  = "老奶奶说话音效";
    public const string TailorTalk   = "裁缝店老板说话音效";

    public const string ShardPickup  = "收集微光碎片音效";
    public const string PuzzleSolved = "解密成功音效";
    public const string SelectClick  = "选择按键音效";
    public const string NodeLightUp  = "节点点亮音效";

    // ============ 人声互斥闸门 ============
    private static AudioClip _playingVoice;
    private static float _voiceEndTime;

    /// <summary>当前是否有"人声"在播（只看人声，不含短音效）。</summary>
    public static bool IsVoicePlaying =>
        _playingVoice != null && Time.unscaledTime < _voiceEndTime;

    /// <summary>
    /// 播放音效。
    /// 若 clipName 属于人声：有人声在播则拦截，返回 false。
    /// 若属于短音效：无条件播放，返回 true。
    /// </summary>
    public static bool Play(string clipName, float volumeScale = 1f)
    {
        if (AudioManager.Instance == null) return false;

        var clip = Resources.Load<AudioClip>("Audio/" + clipName);
        if (clip == null)
        {
            Debug.LogWarning($"[Level2SFX] 找不到音效：Audio/{clipName}");
            return false;
        }

        if (IsVoiceClip(clipName))
        {
            // 人声互斥
            if (IsVoicePlaying) return false;

            _playingVoice = clip;
            _voiceEndTime = Time.unscaledTime + clip.length;
        }

        AudioManager.Instance.PlaySFX(clip, volumeScale);
        return true;
    }

    /// <summary>
    /// 强制播放人声，打断正在播的人声（用于剧情强推、Boss 夺话等场合）。
    /// 短音效直接走 Play 即可。
    /// </summary>
    public static void PlayVoiceForce(string clipName, float volumeScale = 1f)
    {
        if (AudioManager.Instance == null) return;

        var clip = Resources.Load<AudioClip>("Audio/" + clipName);
        if (clip == null)
        {
            Debug.LogWarning($"[Level2SFX] 找不到音效：Audio/{clipName}");
            return;
        }

        if (IsVoiceClip(clipName))
        {
            _playingVoice = clip;
            _voiceEndTime = Time.unscaledTime + clip.length;
        }

        AudioManager.Instance.PlaySFX(clip, volumeScale);
    }
    /// <summary>
/// 无视人声互斥，无条件播放（短音效专用）。
/// </summary>
public static void PlayForce(string clipName, float volumeScale = 1f)
{
    if (AudioManager.Instance == null) return;

    var clip = Resources.Load<AudioClip>("Audio/" + clipName);
    if (clip == null)
    {
        Debug.LogWarning($"[Level2SFX] 找不到音效：Audio/{clipName}");
        return;
    }

    if (IsVoiceClip(clipName))
    {
        _playingVoice = clip;
        _voiceEndTime = Time.unscaledTime + clip.length;
    }

    AudioManager.Instance.PlaySFX(clip, volumeScale);
}


    /// <summary>强制清空人声闸门。场景切换/对话关闭时调。</summary>
    public static void ResetVoiceGate()
    {
        _playingVoice = null;
        _voiceEndTime = 0f;
    }

    /// <summary>判断是否为人声 clip。</summary>
    public static bool IsVoiceClip(string clipName)
    {
        switch (clipName)
        {
            case PlayerTalk:
            case GardenerTalk:
            case WorkerTalk:
            case Narration:
            case BossTalk:
            case OldLadyTalk:
            case TailorTalk:
                return true;
            default:
                return false;
        }
    }
}
