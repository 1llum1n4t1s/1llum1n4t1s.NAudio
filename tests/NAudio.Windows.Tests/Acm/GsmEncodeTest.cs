using System;
using NUnit.Framework;
using NAudio.Wave;
using System.IO;
using NAudio.Wave.SampleProviders;

namespace NAudio.Windows.Tests.Acm;

[TestFixture]
public class GsmEncodeTest
{
    private const int DurationInSeconds = 5;

    private static WaveStream CreatePcmTestStream()
    {
        var outFormat = new WaveFormat(8000, 16, 1);
        var sg = new SignalGenerator(outFormat.SampleRate, outFormat.Channels)
        {
            Frequency = 1000,
            Gain = 0.25,
            Type = SignalGeneratorType.Sin
        };
        var sp = sg.ToWaveProvider16();


        byte[] data = new byte[outFormat.AverageBytesPerSecond * DurationInSeconds];
        var bytesRead = sp.Read(data.AsSpan());
        Assert.That(bytesRead, Is.EqualTo(data.Length));
        return new RawSourceWaveStream(new MemoryStream(data), outFormat);
    }

    [Test]
    public void CanEncodeAndDecodeGsm()
    {
        // Encode and decode in one test, in memory: the decode used to read a temp file written
        // by a separate encode test, so it was skipped whenever it ran first or alone.
        using var source = CreatePcmTestStream();
        var encoded = new MemoryStream();
        using (var gsm = new WaveFormatConversionStream(new Gsm610WaveFormat(), source))
        {
            WaveFileWriter.WriteWavFileToStream(encoded, gsm);
        }
        Assert.That(encoded.Length, Is.GreaterThan(0), "GSM encode produced no data");

        encoded.Position = 0;
        using var reader = new WaveFileReader(encoded);
        Assert.That(reader.WaveFormat.Encoding, Is.EqualTo(WaveFormatEncoding.Gsm610));
        using var pcm = WaveFormatConversionStream.CreatePcmStream(reader);
        Assert.That(pcm.WaveFormat.Encoding, Is.EqualTo(WaveFormatEncoding.Pcm));
        Assert.That(pcm.WaveFormat.SampleRate, Is.EqualTo(8000));
        Assert.That(pcm.WaveFormat.Channels, Is.EqualTo(1));

        var decoded = new MemoryStream();
        var buffer = new byte[pcm.WaveFormat.AverageBytesPerSecond];
        int read;
        long peak = 0;
        while ((read = pcm.Read(buffer, 0, buffer.Length)) > 0)
        {
            decoded.Write(buffer, 0, read);
            for (int n = 0; n + 1 < read; n += 2)
            {
                peak = Math.Max(peak, Math.Abs((long)BitConverter.ToInt16(buffer, n)));
            }
        }

        // GSM 06.10 codes 160-sample frames, so the round trip keeps the length to within a frame
        long expectedBytes = (long)source.WaveFormat.AverageBytesPerSecond * DurationInSeconds;
        const int frameBytes = 160 * 2;
        Assert.That(decoded.Length, Is.EqualTo(expectedBytes).Within(frameBytes), "Decoded length");
        Assert.That(peak, Is.GreaterThan(1000), "Decoded audio should not be silent");
    }
}
