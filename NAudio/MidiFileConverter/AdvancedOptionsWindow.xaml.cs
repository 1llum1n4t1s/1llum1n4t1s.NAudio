using System.Windows;

namespace MarkHeath.MidiUtils;

/// <summary>
/// 詳細オプションウィンドウ。
/// </summary>
public partial class AdvancedOptionsWindow : Window
{
    private readonly Properties.Settings _settings;

    /// <summary>
    /// コンストラクター。
    /// </summary>
    public AdvancedOptionsWindow()
    {
        InitializeComponent();
        _settings = Properties.Settings.Default;
        LoadSettings();
    }

    private void LoadSettings()
    {
        CheckBoxRemoveSequencerSpecific.IsChecked = _settings.RemoveSequencerSpecific;
        CheckBoxRecreateEndTrack.IsChecked = _settings.RecreateEndTrackMarkers;
        CheckBoxAddNameMarker.IsChecked = _settings.AddNameMarker;
        CheckBoxTrimTextEvents.IsChecked = _settings.TrimTextEvents;
        CheckBoxRemoveEmptyTracks.IsChecked = _settings.RemoveEmptyTracks;
        CheckBoxRemoveExtraTempoEvents.IsChecked = _settings.RemoveExtraTempoEvents;
        CheckBoxRemoveExtraMarkers.IsChecked = _settings.RemoveExtraMarkers;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        _settings.RemoveSequencerSpecific = CheckBoxRemoveSequencerSpecific.IsChecked == true;
        _settings.RecreateEndTrackMarkers = CheckBoxRecreateEndTrack.IsChecked == true;
        _settings.AddNameMarker = CheckBoxAddNameMarker.IsChecked == true;
        _settings.TrimTextEvents = CheckBoxTrimTextEvents.IsChecked == true;
        _settings.RemoveEmptyTracks = CheckBoxRemoveEmptyTracks.IsChecked == true;
        _settings.RemoveExtraTempoEvents = CheckBoxRemoveExtraTempoEvents.IsChecked == true;
        _settings.RemoveExtraMarkers = CheckBoxRemoveExtraMarkers.IsChecked == true;
        _settings.Save();
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
