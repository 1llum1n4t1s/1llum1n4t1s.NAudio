using System;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using DrawingColor = System.Drawing.Color;

namespace NAudio.Utils;

/// <summary>
/// スレッドセーフなログ表示用 WPF コントロール。
/// </summary>
public partial class ProgressLog
{
    /// <summary>
    /// コンストラクター。
    /// </summary>
    public ProgressLog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// ログのテキスト。
    /// </summary>
    public string Text => new TextRange(LogBox.Document.ContentStart, LogBox.Document.ContentEnd).Text;

    /// <summary>
    /// メッセージをログに追加する。
    /// </summary>
    /// <param name="color">文字色。</param>
    /// <param name="message">メッセージ。</param>
    public void LogMessage(DrawingColor color, string message)
    {
        void Append()
        {
            var wpfColor = Color.FromRgb(color.R, color.G, color.B);
            var span = new System.Windows.Documents.Run(message + Environment.NewLine)
            {
                Foreground = new SolidColorBrush(wpfColor)
            };
            LogBox.Document.Blocks.Add(new Paragraph(span));
        }
        if (!LogBox.Dispatcher.CheckAccess())
            LogBox.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)Append);
        else
            Append();
    }

    /// <summary>
    /// ログをクリアする。
    /// </summary>
    public void ClearLog()
    {
        void Clear()
        {
            LogBox.Document.Blocks.Clear();
        }
        if (!LogBox.Dispatcher.CheckAccess())
            LogBox.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)Clear);
        else
            Clear();
    }
}
