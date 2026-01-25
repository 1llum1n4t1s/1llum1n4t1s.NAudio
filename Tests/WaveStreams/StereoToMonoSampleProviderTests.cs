using NAudio.Wave;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// ステレオをモノラルに変換する ToMono のテスト。
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class StereoToMonoSampleProviderTests
    {
        /// <summary>
        /// 右チャンネルのみでモノラルになることを確認する。
        /// </summary>
        [Test]
        public void RightChannelOnly()
        {
            var stereoSampleProvider = new TestSampleProvider(44100, 2);
            var mono = stereoSampleProvider.ToMono(0f, 1f);
            var samples = 1000;
            var buffer = new float[samples];
            var read = mono.Read(buffer, 0, buffer.Length);
            ClassicAssert.AreEqual(buffer.Length, read, "samples read");
            for (var sample = 0; sample < samples; sample++)
            {
                ClassicAssert.AreEqual(1 + 2*sample, buffer[sample], "sample #" + sample);
            }
        }

        /// <summary>
        /// 出力 WaveFormat がモノラル IEEE float であることを確認する。
        /// </summary>
        [Test]
        public void CorrectOutputFormat()
        {
            var stereoSampleProvider = new TestSampleProvider(44100, 2);
            var mono = stereoSampleProvider.ToMono(0f, 1f);
            ClassicAssert.AreEqual(WaveFormatEncoding.IeeeFloat, mono.WaveFormat.Encoding);
            ClassicAssert.AreEqual(1, mono.WaveFormat.Channels);
            ClassicAssert.AreEqual(44100, mono.WaveFormat.SampleRate);
        }

        /// <summary>
        /// 左右のオフセットが正しく適用されることを確認する。
        /// </summary>
        [Test]
        public void CorrectOffset()
        {
            var stereoSampleProvider = new TestSampleProvider(44100, 2)
            {
                UseConstValue = true,
                ConstValue = 1
            };
            var mono = stereoSampleProvider.ToMono();

            var bufferLength = 30;
            var offset = 10;
            var samples = 10;

            // [10,20) in buffer will be filled with 1
            var buffer = new float[bufferLength];
            var read = mono.Read(buffer, offset, samples);
            ClassicAssert.AreEqual(samples, read, "samples read");

            for (var i = 0; i < bufferLength; i++)
            {
                var sample = buffer[i];

                if (i < offset || i >= offset + samples)
                {
                    ClassicAssert.AreEqual(0, sample, "not in Read range");
                }
                else
                {
                    ClassicAssert.AreNotEqual(0, sample, "in Read range");
                }
            }
        }
    }
}