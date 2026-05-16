using System;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// SampleToWaveProvider24 でファイル変換するテスト。
    /// 環境依存ファイルを使う統合テスト。
    /// </summary>
    [TestFixture]
    public class SampleToWaveProvider24Tests
    {
        /// <summary>
        /// WAV を 24bit に変換して書き出せることを確認する。
        /// 入力ファイルは環境変数 NAUDIO_TEST_WAV か Path.GetTempPath()/Region-1.wav で指定。
        /// 既定の単体テスト実行 (TestCategory!=IntegrationTest) では Ignored。
        /// </summary>
        [Test]
        [Category("IntegrationTest")]
        public void ConvertAFile()
        {
            var input = Environment.GetEnvironmentVariable("NAUDIO_TEST_WAV")
                        ?? Path.Combine(Path.GetTempPath(), "Region-1.wav");
            if (!File.Exists(input)) ClassicAssert.Ignore($"Test file not found: {input}");
            var output = Path.Combine(Path.GetTempPath(), "Region1-24.wav");
            using (var reader = new WaveFileReader(input))
            {
                var sp = reader.ToSampleProvider();
                var wp24 = new SampleToWaveProvider24(sp);
                WaveFileWriter.CreateWaveFile(output, wp24);
            }
            ClassicAssert.IsTrue(File.Exists(output), "出力ファイルが生成されているはず");
        }
    }
}
