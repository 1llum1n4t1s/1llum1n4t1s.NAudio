using System.IO;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave;

/// <summary>
/// A WaveFormat that keeps the format-specific extra bytes (cbSize) it was read with, without
/// interpreting them. Reading a WAV fmt chunk produces one of these, and
/// <see cref="WaveFormat.MarshalFromPtr"/> falls back to it for an encoding NAudio has no
/// dedicated subclass for.
/// </summary>
public class WaveFormatExtraData : WaveFormat
{
    private const int CompatibilityBufferSize = 100;
    private byte[] extraData = new byte[CompatibilityBufferSize];

    /// <summary>
    /// The extra bytes that followed the WAVEFORMATEX header. The first
    /// <see cref="WaveFormat.ExtraSize"/> bytes contain the declared data; formats with less
    /// than 100 bytes retain the historical zero-filled buffer tail for compatibility.
    /// </summary>
    public byte[] ExtraData => extraData;

    /// <summary>
    /// Creates an empty instance, to be filled in by <see cref="WaveFormat.FromFormatChunk"/>
    /// </summary>
    internal WaveFormatExtraData()
    {
    }

    /// <summary>
    /// Reads this structure from a BinaryReader
    /// </summary>
    public WaveFormatExtraData(BinaryReader reader)
    {
        int formatChunkLength = reader.ReadInt32();
        int extraDataLength = ReadWaveFormat(reader, formatChunkLength);
        ReadExtraData(reader, extraDataLength);
    }

    internal void ReadExtraData(BinaryReader reader, int extraDataLength)
    {
        if (extraDataLength <= 0)
        {
            extraSize = 0;
            return;
        }

        if (extraDataLength > short.MaxValue)
        {
            // extraSize remains a signed short for compatibility with existing subclasses.
            // Consume an unrepresentable fmt payload so the following chunk stays aligned.
            Debug.WriteLine($"Discarding {extraDataLength} bytes of fmt extra data exceeding the supported {short.MaxValue}-byte maximum");
            SkipBytes(reader, extraDataLength);
            extraSize = 0;
            return;
        }

        if (extraDataLength > extraData.Length)
        {
            extraData = new byte[extraDataLength];
        }
        ReadExactly(reader, extraData, extraDataLength);
        extraSize = (short)extraDataLength;
    }

    private static void ReadExactly(BinaryReader reader, byte[] destination, int count)
    {
        int offset = 0;
        while (offset < count)
        {
            int read = reader.Read(destination, offset, count - offset);
            if (read == 0)
                throw new EndOfStreamException();
            offset += read;
        }
    }

    private static void SkipBytes(BinaryReader reader, int count)
    {
        var stream = reader.BaseStream;
        if (stream.CanSeek)
        {
            if (stream.Length - stream.Position < count)
                throw new EndOfStreamException();
            stream.Position += count;
            return;
        }

        var buffer = new byte[System.Math.Min(4096, count)];
        int remaining = count;
        while (remaining > 0)
        {
            int read = reader.Read(buffer, 0, System.Math.Min(buffer.Length, remaining));
            if (read == 0)
                throw new EndOfStreamException();
            remaining -= read;
        }
    }

    /// <summary>
    /// Writes this structure to a BinaryWriter
    /// </summary>
    public override void Serialize(BinaryWriter writer)
    {
        base.Serialize(writer);
        if (extraSize > 0)
        {
            writer.Write(extraData, 0, extraSize);
        }
    }
}
