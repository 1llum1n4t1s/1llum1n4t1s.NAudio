using System.Linq;
using NAudio.Wave;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// SilenceProvider の読み取り・オフセット/カウントのテスト。
    /// </summary>
    [TestFixture]
    public class SilenceProviderTests
    {
        /// <summary>
        /// 無音が読めることを確認する。
        /// </summary>
        [Test]
        public void CanReadSilence()
        {
            var sp = new SilenceProvider(new WaveFormat(44100, 2));
            var length = 1000;
            var b = Enumerable.Range(1, length).Select(n => (byte) 1).ToArray();
            var read = sp.Read(b, 0, length);
            ClassicAssert.AreEqual(length, read);
            ClassicAssert.AreEqual(new byte[length], b);
        }

        /// <summary>
        /// Read の offset と count が尊重されることを確認する。
        /// </summary>
        [Test]
        public void RespectsOffsetAndCount()
        {
            var sp = new SilenceProvider(new WaveFormat(44100, 2));
            var length = 10;
            var b = Enumerable.Range(1, length).Select(n => (byte)1).ToArray();
            var read = sp.Read(b, 2, 4);
            ClassicAssert.AreEqual(4, read);
            ClassicAssert.AreEqual(new byte[] { 1, 1, 0, 0, 0, 0, 1, 1, 1, 1}, b);
        }
    }
}
