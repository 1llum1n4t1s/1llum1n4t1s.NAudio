using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using NAudioTests.Utils;
using NAudio.Wave;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    [Category("UnitTest")]
    public class BlockAlignmentReductionStreamTests
    {
        [Test]
        public void CanCreateBlockAlignmentReductionStream()
        {
            BlockAlignedWaveStream inputStream = new BlockAlignedWaveStream(726, 80000);
            BlockAlignReductionStream blockStream = new BlockAlignReductionStream(inputStream);
            ClassicAssert.AreEqual(726, inputStream.BlockAlign);
            ClassicAssert.AreEqual(2, blockStream.BlockAlign);
        }

        [Test]
        public void CanReadNonBlockAlignedLengths()
        {
            BlockAlignedWaveStream inputStream = new BlockAlignedWaveStream(726, 80000);
            BlockAlignReductionStream blockStream = new BlockAlignReductionStream(inputStream);
            
            
            byte[] inputBuffer = new byte[1024];
            int read = blockStream.Read(inputBuffer, 0, 1024);
            ClassicAssert.AreEqual(1024, read, "bytes read 1");
            ClassicAssert.AreEqual(blockStream.Position, 1024);
            CheckReadBuffer(inputBuffer, 1024, 0);

            read = blockStream.Read(inputBuffer, 0, 1024);
            ClassicAssert.AreEqual(1024, read, "bytes read 2");
            ClassicAssert.AreEqual(2048, blockStream.Position, "position 2");
            CheckReadBuffer(inputBuffer, 1024, 1024);



        }

        [Test]
        public void CanRepositionToNonBlockAlignedPositions()
        {
            BlockAlignedWaveStream inputStream = new BlockAlignedWaveStream(726, 80000);
            BlockAlignReductionStream blockStream = new BlockAlignReductionStream(inputStream);


            byte[] inputBuffer = new byte[1024];
            int read = blockStream.Read(inputBuffer, 0, 1024);
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
            for (int n = 0; n < count; n++)
            {
                byte expected = (byte)((startPosition + n) % 256);
                ClassicAssert.AreEqual(expected, readBuffer[n],"Read buffer at position {0}",startPosition+ n);
            }
        }

    }
}
