using System;
using System.Collections.Generic;
using System.Numerics.Tensors;
using System.Runtime.InteropServices;
using NAudio.Utils;

namespace NAudio.Wave;

/// <summary>
/// WaveStream that can mix together multiple 32 bit input streams
/// (Normally used with stereo input channels)
/// All channels must have the same number of inputs
/// </summary>
public class WaveMixerStream32 : WaveStream
{
    private readonly List<WaveStream> inputStreams;
    private readonly object inputsLock;
    private WaveFormat waveFormat;
    private long length;
    private long position;
    private readonly int bytesPerSample;
    private byte[] readBuffer;

    /// <summary>
    /// Creates a new 32 bit WaveMixerStream
    /// </summary>
    public WaveMixerStream32()
    {
        AutoStop = true;
        waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        bytesPerSample = 4;
        inputStreams = new List<WaveStream>();
        inputsLock = new object();
    }

    /// <summary>
    /// Creates a new 32 bit WaveMixerStream
    /// </summary>
    /// <param name="inputStreams">An Array of WaveStreams - must all have the same format.
    /// Use WaveChannel is designed for this purpose.</param>
    /// <param name="autoStop">Automatically stop when all inputs have been read</param>
    /// <exception cref="ArgumentException">Thrown if the input streams are not 32 bit floating point,
    /// or if they have different formats to each other</exception>
    public WaveMixerStream32(IEnumerable<WaveStream> inputStreams, bool autoStop)
        : this()
    {
        AutoStop = autoStop;

        foreach (var inputStream in inputStreams)
        {
            AddInputStream(inputStream);
        }
    }

    /// <summary>
    /// Add a new input to the mixer
    /// </summary>
    /// <param name="waveStream">The wave input to add</param>
    public void AddInputStream(WaveStream waveStream)
    {
        ArgumentNullException.ThrowIfNull(waveStream);
        var inputFormat = waveStream.WaveFormat;
        if (inputFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            throw new ArgumentException("Must be IEEE floating point", "waveStream");
        if (inputFormat.BitsPerSample != 32)
            throw new ArgumentException("Only 32 bit audio currently supported", "waveStream");

        lock (inputsLock)
        {
            if (inputStreams.Count == 0)
            {
                // first one - set the format
                waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(inputFormat.SampleRate, inputFormat.Channels);
            }
            else if (!inputFormat.Equals(waveFormat))
            {
                throw new ArgumentException("All incoming channels must have the same format", "waveStream");
            }

            inputStreams.Add(waveStream);
            length = Math.Max(length, waveStream.Length);
            // get to the right point in this input file
            waveStream.Position = position;
        }
    }

    /// <summary>
    /// Remove a WaveStream from the mixer
    /// </summary>
    /// <param name="waveStream">waveStream to remove</param>
    public void RemoveInputStream(WaveStream waveStream)
    {
        lock (inputsLock)
        {
            if (inputStreams.Remove(waveStream))
            {
                // recalculate the length
                long newLength = 0;
                foreach (var inputStream in inputStreams)
                {
                    newLength = Math.Max(newLength, inputStream.Length);
                }
                length = newLength;
                position = Math.Min(position, length);
            }
        }
    }

    /// <summary>
    /// The number of inputs to this mixer
    /// </summary>
    public int InputCount
    {
        get
        {
            lock (inputsLock)
            {
                return inputStreams.Count;
            }
        }
    }

    /// <summary>
    /// Automatically stop when all inputs have been read
    /// </summary>
    public bool AutoStop { get; set; }

    /// <summary>
    /// Reads bytes from this wave stream
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if an invalid number of bytes requested</exception>
    public override int Read(Span<byte> buffer)
    {
        int count = buffer.Length;
        if (AutoStop)
        {
            long remaining = length - position;
            if (remaining <= 0)
                return 0;
            if (count > remaining)
                count = (int)remaining;
        }


        if (count % bytesPerSample != 0)
            throw new ArgumentException("Must read an whole number of samples", nameof(buffer));

        // blank the buffer
        var dest = buffer.Slice(0, count);
        dest.Clear();
        int bytesRead = 0;

        // sum the channels in
        readBuffer = BufferHelpers.Ensure(readBuffer, count);
        WaveStream[] inputSnapshot;
        lock (inputsLock)
        {
            inputSnapshot = inputStreams.ToArray();
        }
        foreach (var inputStream in inputSnapshot)
        {
            if (inputStream.HasData(count))
            {
                int readFromThisStream = inputStream.Read(readBuffer, 0, count);
                // don't worry if input stream returns less than we requested - may indicate we have got to the end
                bytesRead = Math.Max(bytesRead, readFromThisStream);
                if (readFromThisStream > 0)
                    Sum32BitAudio(dest, readBuffer.AsSpan(0, readFromThisStream));
            }
            else
            {
                bytesRead = Math.Max(bytesRead, count);
                inputStream.Position += count;
            }
        }
        position += count;
        return count;
    }

    /// <summary>
    /// Reads bytes from this wave stream
    /// </summary>
    public override int Read(byte[] buffer, int offset, int count)
        => Read(buffer.AsSpan(offset, count));

    /// <summary>
    /// Actually performs the mixing
    /// </summary>
    private static void Sum32BitAudio(Span<byte> destBuffer, ReadOnlySpan<byte> sourceBuffer)
    {
        var dest = MemoryMarshal.Cast<byte, float>(destBuffer);
        var source = MemoryMarshal.Cast<byte, float>(sourceBuffer);
        // The source slice may be shorter than dest (last read underflowed); restrict to what we have.
        var destSlice = dest.Slice(0, source.Length);
        TensorPrimitives.Add(destSlice, source, destSlice);
    }

    /// <summary>
    /// <see cref="WaveStream.BlockAlign"/>
    /// </summary>
    public override int BlockAlign => waveFormat.BlockAlign;

    /// <summary>
    /// Length of this Wave Stream (in bytes)
    /// <see cref="System.IO.Stream.Length"/>
    /// </summary>
    public override long Length => length;

    /// <summary>
    /// Position within this Wave Stream (in bytes)
    /// <see cref="System.IO.Stream.Position"/>
    /// </summary>
    public override long Position
    {
        get
        {
            // all streams are at the same position
            return position;
        }
        set
        {
            lock (inputsLock)
            {
                value = Math.Min(value, Length);
                foreach (WaveStream inputStream in inputStreams)
                {
                    inputStream.Position = Math.Min(value, inputStream.Length);
                }
                position = value;
            }
        }
    }

    /// <summary>
    /// <see cref="WaveStream.WaveFormat"/>
    /// </summary>
    public override WaveFormat WaveFormat => waveFormat;

    /// <summary>
    /// Disposes this WaveStream
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (inputsLock)
            {
                foreach (WaveStream inputStream in inputStreams)
                {
                    inputStream.Dispose();
                }
            }
        }
        else
        {
            System.Diagnostics.Debug.Assert(false, "WaveMixerStream32 was not disposed");
        }
        base.Dispose(disposing);
    }
}
