using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Utils;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Core.Tests.WaveStreams;

[TestFixture]
public class WaveMixerStream32Tests
{
    private sealed class CallbackWaveStream : WaveStream
    {
        private readonly Action onFirstRead;
        private readonly WaveFormat waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        private long position;
        private bool callbackInvoked;

        public CallbackWaveStream(long length, Action onFirstRead)
        {
            Length = length;
            this.onFirstRead = onFirstRead;
        }

        public override WaveFormat WaveFormat => waveFormat;
        public override long Length { get; }
        public override long Position
        {
            get => position;
            set => position = Math.Min(value, Length);
        }

        public override int Read(Span<byte> buffer)
        {
            if (!callbackInvoked)
            {
                callbackInvoked = true;
                onFirstRead();
            }

            var count = (int)Math.Min(buffer.Length, Length - position);
            buffer.Slice(0, count).Clear();
            position += count;
            return count;
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            Read(buffer.AsSpan(offset, count));
    }

    private sealed class CoordinatedFormatWaveStream : WaveStream
    {
        private readonly WaveFormat waveFormat;
        private readonly Barrier firstFormatRead;
        private readonly Barrier emptyPathFormatRead;
        private int formatReadCount;
        private long position;

        public CoordinatedFormatWaveStream(
            WaveFormat waveFormat,
            Barrier firstFormatRead,
            Barrier emptyPathFormatRead)
        {
            this.waveFormat = waveFormat;
            this.firstFormatRead = firstFormatRead;
            this.emptyPathFormatRead = emptyPathFormatRead;
        }

        public override WaveFormat WaveFormat
        {
            get
            {
                int readCount = Interlocked.Increment(ref formatReadCount);
                if (readCount == 1 && !firstFormatRead.SignalAndWait(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("Timed out coordinating the first format read.");
                if (readCount == 4 && !emptyPathFormatRead.SignalAndWait(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("Timed out coordinating the empty-input path.");
                return waveFormat;
            }
        }

        public override long Length => 8;

        public override long Position
        {
            get => position;
            set => position = value;
        }

        public override int Read(Span<byte> buffer) => 0;
        public override int Read(byte[] buffer, int offset, int count) => 0;
    }

    /// <summary>
    /// Build an in-memory IEEE-float stereo WAV filled with a constant sample value,
    /// then wrap it in a WaveFileReader so it can be fed straight into the mixer.
    /// </summary>
    private static WaveFileReader CreateConstantFloatStream(float sampleValue, int frames, int sampleRate = 44100)
    {
        var ms = new MemoryStream();
        using (var writer = new WaveFileWriter(new IgnoreDisposeStream(ms),
                   WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2)))
        {
            for (int i = 0; i < frames; i++)
            {
                writer.WriteSample(sampleValue);
                writer.WriteSample(sampleValue);
            }
        }
        return new WaveFileReader(new MemoryStream(ms.ToArray()));
    }

    [Test]
    public void MixingThreeConstantStreams_ProducesArithmeticSumPerSample()
    {
        const int frames = 512;
        using var mixer = new WaveMixerStream32 { AutoStop = true };
        mixer.AddInputStream(CreateConstantFloatStream(0.1f, frames));
        mixer.AddInputStream(CreateConstantFloatStream(0.2f, frames));
        mixer.AddInputStream(CreateConstantFloatStream(0.3f, frames));

        var buffer = new byte[(int)mixer.Length];
        int read = mixer.Read(buffer, 0, buffer.Length);
        Assert.That(read, Is.EqualTo(buffer.Length));

        var floats = MemoryMarshal.Cast<byte, float>(buffer.AsSpan()).ToArray();
        const float expected = 0.1f + 0.2f + 0.3f;
        foreach (var f in floats)
        {
            Assert.That(f, Is.EqualTo(expected).Within(1e-6f));
        }
    }

    [Test]
    public void ReadWithCountNotMultipleOfBytesPerSample_Throws()
    {
        using var mixer = new WaveMixerStream32 { AutoStop = false };
        mixer.AddInputStream(CreateConstantFloatStream(0.1f, 512));

        // bytesPerSample is 4 for IEEE float — 7 is deliberately misaligned.
        var buffer = new byte[8];
        Assert.Throws<ArgumentException>(() => { _ = mixer.Read(buffer, 0, 7); });
    }

    [Test]
    public void InputCanRemoveItselfDuringRead()
    {
        using var mixer = new WaveMixerStream32 { AutoStop = false };
        CallbackWaveStream input = null;
        using (input = new CallbackWaveStream(8, () => mixer.RemoveInputStream(input)))
        {
            mixer.AddInputStream(input);
            var buffer = new byte[8];

            Assert.That(mixer.Read(buffer, 0, buffer.Length), Is.EqualTo(buffer.Length));
            Assert.That(mixer.InputCount, Is.Zero);
        }
    }

    [Test]
    public void RemovingLongestInputClampsPositionBeforeAutoStopRead()
    {
        using var mixer = new WaveMixerStream32 { AutoStop = true };
        using var shortInput = CreateConstantFloatStream(0.1f, 2);
        using var longInput = CreateConstantFloatStream(0.2f, 8);
        mixer.AddInputStream(shortInput);
        mixer.AddInputStream(longInput);

        var buffer = new byte[32];
        Assert.That(mixer.Read(buffer, 0, buffer.Length), Is.EqualTo(buffer.Length));
        mixer.RemoveInputStream(longInput);

        Assert.That(mixer.Position, Is.EqualTo(mixer.Length));
        Assert.That(mixer.Read(buffer, 0, buffer.Length), Is.Zero);
    }

    [Test]
    public void ConcurrentFirstInputsWithDifferentFormatsAcceptOnlyOne()
    {
        using var firstFormatRead = new Barrier(2);
        using var emptyPathFormatRead = new Barrier(2);
        using var mixer = new WaveMixerStream32();
        using var first = new CoordinatedFormatWaveStream(
            WaveFormat.CreateIeeeFloatWaveFormat(44100, 2),
            firstFormatRead,
            emptyPathFormatRead);
        using var second = new CoordinatedFormatWaveStream(
            WaveFormat.CreateIeeeFloatWaveFormat(48000, 2),
            firstFormatRead,
            emptyPathFormatRead);
        var errors = new ConcurrentQueue<Exception>();

        Task.WaitAll(
            Task.Run(() => TryAdd(mixer, first, errors)),
            Task.Run(() => TryAdd(mixer, second, errors)));

        Assert.Multiple(() =>
        {
            Assert.That(mixer.InputCount, Is.EqualTo(1));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors.TryPeek(out var error) ? error : null, Is.TypeOf<ArgumentException>());
        });
    }

    private static void TryAdd(
        WaveMixerStream32 mixer,
        WaveStream input,
        ConcurrentQueue<Exception> errors)
    {
        try
        {
            mixer.AddInputStream(input);
        }
        catch (Exception ex)
        {
            errors.Enqueue(ex);
        }
    }

}
