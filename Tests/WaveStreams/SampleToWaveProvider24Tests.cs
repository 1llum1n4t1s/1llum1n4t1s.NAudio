using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// SampleToWaveProvider24 でファイル変換するテスト。
    /// </summary>
    [TestFixture]
    public class SampleToWaveProvider24Tests
    {
        /// <summary>
        /// WAV を 24bit に変換して書き出せることを確認する。
        /// </summary>
        [Test]
        public void ConvertAFile()
        {
            const string input = @"C:\Users\Mark\Downloads\Region-1.wav";
            if (!File.Exists(input)) ClassicAssert.Ignore("Test file not found");
            using (var reader = new WaveFileReader(input))
            {
                var sp = reader.ToSampleProvider();
                var wp24 = new SampleToWaveProvider24(sp);
                WaveFileWriter.CreateWaveFile(@"C:\Users\Mark\Downloads\Region1-24.wav", wp24);
            }
        }
    }
}
