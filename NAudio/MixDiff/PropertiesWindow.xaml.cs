using System.Windows;

namespace MarkHeath.AudioUtils;

/// <summary>
/// ミックスダウンプロパティウィンドウ。
/// </summary>
public partial class PropertiesWindow : Window
{
    private readonly MixdownInfo _mixdownInfo;
    private int _originalVolumeDecibels;

    /// <summary>
    /// コンストラクター。
    /// </summary>
    public PropertiesWindow(MixdownInfo mixdownInfo)
    {
        InitializeComponent();
        _mixdownInfo = mixdownInfo;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _originalVolumeDecibels = _mixdownInfo.VolumeDecibels;
        TextBoxDelay.Text = _mixdownInfo.DelayMilliseconds.ToString();
        TextBoxOffset.Text = _mixdownInfo.OffsetMilliseconds.ToString();
        SliderVolume.Value = _mixdownInfo.VolumeDecibels;
        UpdateVolumeText();
    }

    private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_mixdownInfo != null)
            _mixdownInfo.VolumeDecibels = (int)SliderVolume.Value;
        UpdateVolumeText();
    }

    private void UpdateVolumeText()
    {
        TextVolume.Text = $"{SliderVolume.Value:0} dB";
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TextBoxDelay.Text, out var delay) || delay < 0)
        {
            MessageBox.Show("Please enter a valid number of milliseconds for the delay.");
            TextBoxDelay.Focus();
            return;
        }
        if (!int.TryParse(TextBoxOffset.Text, out var offset) || offset < 0)
        {
            MessageBox.Show("Please enter a valid number of milliseconds to trim from the start.");
            TextBoxOffset.Focus();
            return;
        }
        _mixdownInfo.DelayMilliseconds = delay;
        _mixdownInfo.OffsetMilliseconds = offset;
        _mixdownInfo.VolumeDecibels = (int)SliderVolume.Value;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _mixdownInfo.VolumeDecibels = _originalVolumeDecibels;
        DialogResult = false;
        Close();
    }
}
