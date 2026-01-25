using System;
using NUnit.Framework;
using NAudio.Dmo;
using System.Diagnostics;

namespace NAudioTests.Dmo
{
    /// <summary>
    /// DMO オーディオエフェクト・エンコーダ・デコーダ列挙のテスト。
    /// </summary>
    [TestFixture]
    public class DmoTests
    {
        /// <summary>
        /// オーディオエフェクト DMO を列挙できることを確認する。
        /// </summary>
        [Test]
        [Category("IntegrationTest")]
        public void CanEnumerateAudioEffects()
        {
            Debug.WriteLine("Audio Effects:");
            foreach (var dmo in DmoEnumerator.GetAudioEffectNames())
            {
                Debug.WriteLine(string.Format("{0} {1}", dmo.Name, dmo.Clsid));
                var mediaObject = Activator.CreateInstance(Type.GetTypeFromCLSID(dmo.Clsid));
            }
        }

        /// <summary>
        /// オーディオエンコーダ DMO を列挙できることを確認する。
        /// </summary>
        [Test]
        [Category("IntegrationTest")]
        public void CanEnumerateAudioEncoders()
        {
            Debug.WriteLine("Audio Encoders:");
            foreach (var dmo in DmoEnumerator.GetAudioEncoderNames())
            {
                Debug.WriteLine(string.Format("{0} {1}", dmo.Name, dmo.Clsid));
            }
        }

        /// <summary>
        /// オーディオデコーダ DMO を列挙できることを確認する。
        /// </summary>
        [Test]
        [Category("IntegrationTest")]
        public void CanEnumerateAudioDecoders()
        {
            Debug.WriteLine("Audio Decoders:");
            foreach (var dmo in DmoEnumerator.GetAudioDecoderNames())
            {
                Debug.WriteLine(string.Format("{0} {1}", dmo.Name, dmo.Clsid));
            }
        }
    }
}

