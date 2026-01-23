using NAudio.Wave;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.IO;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    public class AudioFileReaderTests
    {
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
