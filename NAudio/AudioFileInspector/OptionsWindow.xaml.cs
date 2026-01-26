using System;
using System.Collections.Generic;
using System.Windows;

namespace AudioFileInspector;

/// <summary>
/// オプションウィンドウ。
/// </summary>
public partial class OptionsWindow : Window
{
    private readonly IEnumerable<IAudioFileInspector> _inspectors;

    /// <summary>
    /// コンストラクター。
    /// </summary>
    public OptionsWindow(IEnumerable<IAudioFileInspector> inspectors)
    {
        InitializeComponent();
        _inspectors = inspectors;
    }

    private static string GetExecutablePath()
    {
        var path = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(path)) return path;
        var loc = System.Reflection.Assembly.GetExecutingAssembly().Location;
        return string.IsNullOrEmpty(loc) ? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "" : loc;
    }

    private void ButtonAssociate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Associate(_inspectors);
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format("Unable to create file associations\r\n{0}", ex),
                "Audio File Inspector", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ButtonDisassociate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Disassociate(_inspectors);
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format("Unable to remove file associations\r\n{0}", ex),
                "Audio File Inspector", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// ファイル関連付けを解除する。
    /// </summary>
    public static void Disassociate(IEnumerable<IAudioFileInspector> inspectors)
    {
        var exePath = GetExecutablePath();
        foreach (var inspector in inspectors)
        {
            if (!FileAssociations.IsFileTypeRegistered(inspector.FileExtension))
                FileAssociations.RegisterFileType(inspector.FileExtension, inspector.FileTypeDescription, null);
            FileAssociations.RemoveAction(inspector.FileExtension, "AudioFileInspector");
        }
    }

    /// <summary>
    /// ファイル関連付けを作成する。
    /// </summary>
    public static void Associate(IEnumerable<IAudioFileInspector> inspectors)
    {
        var exePath = GetExecutablePath();
        var command = "\"" + exePath + "\" \"%1\"";
        foreach (var inspector in inspectors)
        {
            if (!FileAssociations.IsFileTypeRegistered(inspector.FileExtension))
                FileAssociations.RegisterFileType(inspector.FileExtension, inspector.FileTypeDescription, null);
            FileAssociations.AddAction(inspector.FileExtension, "AudioFileInspector", "Describe", command);
        }
    }
}
