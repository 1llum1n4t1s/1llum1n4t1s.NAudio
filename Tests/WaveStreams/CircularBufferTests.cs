using System;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using NAudio.Utils;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// CircularBuffer の MaxLength/Count/Read/Write/ラップのテスト。
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class CircularBufferTests
    {
        /// <summary>
        /// CircularBuffer が MaxLength と Count を持つことを確認する。
        /// </summary>
        [Test]
        public void CircularBufferHasMaxLengthAndCount()
        {
            var circularBuffer = new CircularBuffer(1024);
            ClassicAssert.AreEqual(1024, circularBuffer.MaxLength);
            ClassicAssert.AreEqual(0, circularBuffer.Count);
        }

        /// <summary>
        /// 空バッファから Read すると 0 が返ることを確認する。
        /// </summary>
        [Test]
        public void ReadFromEmptyBufferReturnsNothing()
        {
            var circularBuffer = new CircularBuffer(1024);
            var buffer = new byte[1024];
            var read = circularBuffer.Read(buffer, 0, 1024);
            ClassicAssert.AreEqual(0, read);
        }

        /// <summary>
        /// バッファに書き込め、Count が増えることを確認する。
        /// </summary>
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

        /// <summary>
        /// Read が利用可能量だけ返すことを確認する。
        /// </summary>
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

        /// <summary>
        /// 最大長を超える書き込みが打ち切られることを確認する。
        /// </summary>
        [Test]
        public void RejectsTooMuchData()
        {
            var circularBuffer = new CircularBuffer(100);
            var buffer = new byte[200];
                
            var written = circularBuffer.Write(buffer, 0, 200);
            ClassicAssert.AreEqual(100, written, "Wrote the wrong amount");
        }

        /// <summary>
        /// 満杯時は追加書き込みが一部だけ受け入れられることを確認する。
        /// </summary>
        [Test]
        public void RejectsWhenFull()
        {
            var circularBuffer = new CircularBuffer(100);
            var buffer = new byte[200];
            circularBuffer.Write(buffer, 0, 75);
            var written = circularBuffer.Write(buffer, 0, 50);
            ClassicAssert.AreEqual(25, written, "Wrote the wrong amount");
        }

        /// <summary>
        /// ちょうど満杯のときは追加で 0 バイトしか書き込めないことを確認する。
        /// </summary>
        [Test]
        public void RejectsWhenExactlyFull()
        {
            var circularBuffer = new CircularBuffer(100);
            var buffer = new byte[200];
            circularBuffer.Write(buffer, 0, 100);
            var written = circularBuffer.Write(buffer, 0, 50);
            ClassicAssert.AreEqual(0, written, "Wrote the wrong amount");
        }

        /// <summary>
        /// 読み取り後に先頭からラップして書き込めることを確認する。
        /// </summary>
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

        /// <summary>
        /// ラップ後もデータが正しく読み取りできることを確認する。
        /// </summary>
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

        /// <summary>
        /// バッファの指定範囲が startNumber からの連続値であることを検証する。
        /// </summary>
        /// <param name="buffer">検証するバッファ。</param>
        /// <param name="startNumber">先頭の期待値。</param>
        /// <param name="length">検証する長さ。</param>
        public void CheckBuffer(byte[] buffer, int startNumber, int length)
        {
            for (var n = 0; n < length; n++)
            {
                ClassicAssert.AreEqual(startNumber + n, buffer[n], "Byte mismatch at offset {0}", n);
            }
        }
    }
}
