using System;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;
using NAudioTests.Utils;

namespace NAudioTests.Wasapi
{
    /// <summary>
    /// WASAPI AudioClient の初期化・フォーマット・バッファ・キャプチャのテスト。
    /// </summary>
    [TestFixture]
    [Category("IntegrationTest")]
    public class AudioClientTests
    {
        /// <summary>
        /// テスト実行前に Vista 以上であることを要求する。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            OSUtils.RequireVista();
        }

        /// <summary>
        /// MixFormat を取得できることを確認する。
        /// </summary>
        [Test]
        public void CanGetMixFormat()
        {
            // don't need to initialize before asking for MixFormat
            Debug.WriteLine(String.Format("Mix Format: {0}", GetAudioClient().MixFormat));
        }

        /// <summary>
        /// 共有モードで初期化できることを確認する。
        /// </summary>
        [Test]
        public void CanInitializeInSharedMode()
        {
            InitializeClient(AudioClientShareMode.Shared);
        }

        /// <summary>
        /// 排他モードで初期化できることを確認する。
        /// </summary>
        [Test]
        public void CanInitializeInExclusiveMode()
        {
            using (var audioClient = GetAudioClient())
            {
                var waveFormat = new WaveFormat(48000, 16, 2); //audioClient.MixFormat;
                long refTimesPerSecond = 10000000;
                audioClient.Initialize(AudioClientShareMode.Exclusive,
                    AudioClientStreamFlags.None,
                    refTimesPerSecond / 10,
                    0,
                    waveFormat,
                    Guid.Empty);
            }
        }

        /// <summary>
        /// AudioRenderClient を取得できることを確認する。
        /// </summary>
        [Test]
        public void CanGetAudioRenderClient()
        {
            ClassicAssert.IsNotNull(InitializeClient(AudioClientShareMode.Shared).AudioRenderClient);
        }


        /// <summary>
        /// BufferSize を取得できることを確認する。
        /// </summary>
        [Test]
        public void CanGetBufferSize()
        {
            Debug.WriteLine(String.Format("Buffer Size: {0}", InitializeClient(AudioClientShareMode.Shared).BufferSize));
        }

        /// <summary>
        /// CurrentPadding を取得できることを確認する。
        /// </summary>
        [Test]
        public void CanGetCurrentPadding()
        {
            Debug.WriteLine(String.Format("CurrentPadding: {0}", InitializeClient(AudioClientShareMode.Shared).CurrentPadding));
        }

        /// <summary>
        /// DefaultDevicePeriod を取得できることを確認する。
        /// </summary>
        [Test]
        public void CanGetDefaultDevicePeriod()
        {
            // should not need initialization
            Debug.WriteLine(String.Format("DefaultDevicePeriod: {0}", GetAudioClient().DefaultDevicePeriod));
        }

        /// <summary>
        /// MinimumDevicePeriod を取得できることを確認する。
        /// </summary>
        [Test]
        public void CanGetMinimumDevicePeriod()
        {
            // should not need initialization
            Debug.WriteLine(String.Format("MinimumDevicePeriod: {0}", GetAudioClient().MinimumDevicePeriod));
        }

        /// <summary>
        /// デフォルトフォーマットが共有モードでサポートされることを確認する。
        /// </summary>
        [Test]
        public void DefaultFormatIsSupportedInSharedMode()
        {
            var client = GetAudioClient();
            var defaultFormat = client.MixFormat;
            ClassicAssert.IsTrue(client.IsFormatSupported(AudioClientShareMode.Shared, defaultFormat), "Is Format Supported");
        }

        /* strange as this may seem, WASAPI doesn't seem to like the default format in exclusive mode
         * it prefers 16 bit (presumably 24 bit on some devices)
        [Test]
        public void DefaultFormatIsSupportedInExclusiveMode()
        {
            AudioClient client = GetAudioClient();
            WaveFormat defaultFormat = client.MixFormat;
            ClassicAssert.IsTrue(client.IsFormatSupported(AudioClientShareMode.Exclusive, defaultFormat), "Is Format Supported");
        }*/


        /// <summary>
        /// 44.1kHz Extensible が共有モードでサポート問い合わせできることを確認する。
        /// </summary>
        [Test]
        public void CanRequestIfFormatIsSupportedExtensible44100SharedMode()
        {
            var desiredFormat = new WaveFormatExtensible(44100, 32, 2);
            Debug.WriteLine(desiredFormat);
            GetAudioClient().IsFormatSupported(AudioClientShareMode.Shared, desiredFormat);
        }

        /// <summary>
        /// 44.1kHz Extensible が排他モードでサポート問い合わせできることを確認する。
        /// </summary>
        [Test]
        public void CanRequestIfFormatIsSupportedExtensible44100ExclusiveMode()
        {
            var desiredFormat = new WaveFormatExtensible(44100, 32, 2);
            Debug.WriteLine(desiredFormat);
            GetAudioClient().IsFormatSupported(AudioClientShareMode.Exclusive, desiredFormat);
        }

