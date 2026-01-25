using System;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.IO;
using NAudio.Wave;
using System.Diagnostics;

namespace NAudioTests.Aiff
{
    /// <summary>
    /// AIFF ファイル読み取りと WAV 変換のテスト。
    /// </summary>
    [TestFixture]
    public class AiffReaderTests
    {
        /// <summary>
        /// 指定フォルダ内の AIFF を WAV に変換できることを確認する。
        /// </summary>
        [Test]
        [Category("IntegrationTest")]
        public void ConvertAiffToWav()
        {
            var testFolder = @"C:\Users\Mark\Downloads\NAudio";
            if (!Directory.Exists(testFolder))
            {
                ClassicAssert.Ignore($"{testFolder} not found");
            }

            foreach (var file in Directory.GetFiles(testFolder, "*.aiff"))
            {
                var baseName=  Path.GetFileNameWithoutExtension(file);
                var wavFile = Path.Combine(testFolder, baseName + ".wav");
                var aiffFile = Path.Combine(testFolder, file);
                Debug.WriteLine(String.Format("Converting {0} to wav", aiffFile));
                ConvertAiffToWav(aiffFile, wavFile);
            }
        }

        private static void ConvertAiffToWav(string aiffFile, string wavFile)
        {
            using (var reader = new AiffFileReader(aiffFile))
            {
                using (var writer = new WaveFileWriter(wavFile, reader.WaveFormat))
                {
                    var buffer = new byte[4096];
                    var bytesRead = 0;
                    do
                    {
                        bytesRead = reader.Read(buffer, 0, buffer.Length);
                        writer.Write(buffer, 0, bytesRead);
                    } while (bytesRead > 0);
                }
            }
        }
    }
}
