using NAudio.Wave;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.IO;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// AudioFileReader の Dispose のテスト。
    /// </summary>
    [TestFixture]
    public class AudioFileReaderTests
    {
        /// <summary>
        /// 複数回 Dispose しても例外が発生しないことを確認する。
        /// </summary>
        [Test]
        [Category("IntegrationTest")]
        public void CanBeDisposedMoreThanOnce()
        {
            var path = @"..\..\..\SampleData\Drums\closed-hat-trimmed.wav";
            if (!File.Exists(path))
                ClassicAssert.Ignore("test file not found");
            var reader = new AudioFileReader(path);
            reader.Dispose();
            ClassicAssert.DoesNotThrow(() => reader.Dispose());
        }
    }
}
