using System;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using NAudio.CoreAudioApi;
using System.Diagnostics;
using NAudioTests.Utils;

namespace NAudioTests.Wasapi
{
    /// <summary>
    /// MMDeviceEnumerator の作成・列挙・デフォルトエンドポイント・オーディオクロックのテスト。
    /// </summary>
    [TestFixture]
    [Category("IntegrationTest")]
    public class MMDeviceEnumeratorTests
    {
        /// <summary>
        /// Vista 以上で MMDeviceEnumerator を生成できることを確認する。
        /// </summary>
        [Test]
        public void CanCreateMMDeviceEnumeratorInVista()
        {
            OSUtils.RequireVista();
            var enumerator = new MMDeviceEnumerator();
        }

        /// <summary>
        /// Vista でオーディオエンドポイントを列挙できることを確認する。
        /// </summary>
        [Test]
        public void CanEnumerateDevicesInVista()
        {
            OSUtils.RequireVista();
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.All);

            foreach (var device in devices)
            {
                if (device.State != DeviceState.NotPresent)
                {
                    Debug.WriteLine(String.Format("{0}, {1}", device.FriendlyName, device.State));
                }
                else
                {
                    Debug.WriteLine(String.Format("{0}, {1}", device.ID, device.State));
                }
            }
        }

        /// <summary>
        /// キャプチャデバイスを列挙できることを確認する。
        /// </summary>
        [Test]
        public void CanEnumerateCaptureDevices()
        {
            OSUtils.RequireVista();
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.All);

            foreach (var device in devices)
            {
                if (device.State != DeviceState.NotPresent)
                {
                    Debug.WriteLine(String.Format("{0}, {1}", device.FriendlyName, device.State));
                }
                else
                {
                    Debug.WriteLine(String.Format("{0}, {1}", device.ID, device.State));
                }
            }
        }

        /// <summary>
        /// デフォルトオーディオエンドポイントを取得できることを確認する。
        /// </summary>
        [Test]
        public void CanGetDefaultAudioEndpoint()
        {
            OSUtils.RequireVista();
            var enumerator = new MMDeviceEnumerator();
            var defaultAudioEndpoint = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            ClassicAssert.IsNotNull(defaultAudioEndpoint);
        }

        /// <summary>
        /// デフォルトエンドポイントから AudioClient をアクティベートできることを確認する。
        /// </summary>
        [Test]
        public void CanActivateDefaultAudioEndpoint()
        {
            OSUtils.RequireVista();
            var enumerator = new MMDeviceEnumerator();
            var defaultAudioEndpoint = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            var audioClient = defaultAudioEndpoint.AudioClient;
            ClassicAssert.IsNotNull(audioClient);
        }

        /// <summary>
        /// XP では MMDeviceEnumerator 生成時に NotSupportedException がスローされることを確認する。
        /// </summary>
        [Test]
        public void ThrowsNotSupportedExceptionInXP()
        {
            OSUtils.RequireXP();
            Assert.Throws<NotSupportedException>(() => new MMDeviceEnumerator());
        }

        /// <summary>
        /// 初期化後に AudioClockClient を取得できることを確認する。
        /// </summary>
        [Test]
        public void CanGetAudioClockClient()
        {
            OSUtils.RequireVista();
            var enumerator = new MMDeviceEnumerator();

            var captureClient = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console).AudioClient;

            var REFTIMES_PER_MILLISEC = 10000;

            captureClient.Initialize(AudioClientShareMode.Shared, AudioClientStreamFlags.None, 
                REFTIMES_PER_MILLISEC * 100, 0, captureClient.MixFormat, Guid.Empty);

            // get AUDCLNT_E_NOT_INITIALIZED if not init    
            
            var clock = captureClient.AudioClockClient;
            Console.WriteLine("Clock Frequency: {0}",clock.Frequency);
            ulong p;
            ulong qpc;
            clock.GetPosition(out p, out qpc);
            Console.WriteLine("Clock Position: {0}:{1}",p,qpc );
            Console.WriteLine("Adjusted Position: {0}", clock.AdjustedPosition);
            Console.WriteLine("Can Adjust Position: {0}", clock.CanAdjustPosition);
            Console.WriteLine("Characteristics: {0}", clock.Characteristics);
            captureClient.Dispose();
        }
    }
}
