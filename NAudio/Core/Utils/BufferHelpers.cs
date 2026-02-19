using System.Buffers;

namespace NAudio.Utils
{
    /// <summary>
    /// Helper methods for working with audio buffers
    /// </summary>
    public static class BufferHelpers
    {
        /// <summary>
        /// Ensures the buffer is big enough
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="bytesRequired"></param>
        /// <returns></returns>
        public static byte[] Ensure(byte[] buffer, int bytesRequired)
        {
            if (buffer == null || buffer.Length < bytesRequired)
            {
                buffer = new byte[bytesRequired];
            }
            return buffer;
        }

        /// <summary>
        /// Ensures the buffer is big enough
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="samplesRequired"></param>
        /// <returns></returns>
        public static float[] Ensure(float[] buffer, int samplesRequired)
        {
            if (buffer == null || buffer.Length < samplesRequired)
            {
                buffer = new float[samplesRequired];
            }
            return buffer;
        }

        /// <summary>
        /// Ensures the pooled buffer is big enough. Returns the old buffer to the pool
        /// and rents a new one if necessary.
        /// </summary>
        /// <param name="buffer">Current pooled buffer (may be null)</param>
        /// <param name="bytesRequired">Minimum buffer size needed</param>
        /// <returns>A buffer from ArrayPool that is at least bytesRequired in length</returns>
        public static byte[] EnsurePooled(byte[] buffer, int bytesRequired)
        {
            if (buffer != null && buffer.Length >= bytesRequired)
            {
                return buffer;
            }
            if (buffer != null)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            return ArrayPool<byte>.Shared.Rent(bytesRequired);
        }

        /// <summary>
        /// Returns a pooled buffer back to the ArrayPool. Safe to call with null.
        /// </summary>
        /// <param name="buffer">The buffer to return (may be null)</param>
        public static void ReturnPooled(byte[] buffer)
        {
            if (buffer != null)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Ensures the pooled float buffer is big enough. Returns the old buffer to the pool
        /// and rents a new one if necessary.
        /// </summary>
        /// <param name="buffer">Current pooled buffer (may be null)</param>
        /// <param name="samplesRequired">Minimum buffer size needed</param>
        /// <returns>A buffer from ArrayPool that is at least samplesRequired in length</returns>
        public static float[] EnsurePooled(float[] buffer, int samplesRequired)
        {
            if (buffer != null && buffer.Length >= samplesRequired)
            {
                return buffer;
            }
            if (buffer != null)
            {
                ArrayPool<float>.Shared.Return(buffer);
            }
            return ArrayPool<float>.Shared.Rent(samplesRequired);
        }

        /// <summary>
        /// Returns a pooled float buffer back to the ArrayPool. Safe to call with null.
        /// </summary>
        /// <param name="buffer">The buffer to return (may be null)</param>
        public static void ReturnPooled(float[] buffer)
        {
            if (buffer != null)
            {
                ArrayPool<float>.Shared.Return(buffer);
            }
        }
    }
}
