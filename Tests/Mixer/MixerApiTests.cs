using NAudio.Mixer;
using NAudio.Wave;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Diagnostics;

namespace NAudioTests
{
    /// <summary>
    /// ミキサー API（デバイス列挙・WaveIn 関連）のテスト。
    /// </summary>
    [TestFixture]
    [Category("IntegrationTest")]
    public class MixerApiTests
    {
        /// <summary>
        /// 全ミキサーデバイスのコントロールを列挙できることを確認する。
        /// </summary>
        [Test]
        public void CanEnumerateAllMixerControls()
        {
            var devices = Mixer.NumberOfDevices;
            ClassicAssert.That(devices > 0, "Expected at least one mixer device");
            for (var device = 0; device < devices; device++)
            {
                ExploreMixerDevice(device);
                Debug.WriteLine("");
            }
        }

        /// <summary>
        /// デフォルト WaveIn のミキサーおよびマイクボリュームを取得できることを確認する。
        /// </summary>
        [Test]
        public void CanFindDefaultWaveIn()
        {
            var defaultWaveInMixerId = MixerLine.GetMixerIdForWaveIn(0);
            var mixer = new Mixer(defaultWaveInMixerId);
            foreach (var destination in mixer.Destinations)
            {
                Debug.WriteLine($"DESTINATION: {destination.Name} {destination.TypeDescription} (Type: {destination.ComponentType}, Target: {destination.TargetName})");

                if (destination.ComponentType == MixerLineComponentType.DestinationWaveIn)
                {
                    foreach (var source in destination.Sources)
                    {
                        Debug.WriteLine($"{source.Name} {source.TypeDescription} (Source: {source.IsSource}, Target: {source.TargetName})");
                        if (source.ComponentType == MixerLineComponentType.SourceMicrophone)
                        {
                            Debug.WriteLine($"Found the microphone: {source.Name}");
                            foreach (var control in source.Controls)
                            {
                                if (control.ControlType == MixerControlType.Volume)
                                {
                                    Debug.WriteLine($"Volume Found: {control}");
                                    var umc = (UnsignedMixerControl)control;
                                    var originalValue = umc.Value;
                                    umc.Value = umc.MinValue;
                                    ClassicAssert.AreEqual(umc.MinValue, umc.Value, "Set Minimum Correctly");
                                    umc.Value = umc.MaxValue;
                                    ClassicAssert.AreEqual(umc.MaxValue, umc.Value, "Set Maximum Correctly");
                                    umc.Value = umc.MaxValue / 2;
                                    ClassicAssert.AreEqual(umc.MaxValue / 2, umc.Value, "Set MidPoint Correctly");
                                    umc.Value = originalValue;
                                    ClassicAssert.AreEqual(originalValue, umc.Value, "Set Original Correctly");
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// WaveIn からミキサーラインを取得できることを確認する。
        /// </summary>
        [Test]
        public void CanGetWaveInMixerLine()
        {
            using (var waveIn = new WaveInEvent())
            {
                var line = waveIn.GetMixerLine();                
                //Debug.WriteLine(String.Format("Mic Level {0}", level));
            }
        }

        private static void ExploreMixerDevice(int deviceIndex)
        {
            var mixer = new Mixer(deviceIndex);
            Debug.WriteLine($"Device {deviceIndex}: {mixer.Name}");
            Debug.WriteLine("--------------------------------------------");
            var destinations = mixer.DestinationCount;
            ClassicAssert.That(destinations > 0, "Expected at least one destination");
            for (var destinationIndex = 0; destinationIndex < destinations; destinationIndex++)
            {
                ExploreMixerDestination(mixer, destinationIndex);
            }
        }

        private static void ExploreMixerDestination(Mixer mixer, int destinationIndex)
        {
            var destination = mixer.GetDestination(destinationIndex);
            Debug.WriteLine($"Destination {destinationIndex}: {destination} ({destination.Channels})");
            foreach (var control in destination.Controls)
            {
                Debug.WriteLine($"CONTROL: {control}");
            }
            var sources = destination.SourceCount;
            for (var sourceIndex = 0; sourceIndex < sources; sourceIndex++)
            {
                ExploreMixerSource(destination, sourceIndex);
            }
        }

        private static void ExploreMixerSource(MixerLine destinationLine, int sourceIndex)
        {
            var sourceLine = destinationLine.GetSource(sourceIndex);
            Debug.WriteLine($"Source {sourceIndex}: {sourceLine}");
            foreach (var control in sourceLine.Controls)
            {
                Debug.WriteLine($"CONTROL: {control}");
            }
        }
    }
}
