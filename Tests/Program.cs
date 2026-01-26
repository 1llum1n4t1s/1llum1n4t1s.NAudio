using System;
using System.Windows.Forms;
using NAudioTests.Wasapi;

namespace NAudioTests
{
    /// <summary>
    /// プロセスループバックキャプチャテスト用のエントリポイント。
    /// プロジェクトを「スタートアップ」にして実行するとこのフォームが起動する。
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ProcessLoopbackCaptureTestForm());
        }
    }
}
