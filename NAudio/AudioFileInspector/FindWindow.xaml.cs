using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace AudioFileInspector;

/// <summary>
/// 検索ウィンドウ。
/// </summary>
public partial class FindWindow : Window
{
    private readonly RichTextBox _target;

    /// <summary>
    /// コンストラクター。
    /// </summary>
    public FindWindow(RichTextBox target)
    {
        InitializeComponent();
        _target = target;
    }

    private void ButtonFind_Click(object sender, RoutedEventArgs e) => FindNext();

    private void TextBoxFind_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            FindNext();
            e.Handled = true;
        }
    }

    private void FindNext()
    {
        var findText = TextBoxFind.Text;
        if (string.IsNullOrEmpty(findText)) return;
        var searchStart = _target.Selection.Start;
        var fullRange = new TextRange(_target.Document.ContentStart, _target.Document.ContentEnd);
        var fullText = fullRange.Text;
        var fromStart = new TextRange(_target.Document.ContentStart, searchStart).Text.Length;
        var idx = fullText.IndexOf(findText, fromStart, fullText.Length - fromStart, System.StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            idx = fullText.IndexOf(findText, System.StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return;
        var docStart = _target.Document.ContentStart;
        TextPointer startPos;
        TextPointer endPos;
        try
        {
            startPos = docStart.GetPositionAtOffset(idx, LogicalDirection.Forward);
            endPos = docStart.GetPositionAtOffset(idx + findText.Length, LogicalDirection.Forward);
        }
        catch (ArgumentOutOfRangeException)
        {
            return;
        }
        if (startPos == null || endPos == null) return;
        _target.Selection.Select(startPos, endPos);
        _target.Focus();
    }
}
