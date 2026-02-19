using System;
using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Takes a stereo input and turns it to mono
    /// </summary>
    public class StereoToMonoSampleProvider : ISampleProvider, IDisposable
    {
        private readonly ISampleProvider sourceProvider;
        private float[] sourceBuffer;

        /// <summary>
        /// Creates a new mono ISampleProvider based on a stereo input
        /// </summary>
        /// <param name="sourceProvider">Stereo 16 bit PCM input</param>
        public StereoToMonoSampleProvider(ISampleProvider sourceProvider)
        {
            LeftVolume = 0.5f;
            RightVolume = 0.5f;
            if (sourceProvider.WaveFormat.Channels != 2)
            {
                throw new ArgumentException("Source must be stereo");
            }
            this.sourceProvider = sourceProvider;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sourceProvider.WaveFormat.SampleRate, 1);
        }

        /// <summary>
        /// 1.0 to mix the mono source entirely to the left channel
        /// </summary>
        public float LeftVolume { get; set; } 

        /// <summary>
        /// 1.0 to mix the mono source entirely to the right channel
        /// </summary>
        public float RightVolume { get; set; }

        /// <summary>
        /// Output Wave Format
        /// </summary>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// Reads bytes from this SampleProvider
        /// </summary>
        public int Read(float[] buffer, int offset, int count)
        {
            var sourceSamplesRequired = count * 2;
            sourceBuffer = BufferHelpers.EnsurePooled(sourceBuffer, sourceSamplesRequired);

            var sourceSamplesRead = sourceProvider.Read(sourceBuffer, 0, sourceSamplesRequired);
            var destOffset = offset;
            var leftVol = LeftVolume;
            var rightVol = RightVolume;
            for (var sourceSample = 0; sourceSample < sourceSamplesRead; sourceSample += 2)
            {
                buffer[destOffset++] = (sourceBuffer[sourceSample] * leftVol) + (sourceBuffer[sourceSample + 1] * rightVol);
            }
            return sourceSamplesRead / 2;
        }

        /// <summary>
        /// Disposes this sample provider, returning pooled buffers
        /// </summary>
        public void Dispose()
        {
            BufferHelpers.ReturnPooled(sourceBuffer);
            sourceBuffer = null;
        }
    }
}