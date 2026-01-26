namespace AudioFileInspector;

/// <summary>
/// オーディオファイルの内容を説明するインスペクターのインターフェース。
/// </summary>
public interface IAudioFileInspector
{
    /// <summary>
    /// 対象ファイルの拡張子（例: .wav）。
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// ファイル種別の表示名。
    /// </summary>
    string FileTypeDescription { get; }

    /// <summary>
    /// 指定ファイルの内容をテキストで説明する。
    /// </summary>
    /// <param name="fileName">ファイルパス。</param>
    /// <returns>説明テキスト。</returns>
    string Describe(string fileName);
}
