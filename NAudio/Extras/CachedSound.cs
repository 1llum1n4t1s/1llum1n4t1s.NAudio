using System;
using System.Buffers;
using NAudio.Wave;

namespace NAudio.Extras
{
    /// <summary>
    /// Used by AudioPlaybackEngine
    /// </summary>
    public class CachedSound
    {
        /// <summary>
        /// Audio data
        /// </summary>
        public float[] AudioData { get; }

        /// <summary>
        /// Format of the audio
        /// </summary>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// Creates a new CachedSound from a file
        /// </summary>
        public CachedSound(string audioFileName)
        {
            using (var audioFileReader = new AudioFileReader(audioFileName))
            {
                WaveFormat = audioFileReader.WaveFormat;
                var estimatedSamples = (int)(audioFileReader.Length / 4);
                var audioData = new float[estimatedSamples];
                var totalSamplesRead = 0;
                var bufferSize = audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels;
                var readBuffer = ArrayPool<float>.Shared.Rent(bufferSize);
                try
                {
                    int samplesRead;
                    while ((samplesRead = audioFileReader.Read(readBuffer, 0, bufferSize)) > 0)
                    {
                        if (totalSamplesRead + samplesRead > audioData.Length)
                        {
                            var newSize = Math.Max(audioData.Length * 2, totalSamplesRead + samplesRead);
                            var newArray = new float[newSize];
                            Array.Copy(audioData, 0, newArray, 0, totalSamplesRead);
                            audioData = newArray;
                        }
                        Array.Copy(readBuffer, 0, audioData, totalSamplesRead, samplesRead);
                        totalSamplesRead += samplesRead;
                    }
                }
                finally
                {
                    ArrayPool<float>.Shared.Return(readBuffer);
                }
                if (totalSamplesRead < audioData.Length)
                {
                    var trimmed = new float[totalSamplesRead];
                    Array.Copy(audioData, 0, trimmed, 0, totalSamplesRead);
                    audioData = trimmed;
                }
                AudioData = audioData;
            }
        }
    }
}