using NAudio.Utils;
using NAudio.Wave;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    public class ChunkIdentifierTests
    {
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
