using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.IO;
using NAudio.Wave;
using System.Diagnostics;
using NAudioTests.Utils;

namespace NAudioTests.Mp3
{
    [TestFixture]
    public class Mp3FileReaderTests
    {
        [Test]
        [Category("IntegrationTest")]
        public void CanLoadAndReadVariousProblemMp3Files()
        {
            var testDataFolder = @"C:\Users\Mark\Downloads\NAudio";
            if (!Directory.Exists(testDataFolder))
            {
                ClassicAssert.Ignore($"{testDataFolder} not found");
            }
            foreach (var file in Directory.GetFiles(testDataFolder, "*.mp3"))
            {
                var mp3File = Path.Combine(testDataFolder, file);
                Debug.WriteLine($"Opening {mp3File}");
                using (var reader = new Mp3FileReader(mp3File))
                {
                    var buffer = new byte[4096];
                    int bytesRead;
                    var total = 0;
                    do
                    {
                        bytesRead = reader.Read(buffer, 0, buffer.Length);
                        total += bytesRead;
                    } while (bytesRead > 0);
                    Debug.WriteLine($"Read {total} bytes");
                }
            }
        }

        [Test]
        public void ReadFrameAdvancesPosition()
        {
            var file = TestFileBuilder.CreateMp3File(5);
            try
            {
                using (var mp3FileReader = new Mp3FileReader(file))
                {
                    var lastPos = mp3FileReader.Position;
                    while ((mp3FileReader.ReadNextFrame()) != null)
                    {
                        ClassicAssert.IsTrue(mp3FileReader.Position > lastPos);
                        lastPos = mp3FileReader.Position;
                    }
                    ClassicAssert.AreEqual(mp3FileReader.Length, mp3FileReader.Position);
                    ClassicAssert.IsTrue(mp3FileReader.Length > 0);
                }
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Test]
        public void CopesWithZeroLengthMp3()
        {
            var ms = new MemoryStream(new byte[0]);
            Assert.Throws<InvalidDataException>(() => new Mp3FileReader(ms));            
        }
    }
}
