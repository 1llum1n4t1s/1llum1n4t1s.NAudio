using System;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.IO;
using NAudio.Wave;

namespace NAudioTests.Mp3
{
    /// <summary>
    /// Mp3Frame のパース（有効フレーム・無効データ・オフセット）のテスト。
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class Mp3FrameTests
    {
        private const int CrcNotPresent = 1;
        private const int BitRateIndex = 1;

        private readonly byte[] validMp3FrameHeader = { 0xff, 
            0xe0 + ((int)MpegVersion.Version2 << 3) + ((int)MpegLayer.Layer3 << 1) + CrcNotPresent, 
            BitRateIndex << 4, 0x00
        };

        private byte[] ConstructValidMp3Frame()
        {
            var frame = new byte[52];
            Array.Copy(validMp3FrameHeader, frame, validMp3FrameHeader.Length);
            return frame;
        }

        /// <summary>
        /// 有効な MP3 フレームヘッダからフレームをパースできることを確認する。
        /// </summary>
        [Test]
        public void CanParseValidMp3Frame()
        {
            var ms = new MemoryStream(ConstructValidMp3Frame());
            var frame = Mp3Frame.LoadFromStream(ms);
            ClassicAssert.IsNotNull(frame);
        }

        /// <summary>
        /// 長さが不足したデータではフレームをパースできず null が返ることを確認する。
        /// </summary>
        /// <param name="length">ストリームのバイト長。</param>
        [TestCase(0)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(8)]
        [TestCase(12)]
        public void FailsToParseInvalidFrame(int length)
        {
            var ms = new MemoryStream(new byte[length]);
            var frame = Mp3Frame.LoadFromStream(ms);
            ClassicAssert.IsNull(frame);
        }

        /// <summary>
        /// ストリーム先頭から N バイトオフセットした位置から有効フレームをパースできることを確認する。
        /// </summary>
        /// <param name="offset">フレームまでのオフセットバイト数。</param>
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void CanParseMp3FrameOffsetByN(int offset)
        {
            var validMp3Frame = ConstructValidMp3Frame();
            var offsetBuffer = new byte[offset + validMp3Frame.Length];
            Array.Copy(validMp3Frame, 0, offsetBuffer, offset, validMp3Frame.Length);
            var ms = new MemoryStream(offsetBuffer);
            var frame = Mp3Frame.LoadFromStream(ms);
            ClassicAssert.IsNotNull(frame);
        }
    }
}
