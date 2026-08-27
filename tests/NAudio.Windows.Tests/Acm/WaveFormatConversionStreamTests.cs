using System;
using NUnit.Framework;
using NAudio.Wave;
using NAudio.Wave.Compression;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NAudio.Tests.Shared;

namespace NAudio.Windows.Tests.Acm;

[TestFixture]
[Category("IntegrationTest")]
public class WaveFormatConversionStreamTests
{
    [Test]
    public void CanConvertPcmToMuLaw()
    {
        int channels = 1;
        int sampleRate = 8000;
        CanCreateConversionStream(
            new WaveFormat(sampleRate, 16, channels),
            WaveFormat.CreateCustomFormat(WaveFormatEncoding.MuLaw, sampleRate, channels, sampleRate * channels, 1, 8));
    }

    [Test]
    public void CanConvertPcmToALaw()
    {
        int channels = 1;
        int sampleRate = 8000;
        CanCreateConversionStream(
            new WaveFormat(sampleRate, 16, channels),
            WaveFormat.CreateCustomFormat(WaveFormatEncoding.ALaw, sampleRate, channels, sampleRate * channels, 1, 8));
    }

    /* Windows does not provide an ACM MP3 encoder, but this test could be run
     * if you install a different ACM MP3 encoder to see if the MP3 Wave Format
     * NAudio creates is sufficient (possibly it will have its own custom metadata
     * in the WaveFormat extra byts).
    [Test]
    public void CanConvertPcmToMp3()
    {
        int channels = 2;
        int sampleRate = 44100;
        CanCreateConversionStream(
            new WaveFormat(sampleRate, 16, channels),
            new Mp3WaveFormat(sampleRate, channels, 0, 128000/8)); 
    }*/

    [Test]
    public void CanConvertALawToPcm()
    {
        int channels = 1;
        int sampleRate = 8000;
        CanCreateConversionStream(
            WaveFormat.CreateCustomFormat(WaveFormatEncoding.ALaw, sampleRate, channels, sampleRate * channels, 1, 8),
            new WaveFormat(sampleRate, 16, channels));
    }

    [Test]
    public void CanConvertMuLawToPcm()
    {
        int channels = 1;
        int sampleRate = 8000;
        CanCreateConversionStream(
            WaveFormat.CreateCustomFormat(WaveFormatEncoding.MuLaw, sampleRate, channels, sampleRate * channels, 1, 8),
            new WaveFormat(sampleRate, 16, channels));
    }

    [Test]
    public void CanConvertAdpcmToPcm()
    {
        int channels = 1;
        int sampleRate = 8000;
        CanCreateConversionStream(
            new AdpcmWaveFormat(8000, 1),
            new WaveFormat(sampleRate, 16, channels));
    }

    [Test]
    public void CanConvertAdpcmToSuggestedPcm()
    {
        using (WaveFormatConversionStream.CreatePcmStream(
            new NullWaveStream(new AdpcmWaveFormat(8000, 1), 1000)))
        {
        }
    }

    [Test]
    public void CanConvertALawToSuggestedPcm()
    {
        using (WaveFormatConversionStream.CreatePcmStream(
            new NullWaveStream(WaveFormat.CreateALawFormat(8000, 1), 1000)))
        {
        }
    }

    [Test]
    public void CanConvertMuLawToSuggestedPcm()
    {
        using (WaveFormatConversionStream.CreatePcmStream(
            new NullWaveStream(WaveFormat.CreateMuLawFormat(8000, 1), 1000)))
        {
        }
    }

    [Test]
    public void CanConvertPcmToAdpcm()
    {
        int channels = 1;
        int sampleRate = 8000;
        CanCreateConversionStream(
            new WaveFormat(sampleRate, 16, channels),
            new AdpcmWaveFormat(8000, 1));
    }

