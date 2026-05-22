using System;
using System.Collections.Generic;
using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Allows any number of inputs to be patched to outputs
    /// Uses could include swapping left and right channels, turning mono into stereo,
    /// feeding different input sources to different soundcard outputs etc
    /// </summary>
    public class MultiplexingSampleProvider : ISampleProvider, IDisposable
    {
        private readonly IList<ISampleProvider> inputs;
        private readonly WaveFormat waveFormat;
        private readonly int outputChannelCount;
        private readonly int inputChannelCount;
        private readonly List<int> mappings;

        /// <summary>
        /// Creates a multiplexing sample provider, allowing re-patching of input channels to different
        /// output channels
        /// </summary>
        /// <param name="inputs">Input sample providers. Must all be of the same sample rate, but can have any number of channels</param>
        /// <param name="numberOfOutputChannels">Desired number of output channels.</param>
        public MultiplexingSampleProvider(IEnumerable<ISampleProvider> inputs, int numberOfOutputChannels)
        {
            this.inputs = new List<ISampleProvider>(inputs);
            outputChannelCount = numberOfOutputChannels;

            if (this.inputs.Count == 0)
            {
                throw new ArgumentException("You must provide at least one input");
            }
            if (numberOfOutputChannels < 1)
            {
                throw new ArgumentException("You must provide at least one output");
            }
            foreach (var input in this.inputs)
            {
                if (waveFormat == null)
                {
                    if (input.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                    {
                        throw new ArgumentException("Only 32 bit float is supported");
                    }
                    waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(input.WaveFormat.SampleRate, numberOfOutputChannels);
                }
                else
                {
                    if (input.WaveFormat.BitsPerSample != waveFormat.BitsPerSample)
                    {
                        throw new ArgumentException("All inputs must have the same bit depth");
                    }
                    if (input.WaveFormat.SampleRate != waveFormat.SampleRate)
                    {
                        throw new ArgumentException("All inputs must have the same sample rate");
                    }
                }
                inputChannelCount += input.WaveFormat.Channels;
            }

            mappings = new List<int>();
            for (var n = 0; n < outputChannelCount; n++)
            {
                mappings.Add(n % inputChannelCount);
            }
        }

        /// <summary>
        /// persistent temporary buffer to prevent creating work for garbage collector
        /// </summary>
        private float[] inputBuffer;

        /// <summary>
        /// Reads samples from this sample provider
        /// </summary>
        /// <param name="buffer">Buffer to be filled with sample data</param>
        /// <param name="offset">Offset into buffer to start writing to, usually 0</param>
        /// <param name="count">Number of samples required</param>
        /// <returns>Number of samples read</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            var sampleFramesRequested = count / outputChannelCount;
            var inputOffset = 0;
            var sampleFramesRead = 0;

            // Pre-zero the output region to avoid per-channel tail clearing
            Array.Fill(buffer, 0f, offset, count);

            // now we must read from all inputs, even if we don't need their data, so they stay in sync
            foreach (var input in inputs)
            {
                var samplesRequired = sampleFramesRequested * input.WaveFormat.Channels;
                inputBuffer = BufferHelpers.EnsurePooled(inputBuffer, samplesRequired);
                var samplesRead = input.Read(inputBuffer, 0, samplesRequired);
                sampleFramesRead = Math.Max(sampleFramesRead, samplesRead / input.WaveFormat.Channels);

                // 各出力チャンネルについて、マップ元がこの入力プロバイダの範囲内なら一括コピーする。
                // 旧実装は入力ch×出力chの二重ループで mappings を逆引きしていたが、出力ch基準にして
                // 入力ch側の線形スキャンを除去した (O(inputCh×outputCh) → O(outputCh))。挙動は等価。
                var inputChannels = input.WaveFormat.Channels;
                for (var outputIndex = 0; outputIndex < outputChannelCount; outputIndex++)
                {
                    var globalInputIndex = mappings[outputIndex];
                    if (globalInputIndex < inputOffset || globalInputIndex >= inputOffset + inputChannels)
                        continue;
                    var inputBufferOffset = globalInputIndex - inputOffset;
                    var outputBufferOffset = offset + outputIndex;
                    while (inputBufferOffset < samplesRead)
                    {
                        buffer[outputBufferOffset] = inputBuffer[inputBufferOffset];
                        outputBufferOffset += outputChannelCount;
                        inputBufferOffset += inputChannels;
                    }
                }
                inputOffset += input.WaveFormat.Channels;
            }

            return sampleFramesRead * outputChannelCount;
        }

        /// <summary>
        /// The output WaveFormat for this SampleProvider
        /// </summary>
        public WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Connects a specified input channel to an output channel
        /// </summary>
        /// <param name="inputChannel">Input Channel index (zero based). Must be less than InputChannelCount</param>
        /// <param name="outputChannel">Output Channel index (zero based). Must be less than OutputChannelCount</param>
        public void ConnectInputToOutput(int inputChannel, int outputChannel)
        {
            if (inputChannel < 0 || inputChannel >= InputChannelCount)
            {
                throw new ArgumentException("Invalid input channel");
            }
            if (outputChannel < 0 || outputChannel >= OutputChannelCount)
            {
                throw new ArgumentException("Invalid output channel");
            }
            mappings[outputChannel] = inputChannel;
        }

        /// <summary>
        /// The number of input channels. Note that this is not the same as the number of input wave providers. If you pass in
        /// one stereo and one mono input provider, the number of input channels is three.
        /// </summary>
        public int InputChannelCount => inputChannelCount;

        /// <summary>
        /// The number of output channels, as specified in the constructor.
        /// </summary>
        public int OutputChannelCount => outputChannelCount;

        /// <summary>
        /// Disposes this sample provider, returning pooled buffers
        /// </summary>
        public void Dispose()
        {
            BufferHelpers.ReturnPooled(inputBuffer);
            inputBuffer = null;
        }
    }
}
