using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using NAudio.Midi;
using System.IO;

namespace NAudioTests.Midi
{
    [TestFixture]
    [Category("UnitTest")]    
    public class PitchWheelChangeEventTests
    {
        [Test]
        public void GetAsShortMessageReturnsCorrectValue()
        {
            int channel = 2;
            int pitch = 0x3FFF; // 0x2000 is the default
            PitchWheelChangeEvent p = new PitchWheelChangeEvent(0, channel, pitch);

            ClassicAssert.AreEqual(0x007F7FE1, p.GetAsShortMessage());
        }

        [Test]
        public void ExportsCorrectValue()
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            int channel = 2;
            int pitch = 0x207D; // 0x2000 is the default
            PitchWheelChangeEvent p = new PitchWheelChangeEvent(0, channel, pitch);
            
            long time = 0;
            p.Export(ref time, writer);

            ClassicAssert.AreEqual(4, ms.Length);
            byte[] b = ms.GetBuffer();
            ClassicAssert.AreEqual(0x0, b[0]); // event time
            ClassicAssert.AreEqual(0xE1, b[1]);
            ClassicAssert.AreEqual(0x7D, b[2]);
            ClassicAssert.AreEqual(0x40, b[3]);
        }
    }
}
