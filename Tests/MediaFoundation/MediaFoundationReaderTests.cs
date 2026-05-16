using System;
using System.Diagnostics;
using System.IO;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NAudioTests.MediaFoundation
{
    /// <summary>
    /// MediaFoundationReader で AAC 等を読み取るテスト。
    /// </summary>
    [TestFixture]
    [Category("IntegrationTest")]
    public class MediaFoundationReaderTests
    {
        /// <summary>
        /// AAC ファイルを読み取れることを確認する。
        /// 環境変数 NAUDIO_TEST_AAC でファイルパスを指定。未設定なら Ignore。
        /// </summary>
        [Test]
        public void CanReadAnAac()
        {
            var testFile = Environment.GetEnvironmentVariable("NAUDIO_TEST_AAC");
            if (string.IsNullOrEmpty(testFile) || !File.Exists(testFile))
                ClassicAssert.Ignore("Set NAUDIO_TEST_AAC environment variable to point to a .aac file");
            var reader = new MediaFoundationReader(testFile);
            Console.WriteLine(reader.WaveFormat);
            var buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
            int bytesRead;
            long total = 0;
            while((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                total += bytesRead;
            }
            ClassicAssert.IsTrue(total > 0);
        }
    }

    /// <summary>
    /// MediaFoundation によるエンコードのテスト。
    /// </summary>
    [TestFixture]
    [Category("IntegrationTest")]
    public class MediaFoundationEncoderTests
    {
        /// <summary>
        /// 大きい GSM610 WAV を MP3 にエンコードできることを確認する。
        /// </summary>
        [Test]
        public void CanEncodeLargeGSM610FileToMp3()
        {
            // 環境依存テスト: 環境変数 NAUDIO_TEST_GSM610_WAV で入力ファイルを指定。
            // 出力は Path.GetTempPath() 配下に書く。
            var fileInPath = Environment.GetEnvironmentVariable("NAUDIO_TEST_GSM610_WAV");
            if (string.IsNullOrEmpty(fileInPath) || !File.Exists(fileInPath))
                ClassicAssert.Ignore("Set NAUDIO_TEST_GSM610_WAV environment variable to point to a GSM610 .wav file");
            var fileOutPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(fileInPath) + ".mp3");
            var sw = Stopwatch.StartNew();
            using (var wavToConvert = new WaveFileReader(fileInPath))
            using (var converter = WaveFormatConversionStream.CreatePcmStream(wavToConvert))
            {
                Console.WriteLine($"Format in = {wavToConvert.WaveFormat}, Sample rate {wavToConvert.WaveFormat.SampleRate}");
                Console.WriteLine($"Format out = {converter.WaveFormat}, Sample rate {converter.WaveFormat.SampleRate}");

                var mediaType = MediaFoundationEncoder.SelectMediaType(AudioSubtypes.MFAudioFormat_MP3, converter.WaveFormat, 0);
                if (mediaType == null) throw new InvalidOperationException("No suitable MP3 encoders available");
                Console.WriteLine($"MediaType = {(mediaType.AverageBytesPerSecond * 8)/1000}kbps, Sample rate {mediaType.SampleRate}, Channels: {mediaType.ChannelCount}");
                using (var encoder = new MediaFoundationEncoder(mediaType))
                {
                    // do a whole minute at a time - makes it faster on long files
                    // n.b. tried 10 minutes, didn't result in any noticable improvement
                    // limitation is now mostly the ACM GSM610 decoder
                    encoder.DefaultReadBufferSize = converter.WaveFormat.AverageBytesPerSecond * 60; 
                    encoder.Encode(fileOutPath, converter);
                }
                //MediaFoundationEncoder.EncodeToMp3(converter, fileOutPath, 2250*8);
            }
            Console.WriteLine($"Converted in {sw.Elapsed}");
        }
    }
}
