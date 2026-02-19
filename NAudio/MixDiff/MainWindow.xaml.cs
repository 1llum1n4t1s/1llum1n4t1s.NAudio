using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using Microsoft.Win32;
using NAudio.Wave;
using MarkHeath.AudioUtils.Properties;

namespace MarkHeath.AudioUtils;

/// <summary>
/// MixDiff メインウィンドウ。
/// </summary>
public partial class MainWindow : Window
{
    private PlaybackStatus _playbackStatus;
    private IWavePlayer _wavePlayer;
    private WaveMixerStream32 _mixer;
    private int _skipSeconds;
    private Button _selectedButton;
    private CompareMode _compareMode;
    private List<Button> _fileButtons;
    private bool _shuffled;
    private Button _contextMenuSourceButton;
    private readonly System.Windows.Threading.DispatcherTimer _timer;

    /// <summary>
    /// コンストラクター。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        _mixer = new WaveMixerStream32();
        _mixer.AutoStop = false;
        _skipSeconds = Settings.Default.SkipBackSeconds;
        if (_skipSeconds <= 0) _skipSeconds = 3;
        _fileButtons = new List<Button> { ButtonA, ButtonB, ButtonC, ButtonD };
        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _timer.Tick += Timer_Tick;
    }

    private void SlotContextMenu_Opening(object sender, ContextMenuEventArgs e)
    {
        _contextMenuSourceButton = e.Source as Button;
    }

    private bool LoadFile(Button button)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "WAV Files (*.wav)|*.wav"
        };
        if (dlg.ShowDialog() != true) return false;
        var info = new MixdownInfo(dlg.FileName);
        info.Letter = button.Name.Substring(button.Name.Length - 1);
        SetButtonInfo(button, info);
        return true;
    }

    private void SetButtonInfo(Button button, MixdownInfo info)
    {
        if (button.Tag != null)
            ClearFile(button);
        button.Tag = info;
        SetButtonAppearance(button);
        _mixer.AddInputStream(info.Stream);
        SetLengthLabel();
    }

    private void ClearFile(Button button)
    {
        if (button.Tag is MixdownInfo buttonInfo)
        {
            _mixer.RemoveInputStream(buttonInfo.Stream);
            buttonInfo.Stream.Close();
            button.Tag = null;
            SetButtonAppearance(button);
        }
    }

    private void SetButtonAppearance(Button button)
    {
        if (button.Tag is not MixdownInfo info)
        {
            button.Content = "<Empty>";
            button.ToolTip = null;
        }
        else
        {
            if (_shuffled)
            {
                button.Content = "?";
                button.ToolTip = null;
            }
            else
            {
                button.Content = info.Letter;
                button.ToolTip = info.FileName;
            }
        }
    }

    private void MixButton_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        if (button.Tag == null)
        {
            if (LoadFile(button))
                SelectButton(button);
        }
        else
        {
            SelectButton(button);
        }
    }

    private void SelectButton(Button button)
    {
        if (button.Tag is not MixdownInfo info)
            return;
        if (_selectedButton != null)
        {
            _selectedButton.Background = System.Windows.Media.Brushes.Transparent;
            _selectedButton.Foreground = System.Windows.Media.Brushes.Black;
            if (_selectedButton.Tag is MixdownInfo prevInfo)
                prevInfo.Stream.Mute = true;
        }
        button.Foreground = Brushes.DarkGreen;
        button.Background = new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xD2));
        info.Stream.Mute = false;
        _selectedButton = button;
        if (_playbackStatus == PlaybackStatus.Playing)
        {
            if (_compareMode == CompareMode.SkipBack)
                SkipBack();
            else if (_compareMode == CompareMode.Restart)
                Rewind();
        }
    }

    private void Play()
    {
        if (_playbackStatus == PlaybackStatus.Playing) return;
        if (_playbackStatus != PlaybackStatus.Paused)
            Rewind();
        if (_wavePlayer == null)
        {
            _wavePlayer = new WaveOut();
            _wavePlayer.Init(_mixer);
        }
        _wavePlayer.Play();
        _playbackStatus = PlaybackStatus.Playing;
        _timer.Start();
    }

    private void Stop()
    {
        if (_playbackStatus == PlaybackStatus.Stopped) return;
        if (_wavePlayer != null)
        {
            _wavePlayer.Stop();
            _playbackStatus = PlaybackStatus.Stopped;
            _timer.Stop();
        }
        Rewind();
    }

    private void Pause()
    {
        if (_playbackStatus == PlaybackStatus.Playing)
        {
            _wavePlayer.Pause();
            _playbackStatus = PlaybackStatus.Paused;
        }
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        if (_playbackStatus != PlaybackStatus.Playing) return;
        if (_mixer.Position >= _mixer.Length)
        {
            if (CheckLoop.IsChecked == true)
                Rewind();
            else
                Stop();
        }
        SetPositionLabel();
    }

    private void SetPositionLabel()
    {
        var t = _mixer.CurrentTime;
        LabelPosition.Text = $"{t.Hours:00}:{t.Minutes:00}:{t.Seconds:00}.{t.Milliseconds:000}";
    }

    private void SetLengthLabel()
    {
        var t = _mixer.TotalTime;
        LabelLength.Text = $"{t.Hours:00}:{t.Minutes:00}:{t.Seconds:00}";
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_playbackStatus != PlaybackStatus.Stopped)
            Stop();
        _wavePlayer?.Dispose();
        _wavePlayer = null;
        _mixer?.Dispose();
        _mixer = null;
    }

    private void Play_Click(object sender, RoutedEventArgs e) => Play();
    private void Pause_Click(object sender, RoutedEventArgs e) => Pause();
    private void Stop_Click(object sender, RoutedEventArgs e) => Stop();
    private void Back_Click(object sender, RoutedEventArgs e) => SkipBack();
    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        if (_mixer != null)
        {
            _mixer.CurrentTime += TimeSpan.FromSeconds(_skipSeconds);
            SetPositionLabel();
        }
    }
    private void Rewind_Click(object sender, RoutedEventArgs e) => Rewind();

    private void SkipBack()
    {
        if (_mixer != null)
        {
            var newTime = _mixer.CurrentTime - TimeSpan.FromSeconds(_skipSeconds);
            _mixer.CurrentTime = newTime < TimeSpan.Zero ? TimeSpan.Zero : newTime;
            SetPositionLabel();
        }
    }

    private void Rewind()
    {
        if (_mixer != null)
        {
            _mixer.Position = 0;
            SetPositionLabel();
        }
    }

    private void CompareMode_UpdateMenu()
    {
        MenuCurrentPosition.IsChecked = _compareMode == CompareMode.CurrentPosition;
        MenuSkipBack.IsChecked = _compareMode == CompareMode.SkipBack;
        MenuRestart.IsChecked = _compareMode == CompareMode.Restart;
    }

    private void CompareModeCurrent_Click(object sender, RoutedEventArgs e) { _compareMode = CompareMode.CurrentPosition; CompareMode_UpdateMenu(); }
    private void CompareModeSkipBack_Click(object sender, RoutedEventArgs e) { _compareMode = CompareMode.SkipBack; CompareMode_UpdateMenu(); }
    private void CompareModeRestart_Click(object sender, RoutedEventArgs e) { _compareMode = CompareMode.Restart; CompareMode_UpdateMenu(); }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void ContextSelectFile_Click(object sender, RoutedEventArgs e)
    {
        if (_contextMenuSourceButton != null)
            LoadFile(_contextMenuSourceButton);
    }

    private void ContextClear_Click(object sender, RoutedEventArgs e)
    {
        if (_contextMenuSourceButton != null)
            ClearFile(_contextMenuSourceButton);
    }

    private void ContextProperties_Click(object sender, RoutedEventArgs e)
    {
        if (_contextMenuSourceButton?.Tag is MixdownInfo mixdownInfo)
        {
            var win = new PropertiesWindow(mixdownInfo);
            win.ShowDialog();
        }
    }

    private void SaveComparison_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            DefaultExt = ".MixDiff",
            Filter = "*.MixDiff (MixDiff Comparison Files)|*.MixDiff|*.* (All Files)|*.*"
        };
        if (dlg.ShowDialog() == true)
            SaveComparison(dlg.FileName);
    }

    private void SaveComparison(string fileName)
    {
        var settings = new XmlWriterSettings { Indent = true, NewLineOnAttributes = true };
        using (var writer = XmlWriter.Create(fileName, settings))
        {
            writer.WriteStartElement("MixDiff");
            writer.WriteStartElement("Settings");
            writer.WriteAttributeString("CompareMode", _compareMode.ToString());
            writer.WriteEndElement();
            foreach (var btn in _fileButtons)
                WriteMixdownInfo(writer, btn.Tag as MixdownInfo);
            writer.WriteEndElement();
        }
    }

    private void WriteMixdownInfo(XmlWriter writer, MixdownInfo mixdownInfo)
    {
        if (mixdownInfo == null) return;
        writer.WriteStartElement("Mix");
        writer.WriteAttributeString("FileName", mixdownInfo.FileName);
        writer.WriteAttributeString("DelayMilliseconds", mixdownInfo.DelayMilliseconds.ToString());
        writer.WriteAttributeString("OffsetMilliseconds", mixdownInfo.OffsetMilliseconds.ToString());
        writer.WriteAttributeString("VolumeDecibels", mixdownInfo.VolumeDecibels.ToString());
        writer.WriteEndElement();
    }

    private void OpenComparison_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            DefaultExt = ".MixDiff",
            Filter = "*.MixDiff (MixDiff Comparison Files)|*.MixDiff|*.* (All Files)|*.*"
        };
        if (dlg.ShowDialog() != true) return;
        Stop();
        foreach (var btn in _fileButtons)
            ClearFile(btn);
        LoadComparison(dlg.FileName);
    }

    private void LoadComparison(string fileName)
    {
        var doc = new XmlDocument();
        doc.Load(fileName);
        var compareModeNode = doc.SelectSingleNode("MixDiff/Settings/@CompareMode");
        if (compareModeNode != null)
        {
            _compareMode = (CompareMode)Enum.Parse(typeof(CompareMode), compareModeNode.Value);
            CompareMode_UpdateMenu();
        }
        var mixes = doc.SelectNodes("MixDiff/Mix");
        var buttonIndex = 0;
        foreach (XmlNode mixNode in mixes)
        {
            if (buttonIndex >= _fileButtons.Count) break;
            var button = _fileButtons[buttonIndex++];
            var path = mixNode.Attributes["FileName"]?.Value;
            if (string.IsNullOrEmpty(path)) continue;
            MixdownInfo info;
            try
            {
                info = new MixdownInfo(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load file: {path}\n{ex.Message}", "MixDiff", MessageBoxButton.OK, MessageBoxImage.Warning);
                continue;
            }
            if (!int.TryParse(mixNode.Attributes["DelayMilliseconds"]?.Value, out var delay)) delay = 0;
            if (!int.TryParse(mixNode.Attributes["OffsetMilliseconds"]?.Value, out var offset)) offset = 0;
            if (!int.TryParse(mixNode.Attributes["VolumeDecibels"]?.Value, out var volume)) volume = 0;
            info.DelayMilliseconds = delay;
            info.OffsetMilliseconds = offset;
            info.VolumeDecibels = volume;
            info.Letter = button.Name.Substring(button.Name.Length - 1);
            info.Stream.Mute = true;
            SetButtonInfo(button, info);
        }
        if (_fileButtons.Count > 0 && _fileButtons[0].Tag != null)
            SelectButton(_fileButtons[0]);
    }

    private void Contents_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("http://www.codeplex.com/naudio/Wiki/View.aspx?title=MixDiff") { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception)
        {
            MessageBox.Show("Failed to launch browser to show help file");
        }
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var win = new AboutWindow();
        win.ShowDialog();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var win = new SettingsWindow();
        if (win.ShowDialog() == true)
            _skipSeconds = Settings.Default.SkipBackSeconds;
    }

    private void Shuffle_Click(object sender, RoutedEventArgs e)
    {
        if (!_shuffled)
            Shuffle();
        else
            Reveal();
        CheckShuffle.IsChecked = _shuffled;
    }

    private void Shuffle()
    {
        var mixdowns = new List<MixdownInfo>();
        foreach (var btn in _fileButtons)
        {
            if (btn.Tag is MixdownInfo info)
                mixdowns.Add(info);
        }
        var rand = new Random();
        if (mixdowns.Count < 2)
        {
            MessageBox.Show("You need to have at least two files to compare to use the shuffle feature", "MixDiff");
            return;
        }
        _shuffled = true;
        if (Settings.Default.UseAllSlots)
        {
            foreach (var btn in _fileButtons)
            {
                if (btn.Tag == null)
                    btn.Tag = mixdowns[rand.Next(mixdowns.Count)];
            }
        }
        for (var n = 0; n < 12; n++)
        {
            var swap1 = rand.Next(_fileButtons.Count);
            var swap2 = rand.Next(_fileButtons.Count);
            if (swap1 != swap2)
            {
                var tag1 = _fileButtons[swap1].Tag;
                _fileButtons[swap1].Tag = _fileButtons[swap2].Tag;
                _fileButtons[swap2].Tag = tag1;
            }
        }
        Button firstMix = null;
        foreach (var btn in _fileButtons)
        {
            SetButtonAppearance(btn);
            if (btn.Tag != null && firstMix == null)
                firstMix = btn;
        }
        if (firstMix != null)
            SelectButton(firstMix);
    }

    private void Reveal()
    {
        _shuffled = false;
        foreach (var btn in _fileButtons)
            SetButtonAppearance(btn);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.D1: MixButton_Click(ButtonA, e); e.Handled = true; break;
            case Key.D2: MixButton_Click(ButtonB, e); e.Handled = true; break;
            case Key.D3: MixButton_Click(ButtonC, e); e.Handled = true; break;
            case Key.D4: MixButton_Click(ButtonD, e); e.Handled = true; break;
            case Key.Space:
                if (_playbackStatus != PlaybackStatus.Playing) Play(); else Pause();
                e.Handled = true;
                break;
            case Key.Home: Rewind(); e.Handled = true; break;
        }
    }
}
