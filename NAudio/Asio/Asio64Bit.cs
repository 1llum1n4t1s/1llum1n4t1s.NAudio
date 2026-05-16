using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NAudio.Wave.Asio
{
    /// <summary>
    /// ASIO 64 bit value
    /// Unfortunately the ASIO API was implemented it before compiler supported consistently 64 bit
    /// integer types. By using the structure the data layout on a little-endian system like the
    /// Intel x86 architecture will result in a "non native" storage of the 64 bit data. The most
    /// significant 32 bit are stored first in memory, the least significant bits are stored in the
    /// higher memory space. However each 32 bit is stored in the native little-endian fashion
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Asio64Bit
    {
        /// <summary>
        /// most significant bits (Bits 32..63)
        /// </summary>
        public uint hi;
        /// <summary>
        /// least significant bits (Bits 0..31)
        /// </summary>
        public uint lo;

        /// <summary>
        /// Converts the ASIO 64-bit value to a long (Int64).
        /// Combines hi and lo parts into a native 64-bit integer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly long ToInt64()
        {
            return (long)(((ulong)hi << 32) | lo);
        }

        /// <summary>
        /// Converts the ASIO 64-bit value to a double.
        /// This is useful for sample positions and time values in the ASIO time info structures.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly double ToDouble()
        {
            return (double)ToInt64();
        }

        /// <summary>
        /// Creates an Asio64Bit from a long (Int64) value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Asio64Bit FromInt64(long value)
        {
            return new Asio64Bit
            {
                hi = (uint)((ulong)value >> 32),
                lo = (uint)(value & 0xFFFFFFFF)
            };
        }

        /// <summary>
        /// Creates an Asio64Bit from a double value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Asio64Bit FromDouble(double value)
        {
            return FromInt64((long)value);
        }
    };
}