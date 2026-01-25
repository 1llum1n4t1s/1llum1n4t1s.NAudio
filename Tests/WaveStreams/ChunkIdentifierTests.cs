using NAudio.Utils;
using NAudio.Wave;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// ChunkIdentifier を Int32 に変換するテスト。
    /// </summary>
    [TestFixture]
    public class ChunkIdentifierTests
    {
        /// <summary>
        /// チャンク識別子文字列を Int32 に変換できることを確認する。
        /// </summary>
        /// <param name="chunkIdentifier">チャンク識別子（4 文字）。</param>
        [TestCase("WAVE")]
        [TestCase("data")]
        [TestCase("fmt ")]
        [TestCase("RF64")]
        [TestCase("ds64")]
        [TestCase("labl")]
        [TestCase("cue ")]
        public void CanConvertChunkIndentiferToInt(string chunkIdentifier)
        {
            var x = WaveInterop.mmioStringToFOURCC(chunkIdentifier, 0);
            ClassicAssert.AreEqual(x, ChunkIdentifier.ChunkIdentifierToInt32(chunkIdentifier));
        }



    }
}
