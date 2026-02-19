using System;
using NAudio.Wave;

namespace MarkHeath.AudioUtils;

/// <summary>
/// ミックスダウン用のオフセット・ボリューム付き Wave ストリーム。
/// </summary>
public class MixDiffStream : WaveStream
{
    private WaveOffsetStream offsetStream;
    private WaveChannel32 channelSteam;
    private bool muted;
    private float volume;

    /// <summary>
    /// 指定ファイルで MixDiffStream を初期化する。
    /// </summary>
    /// <param name="fileName">WAV ファイルパス。</param>
    public MixDiffStream(string fileName)
    {
        WaveFileReader reader = null;
        try
        {
            reader = new WaveFileReader(fileName);
            offsetStream = new WaveOffsetStream(reader);
            channelSteam = new WaveChannel32(offsetStream);
            muted = false;
            volume = 1.0f;
        }
        catch
        {
            channelSteam?.Dispose();
            offsetStream?.Dispose();
            reader?.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public override int BlockAlign
    {
        get => channelSteam.BlockAlign;
    }

    /// <inheritdoc />
    public override WaveFormat WaveFormat
    {
        get { return channelSteam.WaveFormat; }
    }

    /// <inheritdoc />
    public override long Length
    {
        get { return channelSteam.Length; }
    }

    /// <inheritdoc />
    public override long Position
    {
        get => channelSteam.Position;
        set => channelSteam.Position = value;
    }

    /// <summary>
    /// ミュートするかどうか。
    /// </summary>
    public bool Mute
    {
        get => muted;
        set
        {
            muted = value;
            if (muted)
                channelSteam.Volume = 0.0f;
            else
                Volume = Volume;
        }
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        return channelSteam.Read(buffer, offset, count);
    }

    /// <inheritdoc />
    public override bool HasData(int count)
    {
        return channelSteam.HasData(count);
    }

    /// <summary>
    /// 再生ボリューム (0.0〜1.0)。
    /// </summary>
    public float Volume
    {
        get => volume;
        set
        {
            volume = value;
            if (!Mute)
                channelSteam.Volume = volume;
        }
    }

    /// <summary>
    /// 再生開始までのプリディレイ。
    /// </summary>
    public TimeSpan PreDelay
    {
        get => offsetStream.StartTime;
        set => offsetStream.StartTime = value;
    }

    /// <summary>
    /// ソース内の再生オフセット。
    /// </summary>
    public TimeSpan Offset
    {
        get => offsetStream.SourceOffset;
        set => offsetStream.SourceOffset = value;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // channelSteam.Dispose() chains to offsetStream and WaveFileReader
            channelSteam?.Dispose();
        }
        base.Dispose(disposing);
    }
}
