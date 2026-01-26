using System;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Diagnostics;
using NAudio.Wave.Compression;

namespace NAudioTests.Acm
{
    /// <summary>
    /// ACM ドライバー列挙・検索・オープン／クローズのテスト。
    /// </summary>
    [TestFixture]
    [Category("IntegrationTest")]
    public class AcmDriverTests
    {
        /// <summary>
        /// ACM ドライバーを列挙できることを確認する。
        /// </summary>
        [Test]
        public void CanEnumerateDrivers()
        {
            var drivers = AcmDriver.EnumerateAcmDrivers();
            ClassicAssert.IsNotNull(drivers);
            foreach (var driver in drivers)
            {
                ClassicAssert.GreaterOrEqual((int)driver.DriverId, 0);
                ClassicAssert.IsTrue(!String.IsNullOrEmpty(driver.ShortName));
                Debug.WriteLine(driver.LongName);
            }
        }

        /// <summary>
        /// 存在しないコーデックでは false が返ることを確認する。
        /// </summary>
        [Test]
        public void DoesntFindNonexistentCodec()
        {
            ClassicAssert.IsFalse(AcmDriver.IsCodecInstalled("ASJHASDHJSAK"));
        }

        /// <summary>
        /// 標準コーデック（MS-ADPCM）がインストールされていることを確認する。
        /// </summary>
        [Test]
        public void FindsStandardCodec()
        {
            ClassicAssert.IsTrue(AcmDriver.IsCodecInstalled("MS-ADPCM"));
        }

        /// <summary>
        /// ショート名でドライバーを検索できることを確認する。
        /// </summary>
        [Test]
        public void HasFindByShortNameMethod()
        {
            var driver = AcmDriver.FindByShortName("WM-AUDIO");
        }

        /// <summary>
        /// 各ドライバーをオープン・クローズできることを確認する。
        /// </summary>
        [Test]
        public void CanOpenAndCloseDriver()
        {
            var drivers = AcmDriver.EnumerateAcmDrivers();
            ClassicAssert.IsNotNull(drivers);
            foreach (var driver in drivers)
            {
                driver.Open();
                driver.Close();
            }
        }

        /// <summary>
        /// 各ドライバーのフォーマットタグを列挙できることを確認する。
        /// </summary>
        [Test]
        public void CanEnumerateFormatTags()
        {
            foreach(var driver in AcmDriver.EnumerateAcmDrivers())
            {
                Debug.WriteLine("Enumerating Format Tags for " + driver.LongName);
                driver.Open();
                var formatTags = driver.FormatTags;
                ClassicAssert.IsNotNull(formatTags, "FormatTags");
                foreach(var formatTag in formatTags)
                {
                    Debug.WriteLine(String.Format("{0} {1} {2} Standard formats: {3} Support Flags: {4} Format Size: {5}",
                        formatTag.FormatTagIndex, 
                        formatTag.FormatTag,
                        formatTag.FormatDescription,
                        formatTag.StandardFormatsCount,
                        formatTag.SupportFlags,
                        formatTag.FormatSize));
                }
                driver.Close();
            }
        }

        /// <summary>
        /// フォーマットタグに属するフォーマットを列挙できることを確認する。
        /// </summary>
        [Test]
        public void CanEnumerateFormats()
        {
            using (var driver = AcmDriver.FindByShortName("MS-ADPCM"))
            {
                driver.Open();
                var formatTags = driver.FormatTags;
                ClassicAssert.IsNotNull(formatTags, "FormatTags");
                foreach (var formatTag in formatTags)
                {                                        
                    var formats = driver.GetFormats(formatTag);
                    ClassicAssert.IsNotNull(formats);
                    foreach (var format in formats)
                    {
                        Debug.WriteLine(String.Format("{0} {1} {2} {3} {4}",
                            format.FormatIndex,
                            format.FormatTag,
                            format.FormatDescription,
                            format.WaveFormat,
                            format.SupportFlags));
                    }
                }
            }
        }
    }
}
