using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace NAudioTests.Wasapi
{
    /// <summary>
    /// プロセスループバックキャプチャの動作確認用フォーム。
    /// ドロップダウンでプロセスを選び、キャプチャ開始/停止でテストする。
    /// </summary>
    public sealed class ProcessLoopbackCaptureTestForm : Form
    {
        private readonly ComboBox _processComboBox;
        private readonly Button _refreshButton;
        private readonly CheckBox _includeProcessTreeCheckBox;
        private readonly Button _startStopButton;
        private readonly TextBox _logTextBox;
        private readonly Label _statusLabel;
        private WasapiCapture _capture;
        private long _totalBytesCaptured;
        private bool _isCapturing;

        /// <summary>
        /// フォームのコンストラクター。
        /// </summary>
        public ProcessLoopbackCaptureTestForm()
        {
            Text = "プロセスループバックキャプチャ テスト";
            Size = new Size(520, 420);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            var processLabel = new Label
            {
                Text = "プロセス:",
                Location = new Point(12, 14),
                AutoSize = true
            };
            _processComboBox = new ComboBox
            {
                Location = new Point(80, 11),
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _refreshButton = new Button
            {
                Text = "一覧を更新",
                Location = new Point(370, 9),
                Width = 90
            };
            _refreshButton.Click += RefreshButton_Click;

            _includeProcessTreeCheckBox = new CheckBox
            {
                Text = "子プロセスを含む (Include process tree)",
                Location = new Point(80, 42),
                AutoSize = true,
                Checked = false
            };

            _startStopButton = new Button
            {
                Text = "キャプチャ開始",
                Location = new Point(80, 72),
                Width = 120
            };
            _startStopButton.Click += StartStopButton_Click;

            _statusLabel = new Label
            {
                Text = "状態: 停止中",
                Location = new Point(210, 76),
                AutoSize = true
            };

            var logLabel = new Label
            {
                Text = "ログ:",
                Location = new Point(12, 108),
                AutoSize = true
            };
            _logTextBox = new TextBox
            {
                Location = new Point(12, 128),
                Size = new Size(476, 240),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9f)
            };

            Controls.Add(processLabel);
            Controls.Add(_processComboBox);
            Controls.Add(_refreshButton);
            Controls.Add(_includeProcessTreeCheckBox);
            Controls.Add(_startStopButton);
            Controls.Add(_statusLabel);
            Controls.Add(logLabel);
            Controls.Add(_logTextBox);

            LoadProcessList();
        }

        /// <summary>
        /// プロセス一覧をコンボボックスに読み込む。
        /// </summary>
        private void LoadProcessList()
        {
            _processComboBox.Items.Clear();
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
            {
                _processComboBox.Items.Add(item);
            }
            if (_processComboBox.Items.Count > 0)
            {
                var self = Process.GetCurrentProcess();
                for (var i = 0; i < _processComboBox.Items.Count; i++)
                {
                    if (((ProcessItem)_processComboBox.Items[i]).ProcessId == self.Id)
                    {
                        _processComboBox.SelectedIndex = i;
                        break;
                    }
                }
                if (_processComboBox.SelectedIndex < 0)
                    _processComboBox.SelectedIndex = 0;
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            LoadProcessList();
            Log("プロセス一覧を更新しました。");
        }

        private async void StartStopButton_Click(object sender, EventArgs e)
        {
            if (_isCapturing)
            {
                StopCapture();
                return;
            }
            if (_processComboBox.SelectedItem is not ProcessItem item)
            {
                Log("プロセスを選択してください。");
                return;
            }
            await StartCaptureAsync(item.ProcessId, _includeProcessTreeCheckBox.Checked);
        }

        /// <summary>
        /// 指定プロセスのループバックキャプチャを非同期で開始する。
        /// </summary>
        private async Task StartCaptureAsync(int processId, bool includeProcessTree)
        {
            _startStopButton.Enabled = false;
            _statusLabel.Text = "状態: 初期化中...";
            Log($"プロセス ID={processId} のキャプチャを開始します (子プロセス含む={includeProcessTree})");
            try
            {
                _capture = await WasapiCapture.CreateForProcessCaptureAsync(processId, includeProcessTree);
                _totalBytesCaptured = 0;
                _capture.DataAvailable += Capture_DataAvailable;
                _capture.RecordingStopped += Capture_RecordingStopped;
                _capture.StartRecording();
                _isCapturing = true;
                _startStopButton.Text = "キャプチャ停止";
                _startStopButton.Enabled = true;
                _statusLabel.Text = "状態: キャプチャ中";
                Log("キャプチャを開始しました。");
            }
            catch (Exception ex)
            {
                Log($"開始エラー: {ex.Message}");
                _statusLabel.Text = "状態: エラー";
                _startStopButton.Enabled = true;
            }
        }

        private void Capture_DataAvailable(object sender, WaveInEventArgs e)
        {
            _totalBytesCaptured += e.BytesRecorded;
            if (InvokeRequired)
            {
                BeginInvoke(() => UpdateCaptureStatus());
            }
            else
            {
                UpdateCaptureStatus();
            }
        }

        private void UpdateCaptureStatus()
        {
            _statusLabel.Text = $"状態: キャプチャ中 ({_totalBytesCaptured:N0} bytes)";
        }

        private void Capture_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
                Log($"停止時エラー: {e.Exception.Message}");
            if (InvokeRequired)
            {
                BeginInvoke(StopCapture);
            }
            else
            {
                StopCapture();
            }
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
            _startStopButton.Text = "キャプチャ開始";
            _statusLabel.Text = $"状態: 停止中 (合計 {_totalBytesCaptured:N0} bytes)";
            Log("キャプチャを停止しました。");
        }

        private void Log(string message)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {message}\r\n";
            if (InvokeRequired)
            {
                BeginInvoke(() => AppendLog(line));
            }
            else
            {
                AppendLog(line);
            }
        }

        private void AppendLog(string line)
        {
            _logTextBox.AppendText(line);
        }

        /// <summary>
        /// フォームクローズ時にキャプチャを止める。
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isCapturing)
                StopCapture();
            base.OnFormClosing(e);
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
}
