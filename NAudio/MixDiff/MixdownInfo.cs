using System;
using NAudio.Utils;

namespace MarkHeath.AudioUtils;

/// <summary>
/// ミックスダウン 1 トラック分のファイル情報とストリーム。
/// </summary>
public class MixdownInfo
{
    private string fileName;
    private string letter;
    private MixDiffStream stream;
    private int offsetMilliseconds;
    private int delayMilliseconds;
    private int volumeDecibels;

    /// <summary>
    /// 指定ファイルで MixdownInfo を初期化する。
    /// </summary>
    /// <param name="fileName">WAV ファイルパス。</param>
    public MixdownInfo(string fileName)
    {
        this.fileName = fileName;
        stream = new MixDiffStream(fileName);
    }

    /// <summary>
    /// ファイル名。
    /// </summary>
    public string FileName => fileName;

    /// <summary>
    /// トラック表示用のラベル文字。
    /// </summary>
    public string Letter
    {
        get => letter;
        set => letter = value;
    }

    /// <summary>
    /// オーディオストリーム。
    /// </summary>
    public MixDiffStream Stream => stream;

    /// <summary>
    /// 再生開始オフセット（ミリ秒）。
    /// </summary>
    public int OffsetMilliseconds
    {
        get => offsetMilliseconds;
        set
        {
            offsetMilliseconds = value;
            stream.Offset = TimeSpan.FromMilliseconds(offsetMilliseconds);
        }
    }

    /// <summary>
    /// プリディレイ（ミリ秒）。
    /// </summary>
    public int DelayMilliseconds
    {
        get => delayMilliseconds;
        set
        {
            delayMilliseconds = value;
            stream.PreDelay = TimeSpan.FromMilliseconds(delayMilliseconds);
        }
    }

    /// <summary>
    /// ボリューム（dB）。
    /// </summary>
    public int VolumeDecibels
    {
        get => volumeDecibels;
        set
        {
            volumeDecibels = value;
            stream.Volume = (float)Decibels.DecibelsToLinear(volumeDecibels);
        }
    }
}
