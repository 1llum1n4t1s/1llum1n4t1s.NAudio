using System;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using NAudio.Utils;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    [Category("UnitTest")]
    public class CircularBufferTests
    {
        [Test]
        public void CircularBufferHasMaxLengthAndCount()
        {
            var circularBuffer = new CircularBuffer(1024);
            ClassicAssert.AreEqual(1024, circularBuffer.MaxLength);
            ClassicAssert.AreEqual(0, circularBuffer.Count);
        }

        [Test]
        public void ReadFromEmptyBufferReturnsNothing()
        {
            var circularBuffer = new CircularBuffer(1024);
            var buffer = new byte[1024];
            var read = circularBuffer.Read(buffer, 0, 1024);
            ClassicAssert.AreEqual(0, read);
        }

        [Test]
        public void CanWriteToBuffer()
        {
            var circularBuffer = new CircularBuffer(1024);
            var buffer = new byte[100];
            circularBuffer.Write(buffer, 0, 100);
            ClassicAssert.AreEqual(100, circularBuffer.Count);
            circularBuffer.Write(buffer, 0, 50);
            ClassicAssert.AreEqual(150, circularBuffer.Count);
        }

        [Test]
        public void BufferReturnsAsMuchAsIsAvailable()
        {
            var circularBuffer = new CircularBuffer(1024);
            var buffer = new byte[100];
            circularBuffer.Write(buffer, 0, 100);
            ClassicAssert.AreEqual(100, circularBuffer.Count);
            var readBuffer = new byte[1000];
            var read = circularBuffer.Read(readBuffer, 0, 1000);
            ClassicAssert.AreEqual(100, read);
        }

        [Test]
        public void RejectsTooMuchData()
        {
            var circularBuffer = new CircularBuffer(100);
            var buffer = new byte[200];
                
            var written = circularBuffer.Write(buffer, 0, 200);
            ClassicAssert.AreEqual(100, written, "Wrote the wrong amount");
        }

        [Test]
        public void RejectsWhenFull()
        {
            var circularBuffer = new CircularBuffer(100);
            var buffer = new byte[200];
            circularBuffer.Write(buffer, 0, 75);
            var written = circularBuffer.Write(buffer, 0, 50);
            ClassicAssert.AreEqual(25, written, "Wrote the wrong amount");
        }

        [Test]
        public void RejectsWhenExactlyFull()
        {
            var circularBuffer = new CircularBuffer(100);
            var buffer = new byte[200];
            circularBuffer.Write(buffer, 0, 100);
            var written = circularBuffer.Write(buffer, 0, 50);
            ClassicAssert.AreEqual(0, written, "Wrote the wrong amount");
        }

        [Test]
        public void CanWritePastEnd()
        {
            var circularBuffer = new CircularBuffer(100);
            var buffer = new byte[200];
            circularBuffer.Write(buffer, 0, 75);
            ClassicAssert.AreEqual(75, circularBuffer.Count, "Initial count");
            var read = circularBuffer.Read(buffer, 0, 75);
            ClassicAssert.AreEqual(0, circularBuffer.Count, "Count after read");
            ClassicAssert.AreEqual(75, read, "Bytes read");
            // write wraps round
            circularBuffer.Write(buffer, 0, 50);
            ClassicAssert.AreEqual(50, circularBuffer.Count, "Count after wrap round");
            // read wraps round
            read = circularBuffer.Read(buffer, 0, 75);
            ClassicAssert.AreEqual(50, read, "Bytes Read 2");
            ClassicAssert.AreEqual(0, circularBuffer.Count, "Final Count");
        }

        [Test]
        public void DataIntegrityTest()
        {
            var numbers = new byte[256];
            var readBuffer = new byte[256];
            for (var n = 0; n < 256; n++)
            {
                numbers[n] = (byte)n;
            }

            var circularBuffer = new CircularBuffer(300);
            circularBuffer.Write(numbers, 0, 200);
            Array.Clear(readBuffer, 0, readBuffer.Length);
            var read = circularBuffer.Read(readBuffer, 0, 200);
            ClassicAssert.AreEqual(200, read);
            CheckBuffer(readBuffer, 0, read);
            
            // now write past the end
            circularBuffer.Write(numbers, 0, 200);
            Array.Clear(readBuffer, 0, readBuffer.Length);
            // now read past the end
            read = circularBuffer.Read(readBuffer, 0, 200);
            ClassicAssert.AreEqual(200, read);
            CheckBuffer(readBuffer, 0, read);
            
        }

        public void CheckBuffer(byte[] buffer, int startNumber, int length)
        {
            for (var n = 0; n < length; n++)
            {
                ClassicAssert.AreEqual(startNumber + n, buffer[n], "Byte mismatch at offset {0}", n);
            }
        }
    }
}
