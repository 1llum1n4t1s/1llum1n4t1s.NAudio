using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace MarkHeath.MidiUtils;

/// <summary>
/// バージョン情報ダイアログ。
/// </summary>
public partial class AboutWindow : Window
{
    /// <summary>
    /// コンストラクター。
    /// </summary>
    public AboutWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetName();
        LabelProductName.Text = name.Name ?? "MIDI File Converter";
        var ver = name.Version;
        LabelVersion.Text = ver != null ? $"Version: {ver}" : "Version: 1.0";
        LabelCopyright.Text = "Copyright © Mark Heath 2016";
        Title = $"About {LabelProductName.Text}";
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Could not open link: {0}", ex.Message),
                Title,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        e.Handled = true;
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => Close();
}
