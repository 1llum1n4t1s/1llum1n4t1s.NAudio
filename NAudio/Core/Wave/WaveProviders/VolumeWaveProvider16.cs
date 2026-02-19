using System;
using System.Buffers.Binary;

namespace NAudio.Wave
{
    /// <summary>
    /// Helper class allowing us to modify the volume of a 16 bit stream without converting to IEEE float
    /// </summary>
    public class VolumeWaveProvider16 : IWaveProvider
    {
        private readonly IWaveProvider sourceProvider;
        private float volume;

        /// <summary>
        /// Constructs a new VolumeWaveProvider16
        /// </summary>
        /// <param name="sourceProvider">Source provider, must be 16 bit PCM</param>
        public VolumeWaveProvider16(IWaveProvider sourceProvider)
        {
            this.Volume = 1.0f;
            this.sourceProvider = sourceProvider;
            if (sourceProvider.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
                throw new ArgumentException("Expecting PCM input");
            if (sourceProvider.WaveFormat.BitsPerSample != 16)
                throw new ArgumentException("Expecting 16 bit");
        }

        /// <summary>
        /// Gets or sets volume. 
        /// 1.0 is full scale, 0.0 is silence, anything over 1.0 will amplify but potentially clip
        /// </summary>
        public float Volume
        {
            get { return volume; }
            set { volume = value; }
        }

        /// <summary>
        /// WaveFormat of this WaveProvider
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return sourceProvider.WaveFormat; }
        }

        /// <summary>
        /// Read bytes from this WaveProvider
        /// </summary>
        /// <param name="buffer">Buffer to read into</param>
        /// <param name="offset">Offset within buffer to read to</param>
        /// <param name="count">Bytes desired</param>
        /// <returns>Bytes read</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            // always read from the source
            var bytesRead = sourceProvider.Read(buffer, offset, count);
            if (this.volume == 0.0f)
            {
                Array.Clear(buffer, offset, bytesRead);
            }
            else if (this.volume != 1.0f)
            {
                var span = buffer.AsSpan(offset, bytesRead);
                for (var n = 0; n < bytesRead; n += 2)
                {
                    var sample = BinaryPrimitives.ReadInt16LittleEndian(span.Slice(n));
                    var newSample = (short)Math.Clamp(sample * this.volume, Int16.MinValue, Int16.MaxValue);
                    BinaryPrimitives.WriteInt16LittleEndian(span.Slice(n), newSample);
                }
            }
            return bytesRead;
        }
    }
}
