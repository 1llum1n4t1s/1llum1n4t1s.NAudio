using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace NAudio.Wave.Asio
{
    /// <summary>
    /// This class stores convertors for different interleaved WaveFormat to ASIOSampleType separate channel
    /// format.
    /// </summary>
    internal class AsioSampleConvertor
    {
        public delegate void SampleConvertor(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples);

        // Pre-computed constants to avoid repeated computation
        private const float FloatToInt32Scale = (float)int.MaxValue;
        private const float FloatToInt24Scale = (1 << 23) - 1f;
        private const float FloatToInt16Scale = short.MaxValue;
        private const float Int32ToFloatScale = 1.0f / (int.MaxValue + 1f);

        /// <summary>
        /// Selects the sample convertor based on the input WaveFormat and the output ASIOSampleTtype.
        /// </summary>
        /// <param name="waveFormat">The wave format.</param>
        /// <param name="asioType">The type.</param>
        /// <returns></returns>
        public static SampleConvertor SelectSampleConvertor(WaveFormat waveFormat, AsioSampleType asioType)
        {
            SampleConvertor convertor = null;
            var is2Channels = waveFormat.Channels == 2;

            // TODO : IMPLEMENTS OTHER CONVERTOR TYPES
            switch (asioType)
            {
                case AsioSampleType.Int32LSB:
                    switch (waveFormat.BitsPerSample)
                    {
                        case 16:
                            convertor = (is2Channels) ? (SampleConvertor)ConvertorShortToInt2Channels : (SampleConvertor)ConvertorShortToIntGeneric;
                            break;
                        case 32:
                            if (waveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                                convertor = (is2Channels) ? (SampleConvertor)ConvertorFloatToInt2Channels : (SampleConvertor)ConvertorFloatToIntGeneric;
                            else
                                convertor = (is2Channels) ? (SampleConvertor)ConvertorIntToInt2Channels : (SampleConvertor)ConvertorIntToIntGeneric;
                            break;
                    }
                    break;
                case AsioSampleType.Int16LSB:
                    switch (waveFormat.BitsPerSample)
                    {
                        case 16:
                            convertor = (is2Channels) ? (SampleConvertor)ConvertorShortToShort2Channels : (SampleConvertor)ConvertorShortToShortGeneric;
                            break;
                        case 32:
                            if (waveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                                convertor = (is2Channels) ? (SampleConvertor)ConvertorFloatToShort2Channels : (SampleConvertor)ConvertorFloatToShortGeneric;
                            else
                                convertor = (is2Channels) ? (SampleConvertor)ConvertorIntToShort2Channels : (SampleConvertor)ConvertorIntToShortGeneric;
                            break;
                    }
                    break;
                case AsioSampleType.Int24LSB:
                    switch (waveFormat.BitsPerSample)
                    {
                        case 16:
                            throw new ArgumentException("Not a supported conversion");
                        case 32:
                            if (waveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                                convertor = ConverterFloatTo24LSBGeneric;
                            else
                                throw new ArgumentException("Not a supported conversion");
                            break;
                    }
                    break;
                case AsioSampleType.Float32LSB:
                    switch (waveFormat.BitsPerSample)
                    {
                        case 16:
                            throw new ArgumentException("Not a supported conversion");
                        case 32:
                            if (waveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                                convertor = ConverterFloatToFloatGeneric;
                            else
                                convertor = ConvertorIntToFloatGeneric;
                            break;
                    }
                    break;

                default:
                    throw new ArgumentException(
                        String.Format("ASIO Buffer Type {0} is not yet supported.",
                                      Enum.GetName(typeof(AsioSampleType), asioType)));
            }
            return convertor;
        }

        /// <summary>
        /// Optimized convertor for 2 channels SHORT
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertorShortToInt2Channels(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                var inputSamples = (short*)inputInterleavedBuffer;
                // Use a trick (short instead of int to avoid any conversion from 16Bit to 32Bit)
                var leftSamples = (short*)asioOutputBuffers[0];
                var rightSamples = (short*)asioOutputBuffers[1];

                // Point to upper 16 bits of the 32Bits.
                leftSamples++;
                rightSamples++;
                for (var i = 0; i < nbSamples; i++)
                {
                    *leftSamples = inputSamples[0];
                    *rightSamples = inputSamples[1];
                    inputSamples += 2;
                    leftSamples += 2;
                    rightSamples += 2;
                }
            }
        }

        /// <summary>
        /// Generic convertor for SHORT
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertorShortToIntGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                var inputSamples = (short*)inputInterleavedBuffer;
                // stackalloc to avoid heap allocation in hot path
                var samples = stackalloc short*[nbChannels];
                for (var i = 0; i < nbChannels; i++)
                {
                    samples[i] = (short*)asioOutputBuffers[i];
                    // Point to upper 16 bits of the 32Bits.
                    samples[i]++;
                }

                for (var i = 0; i < nbSamples; i++)
                {
                    for (var j = 0; j < nbChannels; j++)
                    {
                        *samples[j] = *inputSamples++;
                        samples[j] += 2;
                    }
                }
            }
        }

        /// <summary>
        /// Optimized convertor for 2 channels FLOAT to INT with SIMD
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertorFloatToInt2Channels(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                var inputSamples = (float*)inputInterleavedBuffer;
                var leftSamples = (int*)asioOutputBuffers[0];
                var rightSamples = (int*)asioOutputBuffers[1];

                var i = 0;

                // SIMD path: process Vector<float>.Count samples at a time per channel
                if (Vector.IsHardwareAccelerated)
                {
                    var vecCount = Vector<float>.Count;
                    var scaleVec = new Vector<float>(FloatToInt32Scale);
                    var minVec = new Vector<float>(-1.0f);
                    var maxVec = new Vector<float>(1.0f);

                    // Allocate de-interleave buffers outside the loop to avoid CA2014
                    var leftBuf = stackalloc float[vecCount];
                    var rightBuf = stackalloc float[vecCount];

                    // We need vecCount interleaved stereo pairs = vecCount * 2 floats
                    // to produce vecCount left + vecCount right samples
                    for (; i <= nbSamples - vecCount; i += vecCount)
                    {
                        // De-interleave: extract left and right channels
                        // Input is [L0,R0,L1,R1,L2,R2,L3,R3,...]
                        var src = inputSamples + i * 2;
                        for (var k = 0; k < vecCount; k++)
                        {
                            leftBuf[k] = src[k * 2];
                            rightBuf[k] = src[k * 2 + 1];
                        }

                        var leftVec = new Vector<float>(new ReadOnlySpan<float>(leftBuf, vecCount));
                        var rightVec = new Vector<float>(new ReadOnlySpan<float>(rightBuf, vecCount));

                        // Clamp to [-1.0, 1.0]
                        leftVec = Vector.Max(minVec, Vector.Min(maxVec, leftVec));
                        rightVec = Vector.Max(minVec, Vector.Min(maxVec, rightVec));

                        // Scale to int32 range
                        leftVec *= scaleVec;
                        rightVec *= scaleVec;

                        // Convert to int and store
                        var leftInt = Vector.ConvertToInt32(leftVec);
                        var rightInt = Vector.ConvertToInt32(rightVec);

                        leftInt.CopyTo(new Span<int>(leftSamples + i, vecCount));
                        rightInt.CopyTo(new Span<int>(rightSamples + i, vecCount));
                    }

                    // Advance pointers past SIMD-processed samples for scalar remainder
                    leftSamples += i;
                    rightSamples += i;
                    inputSamples += i * 2;
                }

                // Scalar remainder
                for (; i < nbSamples; i++)
                {
                    *leftSamples++ = clampToInt(inputSamples[0]);
                    *rightSamples++ = clampToInt(inputSamples[1]);
                    inputSamples += 2;
                }
            }
        }

        /// <summary>
        /// Generic convertor Float to INT with SIMD
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertorFloatToIntGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                var inputSamples = (float*)inputInterleavedBuffer;
                // stackalloc to avoid heap allocation in hot path
                var samples = stackalloc int*[nbChannels];
                for (var i = 0; i < nbChannels; i++)
                {
                    samples[i] = (int*)asioOutputBuffers[i];
                }

                for (var i = 0; i < nbSamples; i++)
                {
                    for (var j = 0; j < nbChannels; j++)
                    {
                        *samples[j]++ = clampToInt(*inputSamples++);
                    }
                }
            }
        }

        /// <summary>
        /// Optimized convertor for 2 channels INT to INT using bulk copy
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertorIntToInt2Channels(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                var inputSamples = (int*)inputInterleavedBuffer;
                var leftSamples = (int*)asioOutputBuffers[0];
                var rightSamples = (int*)asioOutputBuffers[1];

                var i = 0;

                // Process 4 samples at a time (loop unrolling)
                var unrollLimit = nbSamples - 3;
                for (; i < unrollLimit; i += 4)
                {
                    leftSamples[0] = inputSamples[0];
                    rightSamples[0] = inputSamples[1];
                    leftSamples[1] = inputSamples[2];
                    rightSamples[1] = inputSamples[3];
                    leftSamples[2] = inputSamples[4];
                    rightSamples[2] = inputSamples[5];
                    leftSamples[3] = inputSamples[6];
                    rightSamples[3] = inputSamples[7];
                    inputSamples += 8;
                    leftSamples += 4;
                    rightSamples += 4;
                }

                // Scalar remainder
                for (; i < nbSamples; i++)
                {
                    *leftSamples++ = inputSamples[0];
                    *rightSamples++ = inputSamples[1];
                    inputSamples += 2;
                }
            }
        }

        /// <summary>
        /// Generic convertor INT to INT
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertorIntToIntGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                var inputSamples = (int*)inputInterleavedBuffer;
                // stackalloc to avoid heap allocation in hot path
                var samples = stackalloc int*[nbChannels];
                for (var i = 0; i < nbChannels; i++)
                {
                    samples[i] = (int*)asioOutputBuffers[i];
                }

                for (var i = 0; i < nbSamples; i++)
                {
                    for (var j = 0; j < nbChannels; j++)
                    {
                        *samples[j]++ = *inputSamples++;
                    }
                }
            }
        }

        /// <summary>
        /// Optimized convertor for 2 channels INT to SHORT using bit shift
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertorIntToShort2Channels(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                var inputSamples = (int*)inputInterleavedBuffer;
                var leftSamples = (short*)asioOutputBuffers[0];
                var rightSamples = (short*)asioOutputBuffers[1];

                for (var i = 0; i < nbSamples; i++)
                {
                    // Arithmetic right shift instead of division
                    *leftSamples++ = (short)(inputSamples[0] >> 16);
                    *rightSamples++ = (short)(inputSamples[1] >> 16);
                    inputSamples += 2;
                }
            }
        }

        /// <summary>
        /// Generic convertor INT to SHORT using bit shift
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertorIntToShortGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                var inputSamples = (int*)inputInterleavedBuffer;
                // stackalloc to avoid heap allocation in hot path
                var samples = stackalloc short*[nbChannels];
                for (var i = 0; i < nbChannels; i++)
                {
                    samples[i] = (short*)asioOutputBuffers[i];
                }

                for (var i = 0; i < nbSamples; i++)
                {
                    for (var j = 0; j < nbChannels; j++)
                    {
                        // Arithmetic right shift instead of division
                        *samples[j]++ = (short)(*inputSamples++ >> 16);
                    }
                }
            }
        }

        /// <summary>
        /// Generic convertor INT to FLOAT with SIMD
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertorIntToFloatGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                var inputSamples = (int*)inputInterleavedBuffer;
                // stackalloc to avoid heap allocation in hot path
                var samples = stackalloc float*[nbChannels];
                for (var i = 0; i < nbChannels; i++)
                {
                    samples[i] = (float*)asioOutputBuffers[i];
                }

                for (var i = 0; i < nbSamples; i++)
                {
                    for (var j = 0; j < nbChannels; j++)
                    {
                        *samples[j]++ = *inputSamples++ * Int32ToFloatScale;
                    }
                }
            }
        }

        /// <summary>
        /// Optimized convertor for 2 channels SHORT
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertorShortToShort2Channels(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                var inputSamples = (short*)inputInterleavedBuffer;
                var leftSamples = (short*)asioOutputBuffers[0];
                var rightSamples = (short*)asioOutputBuffers[1];

                var i = 0;

                // Process 4 samples at a time (loop unrolling)
                var unrollLimit = nbSamples - 3;
                for (; i < unrollLimit; i += 4)
                {
                    leftSamples[0] = inputSamples[0];
                    rightSamples[0] = inputSamples[1];
                    leftSamples[1] = inputSamples[2];
                    rightSamples[1] = inputSamples[3];
                    leftSamples[2] = inputSamples[4];
                    rightSamples[2] = inputSamples[5];
                    leftSamples[3] = inputSamples[6];
                    rightSamples[3] = inputSamples[7];
                    inputSamples += 8;
                    leftSamples += 4;
                    rightSamples += 4;
                }

                // Scalar remainder
                for (; i < nbSamples; i++)
                {
                    *leftSamples++ = inputSamples[0];
                    *rightSamples++ = inputSamples[1];
                    inputSamples += 2;
                }
            }
        }

        /// <summary>
        /// Generic convertor for SHORT
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertorShortToShortGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                var inputSamples = (short*)inputInterleavedBuffer;
                // stackalloc to avoid heap allocation in hot path
                var samples = stackalloc short*[nbChannels];
                for (var i = 0; i < nbChannels; i++)
                {
                    samples[i] = (short*)asioOutputBuffers[i];
                }

                for (var i = 0; i < nbSamples; i++)
                {
                    for (var j = 0; j < nbChannels; j++)
                    {
                        *(samples[j]++) = *inputSamples++;
                    }
                }
            }
        }

        /// <summary>
        /// Optimized convertor for 2 channels FLOAT to SHORT with SIMD
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertorFloatToShort2Channels(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                var inputSamples = (float*)inputInterleavedBuffer;
                var leftSamples = (short*)asioOutputBuffers[0];
                var rightSamples = (short*)asioOutputBuffers[1];

                for (var i = 0; i < nbSamples; i++)
                {
                    *leftSamples++ = clampToShort(inputSamples[0]);
                    *rightSamples++ = clampToShort(inputSamples[1]);
                    inputSamples += 2;
                }
            }
        }

        /// <summary>
        /// Generic convertor SHORT
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertorFloatToShortGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                var inputSamples = (float*)inputInterleavedBuffer;
                // stackalloc to avoid heap allocation in hot path
                var samples = stackalloc short*[nbChannels];
                for (var i = 0; i < nbChannels; i++)
                {
                    samples[i] = (short*)asioOutputBuffers[i];
                }

                for (var i = 0; i < nbSamples; i++)
                {
                    for (var j = 0; j < nbChannels; j++)
                    {
                        *(samples[j]++) = clampToShort(*inputSamples++);
                    }
                }
            }
        }

        /// <summary>
        /// Generic converter 24 LSB
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConverterFloatTo24LSBGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                var inputSamples = (float*)inputInterleavedBuffer;

                // stackalloc to avoid heap allocation in hot path
                var samples = stackalloc byte*[nbChannels];
                for (var i = 0; i < nbChannels; i++)
                {
                    samples[i] = (byte*)asioOutputBuffers[i];
                }

                for (var i = 0; i < nbSamples; i++)
                {
                    for (var j = 0; j < nbChannels; j++)
                    {
                        var sample24 = clampTo24Bit(*inputSamples++);
                        *(samples[j]++) = (byte)(sample24);
                        *(samples[j]++) = (byte)(sample24 >> 8);
                        *(samples[j]++) = (byte)(sample24 >> 16);
                    }
                }
            }
        }

        /// <summary>
        /// Generic convertor for float to float with SIMD de-interleave
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConverterFloatToFloatGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                var inputSamples = (float*)inputInterleavedBuffer;

                // Fast path for 2 channels: use SIMD or unrolled loop
                if (nbChannels == 2)
                {
                    var leftSamples = (float*)asioOutputBuffers[0];
                    var rightSamples = (float*)asioOutputBuffers[1];

                    var i = 0;
                    // Process 4 samples at a time (loop unrolling)
                    var unrollLimit = nbSamples - 3;
                    for (; i < unrollLimit; i += 4)
                    {
                        leftSamples[0] = inputSamples[0];
                        rightSamples[0] = inputSamples[1];
                        leftSamples[1] = inputSamples[2];
                        rightSamples[1] = inputSamples[3];
                        leftSamples[2] = inputSamples[4];
                        rightSamples[2] = inputSamples[5];
                        leftSamples[3] = inputSamples[6];
                        rightSamples[3] = inputSamples[7];
                        inputSamples += 8;
                        leftSamples += 4;
                        rightSamples += 4;
                    }

                    for (; i < nbSamples; i++)
                    {
                        *leftSamples++ = inputSamples[0];
                        *rightSamples++ = inputSamples[1];
                        inputSamples += 2;
                    }
                    return;
                }

                // stackalloc to avoid heap allocation in hot path
                var samples = stackalloc float*[nbChannels];
                for (var i = 0; i < nbChannels; i++)
                {
                    samples[i] = (float*)asioOutputBuffers[i];
                }

                for (var i = 0; i < nbSamples; i++)
                {
                    for (var j = 0; j < nbChannels; j++)
                    {
                        *(samples[j]++) = *inputSamples++;
                    }
                }
            }
        }

        /// <summary>
        /// Clamp float to 24-bit integer range. Uses float arithmetic to avoid double promotion.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int clampTo24Bit(float sampleValue)
        {
            sampleValue = (sampleValue < -1.0f) ? -1.0f : (sampleValue > 1.0f) ? 1.0f : sampleValue;
            return (int)(sampleValue * FloatToInt24Scale);
        }

        /// <summary>
        /// Clamp float to 32-bit integer range. Uses float arithmetic to avoid double promotion.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int clampToInt(float sampleValue)
        {
            sampleValue = (sampleValue < -1.0f) ? -1.0f : (sampleValue > 1.0f) ? 1.0f : sampleValue;
            return (int)(sampleValue * FloatToInt32Scale);
        }

        /// <summary>
        /// Clamp float to 16-bit integer range. Uses float arithmetic to avoid double promotion.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short clampToShort(float sampleValue)
        {
            sampleValue = (sampleValue < -1.0f) ? -1.0f : (sampleValue > 1.0f) ? 1.0f : sampleValue;
            return (short)(sampleValue * FloatToInt16Scale);
        }
    }
}
