namespace MarkHeath.AudioUtils;

/// <summary>
/// 比較モード。
/// </summary>
public enum CompareMode
{
    /// <summary>現在位置で比較。</summary>
    CurrentPosition,
    /// <summary>巻き戻しで比較。</summary>
    SkipBack,
    /// <summary>再開で比較。</summary>
    Restart,
}
