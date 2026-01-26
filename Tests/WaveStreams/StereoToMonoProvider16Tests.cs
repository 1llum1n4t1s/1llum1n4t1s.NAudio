using NUnit.Framework;
using NUnit.Framework.Legacy;
using NAudio.Wave;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// StereoToMonoProvider16 の右チャンネルのみ出力のテスト。
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class StereoToMonoProvider16Tests
    {
        /// <summary>
        /// 右チャンネルのみにボリュームを付けてモノラルで読めることを確認する。
        /// </summary>
        [Test]
        public void RightChannelOnly()
        {
            IWaveProvider stereoStream = new TestStereoProvider();
            var mono = new StereoToMonoProvider16(stereoStream);
            mono.LeftVolume = 0.0f;
            mono.RightVolume = 1.0f;
            var samples = 1000;
            var buffer = new byte[samples * 2];
            var read = mono.Read(buffer, 0, buffer.Length);
            ClassicAssert.AreEqual(buffer.Length, read, "bytes read");
            var waveBuffer = new WaveBuffer(buffer);
            short expected = 0;
            for (var sample = 0; sample < samples; sample++)
            {
                var sampleVal = waveBuffer.ShortBuffer[sample];
                ClassicAssert.AreEqual(expected--, sampleVal, "sample #" + sample.ToString());
            }
        }
    }

    class TestStereoProvider : WaveProvider16
    {
        public TestStereoProvider()
            : base(44100, 2)
        { }

        short current;

        public override int Read(short[] buffer, int offset, int sampleCount)
        {
            for (var sample = 0; sample < sampleCount; sample+=2)
            {
                buffer[offset + sample] = current;
                buffer[offset + sample + 1] = (short)(0 - current);
                current++;
            }
            return sampleCount;
        }
    }
}
