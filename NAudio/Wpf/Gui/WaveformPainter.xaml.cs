using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NAudio.Gui;

/// <summary>
/// 波形描画用 WPF コントロール。
/// </summary>
public partial class WaveformPainter
{
    private readonly List<float> _samples = new(1000);
    private int _maxSamples;
    private int _insertPos;

    /// <summary>
    /// コンストラクター。
    /// </summary>
    public WaveformPainter()
    {
        InitializeComponent();
        SizeChanged += (_, _) =>
        {
            _maxSamples = (int)ActualWidth;
            if (_maxSamples < 0) _maxSamples = 0;
        };
    }

    /// <summary>
    /// 最大サンプル値を追加する。
    /// </summary>
    /// <param name="maxSample">最大サンプル値。</param>
    public void AddMax(float maxSample)
    {
        if (_maxSamples <= 0)
            return;
        if (_samples.Count <= _maxSamples)
            _samples.Add(maxSample);
        else if (_insertPos < _maxSamples)
            _samples[_insertPos] = maxSample;
        _insertPos = (_insertPos + 1) % _maxSamples;
        Redraw();
    }

    private float GetSample(int index)
    {
        if (index < 0)
            index += _maxSamples;
        if (index >= 0 && index < _samples.Count)
            return _samples[index];
        return 0f;
    }

    private void Redraw()
    {
        WaveCanvas.Children.Clear();
        var w = (int)ActualWidth;
        var h = ActualHeight;
        if (w <= 0 || h <= 0)
            return;
        var brush = Foreground?.Clone() ?? Brushes.Black;
        for (var x = 0; x < w; x++)
        {
            var lineHeight = h * GetSample(x - w + _insertPos);
            var y1 = (h - lineHeight) / 2;
            var line = new Line
            {
                X1 = x,
                Y1 = y1,
                X2 = x,
                Y2 = y1 + lineHeight,
                Stroke = brush
            };
            WaveCanvas.Children.Add(line);
        }
    }
}
