using System.Reflection;
using System.Windows;

namespace AudioFileInspector;

/// <summary>
/// バージョン情報ウィンドウ。
/// </summary>
public partial class AboutWindow : Window
{
    /// <summary>
    /// コンストラクター。
    /// </summary>
    public AboutWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            var asm = Assembly.GetExecutingAssembly();
            var name = asm.GetName();
            LabelProductName.Text = name.Name ?? "Audio File Inspector";
            var ver = name.Version;
            LabelVersion.Text = ver != null ? $"Version: {ver}" : "Version: 1.0";
            LabelCopyright.Text = asm.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty;
            Title = $"About {LabelProductName.Text}";
        };
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => Close();
}
