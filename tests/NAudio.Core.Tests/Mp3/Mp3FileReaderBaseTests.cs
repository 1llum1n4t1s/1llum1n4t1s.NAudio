using System.IO;
using NAudio.Wave;
using NAudio.Core.Tests.Utils;
using NUnit.Framework;

namespace NAudio.Core.Tests.Mp3;

[TestFixture]
public class Mp3FileReaderBaseTests
{
    [Test]
    [Category("UnitTest")]
    public void DisposesFileOnFailToParse()
    {
        // If File.Delete here fails with a sharing violation, the ctor failed to release
        // the file handle on its parsing-error path (see Mp3FileReaderBase ctor catch block).
        string tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, "Some test content");
        try
        {
            Assert.Throws<InvalidDataException>(() =>
                new Mp3FileReaderBase(tempFilePath, fmt => new FakeMp3FrameDecompressor(fmt)));
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    [Test]
    [Category("UnitTest")]
    public void CopesWithZeroLengthStream()
    {
        var ms = new MemoryStream(new byte[0]);
        Assert.Throws<InvalidDataException>(() =>
            new Mp3FileReaderBase(ms, fmt => new FakeMp3FrameDecompressor(fmt)));
    }

    [Test]
    [Category("UnitTest")]
    public void OpensSingleFrameMp3ShorterThanId3v1Tag()
    {
        byte[] frame = new byte[26];
        frame[0] = 0xFF;
        frame[1] = 0xF3;
        frame[2] = 0x10;
        frame[3] = 0x00;

        using var reader = new Mp3FileReaderBase(
            new MemoryStream(frame), fmt => new FakeMp3FrameDecompressor(fmt));

        Assert.That(reader.Mp3WaveFormat.SampleRate, Is.EqualTo(22050));
    }
}
