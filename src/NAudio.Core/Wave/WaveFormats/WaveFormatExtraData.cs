using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave;

/// <summary>
/// This class used for marshalling from unmanaged code
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 2)]
public class WaveFormatExtraData : WaveFormat
{
    // try with 100 bytes for now, increase if necessary
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
    private readonly byte[] extraData = new byte[100];

    /// <summary>
    /// Allows the extra data to be read
    /// </summary>
    public byte[] ExtraData => extraData;

    /// <summary>
    /// parameterless constructor for marshalling
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
        if (extraDataLength > extraData.Length)
        {
            // The fmt chunk declares more extra bytes than our fixed buffer can hold.
            // Consume them so the stream stays aligned for the next chunk, then discard.
            Debug.WriteLine($"Discarding {extraDataLength} bytes of fmt extra data exceeding the {extraData.Length}-byte buffer");
            SkipBytes(reader, extraDataLength);
            extraSize = 0;
            return;
        }
        if (extraDataLength > 0)
        {
            ReadExactly(reader, extraData, extraDataLength);
            extraSize = (short)extraDataLength;
        }
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
