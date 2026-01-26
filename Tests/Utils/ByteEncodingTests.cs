using NAudio.Utils;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NAudioTests.Utils
{
    /// <summary>
    /// ByteEncoding のデコード・切り詰めのテスト。
    /// </summary>
    [TestFixture]
    public class ByteEncodingTests
    {
        /// <summary>
        /// バイト配列を文字列にデコードできることを確認する。
        /// </summary>
        [Test]
        public void CanDecodeString()
        {
            var b = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o', };
            ClassicAssert.AreEqual("Hello", ByteEncoding.Instance.GetString(b));
        }

        /// <summary>
        /// 末尾の null が切り捨てられることを確認する。
        /// </summary>
        [Test]
        public void CanTruncate()
        {
            var b = new byte[] {(byte) 'H', (byte) 'e', (byte) 'l', (byte) 'l', (byte) 'o', 0};
            ClassicAssert.AreEqual("Hello", ByteEncoding.Instance.GetString(b));
        }

        /// <summary>
        /// 3 引数オーバーロードで切り詰めできることを確認する。
        /// </summary>
        [Test]
        public void CanTruncateWithThreeParamOverride()
        {
            var b = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o', 0 };
            ClassicAssert.AreEqual("Hello", ByteEncoding.Instance.GetString(b,0,b.Length));
        }
    }
}
