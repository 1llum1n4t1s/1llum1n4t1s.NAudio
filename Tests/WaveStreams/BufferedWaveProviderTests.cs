using System.Linq;
using NAudio.Wave;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// BufferedWaveProvider の Clear・Read・ReadFully のテスト。
    /// </summary>
    [TestFixture]
    public class BufferedWaveProviderTests
    {
        /// <summary>
        /// 書き込み前に ClearBuffer できることを確認する。
        /// </summary>
        [Test]
        public void CanClearBeforeWritingSamples()
        {
            var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
            bwp.ClearBuffer();
            ClassicAssert.AreEqual(0, bwp.BufferedBytes);
        }
        
        /// <summary>
        /// バッファしたバイトが Read で返ることを確認する。
        /// </summary>
        [Test]
        public void BufferedBytesAreReturned()
        {
            var bytesToBuffer = 1000;
            var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
            var data = Enumerable.Range(1, bytesToBuffer).Select(n => (byte)(n % 256)).ToArray();
            bwp.AddSamples(data, 0, data.Length);
            ClassicAssert.AreEqual(bytesToBuffer, bwp.BufferedBytes);
            var readBuffer = new byte[bytesToBuffer];
            var bytesRead = bwp.Read(readBuffer, 0, bytesToBuffer);
            ClassicAssert.AreEqual(bytesRead, bytesToBuffer);
            ClassicAssert.AreEqual(readBuffer,data);
            ClassicAssert.AreEqual(0, bwp.BufferedBytes);
        }

        /// <summary>
        /// 空バッファで Read が 0 を返すことを確認する。
        /// </summary>
        [Test]
        public void EmptyBufferCanReturnZeroFromRead()
        {
            var bwp = new BufferedWaveProvider(new WaveFormat());
            bwp.ReadFully = false;
            var buffer = new byte[44100];
            var read = bwp.Read(buffer, 0, buffer.Length);
            ClassicAssert.AreEqual(0, read);
        }

        /// <summary>
        /// ReadFully が false のとき部分読み取りできることを確認する。
        /// </summary>
        [Test]
        public void PartialReadsPossibleWithReadFullyFalse()
        {
            var bwp = new BufferedWaveProvider(new WaveFormat());
            bwp.ReadFully = false;
            var buffer = new byte[44100];
            bwp.AddSamples(buffer, 0, 2000);
            var read = bwp.Read(buffer, 0, buffer.Length);
            ClassicAssert.AreEqual(2000, read);
            ClassicAssert.AreEqual(0, bwp.BufferedBytes);
        }

        /// <summary>
        /// デフォルトでは要求分いっぱい読み取ることを確認する。
        /// </summary>
        [Test]
        public void FullReadsByDefault()
        {
            var bwp = new BufferedWaveProvider(new WaveFormat());
            var buffer = new byte[44100];
            bwp.AddSamples(buffer, 0, 2000);
            var read = bwp.Read(buffer, 0, buffer.Length);
            ClassicAssert.AreEqual(buffer.Length, read);
            ClassicAssert.AreEqual(0, bwp.BufferedBytes);
        }

        /// <summary>
        /// バッファが要求より多いときも要求分だけ読み取ることを確認する。
        /// </summary>
        [Test]
        public void WhenBufferHasMoreThanNeededReadFully()
        {
            var bwp = new BufferedWaveProvider(new WaveFormat());
            var buffer = new byte[44100];
            bwp.AddSamples(buffer, 0, 5000);
            var read = bwp.Read(buffer, 0, 2000);
            ClassicAssert.AreEqual(2000, read);
            ClassicAssert.AreEqual(3000, bwp.BufferedBytes);
        }

    }
}