        /// <summary>
        /// 48kHz Extensible のサポート問い合わせができることを確認する。
        /// </summary>
        [Test]
        public void CanRequestIfFormatIsSupportedExtensible48000()
        {
            var desiredFormat = new WaveFormatExtensible(48000, 32, 2);
            Debug.WriteLine(desiredFormat);
            GetAudioClient().IsFormatSupported(AudioClientShareMode.Shared, desiredFormat);
        }

        /// <summary>
        /// 48kHz 16bit Extensible のサポート問い合わせができることを確認する。
        /// </summary>
        [Test]
        public void CanRequestIfFormatIsSupportedExtensible48000_16bit()
        {
            var desiredFormat = new WaveFormatExtensible(48000, 16, 2);
            Debug.WriteLine(desiredFormat);
            GetAudioClient().IsFormatSupported(AudioClientShareMode.Shared, desiredFormat);
        }

        /// <summary>
        /// PCM ステレオのサポート問い合わせができることを確認する。
        /// </summary>
        [Test]
        public void CanRequestIfFormatIsSupportedPCMStereo()
        {
            GetAudioClient().IsFormatSupported(AudioClientShareMode.Shared, new WaveFormat(44100, 16, 2));
        }

        /// <summary>
        /// 8kHz モノのサポート問い合わせができることを確認する。
        /// </summary>
        [Test]
        public void CanRequestIfFormatIsSupported8KHzMono()
        {
            GetAudioClient().IsFormatSupported(AudioClientShareMode.Shared, new WaveFormat(8000, 16, 1));
        }

        /// <summary>
        /// 48kHz 16bit ステレオのサポート問い合わせができることを確認する。
        /// </summary>
        [Test]
        public void CanRequest48kHz16BitStereo()
        {
            GetAudioClient().IsFormatSupported(AudioClientShareMode.Shared, new WaveFormat(48000, 16, 2));

        }

        /// <summary>
        /// 48kHz 16bit モノのサポート問い合わせができることを確認する。
        /// </summary>
        [Test]
        public void CanRequest48kHz16BitMono()
        {
            GetAudioClient().IsFormatSupported(AudioClientShareMode.Shared, new WaveFormat(48000, 16, 1));
        }

        /// <summary>
        /// IEEE float のサポート問い合わせができることを確認する。
        /// </summary>
        [Test]
        public void CanRequestIfFormatIsSupportedIeee()
        {
            GetAudioClient().IsFormatSupported(AudioClientShareMode.Shared, WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
        }

        /// <summary>
        /// バッファを取得してサイレントで解放できることを確認する。
        /// </summary>
        [Test]
        public void CanPopulateABuffer()
        {
            var audioClient = InitializeClient(AudioClientShareMode.Shared);
            var renderClient = audioClient.AudioRenderClient;
            var bufferFrameCount = audioClient.BufferSize;
            var buffer = renderClient.GetBuffer(bufferFrameCount);
            // TODO put some stuff in
            // will tell it it has a silent buffer
            renderClient.ReleaseBuffer(bufferFrameCount, AudioClientBufferFlags.Silent);
        }

        /// <summary>
        /// WasapiCapture でデフォルトデバイスをデフォルトフォーマットでキャプチャできることを確認する。
        /// </summary>
        [Test, MaxTime(2000)]
        public void CanCaptureDefaultDeviceInDefaultFormatUsingWasapiCapture()
        {
            using (var wasapiClient = new WasapiCapture())
            {
                wasapiClient.StartRecording();
                Thread.Sleep(1000);
                wasapiClient.StopRecording();
            }
        }
 
        /// <summary>
        /// WasapiCapture の停止後に再開できることを確認する。
        /// </summary>
        [Test, MaxTime(3000)]
        public void CanReuseWasapiCapture()
        {
            using (var wasapiClient = new WasapiCapture())
            {
                wasapiClient.StartRecording();
                Thread.Sleep(1000);
                wasapiClient.StopRecording();
                Thread.Sleep(1000);
                wasapiClient.StartRecording();
                Console.WriteLine("Disposing");
            }
        } 

        private AudioClient InitializeClient(AudioClientShareMode shareMode)
        {
            var audioClient = GetAudioClient();
            var waveFormat = audioClient.MixFormat;
            long refTimesPerSecond = 10000000;
            audioClient.Initialize(shareMode,
                AudioClientStreamFlags.None,
                refTimesPerSecond,
                0,
                waveFormat,
                Guid.Empty);
            return audioClient;
        }

        private AudioClient GetAudioClient()
        {
            var enumerator = new MMDeviceEnumerator();
            var defaultAudioEndpoint = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            var audioClient = defaultAudioEndpoint.AudioClient;
            ClassicAssert.IsNotNull(audioClient);
            return audioClient;
        }
    
    }
}
