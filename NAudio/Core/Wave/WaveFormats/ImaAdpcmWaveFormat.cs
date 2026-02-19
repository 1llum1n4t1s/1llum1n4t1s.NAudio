using System.Runtime.InteropServices;

namespace NAudio.Wave
{
    /// <summary>
    /// IMA/DVI ADPCM Wave Format
    /// Work in progress
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public class ImaAdpcmWaveFormat : WaveFormat
    {
        short samplesPerBlock;

        /// <summary>
        /// parameterless constructor for Marshalling
        /// </summary>
        ImaAdpcmWaveFormat()
        {
        }

        /// <summary>
        /// Creates a new IMA / DVI ADPCM Wave Format
        /// </summary>
        /// <param name="sampleRate">Sample Rate</param>
        /// <param name="channels">Number of channels</param>
        /// <param name="bitsPerSample">Bits Per Sample</param>
        public ImaAdpcmWaveFormat(int sampleRate, int channels, int bitsPerSample)
        {
            this.waveFormatTag = WaveFormatEncoding.DviAdpcm; // can also be ImaAdpcm - they are the same
            this.sampleRate = sampleRate;
            this.channels = (short)channels;
            this.bitsPerSample = (short)bitsPerSample;
            this.extraSize = 2;
            // Standard IMA ADPCM block size: 256 bytes per channel for 4-bit
            this.blockAlign = (short)(256 * channels);
            // Samples per block: 4 byte header per channel gives 1 initial sample,
            // remaining bytes hold 2 samples each (4 bits per sample)
            this.samplesPerBlock = (short)((((blockAlign - (4 * channels)) * 2) / channels) + 1);
            this.averageBytesPerSecond = (this.sampleRate * blockAlign) / samplesPerBlock;
        }
    }
}
