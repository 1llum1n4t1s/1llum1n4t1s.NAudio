using System;
using System.Windows;
using NAudio.Wave;
using MarkHeath.AudioUtils.Properties;

namespace MarkHeath.AudioUtils;

/// <summary>
/// 設定ウィンドウ。
/// </summary>
public partial class SettingsWindow : Window
{
    /// <summary>
    /// コンストラクター。
    /// </summary>
    public SettingsWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ComboOutputDevice.Items.Add(new WaveOutComboItem("(Default)", -1));
        for (var n = 0; n < WaveOut.DeviceCount; n++)
        {
            var caps = WaveOut.GetCapabilities(n);
            ComboOutputDevice.Items.Add(new WaveOutComboItem(caps.ProductName, n));
        }
        var settings = Settings.Default;
        TextBoxSkipBackSeconds.Text = settings.SkipBackSeconds.ToString();
        CheckUseAllSlots.IsChecked = settings.UseAllSlots;
        foreach (WaveOutComboItem item in ComboOutputDevice.Items)
        {
            if (item.DeviceNumber == settings.WaveOutDevice)
            {
                ComboOutputDevice.SelectedItem = item;
                break;
            }
        }
        if (ComboOutputDevice.SelectedItem == null && ComboOutputDevice.Items.Count > 0)
            ComboOutputDevice.SelectedIndex = 0;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TextBoxSkipBackSeconds.Text, out var skipBackSeconds) || skipBackSeconds <= 0)
        {
            MessageBox.Show("Please enter a valid number of skip back seconds");
            TextBoxSkipBackSeconds.Focus();
            return;
        }
        var settings = Settings.Default;
        settings.SkipBackSeconds = skipBackSeconds;
        settings.UseAllSlots = CheckUseAllSlots.IsChecked == true;
        if (ComboOutputDevice.SelectedItem is WaveOutComboItem item)
            settings.WaveOutDevice = item.DeviceNumber;
        settings.Save();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

/// <summary>
/// 出力デバイス用コンボボックス項目。
/// </summary>
internal class WaveOutComboItem
{
    public string DeviceName { get; }
    public int DeviceNumber { get; }

    public WaveOutComboItem(string deviceName, int deviceNumber)
    {
        DeviceName = deviceName;
        DeviceNumber = deviceNumber;
    }
}
