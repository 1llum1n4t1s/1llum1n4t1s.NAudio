using System;
using NUnit.Framework;
using NAudio.Wave;
using System.Diagnostics;

namespace NAudioTests.DirectSound
{
    /// <summary>
    /// DirectSound デバイス列挙のテスト。
    /// </summary>
    [TestFixture]
    public class DirectSoundTests
    {
        /// <summary>
        /// DirectSound デバイスを列挙できることを確認する。
        /// </summary>
        [Test]
        [Category("IntegrationTest")]
        public void CanEnumerateDevices()
        {
            foreach(var device in DirectSoundOut.Devices)
            {
                Debug.WriteLine(String.Format("{0} {1} {2}", device.Description, device.ModuleName, device.Guid));
            }
        }
    }
}
