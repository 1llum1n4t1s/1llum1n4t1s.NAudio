using NUnit.Framework;
using NUnit.Framework.Legacy;
using NAudioTests.Utils;
using NAudio.Wave;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// BlockAlignReductionStream の作成・読み取り・シークのテスト。
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class BlockAlignmentReductionStreamTests
    {
        /// <summary>
        /// BlockAlignReductionStream を生成できることを確認する。
        /// </summary>
        [Test]
        public void CanCreateBlockAlignmentReductionStream()
        {
            var inputStream = new BlockAlignedWaveStream(726, 80000);
            var blockStream = new BlockAlignReductionStream(inputStream);
            ClassicAssert.AreEqual(726, inputStream.BlockAlign);
            ClassicAssert.AreEqual(2, blockStream.BlockAlign);
        }

        /// <summary>
        /// ブロック境界に揃わない長さで読み取りできることを確認する。
        /// </summary>
        [Test]
        public void CanReadNonBlockAlignedLengths()
        {
            var inputStream = new BlockAlignedWaveStream(726, 80000);
            var blockStream = new BlockAlignReductionStream(inputStream);
            
            
            var inputBuffer = new byte[1024];
            var read = blockStream.Read(inputBuffer, 0, 1024);
            ClassicAssert.AreEqual(1024, read, "bytes read 1");
            ClassicAssert.AreEqual(blockStream.Position, 1024);
            CheckReadBuffer(inputBuffer, 1024, 0);

            read = blockStream.Read(inputBuffer, 0, 1024);
            ClassicAssert.AreEqual(1024, read, "bytes read 2");
            ClassicAssert.AreEqual(2048, blockStream.Position, "position 2");
            CheckReadBuffer(inputBuffer, 1024, 1024);



        }

        /// <summary>
        /// ブロック境界に揃わない位置にシークできることを確認する。
        /// </summary>
        [Test]
        public void CanRepositionToNonBlockAlignedPositions()
        {
            var inputStream = new BlockAlignedWaveStream(726, 80000);
            var blockStream = new BlockAlignReductionStream(inputStream);


            var inputBuffer = new byte[1024];
            var read = blockStream.Read(inputBuffer, 0, 1024);
            ClassicAssert.AreEqual(1024, read, "bytes read 1");
            ClassicAssert.AreEqual(blockStream.Position, 1024);
            CheckReadBuffer(inputBuffer, 1024, 0);

            read = blockStream.Read(inputBuffer, 0, 1024);
            ClassicAssert.AreEqual(1024, read, "bytes read 2");
            ClassicAssert.AreEqual(2048, blockStream.Position, "position 2");
            CheckReadBuffer(inputBuffer, 1024, 1024);


            // can reposition correctly
            blockStream.Position = 1000;
            read = blockStream.Read(inputBuffer, 0, 1024);
            ClassicAssert.AreEqual(1024, read, "bytes read 3");
            ClassicAssert.AreEqual(2024, blockStream.Position, "position 3");
            CheckReadBuffer(inputBuffer, 1024, 1000);
            
        }

        private void CheckReadBuffer(byte[] readBuffer, int count, int startPosition)
        {
            for (var n = 0; n < count; n++)
            {
                var expected = (byte)((startPosition + n) % 256);
                ClassicAssert.AreEqual(expected, readBuffer[n],"Read buffer at position {0}",startPosition+ n);
            }
        }

    }
}