    [Test]
    public void CanConvertImeAdpcmToPcm()
    {
        AcmDriver driver = AcmDriver.FindByShortName("Microsoft IMA ADPCM");
        driver.Open();
        try
        {
            foreach (var format in driver.FormatTags
                .SelectMany(formatTag => driver.GetFormats(formatTag)
                .Where(format => format.FormatTag == WaveFormatEncoding.DviAdpcm ||
                                 format.FormatTag == WaveFormatEncoding.ImaAdpcm)))
            {
                // see if we can convert it to 16 bit PCM
                Debug.WriteLine(String.Format("Converting {0} to PCM", format.WaveFormat));
                CanCreateConversionStream(format.WaveFormat,
                    new WaveFormat(format.WaveFormat.SampleRate, 16, format.WaveFormat.Channels));
            }
        }
        finally
        {
            driver.Close();
        }
    }

    [Test]
    public void LengthPreservesValuesBeyondInt32Range()
    {
        var sourceFormat = WaveFormat.CreateALawFormat(8000, 1);
        var targetFormat = new WaveFormat(8000, 16, 1);
        const long sourceLength = 3L * 1024 * 1024 * 1024;
        using var inputStream = new NullWaveStream(sourceFormat, sourceLength);

        using var stream = new WaveFormatConversionStream(targetFormat, inputStream);

        Assert.That(stream.Length, Is.EqualTo(sourceLength * 2));
    }

    [Test]
    public void PositionPreservesValuesBeyondInt32Range()
    {
        var sourceFormat = WaveFormat.CreateALawFormat(8000, 1);
        var targetFormat = new WaveFormat(8000, 16, 1);
        const long sourceLength = 3L * 1024 * 1024 * 1024;
        const long targetPosition = 5L * 1024 * 1024 * 1024;
        using var inputStream = new NullWaveStream(sourceFormat, sourceLength);
        using var stream = new WaveFormatConversionStream(targetFormat, inputStream);

        stream.Position = targetPosition;

        Assert.Multiple(() =>
        {
            Assert.That(inputStream.Position, Is.EqualTo(targetPosition / 2));
            Assert.That(stream.Position, Is.EqualTo(targetPosition));
        });
    }

    [TestCase(false, 0, 1)]
    [TestCase(false, 8000, 0)]
    [TestCase(true, 0, 2)]
    [TestCase(true, 16000, 0)]
    public void RejectsFormatsThatCannotScalePositions(bool invalidTarget, int averageBytesPerSecond, int blockAlign)
    {
        var sourceFormat = WaveFormat.CreateALawFormat(8000, 1);
        var targetFormat = new WaveFormat(8000, 16, 1);
        var invalidFormat = WaveFormat.CreateCustomFormat(
            invalidTarget ? WaveFormatEncoding.Pcm : WaveFormatEncoding.ALaw,
            8000,
            1,
            averageBytesPerSecond,
            blockAlign,
            invalidTarget ? 16 : 8);
        if (invalidTarget)
            targetFormat = invalidFormat;
        else
            sourceFormat = invalidFormat;
        using var inputStream = new NullWaveStream(sourceFormat, 1000);

        Assert.Throws<InvalidDataException>(() =>
        {
            using var stream = new WaveFormatConversionStream(targetFormat, inputStream);
        });
    }

    private void CanCreateConversionStream(WaveFormat inputFormat, WaveFormat outputFormat)
    {
        var inputStream = new NullWaveStream(inputFormat, 10000);
        using var stream = new WaveFormatConversionStream(
            outputFormat, inputStream);
        byte[] buffer = new byte[stream.WaveFormat.AverageBytesPerSecond];
        int totalRead = 0;
        int bytesRead;
        do
        {
            bytesRead = stream.Read(buffer, 0, buffer.Length);
            totalRead += bytesRead;
        } while (bytesRead > 0);
        Debug.WriteLine(String.Format("Converted {0}", totalRead));
        Assert.That(inputStream.Position, Is.EqualTo(inputStream.Length));
    }
}

