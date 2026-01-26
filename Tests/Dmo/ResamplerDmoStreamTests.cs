using NUnit.Framework;
using NUnit.Framework.Legacy;
using NAudio.Wave;
using NAudioTests.Utils;

namespace NAudioTests.Dmo
{
    /// <summary>
    /// ResamplerDmoStream の作成・読み取り・リサンプル出力のテスト。
    /// </summary>
    [TestFixture]
    public class ResamplerDmoStreamTests
    {
        /// <summary>
        /// テスト実行前に Vista 以上であることを要求する。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            OSUtils.RequireVista();
        }

        /// <summary>
        /// ResamplerDmoStream を生成できることを確認する。
        /// </summary>
        [Test]
        [Category("IntegrationTest")]
        public void CanCreateResamplerStream()
        {
            //using (WaveFileReader reader = new WaveFileReader("C:\\Users\\Mark\\Recording\\REAPER\\ideas-2008-05-17.wav"))
            using (WaveStream reader = new NullWaveStream(new WaveFormat(44100,16,1),1000 ))
            {
                using (var resampler = new ResamplerDmoStream(reader, WaveFormat.CreateIeeeFloatWaveFormat(48000,2)))
                {
                    ClassicAssert.Greater(resampler.Length, reader.Length, "Length");
                    ClassicAssert.AreEqual(0, reader.Position, "Position");
                    ClassicAssert.AreEqual(0, resampler.Position, "Position");            
                }
            }
        }

        /// <summary>
        /// リサンプラストリームから 1 ブロック読み取りできることを確認する。
        /// </summary>
        [Test]
        [Category("IntegrationTest")]
        public void CanReadABlockFromResamplerStream()
        {
            //using (WaveFileReader reader = new WaveFileReader("C:\\Users\\Mark\\Recording\\REAPER\\ideas-2008-05-17.wav"))
            var inputFormat = new WaveFormat(44100, 16, 1);
            using (WaveStream reader = new NullWaveStream(inputFormat, inputFormat.AverageBytesPerSecond * 20))
            {
                using (var resampler = new ResamplerDmoStream(reader, WaveFormat.CreateIeeeFloatWaveFormat(48000, 2)))
                {
                    // try to read 10 ms;
                    var bytesToRead = resampler.WaveFormat.AverageBytesPerSecond / 100;
                    var buffer = new byte[bytesToRead];
                    var count = resampler.Read(buffer, 0, bytesToRead);
                    ClassicAssert.That(count > 0, "Bytes Read");
                }
            }
        }

        /// <summary>
        /// ストリーム全体を IEEE float にリサンプルできることを確認する。
        /// </summary>
        [Test]
        [Category("IntegrationTest")]
        public void CanResampleAWholeStreamToIEEE()
        {
            var inputFormat = new WaveFormat(44100, 16, 2);
            var outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
            ResampleAWholeStream(inputFormat, outputFormat);
        }

        /// <summary>
        /// ストリーム全体を 48kHz PCM にリサンプルできることを確認する。
        /// </summary>
        [Test]
        [Category("IntegrationTest")]
        public void CanResampleAWholeStreamTo48000PCM()
        {
            var inputFormat = new WaveFormat(44100, 16, 2);
            var outputFormat = new WaveFormat(48000, 16, 2);
            ResampleAWholeStream(inputFormat, outputFormat);
        }


        /// <summary>
        /// ストリーム全体を 44100Hz IEEE にリサンプルできることを確認する。
        /// </summary>
        [Test]
        [Category("IntegrationTest")]
        public void CanResampleAWholeStreamTo44100IEEE()
        {
            var inputFormat = new WaveFormat(48000, 16, 2);
            var outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            ResampleAWholeStream(inputFormat, outputFormat);
        }

        /// <summary>
        /// ストリーム全体を 44100Hz PCM にリサンプルできることを確認する。
        /// </summary>
        [Test]
        [Category("IntegrationTest")]
        public void CanResampleAWholeStreamTo44100PCM()
        {
            var inputFormat = new WaveFormat(48000, 16, 2);
            var outputFormat = new WaveFormat(44100, 16, 2);
            ResampleAWholeStream(inputFormat, outputFormat);
        }

        private void ResampleAWholeStream(WaveFormat inputFormat, WaveFormat outputFormat)
        {
            using (WaveStream reader = new NullWaveStream(inputFormat, inputFormat.AverageBytesPerSecond * 20))
            {
                using (var resampler = new ResamplerDmoStream(reader, outputFormat))
                {
                    // try to read 10 ms;
                    var bytesToRead = resampler.WaveFormat.AverageBytesPerSecond / 100;
                    var buffer = new byte[bytesToRead];
                    int count;
                    var total = 0;
                    do
                    {
                        count = resampler.Read(buffer, 0, bytesToRead);
                        total += count;
                        //ClassicAssert.AreEqual(count, bytesToRead, "Bytes Read");
                    } while (count > 0);
                    //Debug.WriteLine(String.Format("Converted input length {0} to {1}", reader.Length, total));
                }
            }
        }

        /*[Test]
        public void CanResampleToWav()
        {
            using (WaveFileReader reader = new WaveFileReader("C:\\Users\\Mark\\Recording\\REAPER\\ideas-2008-05-17.wav"))
            {
                using (ResamplerDmoStream resampler = new ResamplerDmoStream(reader, new WaveFormat(48000, 16, 2)))
                {
                    using (WaveFileWriter writer = new WaveFileWriter("C:\\Users\\Mark\\Recording\\REAPER\\ideas-converted.wav", resampler.WaveFormat))
                    {
                        // try to read 10 ms;
                        int bytesToRead = resampler.WaveFormat.AverageBytesPerSecond / 100;
                        byte[] buffer = new byte[bytesToRead];
                        int count;
                        int total = 0;
                        do
                        {
                            count = resampler.Read(buffer, 0, bytesToRead);
                            writer.WriteData(buffer, 0, count);
                            total += count;
                            //ClassicAssert.AreEqual(count, bytesToRead, "Bytes Read");
                        } while (count > 0);
                        Debug.WriteLine(String.Format("Converted input length {0} to {1}", reader.Length, total));
                    }
                }
            }
        }*/
    }
}
