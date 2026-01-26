using System;
using System.Windows;
using System.Windows.Media;

namespace NAudio.Gui;

/// <summary>
/// 簡易ボリュームメーター WPF コントロール。
/// </summary>
public partial class VolumeMeter
{
    private float _amplitude;

    /// <summary>
    /// コンストラクター。
    /// </summary>
    public VolumeMeter()
    {
        InitializeComponent();
        MinDb = -60f;
        MaxDb = 18f;
        Amplitude = 0f;
        Orientation = System.Windows.Controls.Orientation.Vertical;
    }

    /// <summary>
    /// 現在の振幅値。
    /// </summary>
    public float Amplitude
    {
        get => _amplitude;
        set
        {
            _amplitude = value;
            UpdateMeter();
        }
    }

    /// <summary>
    /// 最小 dB。
    /// </summary>
    public float MinDb { get; set; }

    /// <summary>
    /// 最大 dB。
    /// </summary>
    public float MaxDb { get; set; }

    /// <summary>
    /// メーターの向き。
    /// </summary>
    public System.Windows.Controls.Orientation Orientation { get; set; }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        UpdateMeter();
    }

    private void UpdateMeter()
    {
        var db = 20.0 * Math.Log10(_amplitude <= 0 ? 1e-6 : _amplitude);
        db = Math.Clamp(db, MinDb, MaxDb);
        var percent = (db - MinDb) / (MaxDb - MinDb);
        var w = ActualWidth - 2;
        var h = ActualHeight - 2;
        if (Orientation == System.Windows.Controls.Orientation.Horizontal)
        {
            MeterFill.Width = Math.Max(0, w * percent);
            MeterFill.Height = Math.Max(0, h);
            MeterFill.VerticalAlignment = VerticalAlignment.Stretch;
            MeterFill.HorizontalAlignment = HorizontalAlignment.Left;
        }
        else
        {
            MeterFill.Height = Math.Max(0, h * percent);
            MeterFill.Width = Math.Max(0, w);
            MeterFill.VerticalAlignment = VerticalAlignment.Bottom;
            MeterFill.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
    }
}
