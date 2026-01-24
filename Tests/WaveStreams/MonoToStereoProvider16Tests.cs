using NUnit.Framework;
using NUnit.Framework.Legacy;
using NAudio.Wave;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    [Category("UnitTest")]
    public class MonoToStereoProvider16Tests
    {
        [Test]
        public void LeftChannelOnly()
        {
            IWaveProvider monoStream = new TestMonoProvider();
            var stereo = new MonoToStereoProvider16(monoStream);
            stereo.LeftVolume = 1.0f;
            stereo.RightVolume = 0.0f;
            var samples = 1000;
            var buffer = new byte[samples * 2];
            var read = stereo.Read(buffer, 0, buffer.Length);
            ClassicAssert.AreEqual(buffer.Length, read, "bytes read");
            var waveBuffer = new WaveBuffer(buffer);
            short expected = 0;
            for (var sample = 0; sample < samples; sample+=2)
            {
                var sampleLeft = waveBuffer.ShortBuffer[sample];
                var sampleRight = waveBuffer.ShortBuffer[sample+1];
                ClassicAssert.AreEqual(expected++, sampleLeft, "sample left");
                ClassicAssert.AreEqual(0, sampleRight, "sample right");
            }
        }
    }


    class TestMonoProvider : WaveProvider16
    {
        short current;

        public override int Read(short[] buffer, int offset, int sampleCount)
        {
            for (var sample = 0; sample < sampleCount; sample++)
            {
                buffer[offset + sample] = current++;
            }
            return sampleCount;
        }
    }
}

