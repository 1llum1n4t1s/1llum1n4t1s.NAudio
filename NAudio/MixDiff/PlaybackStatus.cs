namespace MarkHeath.AudioUtils;

/// <summary>
/// 再生状態。
/// </summary>
internal enum PlaybackStatus
{
    /// <summary>停止中。</summary>
    Stopped,
    /// <summary>再生中。</summary>
    Playing,
    /// <summary>一時停止中。</summary>
    Paused,
}
