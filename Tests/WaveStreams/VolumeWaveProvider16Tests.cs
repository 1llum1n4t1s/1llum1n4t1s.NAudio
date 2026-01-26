using System;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using NAudio.Wave;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// VolumeWaveProvider16 のデフォルト値・透過・ボリューム・クリップのテスト。
    /// </summary>
    [TestFixture]
    public class VolumeWaveProvider16Tests
    {
        /// <summary>
        /// デフォルトの Volume が 1 であることを確認する。
        /// </summary>
        [Test]
        public void DefaultVolumeIs1()
        {
            var testProvider = new TestWaveProvider(new WaveFormat(44100, 16, 2));
            var vwp = new VolumeWaveProvider16(testProvider);
            ClassicAssert.AreEqual(1.0f, vwp.Volume);
        }

        /// <summary>
        /// WaveFormat がソースをそのまま返すことを確認する。
        /// </summary>
        [Test]
        public void PassesThroughSourceWaveFormat()
        {
            var testProvider = new TestWaveProvider(new WaveFormat(44100, 16, 2));
            var vwp = new VolumeWaveProvider16(testProvider);
            ClassicAssert.AreSame(testProvider.WaveFormat, vwp.WaveFormat);
        }

        /// <summary>
        /// Volume 1 でデータがそのまま通過することを確認する。
        /// </summary>
        [Test]
        public void PassesThroughDataUnchangedAtVolume1()
        {
            var testProvider= new TestWaveProvider(new WaveFormat(44100,16,2));
            var vwp = new VolumeWaveProvider16(testProvider);
            var buffer = new byte[20];
            var bytesRead = vwp.Read(buffer, 0, buffer.Length);
            ClassicAssert.AreEqual(buffer.Length, bytesRead);
            ClassicAssert.AreEqual(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 }, buffer);
        }

        /// <summary>
        /// 0.5 倍ボリュームが正しく適用されることを確認する。
        /// </summary>
        [Test]
        public void HalfVolumeWorks()
        {
            var testProvider = new TestWaveProvider(new WaveFormat(44100, 16, 2));
            testProvider.ConstValue = 100;
            var vwp = new VolumeWaveProvider16(testProvider);
            vwp.Volume = 0.5f;
            var buffer = new byte[4];
            var bytesRead = vwp.Read(buffer, 0, buffer.Length);
            ClassicAssert.AreEqual(new byte[] { 50, 50, 50, 50 }, buffer);
        }

        /// <summary>
        /// 0 ボリュームで無音になることを確認する。
        /// </summary>
        [Test]
        public void ZeroVolumeWorks()
        {
            var testProvider = new TestWaveProvider(new WaveFormat(44100, 16, 2));
            testProvider.ConstValue = 100;
            var vwp = new VolumeWaveProvider16(testProvider);
            vwp.Volume = 0f;
            var buffer = new byte[4];
            var bytesRead = vwp.Read(buffer, 0, buffer.Length);
            ClassicAssert.AreEqual(new byte[] { 0, 0, 0, 0 }, buffer);
        }

        /// <summary>
        /// 2 倍ボリュームが正しく適用されることを確認する。
        /// </summary>
        [Test]
        public void DoubleVolumeWorks()
        {
            var testProvider = new TestWaveProvider(new WaveFormat(44100, 16, 1));
            testProvider.ConstValue = 2;
            var sampleValue = BitConverter.ToInt16(new byte[] { 2, 2 }, 0);
            sampleValue = (short)(sampleValue * 2);

            var vwp = new VolumeWaveProvider16(testProvider);
            vwp.Volume = 2f;
            var buffer = new byte[2];
            var bytesRead = vwp.Read(buffer, 0, buffer.Length);
            ClassicAssert.AreEqual(BitConverter.GetBytes(sampleValue), buffer);
        }

        /// <summary>
        /// 2 倍ボリュームでクリップすることを確認する。
        /// </summary>
        [Test]
        public void DoubleVolumeClips()
        {
            var testProvider = new TestWaveProvider(new WaveFormat(44100, 16, 1));
            testProvider.ConstValue = 100;
            var sampleValue = BitConverter.ToInt16(new byte[] { 100, 100 }, 0);
            sampleValue = Int16.MaxValue;

            var vwp = new VolumeWaveProvider16(testProvider);
            vwp.Volume = 2f;
            var buffer = new byte[2];
            var bytesRead = vwp.Read(buffer, 0, buffer.Length);
            ClassicAssert.AreEqual(BitConverter.GetBytes(sampleValue), buffer);
        }
    }
}
