using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace NAudioTests.Wasapi;

/// <summary>
/// プロセスループバックキャプチャの動作確認用 WPF ウィンドウ。
/// </summary>
public partial class ProcessLoopbackCaptureTestWindow : Window
{
    private WasapiCapture _capture;
    private WaveFileWriter _waveFileWriter;
    private string _savingFilePath;
    private long _totalBytesCaptured;
    private bool _isCapturing;

    /// <summary>
    /// ウィンドウのコンストラクター。
    /// </summary>
    public ProcessLoopbackCaptureTestWindow()
    {
        InitializeComponent();
        LoadProcessList();
        Closing += (_, e) =>
        {
            if (_isCapturing)
                StopCapture();
        };
    }

    /// <summary>
    /// プロセス一覧をコンボボックスに読み込む。
    /// </summary>
    private void LoadProcessList()
    {
        ProcessComboBox.Items.Clear();
        var items = new List<ProcessItem>();
        foreach (var p in Process.GetProcesses().OrderBy(x => x.ProcessName))
        {
            try
            {
                var name = string.IsNullOrEmpty(p.MainWindowTitle) ? p.ProcessName : $"{p.ProcessName} - {p.MainWindowTitle}";
                items.Add(new ProcessItem(name, p.Id));
            }
            catch
            {
                items.Add(new ProcessItem(p.ProcessName, p.Id));
            }
        }
        foreach (var item in items)
            ProcessComboBox.Items.Add(item);
        if (ProcessComboBox.Items.Count > 0)
        {
            var self = Process.GetCurrentProcess();
            for (var i = 0; i < ProcessComboBox.Items.Count; i++)
            {
                if (((ProcessItem)ProcessComboBox.Items[i]).ProcessId == self.Id)
                {
                    ProcessComboBox.SelectedIndex = i;
                    break;
                }
            }
            if (ProcessComboBox.SelectedIndex < 0)
                ProcessComboBox.SelectedIndex = 0;
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        LoadProcessList();
        Log("プロセス一覧を更新しました。");
    }

    private async void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isCapturing)
        {
            StopCapture();
            return;
        }
        if (ProcessComboBox.SelectedItem is not ProcessItem item)
        {
            Log("プロセスを選択してください。");
            return;
        }
        await StartCaptureAsync(item.ProcessId, IncludeProcessTreeCheckBox.IsChecked == true);
    }

    /// <summary>
    /// ファイル保存用の保存先パスをユーザーに選択させる。
    /// </summary>
    /// <returns>選択されたフルパス。キャンセル時は null。</returns>
    private string AskSaveFilePath()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "WAV ファイル (*.wav)|*.wav|すべてのファイル (*.*)|*.*",
            DefaultExt = "wav",
            FileName = $"loopback_{DateTime.Now:yyyyMMdd_HHmmss}.wav",
            Title = "キャプチャ保存先を指定"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    private async Task StartCaptureAsync(int processId, bool includeProcessTree)
    {
        StartStopButton.IsEnabled = false;
        StatusLabel.Text = "状態: 初期化中...";
        Log($"プロセス ID={processId} のキャプチャを開始します (子プロセス含む={includeProcessTree})");
        var wantSaveToFile = SaveToFileCheckBox.IsChecked == true;
        if (wantSaveToFile)
        {
            _savingFilePath = AskSaveFilePath();
            if (string.IsNullOrEmpty(_savingFilePath))
            {
                Log("ファイル保存をキャンセルしました。キャプチャのみ行います。");
                wantSaveToFile = false;
            }
        }
        try
        {
            _capture = await WasapiCapture.CreateForProcessCaptureAsync(processId, includeProcessTree);
            _totalBytesCaptured = 0;
            _waveFileWriter = null;
            if (wantSaveToFile && !string.IsNullOrEmpty(_savingFilePath))
            {
                _waveFileWriter = new WaveFileWriter(_savingFilePath, _capture.WaveFormat);
                Log($"保存先: {_savingFilePath}");
            }
            _capture.DataAvailable += Capture_DataAvailable;
            _capture.RecordingStopped += Capture_RecordingStopped;
            _capture.StartRecording();
            _isCapturing = true;
            StartStopButton.Content = "キャプチャ停止";
            StartStopButton.IsEnabled = true;
            StatusLabel.Text = "状態: キャプチャ中";
            Log("キャプチャを開始しました。");
        }
        catch (Exception ex)
        {
            Log($"開始エラー: {ex.Message}");
            LogExceptionDetail(ex);
            StatusLabel.Text = "状態: エラー";
            StartStopButton.IsEnabled = true;
        }
    }

    private void Capture_DataAvailable(object sender, WaveInEventArgs e)
    {
        _totalBytesCaptured += e.BytesRecorded;
        if (e.BytesRecorded > 0)
            _waveFileWriter?.Write(e.Buffer, 0, e.BytesRecorded);
        Dispatcher.BeginInvoke(() => UpdateCaptureStatus());
    }

    private void UpdateCaptureStatus()
    {
        StatusLabel.Text = $"状態: キャプチャ中 ({_totalBytesCaptured:N0} bytes)";
    }

    private void Capture_RecordingStopped(object sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
            Log($"停止時エラー: {e.Exception.Message}");
        Dispatcher.BeginInvoke(StopCapture);
    }

    private void StopCapture()
    {
        if (_capture == null) return;
        _isCapturing = false;
        try
        {
            _capture.DataAvailable -= Capture_DataAvailable;
            _capture.RecordingStopped -= Capture_RecordingStopped;
            _capture.StopRecording();
            _capture.Dispose();
        }
        catch (Exception ex)
        {
            Log($"停止処理でエラー: {ex.Message}");
        }
        _capture = null;
        if (_waveFileWriter != null)
        {
            try
            {
                _waveFileWriter.Dispose();
                Log($"ファイルに保存しました: {_savingFilePath}");
            }
            catch (Exception ex)
            {
                Log($"ファイル保存のクローズでエラー: {ex.Message}");
            }
            _waveFileWriter = null;
            _savingFilePath = null;
        }
        StartStopButton.Content = "キャプチャ開始";
        StatusLabel.Text = $"状態: 停止中 (合計 {_totalBytesCaptured:N0} bytes)";
        Log("キャプチャを停止しました。");
    }

    /// <summary>
    /// ログテキストボックスに1行書き出す。
    /// </summary>
    private void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}\r\n";
        if (!Dispatcher.CheckAccess())
            Dispatcher.BeginInvoke(() => AppendLog(line));
        else
            AppendLog(line);
    }

    /// <summary>
    /// 例外の型・スタックトレース・内部例外をログに追記する。
    /// </summary>
    private void LogExceptionDetail(Exception ex, int depth = 0)
    {
        var prefix = new string(' ', depth * 2);
        Log($"{prefix}例外型: {ex.GetType().FullName}");
        if (!string.IsNullOrEmpty(ex.StackTrace))
        {
            foreach (var line in ex.StackTrace.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                Log($"{prefix}  {line.Trim()}");
        }
        if (ex.InnerException != null && depth < 5)
        {
            Log($"{prefix}内部例外: {ex.InnerException.Message}");
            LogExceptionDetail(ex.InnerException, depth + 1);
        }
    }

    private void AppendLog(string line)
    {
        LogTextBox.AppendText(line);
    }

    private sealed class ProcessItem
    {
        internal string Display { get; }
        internal int ProcessId { get; }

        internal ProcessItem(string display, int processId)
        {
            Display = $"{display} (PID: {processId})";
            ProcessId = processId;
        }

        public override string ToString() => Display;
    }
}
