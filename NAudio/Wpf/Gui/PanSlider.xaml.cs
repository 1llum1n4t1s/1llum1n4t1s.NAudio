using System;
using System.Windows;
using System.Windows.Input;

namespace NAudio.Gui;

/// <summary>
/// パンスライダー WPF コントロール。
/// </summary>
public partial class PanSlider
{
    private float _pan;
    private bool _capture;

    /// <summary>
    /// パン変更イベント。
    /// </summary>
    public event EventHandler PanChanged;

    /// <summary>
    /// コンストラクター。
    /// </summary>
    public PanSlider()
    {
        InitializeComponent();
        UpdateDisplay();
    }

    /// <summary>
    /// パン (-1.0=左 〜 1.0=右)。
    /// </summary>
    public float Pan
    {
        get => _pan;
        set
        {
            var v = Math.Clamp(value, -1f, 1f);
            if (Math.Abs(_pan - v) < 1e-6)
                return;
            _pan = v;
            PanChanged?.Invoke(this, EventArgs.Empty);
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
            return;
        var half = ActualWidth / 2.0;
        var top = 1.0;
        var h = Math.Max(0, ActualHeight - 2);
        if (Math.Abs(_pan) < 0.001f)
        {
            System.Windows.Controls.Canvas.SetLeft(PanFill, half - 1.5);
            System.Windows.Controls.Canvas.SetTop(PanFill, top);
            PanFill.Width = 3;
            PanFill.Height = h;
            PanText.Text = "C";
        }
        else if (_pan > 0)
        {
            System.Windows.Controls.Canvas.SetLeft(PanFill, half);
            System.Windows.Controls.Canvas.SetTop(PanFill, top);
            PanFill.Width = Math.Max(1, half * _pan);
            PanFill.Height = h;
            PanText.Text = $"{_pan * 100:F0}%R";
        }
        else
        {
            System.Windows.Controls.Canvas.SetLeft(PanFill, half * (1 + _pan));
            System.Windows.Controls.Canvas.SetTop(PanFill, top);
            PanFill.Width = Math.Max(1, half * -_pan);
            PanFill.Height = h;
            PanText.Text = $"{_pan * -100:F0}%L";
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

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _capture = true;
        CaptureMouse();
        SetPanFromMouse(e.GetPosition(this).X);
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
            SetPanFromMouse(e.GetPosition(this).X);
    }

    private void SetPanFromMouse(double x)
    {
        if (ActualWidth <= 0)
            return;
        Pan = (float)((x / ActualWidth) * 2.0 - 1.0);
    }
}
