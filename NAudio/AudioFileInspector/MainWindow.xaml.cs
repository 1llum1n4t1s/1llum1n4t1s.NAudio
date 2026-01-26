using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using Microsoft.Win32;

namespace AudioFileInspector;

/// <summary>
/// メインウィンドウ。
/// </summary>
[Export(typeof(MainWindow))]
public partial class MainWindow : Window
{
    private string _filterString;
    private int _filterIndex;
    private string _currentFile;
    private FindWindow _findWindow;

    /// <summary>
    /// インスペクター一覧。
    /// </summary>
    public ICollection<IAudioFileInspector> Inspectors { get; private set; }

    /// <summary>
    /// コマンドライン引数。
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string[] CommandLineArguments { get; set; }

    /// <summary>
    /// 指定したインスペクター一覧でメインウィンドウを初期化する。
    /// </summary>
    /// <param name="inspectors">MEF でインポートされた IAudioFileInspector の一覧。</param>
    [ImportingConstructor]
    public MainWindow([ImportMany(typeof(IAudioFileInspector))] IEnumerable<IAudioFileInspector> inspectors)
    {
        InitializeComponent();
        Inspectors = new List<IAudioFileInspector>(inspectors);
        Loaded += (_, _) =>
        {
            CreateFilterString();
            if (CommandLineArguments != null && CommandLineArguments.Length > 0)
                DescribeFile(CommandLineArguments[0]);
        };
    }

    private void DescribeFile(string fileName)
    {
        _currentFile = fileName;
        TextLog.Document.Blocks.Clear();
        TextLog.Document.Blocks.Add(new Paragraph(new Run(string.Format("Opening {0}\r\n", fileName))));
        try
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var described = false;
            foreach (var inspector in Inspectors)
            {
                if (extension == inspector.FileExtension)
                {
                    var desc = inspector.Describe(fileName);
                    var p = new Paragraph(new Run(desc));
                    TextLog.Document.Blocks.Add(p);
                    described = true;
                    break;
                }
            }
            if (!described)
                TextLog.Document.Blocks.Add(new Paragraph(new Run("Unrecognised file type")));
        }
        catch (Exception ex)
        {
            TextLog.Document.Blocks.Add(new Paragraph(new Run(ex.ToString())));
        }
    }

    private void CreateFilterString()
    {
        var sb = new StringBuilder();
        if (Inspectors.Count > 0)
        {
            sb.Append("All Supported Files|");
            foreach (var inspector in Inspectors)
                sb.Append("*").Append(inspector.FileExtension).Append(";");
            sb.Length--;
            sb.Append("|");
            foreach (var inspector in Inspectors)
                sb.AppendFormat("{0}|*{1}|", inspector.FileTypeDescription, inspector.FileExtension);
        }
        sb.Append("All files (*.*)|*.*");
        _filterString = sb.ToString();
        _filterIndex = 1;
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = _filterString,
            FilterIndex = _filterIndex
        };
        if (dlg.ShowDialog() != true) return;
        _filterIndex = dlg.FilterIndex;
        DescribeFile(dlg.FileName);
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var win = new AboutWindow();
        win.ShowDialog();
    }

    private void Options_Click(object sender, RoutedEventArgs e)
    {
        var win = new OptionsWindow(Inspectors);
        win.ShowDialog();
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effects = DragDropEffects.Copy;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (files.Length > 0)
            DescribeFile(files[0]);
    }

    private void SaveLog_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            DefaultExt = ".txt",
            Filter = "Text Files (*.txt)|*.txt",
            FilterIndex = 1
        };
        if (_currentFile != null)
        {
            dlg.InitialDirectory = Path.GetDirectoryName(_currentFile);
            dlg.FileName = Path.GetFileNameWithoutExtension(_currentFile) + ".txt";
        }
        if (dlg.ShowDialog() != true) return;
        try
        {
            var text = new TextRange(TextLog.Document.ContentStart, TextLog.Document.ContentEnd).Text;
            if (!text.Contains("\r"))
                text = text.Replace("\n", "\r\n");
            File.WriteAllText(dlg.FileName, text);
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format("Error saving conversion log\r\n{0}", ex.Message),
                "Audio File Inspector", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Contents_Click(object sender, RoutedEventArgs e)
    {
        var helpFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio_file_inspector.html");
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(helpFilePath) { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception)
        {
            MessageBox.Show("Could not display the help file", "Audio File Inspector");
        }
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        TextLog.Document.Blocks.Clear();
    }

    private void Find_Click(object sender, RoutedEventArgs e)
    {
        if (_findWindow == null)
        {
            _findWindow = new FindWindow(TextLog);
            _findWindow.Closed += (_, _) => _findWindow = null;
        }
        _findWindow.Show();
        _findWindow.Owner = this;
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        _findWindow?.Close();
    }
}
