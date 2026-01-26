using System;
using System.Windows;
using System.Windows.Input;

namespace NAudio.Gui;

/// <summary>
/// WPF ボリュームスライダーコントロール。
/// </summary>
public partial class VolumeSlider
{
    private const float MinDb = -48f;
    private float _volume = 1.0f;

    /// <summary>
    /// ボリューム変更イベント。
    /// </summary>
    public event EventHandler VolumeChanged;

    /// <summary>
    /// コンストラクター。
    /// </summary>
    public VolumeSlider()
    {
        InitializeComponent();
        UpdateDisplay();
    }

    /// <summary>
    /// ボリューム (0.0〜1.0)。
    /// </summary>
    public float Volume
    {
        get => _volume;
        set
        {
            var v = Math.Clamp(value, 0f, 1f);
            if (Math.Abs(_volume - v) < 1e-6)
                return;
            _volume = v;
            VolumeChanged?.Invoke(this, EventArgs.Empty);
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        var db = 20f * (float)Math.Log10(_volume <= 0 ? 1e-6 : _volume);
        var percent = 1f - (db / MinDb);
        DbText.Text = $"{db:F2} dB";
        if (ActualWidth > 0)
        {
            FillRect.Width = Math.Max(0, (ActualWidth - 2) * percent);
            FillRect.Height = Math.Max(0, ActualHeight - 2);
        }
    }

    /// <summary>
    /// レンダリングサイズ変更時に表示を更新する。
    /// </summary>
    /// <param name="sizeInfo">サイズ変更情報。</param>
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        UpdateDisplay();
    }

    private bool _capture;

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _capture = true;
        CaptureMouse();
        SetVolumeFromMouse(e.GetPosition(this).X);
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_capture)
        {
            _capture = false;
            ReleaseMouseCapture();
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && _capture)
            SetVolumeFromMouse(e.GetPosition(this).X);
    }

    private void SetVolumeFromMouse(double x)
    {
        var w = ActualWidth;
        if (w <= 0)
            return;
        var dbVolume = (1f - (float)(x / w)) * MinDb;
        Volume = x <= 0 ? 0f : (float)Math.Pow(10, dbVolume / 20f);
    }
}
