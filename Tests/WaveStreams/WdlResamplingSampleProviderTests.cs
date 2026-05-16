using System;
using System.Diagnostics;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// WdlResamplingSampleProvider のダウンサンプル・アップ/ダウンリサンプルのテスト。
    /// </summary>
    [TestFixture]
    public class WdlResamplingSampleProviderTests
    {
        /// <summary>
        /// MP3 ファイルをダウンサンプルして WAV に書き出せることを確認する。
        /// 環境依存ファイルを使うので IntegrationTest カテゴリに分類。
        /// 入力ファイルは環境変数 NAUDIO_TEST_MP3 で指定。
        /// </summary>
        [Test]
        [Category("IntegrationTest")]
        public void CanDownsampleAnMp3File()
        {
            var testFile = Environment.GetEnvironmentVariable("NAUDIO_TEST_MP3");
            if (string.IsNullOrEmpty(testFile) || !File.Exists(testFile))
                ClassicAssert.Ignore("Set NAUDIO_TEST_MP3 environment variable to point to a real .mp3 file");
            var outFile = Path.Combine(Path.GetTempPath(), "naudio-test-downsample.wav");
            using (var reader = new AudioFileReader(testFile))
            {
                // downsample to 22kHz
                var resampler = new WdlResamplingSampleProvider(reader, 22050);
                var wp = new SampleToWaveProvider(resampler);
                using (var writer = new WaveFileWriter(outFile, wp.WaveFormat))
                {
                    var b = new byte[wp.WaveFormat.AverageBytesPerSecond];
                    while (true)
                    {
                        var read = wp.Read(b, 0, b.Length);
                        if (read > 0)
                            writer.Write(b, 0, read);
                        else
                            break;
                    }
                }
            }
            ClassicAssert.IsTrue(File.Exists(outFile), "出力ファイルが生成されているはず");
        }

        /// <summary>
        /// 指定サンプルレートから別レートへリサンプルして読めることを確認する。
        /// </summary>
        /// <param name="from">入力サンプルレート。</param>
        /// <param name="to">出力サンプルレート。</param>
        [TestCase(8000, 16000)]
        [TestCase(8000, 22050)]
        [TestCase(8000, 32000)]
        [TestCase(8000, 44100)]
        [TestCase(8000, 48000)]
        [TestCase(8000, 96000)]
        [TestCase(44100, 8000)]
        [TestCase(44100, 16000)]
        [TestCase(44100, 22050)]
        [TestCase(44100, 32000)]
        [TestCase(44100, 48000)]
        [TestCase(44100, 96000)]
        [TestCase(48000, 8000)]
        [TestCase(48000, 16000)]
        [TestCase(48000, 22050)]
        [TestCase(48000, 32000)]
        [TestCase(48000, 44100)]
        [TestCase(48000, 96000)]
        public void CanResampleUpAndDown(int from, int to)
        {
            var channels = 1;
            var offset = CreateSignalGenerator(@from, channels);
            var resampler = new WdlResamplingSampleProvider(offset, to);
            var buffer = new float[to * channels];
            Debug.WriteLine(String.Format("From {0} to {1}", from, to));
            var totalRead = 0;
            for (var n = 0; n < 10; n++)
            {
                var read = resampler.Read(buffer, 0, buffer.Length);
                Debug.WriteLine(String.Format("read {0}", read));
                totalRead += read;
            }
            // 5 秒分の入力 (CreateSignalGenerator の TakeSamples = from * channels * 5) から
            // to レートでリサンプルすれば概ね to * 5 サンプル前後が読めるはず。
            // 「全く何も読めない (Read が 0 で固まる)」回帰を検出するため、
            // 少なくとも 1 サンプル以上は読み出せていることを確認する。
            ClassicAssert.Greater(totalRead, 0, $"Resampling {from}Hz -> {to}Hz で 1 サンプルも読めていません");
        }

        private static OffsetSampleProvider CreateSignalGenerator(int @from, int channels)
        {
            var signalGenerator = new SignalGenerator(@from, channels);
            signalGenerator.Type = SignalGeneratorType.SawTooth;
            signalGenerator.Frequency = 512;
            signalGenerator.Gain = 0.3f;
            var offset = new OffsetSampleProvider(signalGenerator);
            offset.TakeSamples = @from * channels * 5; // 5 seconds
            return offset;
        }
    }
}
