using System;
using System.Linq;
using NAudio.Utils;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NAudioTests.Utils
{
    [TestFixture]
    public class ByteEncodingTests
    {
        [Test]
        public void CanDecodeString()
        {
            var b = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o', };
            ClassicAssert.AreEqual("Hello", ByteEncoding.Instance.GetString(b));
        }

        [Test]
        public void CanTruncate()
        {
            var b = new byte[] {(byte) 'H', (byte) 'e', (byte) 'l', (byte) 'l', (byte) 'o', 0};
            ClassicAssert.AreEqual("Hello", ByteEncoding.Instance.GetString(b));
        }

        [Test]
        public void CanTruncateWithThreeParamOverride()
        {
            var b = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o', 0 };
            ClassicAssert.AreEqual("Hello", ByteEncoding.Instance.GetString(b,0,b.Length));
        }
    }
}
