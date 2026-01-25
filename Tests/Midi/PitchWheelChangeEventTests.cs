using NUnit.Framework;
using NUnit.Framework.Legacy;
using NAudio.Midi;
using System.IO;

namespace NAudioTests.Midi
{
    /// <summary>
    /// PitchWheelChangeEvent の GetAsShortMessage と Export のテスト。
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class PitchWheelChangeEventTests
    {
        /// <summary>
        /// GetAsShortMessage が期待した値を返すことを確認する。
        /// </summary>
        [Test]
        public void GetAsShortMessageReturnsCorrectValue()
        {
            var channel = 2;
            var pitch = 0x3FFF; // 0x2000 is the default
            var p = new PitchWheelChangeEvent(0, channel, pitch);

            ClassicAssert.AreEqual(0x007F7FE1, p.GetAsShortMessage());
        }

        /// <summary>
        /// Export で正しいバイト列が出力されることを確認する。
        /// </summary>
        [Test]
        public void ExportsCorrectValue()
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            var channel = 2;
            var pitch = 0x207D; // 0x2000 is the default
            var p = new PitchWheelChangeEvent(0, channel, pitch);
            
            long time = 0;
            p.Export(ref time, writer);

            ClassicAssert.AreEqual(4, ms.Length);
            var b = ms.GetBuffer();
            ClassicAssert.AreEqual(0x0, b[0]); // event time
            ClassicAssert.AreEqual(0xE1, b[1]);
            ClassicAssert.AreEqual(0x7D, b[2]);
            ClassicAssert.AreEqual(0x40, b[3]);
        }
    }
}
