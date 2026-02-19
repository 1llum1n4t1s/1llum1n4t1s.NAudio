using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Shapes;
using NAudio.Wave;

namespace NAudio.Gui;

/// <summary>
/// 波形表示用 WPF コントロール。
/// </summary>
public partial class WaveViewer
{
    private WaveStream _waveStream;
    private int _samplesPerPixel = 128;
    private long _startPosition;
    private int _bytesPerSample;

    /// <summary>
    /// コンストラクター。
    /// </summary>
    public WaveViewer()
    {
        InitializeComponent();
        Loaded += (_, _) => Redraw();
        SizeChanged += (_, _) => Redraw();
    }

    /// <summary>
    /// 関連付ける WaveStream。
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public WaveStream WaveStream
    {
        get => _waveStream;
        set
        {
            _waveStream = value;
            if (_waveStream != null)
                _bytesPerSample = (_waveStream.WaveFormat.BitsPerSample / 8) * _waveStream.WaveFormat.Channels;
            Redraw();
        }
    }

    /// <summary>
    /// ズームレベル（ピクセルあたりサンプル数）。
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SamplesPerPixel
    {
        get => _samplesPerPixel;
        set
        {
            _samplesPerPixel = value;
            Redraw();
        }
    }

    /// <summary>
    /// 開始位置（バイト）。
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public long StartPosition
    {
        get => _startPosition;
        set => _startPosition = value;
    }

    private void Redraw()
    {
        WaveCanvas.Children.Clear();
        if (_waveStream == null || ActualWidth <= 0 || ActualHeight <= 0)
            return;
        var waveData = new byte[_samplesPerPixel * _bytesPerSample];
        var width = (int)ActualWidth;
        var height = ActualHeight;
        for (var x = 0; x < width; x++)
        {
            _waveStream.Position = _startPosition + (x * _bytesPerSample * _samplesPerPixel);
            var bytesRead = _waveStream.Read(waveData, 0, waveData.Length);
            if (bytesRead == 0)
                break;
            short low = 0, high = 0;
            for (var n = 0; n < bytesRead; n += 2)
            {
                var sample = BinaryPrimitives.ReadInt16LittleEndian(waveData.AsSpan(n));
                if (sample < low) low = sample;
                if (sample > high) high = sample;
            }
            var lowPercent = (((float)low) - short.MinValue) / ushort.MaxValue;
            var highPercent = (((float)high) - short.MinValue) / ushort.MaxValue;
            var line = new Line
            {
                X1 = x,
                Y1 = (float)(height * lowPercent),
                X2 = x,
                Y2 = (float)(height * highPercent),
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            WaveCanvas.Children.Add(line);
        }
    }
}
