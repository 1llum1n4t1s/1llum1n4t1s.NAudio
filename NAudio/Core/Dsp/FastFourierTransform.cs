using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace NAudio.Dsp
{
    /// <summary>
    /// Summary description for FastFourierTransform.
    /// </summary>
    public static class FastFourierTransform
    {
        /// <summary>
        /// This computes an in-place complex-to-complex FFT
        /// x and y are the real and imaginary arrays of 2^m points.
        /// </summary>
        public static void FFT(bool forward, int m, Complex[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (m < 1 || m > 30) throw new ArgumentOutOfRangeException(nameof(m), "Must be between 1 and 30 inclusive");
            if (data.Length < (1 << m)) throw new ArgumentException($"Data array length must be at least {1 << m} for m={m}", nameof(data));
            int n, i, i1, j, k, i2, l, l1, l2;
            float c1, c2, tx, ty, t1, t2, u1, u2, z;

            // Calculate the number of points
            n = 1 << m;

            // Do the bit reversal
            i2 = n >> 1;
            j = 0;
            for (i = 0; i < n - 1; i++)
            {
                if (i < j)
                {
                    tx = data[i].X;
                    ty = data[i].Y;
                    data[i].X = data[j].X;
                    data[i].Y = data[j].Y;
                    data[j].X = tx;
                    data[j].Y = ty;
                }
                k = i2;

                while (k <= j)
                {
                    j -= k;
                    k >>= 1;
                }
                j += k;
            }

            // Compute the FFT
            c1 = -1.0f;
            c2 = 0.0f;
            l2 = 1;
            for (l = 0; l < m; l++)
            {
                l1 = l2;
                l2 <<= 1;
                u1 = 1.0f;
                u2 = 0.0f;
                for (j = 0; j < l1; j++)
                {
                    for (i = j; i < n; i += l2)
                    {
                        i1 = i + l1;
                        t1 = u1 * data[i1].X - u2 * data[i1].Y;
                        t2 = u1 * data[i1].Y + u2 * data[i1].X;
                        data[i1].X = data[i].X - t1;
                        data[i1].Y = data[i].Y - t2;
                        data[i].X += t1;
                        data[i].Y += t2;
                    }
                    z = u1 * c1 - u2 * c2;
                    u2 = u1 * c2 + u2 * c1;
                    u1 = z;
                }
                c2 = MathF.Sqrt((1.0f - c1) * 0.5f);
                if (forward)
                    c2 = -c2;
                c1 = MathF.Sqrt((1.0f + c1) * 0.5f);
            }

            // Scaling for forward transform using SIMD where possible
            if (forward)
            {
                var invN = 1.0f / n;
                // Vector path: scale X and Y components
                if (Vector.IsHardwareAccelerated && n >= Vector<float>.Count)
                {
                    // Treat the Complex array as a flat float buffer via Span
                    var floatSpan = System.Runtime.InteropServices.MemoryMarshal.Cast<Complex, float>(data.AsSpan(0, n));
                    var totalFloats = floatSpan.Length;
                    var vecSize = Vector<float>.Count;
                    var invNVec = new Vector<float>(invN);

                    var fi = 0;
                    for (; fi <= totalFloats - vecSize; fi += vecSize)
                    {
                        var v = new Vector<float>(floatSpan.Slice(fi));
                        (v * invNVec).CopyTo(floatSpan.Slice(fi));
                    }
                    // scalar remainder
                    for (; fi < totalFloats; fi++)
                    {
                        floatSpan[fi] *= invN;
                    }
                }
                else
                {
                    for (i = 0; i < n; i++)
                    {
                        data[i].X *= invN;
                        data[i].Y *= invN;
                    }
                }
            }
        }

        /// <summary>
        /// Applies a Hamming Window
        /// </summary>
        /// <param name="n">Index into frame</param>
        /// <param name="frameSize">Frame size (e.g. 1024)</param>
        /// <returns>Multiplier for Hamming window</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double HammingWindow(int n, int frameSize)
        {
            if (frameSize <= 1) return 1.0;
            return 0.54 - 0.46 * Math.Cos((2 * Math.PI * n) / (frameSize - 1));
        }

        /// <summary>
        /// Applies a Hann Window
        /// </summary>
        /// <param name="n">Index into frame</param>
        /// <param name="frameSize">Frame size (e.g. 1024)</param>
        /// <returns>Multiplier for Hann window</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double HannWindow(int n, int frameSize)
        {
            if (frameSize <= 1) return 1.0;
            return 0.5 * (1 - Math.Cos((2 * Math.PI * n) / (frameSize - 1)));
        }

        /// <summary>
        /// Applies a Blackman-Harris Window
        /// </summary>
        /// <param name="n">Index into frame</param>
        /// <param name="frameSize">Frame size (e.g. 1024)</param>
        /// <returns>Multiplier for Blackmann-Harris window</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double BlackmannHarrisWindow(int n, int frameSize)
        {
            if (frameSize <= 1) return 1.0;
            var phase = (2 * Math.PI * n) / (frameSize - 1);
            return 0.35875 - 0.48829 * Math.Cos(phase) + 0.14128 * Math.Cos(2 * phase) - 0.01168 * Math.Cos(3 * phase);
        }
    }
}
