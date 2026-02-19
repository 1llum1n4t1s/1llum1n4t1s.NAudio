using System;
using System.Numerics;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Very simple sample provider supporting adjustable gain
    /// </summary>
    public class VolumeSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;

        /// <summary>
        /// Initializes a new instance of VolumeSampleProvider
        /// </summary>
        /// <param name="source">Source Sample Provider</param>
        public VolumeSampleProvider(ISampleProvider source)
        {
            this.source = source;
            Volume = 1.0f;
        }

        /// <summary>
        /// WaveFormat
        /// </summary>
        public WaveFormat WaveFormat => source.WaveFormat;

        /// <summary>
        /// Reads samples from this sample provider
        /// </summary>
        /// <param name="buffer">Sample buffer</param>
        /// <param name="offset">Offset into sample buffer</param>
        /// <param name="sampleCount">Number of samples desired</param>
        /// <returns>Number of samples read</returns>
        public int Read(float[] buffer, int offset, int sampleCount)
        {
            var samplesRead = source.Read(buffer, offset, sampleCount);
            if (Volume != 1f)
            {
                var span = new Span<float>(buffer, offset, samplesRead);
                if (Vector.IsHardwareAccelerated && samplesRead >= Vector<float>.Count)
                {
                    var volVec = new Vector<float>(Volume);
                    var vecSize = Vector<float>.Count;
                    var n = 0;
                    for (; n <= samplesRead - vecSize; n += vecSize)
                    {
                        var v = new Vector<float>(span.Slice(n));
                        (v * volVec).CopyTo(span.Slice(n));
                    }
                    // scalar remainder
                    for (; n < samplesRead; n++)
                    {
                        span[n] *= Volume;
                    }
                }
                else
                {
                    for (var n = 0; n < samplesRead; n++)
                    {
                        span[n] *= Volume;
                    }
                }
            }
            return samplesRead;
        }

        private float volume;

        /// <summary>
        /// Allows adjusting the volume, 1.0f = full volume
        /// </summary>
        public float Volume
        {
            get { return volume; }
            set { volume = Math.Max(0f, value); }
        }
    }
}
