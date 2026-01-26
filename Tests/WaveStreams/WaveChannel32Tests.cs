using NUnit.Framework;
using NUnit.Framework.Legacy;
using NAudio.Wave;
using System.IO;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// WaveChannel32 で WAV を作成するテスト。
    /// </summary>
    [TestFixture]
    public class WaveChannel32Tests
    {
        /// <summary>
        /// WaveChannel32 から WAV ファイルを生成できることを確認する。
        /// </summary>
        [Test]
        [Category("IntegrationTest")]
        public void CanCreateWavFileFromWaveChannel32()
        {
            var inFile = @"F:\Recording\wav\pcm\16bit mono 8kHz.wav";
            var outFile = @"F:\Recording\wav\pcm\32bit stereo 8kHz.wav";
            if (!File.Exists(inFile))
            {
                ClassicAssert.Ignore("Input test file not found");
            }
            var audio32 = new WaveChannel32(new WaveFileReader(inFile));
            audio32.PadWithZeroes = false;
            WaveFileWriter.CreateWaveFile(outFile, audio32);
        }
    }
}
