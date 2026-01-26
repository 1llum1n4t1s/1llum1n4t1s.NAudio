using System;
using System.ComponentModel;
using System.Windows.Input;

namespace NAudio.Gui;

/// <summary>
/// フェーダー WPF コントロール。
/// </summary>
public partial class Fader
{
    private const int SliderHeight = 30;
    private const int SliderWidth = 15;
    private int _minimum;
    private int _maximum = 100;
    private float _percent;
    private bool _dragging;
    private double _dragOffset;

    /// <summary>
    /// コンストラクター。
    /// </summary>
    public Fader()
    {
        InitializeComponent();
        Loaded += (_, _) => Redraw();
        SizeChanged += (_, _) => Redraw();
    }

    /// <summary>
    /// 最小値。
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Minimum
    {
        get => _minimum;
        set => _minimum = value;
    }

    /// <summary>
    /// 最大値。
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Maximum
    {
        get => _maximum;
        set => _maximum = value;
    }

    /// <summary>
    /// 現在値。
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => (int)(_percent * (_maximum - _minimum)) + _minimum;
        set => _percent = (_maximum - _minimum) != 0 ? (float)(value - _minimum) / (_maximum - _minimum) : 0f;
    }

    /// <summary>
    /// 向き（未使用・縦のみ）。
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Orientation { get; set; }

    private void Redraw()
    {
        var w = ActualWidth;
        var h = ActualHeight;
        if (w <= 0 || h <= SliderHeight)
            return;
        var cx = w / 2;
        var trackH = h - SliderHeight;
        Groove.X1 = cx;
        Groove.Y1 = SliderHeight / 2.0;
        Groove.X2 = cx;
        Groove.Y2 = h - SliderHeight / 2.0;
        var thumbY = (h - SliderHeight) * _percent;
        System.Windows.Controls.Canvas.SetLeft(SliderRect, (w - SliderWidth) / 2);
        System.Windows.Controls.Canvas.SetTop(SliderRect, thumbY);
        SliderRect.Width = SliderWidth;
        SliderRect.Height = SliderHeight;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var p = e.GetPosition(FaderCanvas);
        var left = (ActualWidth - SliderWidth) / 2;
        var top = (ActualHeight - SliderHeight) * _percent;
        if (p.X >= left && p.X <= left + SliderWidth && p.Y >= top && p.Y <= top + SliderHeight)
        {
            _dragging = true;
            _dragOffset = p.Y - top;
            CaptureMouse();
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragging)
        {
            _dragging = false;
            ReleaseMouseCapture();
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging)
            return;
        var p = e.GetPosition(FaderCanvas).Y - _dragOffset;
        var trackH = ActualHeight - SliderHeight;
        if (trackH <= 0)
            return;
        _percent = (float)Math.Clamp(p / trackH, 0, 1);
        Redraw();
    }
}
