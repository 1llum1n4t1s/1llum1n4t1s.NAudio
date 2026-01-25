using System;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using NAudio.Wave;
using System.IO;
using NAudio.Utils;
using NAudioTests.Utils;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// WaveFileWriter の Write/Flush/CreateWaveFile/WriteSample/大ファイルのテスト。
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class WaveFileWriterTests
    {
        /// <summary>
        /// Write で書き込んだデータを Reader で同一に読めることを確認する。
        /// </summary>
        [Test]
        public void ReaderShouldReadBackSameDataWrittenWithWrite()
        {
            var ms = new MemoryStream();
            var testSequence = new byte[] { 0x1, 0x2, 0xFF, 0xFE };
            using (var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), new WaveFormat(16000, 24, 1)))
            {
                writer.Write(testSequence, 0, testSequence.Length);
            }
            // check the Reader can read it
            ms.Position = 0;
            using (var reader = new WaveFileReader(ms))
            {
                ClassicAssert.AreEqual(16000, reader.WaveFormat.SampleRate, "Sample Rate");
                ClassicAssert.AreEqual(24, reader.WaveFormat.BitsPerSample, "Bits Per Sample");
                ClassicAssert.AreEqual(1, reader.WaveFormat.Channels, "Channels");
                ClassicAssert.AreEqual(testSequence.Length, reader.Length, "File Length");
                var buffer = new byte[600]; // 24 bit audio, block align is 3
                var read = reader.Read(buffer, 0, buffer.Length);
                ClassicAssert.AreEqual(testSequence.Length, read, "Data Length");
                for (var n = 0; n < read; n++)
                {
                    ClassicAssert.AreEqual(testSequence[n], buffer[n], "Byte " + n);
                }
            }
        }


        /// <summary>
        /// Dispose 前に Flush すればヘッダが更新されることを確認する。
        /// </summary>
        [Test]
        public void FlushUpdatesHeaderEvenIfDisposeNotCalled()
        {
            var ms = new MemoryStream();
            var testSequence = new byte[] { 0x1, 0x2, 0xFF, 0xFE };
            var testSequence2 = new byte[] { 0x3, 0x4, 0x5 };
            var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), new WaveFormat(16000, 24, 1));
            writer.Write(testSequence, 0, testSequence.Length);
            writer.Flush();
            // BUT NOT DISPOSED
            // another write that was not flushed
            writer.Write(testSequence2, 0, testSequence2.Length);
            
            // check the Reader can read it
            ms.Position = 0;
            using (var reader = new WaveFileReader(ms))
            {
                ClassicAssert.AreEqual(16000, reader.WaveFormat.SampleRate, "Sample Rate");
                ClassicAssert.AreEqual(24, reader.WaveFormat.BitsPerSample, "Bits Per Sample");
                ClassicAssert.AreEqual(1, reader.WaveFormat.Channels, "Channels");
                ClassicAssert.AreEqual(testSequence.Length, reader.Length, "File Length");
                var buffer = new byte[600]; // 24 bit audio, block align is 3
                var read = reader.Read(buffer, 0, buffer.Length);
                ClassicAssert.AreEqual(testSequence.Length, read, "Data Length");
                
                for (var n = 0; n < read; n++)
                {
                    ClassicAssert.AreEqual(testSequence[n], buffer[n], "Byte " + n);
                }
            }
            writer.Dispose(); // to stop the finalizer from moaning
        }


        /// <summary>
        /// CreateWaveFile で指定長のファイルができることを確認する。
        /// </summary>
        [Test]
        public void CreateWaveFileCreatesFileOfCorrectLength()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");
            try
            {
                long length = 4200;
                var waveFormat = new WaveFormat(8000, 8, 2);
                WaveFileWriter.CreateWaveFile(tempFile, new NullWaveStream(waveFormat, length));
                using (var reader = new WaveFileReader(tempFile))
                {
                    ClassicAssert.AreEqual(waveFormat, reader.WaveFormat, "WaveFormat");
                    ClassicAssert.AreEqual(length, reader.Length, "Length");
                    var buffer = new byte[length + 20];
                    var read = reader.Read(buffer, 0, buffer.Length);
                    ClassicAssert.AreEqual(length, read, "Read");
                }
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        /// <summary>
        /// 16bit ファイルに WriteSample で書き込めることを確認する。
        /// </summary>
        [Test]
        public void CanUseWriteSampleToA16BitFile()
        {
            var amplitude = 0.25f;
            float frequency = 1000;
            using (var writer = new WaveFileWriter(new MemoryStream(), new WaveFormat(16000, 16, 1)))
            {
                for (var n = 0; n < 1000; n++)
                {
                    var sample = (float)(amplitude * Math.Sin((2 * Math.PI * n * frequency) / writer.WaveFormat.SampleRate));
                    writer.WriteSample(sample);
                }
            }
        }

        /// <summary>
        /// 2GB 超の WAV ファイルを作成できることを確認する（Explicit）。
        /// </summary>
        [Test]
        [Explicit]
        public void CanCreateWaveFileGreaterThan2Gb()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                var dataLength = Int32.MaxValue + 1001L;
                WaveFileWriter.CreateWaveFile(tempFile, new NullWaveStream(new WaveFormat(44100,2), dataLength));
                ClassicAssert.AreEqual(dataLength + 46, new FileInfo(tempFile).Length);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        /// <summary>
        /// 4GB 超の WAV 作成が ArgumentException で失敗することを確認する（Explicit）。
        /// </summary>
        [Test]
        [Explicit]
        public void FailsToCreateWaveFileGreaterThan4Gb()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                var dataLength = UInt32.MaxValue - 10; // will be too big as not enough room for RIFF header, fmt chunk etc
                var ae = Assert.Throws<ArgumentException>(
                    () =>
                        WaveFileWriter.CreateWaveFile(tempFile, new NullWaveStream(new WaveFormat(44100, 2), dataLength)));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}
