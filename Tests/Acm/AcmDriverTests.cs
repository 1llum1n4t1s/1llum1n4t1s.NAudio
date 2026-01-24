using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Diagnostics;
using NAudio.Wave.Compression;

namespace NAudioTests.Acm
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class AcmDriverTests
    {
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

        [Test]
        public void DoesntFindNonexistentCodec()
        {
            ClassicAssert.IsFalse(AcmDriver.IsCodecInstalled("ASJHASDHJSAK"));
        }

        [Test]
        public void FindsStandardCodec()
        {
            ClassicAssert.IsTrue(AcmDriver.IsCodecInstalled("MS-ADPCM"));
        }

        [Test]
        public void HasFindByShortNameMethod()
        {
            var driver = AcmDriver.FindByShortName("WM-AUDIO");
        }

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
