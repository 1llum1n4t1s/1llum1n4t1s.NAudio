using System;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using NAudio.Wave;
using NAudio.Wave.Compression;
using System.Diagnostics;
using System.Linq;
using NAudioTests.Utils;

namespace NAudioTests.Acm
{
    /// <summary>
    /// WaveFormatConversionStream によるフォーマット変換のテスト。
    /// </summary>
    [TestFixture]
    [Category("IntegrationTest")]
    public class WaveFormatConversionStreamTests
    {
        /// <summary>
        /// PCM から μLaw への変換ができることを確認する。
        /// </summary>
        [Test]
        public void CanConvertPcmToMuLaw()
        {
            var channels = 1;
            var sampleRate = 8000;
            CanCreateConversionStream(
                new WaveFormat(sampleRate, 16, channels),
                WaveFormat.CreateCustomFormat(WaveFormatEncoding.MuLaw, sampleRate, channels, sampleRate * channels, 1, 8));
        }

        /// <summary>
        /// PCM から A-law への変換ができることを確認する。
        /// </summary>
        [Test]
        public void CanConvertPcmToALaw()
        {
            var channels = 1;
            var sampleRate = 8000;
            CanCreateConversionStream(
                new WaveFormat(sampleRate, 16, channels),
                WaveFormat.CreateCustomFormat(WaveFormatEncoding.ALaw, sampleRate, channels, sampleRate * channels, 1, 8));
        }

        /* Windows does not provide an ACM MP3 encoder, but this test could be run
         * if you install a different ACM MP3 encoder to see if the MP3 Wave Format
         * NAudio creates is sufficient (possibly it will have its own custom metadata
         * in the WaveFormat extra byts).
        [Test]
        public void CanConvertPcmToMp3()
        {
            int channels = 2;
            int sampleRate = 44100;
            CanCreateConversionStream(
                new WaveFormat(sampleRate, 16, channels),
                new Mp3WaveFormat(sampleRate, channels, 0, 128000/8)); 
        }*/

        /// <summary>
        /// A-law から PCM への変換ができることを確認する。
        /// </summary>
        [Test]
        public void CanConvertALawToPcm()
        {
            var channels = 1;
            var sampleRate = 8000;
            CanCreateConversionStream(
                WaveFormat.CreateCustomFormat(WaveFormatEncoding.ALaw, sampleRate, channels, sampleRate * channels, 1, 8),
                new WaveFormat(sampleRate, 16, channels));
        }

        /// <summary>
        /// μLaw から PCM への変換ができることを確認する。
        /// </summary>
        [Test]
        public void CanConvertMuLawToPcm()
        {
            var channels = 1;
            var sampleRate = 8000;
            CanCreateConversionStream(
                WaveFormat.CreateCustomFormat(WaveFormatEncoding.MuLaw, sampleRate, channels, sampleRate * channels, 1, 8),
                new WaveFormat(sampleRate, 16, channels));
        }

        /// <summary>
        /// ADPCM から PCM への変換ができることを確認する。
        /// </summary>
        [Test]
        public void CanConvertAdpcmToPcm()
        {
            var channels = 1;
            var sampleRate = 8000;
            CanCreateConversionStream(
                new AdpcmWaveFormat(8000,1),
                new WaveFormat(sampleRate, 16, channels));
        }

        /// <summary>
        /// ADPCM を推奨 PCM フォーマットに変換できることを確認する。
        /// </summary>
        [Test]
        public void CanConvertAdpcmToSuggestedPcm()
        {
            using (WaveFormatConversionStream.CreatePcmStream(
                new NullWaveStream(new AdpcmWaveFormat(8000, 1),1000)))
            {
            }
        }

        /// <summary>
        /// A-law を推奨 PCM フォーマットに変換できることを確認する。
        /// </summary>
        [Test]
        public void CanConvertALawToSuggestedPcm()
        {
            using (WaveFormatConversionStream.CreatePcmStream(
                new NullWaveStream(WaveFormat.CreateALawFormat(8000,1),1000)))
            {
            }
        }

        /// <summary>
        /// μLaw を推奨 PCM フォーマットに変換できることを確認する。
        /// </summary>
        [Test]
        public void CanConvertMuLawToSuggestedPcm()
        {
            using (WaveFormatConversionStream.CreatePcmStream(
                new NullWaveStream(WaveFormat.CreateMuLawFormat(8000, 1), 1000)))
            {
            }
        }

        /// <summary>
        /// PCM から ADPCM への変換ができることを確認する。
        /// </summary>
        [Test]
        public void CanConvertPcmToAdpcm()
        {
            var channels = 1;
            var sampleRate = 8000;
            CanCreateConversionStream(
                new WaveFormat(sampleRate, 16, channels),
                new AdpcmWaveFormat(8000, 1));
        }

        /// <summary>
        /// IMA/IMA ADPCM から PCM への変換ができることを確認する。
        /// </summary>
        [Test]
        public void CanConvertImeAdpcmToPcm()
        {
            var driver = AcmDriver.FindByShortName("Microsoft IMA ADPCM");
            driver.Open();
            try
            {
                foreach (var format in driver.FormatTags
                    .SelectMany(formatTag => driver.GetFormats(formatTag)
                    .Where(format => format.FormatTag == WaveFormatEncoding.DviAdpcm ||
                                     format.FormatTag == WaveFormatEncoding.ImaAdpcm)))
                {
                    // see if we can convert it to 16 bit PCM
                    Debug.WriteLine(String.Format("Converting {0} to PCM", format.WaveFormat));
                    CanCreateConversionStream(format.WaveFormat,
                        new WaveFormat(format.WaveFormat.SampleRate, 16, format.WaveFormat.Channels));
                }
            }
            finally
            {
                driver.Close();
            }
        }

        private void CanCreateConversionStream(WaveFormat inputFormat, WaveFormat outputFormat)
        {
            var inputStream = new NullWaveStream(inputFormat, 10000);
            using (var stream = new WaveFormatConversionStream(
                outputFormat, inputStream))
            {
                var buffer = new byte[stream.WaveFormat.AverageBytesPerSecond];
                var totalRead = 0;
                int bytesRead;
                do
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    totalRead += bytesRead;
                } while (bytesRead > 0);
                Debug.WriteLine(String.Format("Converted {0}", totalRead));
                ClassicAssert.AreEqual(inputStream.Length, inputStream.Position);
            }
        }
    }


}

