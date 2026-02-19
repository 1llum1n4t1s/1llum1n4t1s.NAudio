using System;
using System.Runtime.CompilerServices;
using NAudio.Wave.Asio;

namespace NAudio.Wave
{
    /// <summary>
    /// Raised when ASIO data has been recorded.
    /// It is important to handle this as quickly as possible as it is in the buffer callback
    /// </summary>
    public class AsioAudioAvailableEventArgs : EventArgs
    {
        // Pre-computed reciprocals to replace divisions with multiplications in hot path
        private const float Int32ToFloatScale = 1.0f / (int.MaxValue + 1f);
        private const float Int16ToFloatScale = 1.0f / (short.MaxValue + 1f);
        private const float Int24ToFloatScale = 1.0f / (1 << 23);
        /// <summary>
        /// Initialises a new instance of AsioAudioAvailableEventArgs
        /// </summary>
        /// <param name="inputBuffers">Pointers to the ASIO buffers for each channel</param>
        /// <param name="outputBuffers">Pointers to the ASIO buffers for each channel</param>
        /// <param name="samplesPerBuffer">Number of samples in each buffer</param>
        /// <param name="asioSampleType">Audio format within each buffer</param>
        public AsioAudioAvailableEventArgs(IntPtr[] inputBuffers, IntPtr[] outputBuffers, int samplesPerBuffer, AsioSampleType asioSampleType)
        {
            InputBuffers = inputBuffers;
            OutputBuffers = outputBuffers;
            SamplesPerBuffer = samplesPerBuffer;
            AsioSampleType = asioSampleType;
        }

        /// <summary>
        /// Pointer to a buffer per input channel
        /// </summary>
        public IntPtr[] InputBuffers { get; private set; }

        /// <summary>
        /// Pointer to a buffer per output channel
        /// Allows you to write directly to the output buffers
        /// If you do so, set SamplesPerBuffer = true,
        /// and make sure all buffers are written to with valid data
        /// </summary>
        public IntPtr[] OutputBuffers { get; private set; }

        /// <summary>
        /// Set to true if you have written to the output buffers
        /// If so, AsioOut will not read from its source
        /// </summary>
        public bool WrittenToOutputBuffers { get; set; }

        /// <summary>
        /// Number of samples in each buffer
        /// </summary>
        public int SamplesPerBuffer { get; private set; }

        /// <summary>
        /// Converts all the recorded audio into a buffer of 32 bit floating point samples, interleaved by channel
        /// </summary>
        /// <samples>The samples as 32 bit floating point, interleaved</samples>
        public int GetAsInterleavedSamples(float[] samples)
        {
            var channels = InputBuffers.Length;
            var samplesPerBuffer = SamplesPerBuffer;
            var totalSamples = samplesPerBuffer * channels;
            if (samples.Length < totalSamples) throw new ArgumentException("Buffer not big enough");
            var index = 0;
            unsafe
            {
                if (AsioSampleType == AsioSampleType.Int32LSB)
                {
                    for (var n = 0; n < samplesPerBuffer; n++)
                    {
                        for (var ch = 0; ch < channels; ch++)
                        {
                            samples[index++] = *((int*)InputBuffers[ch] + n) * Int32ToFloatScale;
                        }
                    }
                }
                else if (AsioSampleType == AsioSampleType.Int16LSB)
                {
                    for (var n = 0; n < samplesPerBuffer; n++)
                    {
                        for (var ch = 0; ch < channels; ch++)
                        {
                            samples[index++] = *((short*)InputBuffers[ch] + n) * Int16ToFloatScale;
                        }
                    }
                }
                else if (AsioSampleType == AsioSampleType.Int24LSB)
                {
                    for (var n = 0; n < samplesPerBuffer; n++)
                    {
                        for (var ch = 0; ch < channels; ch++)
                        {
                            var pSample = ((byte*)InputBuffers[ch] + n * 3);
                            var sample = pSample[0] | (pSample[1] << 8) | ((sbyte)pSample[2] << 16);
                            samples[index++] = sample * Int24ToFloatScale;
                        }
                    }
                }
                else if (AsioSampleType == AsioSampleType.Float32LSB)
                {
                    for (var n = 0; n < samplesPerBuffer; n++)
                    {
                        for (var ch = 0; ch < channels; ch++)
                        {
                            samples[index++] = *((float*)InputBuffers[ch] + n);
                        }
                    }
                }
                else
                {
                    throw new NotImplementedException($"ASIO Sample Type {AsioSampleType} not supported");
                }
            }
            return totalSamples;
        }

        /// <summary>
        /// Audio format within each buffer
        /// Most commonly this will be one of, Int32LSB, Int16LSB, Int24LSB or Float32LSB
        /// </summary>
        public AsioSampleType AsioSampleType { get; private set; }

        /// <summary>
        /// Gets as interleaved samples, allocating a float array
        /// </summary>
        /// <returns>The samples as 32 bit floating point values</returns>
        [Obsolete("Better performance if you use the overload that takes an array, and reuse the same one")]
        public float[] GetAsInterleavedSamples()
        {
            var channels = InputBuffers.Length;
            var samples = new float[SamplesPerBuffer*channels];
            GetAsInterleavedSamples(samples);
            return samples;
        }
    }
}
