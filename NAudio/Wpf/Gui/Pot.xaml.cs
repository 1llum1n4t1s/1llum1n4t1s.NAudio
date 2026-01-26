using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NAudio.Gui;

/// <summary>
/// ポテンショメーター風 WPF コントロール。
/// </summary>
public partial class Pot
{
    private double _minimum = 0.0;
    private double _maximum = 1.0;
    private double _value = 0.5;
    private bool _dragging;
    private double _dragStartY;
    private double _dragStartValue;

    /// <summary>
    /// 値変更イベント。
    /// </summary>
    public event EventHandler ValueChanged;

    /// <summary>
    /// コンストラクター。
    /// </summary>
    public Pot()
    {
        InitializeComponent();
        Loaded += (_, _) => Redraw();
        SizeChanged += (_, _) => Redraw();
    }

    /// <summary>
    /// 最小値。
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double Minimum
    {
        get => _minimum;
        set
        {
            if (value >= _maximum)
                throw new ArgumentOutOfRangeException(nameof(value), "Minimum must be less than maximum");
            _minimum = value;
            if (Value < _minimum)
                Value = _minimum;
        }
    }

    /// <summary>
    /// 最大値。
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double Maximum
    {
        get => _maximum;
        set
        {
            if (value <= _minimum)
                throw new ArgumentOutOfRangeException(nameof(value), "Maximum must be greater than minimum");
            _maximum = value;
            if (Value > _maximum)
                Value = _maximum;
        }
    }

    /// <summary>
    /// 現在値。
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double Value
    {
        get => _value;
        set => SetValue(value, false);
    }

    private void SetValue(double newValue, bool raiseEvents)
    {
        if (Math.Abs(_value - newValue) < 1e-9)
            return;
        _value = newValue;
        if (raiseEvents)
            ValueChanged?.Invoke(this, EventArgs.Empty);
        Redraw();
    }

    private void Redraw()
    {
        var w = ActualWidth;
        var h = ActualHeight;
        if (w <= 0 || h <= 0)
            return;
        var diameter = Math.Min(w - 4, h - 4);
        var cx = w / 2.0;
        var cy = h / 2.0;
        var r = diameter / 2.0;
        var startAngle = 135.0;
        var sweepAngle = 270.0;
        var startRad = startAngle * Math.PI / 180.0;
        var sweepRad = sweepAngle * Math.PI / 180.0;
        var pathFigure = new PathFigure
        {
            StartPoint = new Point(cx + r * Math.Cos(startRad), cy + r * Math.Sin(startRad))
        };
        pathFigure.Segments.Add(new ArcSegment
        {
            Point = new Point(cx + r * Math.Cos(startRad + sweepRad), cy + r * Math.Sin(startRad + sweepRad)),
            Size = new Size(r, r),
            IsLargeArc = false,
            SweepDirection = SweepDirection.Clockwise
        });
        ArcPath.Data = new PathGeometry { Figures = { pathFigure } };
        var percent = (_value - _minimum) / (_maximum - _minimum);
        var degrees = 135 + (percent * 270);
        var rad = degrees * Math.PI / 180.0;
        var x = r * Math.Cos(rad);
        var y = r * Math.Sin(rad);
        KnobLine.X1 = cx;
        KnobLine.Y1 = cy;
        KnobLine.X2 = cx + x;
        KnobLine.Y2 = cy + y;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragging = true;
        _dragStartY = e.GetPosition(this).Y;
        _dragStartValue = _value;
        CaptureMouse();
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
        var yDiff = _dragStartY - e.GetPosition(this).Y;
        var delta = (_maximum - _minimum) * (yDiff / 150.0);
        var newValue = Math.Clamp(_dragStartValue + delta, _minimum, _maximum);
        SetValue(newValue, true);
    }
}
