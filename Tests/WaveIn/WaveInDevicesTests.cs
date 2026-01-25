using System;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using NAudio.Wave;

namespace NAudioTests
{
    /// <summary>
    /// WaveIn / WaveOut デバイス数・ケーパビリティ・レジストリ名のテスト。
    /// </summary>
    [TestFixture]
    [Category("IntegrationTest")]
    public class WaveInDevicesTests
    {
        /// <summary>
        /// WaveIn デバイス数を取得できることを確認する。
        /// </summary>
        [Test]
        public void CanRequestNumberOfWaveInDevices()
        {
            var deviceCount = WaveIn.DeviceCount;
            ClassicAssert.That(deviceCount > 0, "Expected at least one WaveIn device");
        }

        /// <summary>
        /// 各 WaveIn デバイスのケーパビリティを取得できることを確認する。
        /// </summary>
        [Test]
        public void CanGetWaveInDeviceCapabilities()
        {
            for (var n = 0; n < WaveIn.DeviceCount; n++)
            {
                var capabilities = WaveIn.GetCapabilities(n);
                ClassicAssert.IsNotNull(capabilities, "Null capabilities");
                //ClassicAssert.That(capabilities.Channels >= 1, "At least one channel"); - seem to get -1 a lot
                ClassicAssert.That(!String.IsNullOrEmpty(capabilities.ProductName), "Needs a name");
            }
        }

        /// <summary>
        /// WaveIn の Caps2 名をレジストリから取得できることを確認する。
        /// </summary>
        [Test]
        public void CanGetWaveInCaps2NamesFromRegistry()
        {
            for (var n = 0; n < WaveIn.DeviceCount; n++)
            {
                var capabilities = WaveIn.GetCapabilities(n);
                Console.WriteLine("PName:        {0}", capabilities.ProductName);
                Console.WriteLine("Name:         {0} {1}", capabilities.NameGuid, WaveCapabilitiesHelpers.GetNameFromGuid(capabilities.NameGuid));
                Console.WriteLine("Product:      {0} {1}", capabilities.ProductGuid, WaveCapabilitiesHelpers.GetNameFromGuid(capabilities.ProductGuid));
                Console.WriteLine("Manufacturer: {0} {1}", capabilities.ManufacturerGuid, WaveCapabilitiesHelpers.GetNameFromGuid(capabilities.ManufacturerGuid));
            }
        }


        /// <summary>
        /// WaveOut の Caps2 名をレジストリから取得できることを確認する。
        /// </summary>
        [Test]
        public void CanGetWaveOutCaps2NamesFromRegistry()
        {
            for (var n = 0; n < WaveOut.DeviceCount; n++)
            {
                var capabilities = WaveOut.GetCapabilities(n);
                Console.WriteLine("PName:        {0}", capabilities.ProductName);
                Console.WriteLine("Name:         {0} {1}", capabilities.NameGuid, WaveCapabilitiesHelpers.GetNameFromGuid(capabilities.NameGuid));
                Console.WriteLine("Product:      {0} {1}", capabilities.ProductGuid, WaveCapabilitiesHelpers.GetNameFromGuid(capabilities.ProductGuid));
                Console.WriteLine("Manufacturer: {0} {1}", capabilities.ManufacturerGuid, WaveCapabilitiesHelpers.GetNameFromGuid(capabilities.ManufacturerGuid));
            }
        }
    }
}
