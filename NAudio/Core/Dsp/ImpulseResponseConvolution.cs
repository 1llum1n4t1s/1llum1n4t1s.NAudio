using System;

namespace NAudio.Dsp
{
    /// <summary>
    /// Summary description for ImpulseResponseConvolution.
    /// </summary>
    public class ImpulseResponseConvolution
    {
        /// <summary>
        /// A very simple mono convolution algorithm
        /// </summary>
        /// <remarks>
        /// This will be very slow
        /// </remarks>
        public float[] Convolve(float[] input, float[] impulseResponse)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (impulseResponse == null) throw new ArgumentNullException(nameof(impulseResponse));

            var output = new float[input.Length + impulseResponse.Length - 1];
            // Optimized inner loop: compute valid overlap range to avoid branching per iteration
            for (var t = 0; t < output.Length; t++)
            {
                // n must satisfy: n >= 0, n < impulseResponse.Length, t - n >= 0, t - n < input.Length
                // => n in [max(0, t - input.Length + 1), min(impulseResponse.Length - 1, t)]
                var nStart = Math.Max(0, t - input.Length + 1);
                var nEnd = Math.Min(impulseResponse.Length - 1, t);
                float sum = 0;
                for (var n = nStart; n <= nEnd; n++)
                {
                    sum += impulseResponse[n] * input[t - n];
                }
                output[t] = sum;
            }
            Normalize(output);
            return output;
        }

        /// <summary>
        /// This is actually a downwards normalize for data that will clip
        /// </summary>
        public void Normalize(float[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            float max = 0;
            for (var n = 0; n < data.Length; n++)
                max = MathF.Max(max, MathF.Abs(data[n]));
            if (max > 1.0f)
            {
                var invMax = 1.0f / max;
                for (var n = 0; n < data.Length; n++)
                    data[n] *= invMax;
            }
        }
    }
}
