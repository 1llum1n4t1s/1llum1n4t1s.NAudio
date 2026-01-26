using System;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using MarkHeath.MidiUtils.Properties;

namespace MarkHeath.MidiUtils;

/// <summary>
/// メインウィンドウ。
/// </summary>
public partial class MainWindow : Window
{
    private bool _workQueued;
    private NamingRules _namingRules;
    private MidiConverter _midiConverter;
    private readonly Properties.Settings _settings;

    /// <summary>
    /// コンストラクター。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        _settings = Properties.Settings.Default;
        if (Settings.Default.FirstTime)
            UpgradeSettings();
        LoadSettings();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var executableFolder = AppContext.BaseDirectory;
        if (string.IsNullOrEmpty(executableFolder) && !string.IsNullOrEmpty(Environment.ProcessPath))
            executableFolder = Path.GetDirectoryName(Environment.ProcessPath) ?? ".";
        try
        {
            _namingRules = NamingRules.LoadRules(Path.Combine(executableFolder, "NamingRules.xml"));
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error reading NamingRules.xml\r\n{ex}", ProductName, MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
        }
    }

    private void UpgradeSettings()
    {
        try
        {
            var productVersion = (string)_settings.GetPreviousVersion("ProductVersion");
            if (!string.IsNullOrEmpty(productVersion))
            {
                _settings.InputFolder = (string)_settings.GetPreviousVersion("InputFolder");
                _settings.OutputFolder = (string)_settings.GetPreviousVersion("OutputFolder");
                _settings.OutputChannelNumber = (int)_settings.GetPreviousVersion("OutputChannelNumber");
                _settings.OutputMidiType = (OutputMidiType)_settings.GetPreviousVersion("OutputMidiType");
                _settings.VerboseOutput = (bool)_settings.GetPreviousVersion("VerboseOutput");
                _settings.UseFileName = (bool)_settings.GetPreviousVersion("UseFileName");
                try
                {
                    _settings.AddNameMarker = (bool)_settings.GetPreviousVersion("AddNameMarker");
                    _settings.TrimTextEvents = (bool)_settings.GetPreviousVersion("TrimTextEvents");
                    _settings.RemoveEmptyTracks = (bool)_settings.GetPreviousVersion("RemoveEmptyTracks");
                    _settings.RemoveSequencerSpecific = (bool)_settings.GetPreviousVersion("RemoveSequencerSpecific");
                    _settings.RecreateEndTrackMarkers = (bool)_settings.GetPreviousVersion("RecreateEndTrackMarkers");
                    _settings.RemoveExtraTempoEvents = (bool)_settings.GetPreviousVersion("RemoveExtraTempoEvents");
                    _settings.RemoveExtraMarkers = (bool)_settings.GetPreviousVersion("RemoveExtraMarkers");
                }
                catch (SettingsPropertyNotFoundException) { }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning("Settings upgrade failed: {0}", ex.Message);
        }
    }

    private static string ProductName => Assembly.GetExecutingAssembly().GetName().Name ?? "MIDI File Converter";

    private void LoadSettings()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        TextBoxInputFolder.Text = string.IsNullOrEmpty(_settings.InputFolder)
            ? Path.Combine(programFiles, "Toontrack\\EZDrummer\\Midi")
            : _settings.InputFolder;
        TextBoxOutputFolder.Text = string.IsNullOrEmpty(_settings.OutputFolder)
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            : _settings.OutputFolder;
        CheckBoxApplyNamingRules.IsChecked = _settings.ApplyNamingRules;
        CheckBoxUseFilename.IsChecked = _settings.UseFileName;
        CheckBoxVerbose.IsChecked = _settings.VerboseOutput;
        switch (_settings.OutputMidiType)
        {
            case OutputMidiType.Type0: RadioType0.IsChecked = true; break;
            case OutputMidiType.Type1: RadioType1.IsChecked = true; break;
            default: RadioTypeUnchanged.IsChecked = true; break;
        }
        if (_settings.OutputChannelNumber == 1)
            RadioChannel1.IsChecked = true;
        else if (_settings.OutputChannelNumber == 10)
            RadioChannel10.IsChecked = true;
        else
            RadioChannelUnchanged.IsChecked = true;
    }

    private void UpdateSettings()
    {
        _settings.InputFolder = TextBoxInputFolder.Text;
        _settings.OutputFolder = TextBoxOutputFolder.Text;
        _settings.ApplyNamingRules = CheckBoxApplyNamingRules.IsChecked == true;
        _settings.VerboseOutput = CheckBoxVerbose.IsChecked == true;
        _settings.UseFileName = CheckBoxUseFilename.IsChecked == true;
        _settings.OutputMidiType = RadioType0.IsChecked == true ? OutputMidiType.Type0
            : RadioType1.IsChecked == true ? OutputMidiType.Type1
            : OutputMidiType.LeaveUnchanged;
        _settings.OutputChannelNumber = RadioChannel1.IsChecked == true ? 1
            : RadioChannel10.IsChecked == true ? 10
            : -1;
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (_workQueued)
        {
            System.Windows.MessageBox.Show("Please wait until the current operation has finished", ProductName, MessageBoxButton.OK, MessageBoxImage.Warning);
            e.Cancel = true;
            return;
        }
        UpdateSettings();
        _settings.FirstTime = false;
        _settings.ProductVersion = typeof(App).Assembly.GetName().Version?.ToString() ?? "";
        _settings.Save();
    }

    private void Convert_Click(object sender, RoutedEventArgs e)
    {
        if (_workQueued)
        {
            System.Windows.MessageBox.Show("Please wait until the current operation has finished", ProductName, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        UpdateSettings();
        if (!CheckInputFolderExists()) return;
        if (!CheckOutputFolderExists()) return;
        if (!CheckOutputFolderIsEmpty()) return;
        Cursor = System.Windows.Input.Cursors.Wait;
        _workQueued = true;
        ThreadPool.QueueUserWorkItem(ConvertThreadProc);
    }

    private void ConvertThreadProc(object state)
    {
        try
        {
            ProgressLog.ClearLog();
            _midiConverter = new MidiConverter(_namingRules);
            _midiConverter.Progress += MidiConverter_Progress;
            _midiConverter.Start();
        }
        finally
        {
            _workQueued = false;
            Dispatcher.BeginInvoke(() =>
            {
                Cursor = null;
                SaveLogMenuItem.IsEnabled = true;
                System.Windows.MessageBox.Show($"Finished:\r\n{_midiConverter.Summary}", ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
    }

    private void MidiConverter_Progress(object sender, ProgressEventArgs e)
    {
        var color = System.Drawing.Color.Black;
        if (e.MessageType == ProgressMessageType.Warning)
            color = System.Drawing.Color.Blue;
        else if (e.MessageType == ProgressMessageType.Error)
            color = System.Drawing.Color.Red;
        else if (e.MessageType == ProgressMessageType.Trace)
            color = System.Drawing.Color.Purple;
        ProgressLog.LogMessage(color, e.Message);
    }

    private bool CheckInputFolderExists()
    {
        if (Directory.Exists(TextBoxInputFolder.Text)) return true;
        System.Windows.MessageBox.Show("Your selected input folder does not exist.", ProductName, MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private bool CheckOutputFolderExists()
    {
        if (Directory.Exists(TextBoxOutputFolder.Text)) return true;
        var result = System.Windows.MessageBox.Show("Your selected output folder does not exist.\r\nWould you like to create it now?", ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
            Directory.CreateDirectory(TextBoxOutputFolder.Text);
        else
            return false;
        return true;
    }

    private bool CheckOutputFolderIsEmpty()
    {
        var path = TextBoxOutputFolder.Text;
        if (Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0) return true;
        System.Windows.MessageBox.Show("Your output folder is not empty.\r\nYou must select an empty folder to store the converted MIDI files.", ProductName, MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private void BrowseInputFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Select Input Folder",
            SelectedPath = TextBoxInputFolder.Text
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            TextBoxInputFolder.Text = dlg.SelectedPath;
    }

    private void BrowseOutputFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Select Output Folder",
            SelectedPath = TextBoxOutputFolder.Text
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            TextBoxOutputFolder.Text = dlg.SelectedPath;
            if (CheckOutputFolderExists())
                CheckOutputFolderIsEmpty();
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void Contents_Click(object sender, RoutedEventArgs e)
    {
        var baseDir = AppContext.BaseDirectory;
        if (string.IsNullOrEmpty(baseDir) && !string.IsNullOrEmpty(Environment.ProcessPath))
            baseDir = Path.GetDirectoryName(Environment.ProcessPath) ?? ".";
        var helpFilePath = Path.Combine(baseDir, "midi_file_converter.html");
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(helpFilePath) { UseShellExecute = true });
        }
        catch (Win32Exception)
        {
            System.Windows.MessageBox.Show("Could not display the help file", ProductName, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var about = new AboutWindow { Owner = this };
        about.ShowDialog();
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        ProgressLog.ClearLog();
        SaveLogMenuItem.IsEnabled = false;
    }

    private void Options_Click(object sender, RoutedEventArgs e)
    {
        if (_workQueued)
        {
            System.Windows.MessageBox.Show("Please wait until the current operation has finished", ProductName, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var options = new AdvancedOptionsWindow { Owner = this };
        options.ShowDialog();
    }

    private void SaveLog_Click(object sender, RoutedEventArgs e)
    {
        if (_workQueued)
        {
            System.Windows.MessageBox.Show("Please wait until the current operation has finished", ProductName, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            InitialDirectory = TextBoxOutputFolder.Text,
            DefaultExt = ".txt",
            FileName = "Conversion Log.txt",
            Filter = "Text Files (*.txt)|*.txt",
            FilterIndex = 1
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var text = ProgressLog.Text;
            if (!text.Contains("\r"))
                text = text.Replace("\n", "\r\n");
            File.WriteAllText(dlg.FileName, text);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error saving conversion log\r\n{ex.Message}", ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
