using System;
using System.Diagnostics;

namespace NAudio.Utils
{
    /// <summary>
    /// A very basic circular buffer implementation
    /// </summary>
    public class CircularBuffer
    {
        private readonly byte[] buffer;
        private readonly object lockObject;
        private int writePosition;
        private int readPosition;
        private int byteCount;

        /// <summary>
        /// Create a new circular buffer
        /// </summary>
        /// <param name="size">Max buffer size in bytes</param>
        public CircularBuffer(int size)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size), "Buffer size must be greater than zero");
            buffer = new byte[size];
            lockObject = new object();
        }

        /// <summary>
        /// Write data to the buffer
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <param name="offset">Offset into data</param>
        /// <param name="count">Number of bytes to write</param>
        /// <returns>number of bytes written</returns>
        public int Write(byte[] data, int offset, int count)
        {
            return Write(data.AsSpan(offset, count));
        }

        /// <summary>
        /// Write data to the buffer from a ReadOnlySpan
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <returns>number of bytes written</returns>
        public int Write(ReadOnlySpan<byte> data)
        {
            lock (lockObject)
            {
                var count = data.Length;
                if (count > buffer.Length - byteCount)
                {
                    count = buffer.Length - byteCount;
                }
                if (count == 0) return 0;

                var bytesWritten = 0;
                // write to end
                var writeToEnd = Math.Min(buffer.Length - writePosition, count);
                data.Slice(0, writeToEnd).CopyTo(buffer.AsSpan(writePosition, writeToEnd));
                writePosition += writeToEnd;
                writePosition %= buffer.Length;
                bytesWritten += writeToEnd;
                if (bytesWritten < count)
                {
                    Debug.Assert(writePosition == 0);
                    // must have wrapped round. Write to start
                    var remaining = count - bytesWritten;
                    data.Slice(bytesWritten, remaining).CopyTo(buffer.AsSpan(writePosition, remaining));
                    writePosition += remaining;
                    bytesWritten = count;
                }
                byteCount += bytesWritten;
                return bytesWritten;
            }
        }

        /// <summary>
        /// Read from the buffer
        /// </summary>
        /// <param name="data">Buffer to read into</param>
        /// <param name="offset">Offset into read buffer</param>
        /// <param name="count">Bytes to read</param>
        /// <returns>Number of bytes actually read</returns>
        public int Read(byte[] data, int offset, int count)
        {
            return Read(data.AsSpan(offset, count));
        }

        /// <summary>
        /// Read from the buffer into a Span
        /// </summary>
        /// <param name="data">Span to read into</param>
        /// <returns>Number of bytes actually read</returns>
        public int Read(Span<byte> data)
        {
            lock (lockObject)
            {
                var count = data.Length;
                if (count > byteCount)
                {
                    count = byteCount;
                }
                if (count == 0) return 0;

                var bytesRead = 0;
                var readToEnd = Math.Min(buffer.Length - readPosition, count);
                buffer.AsSpan(readPosition, readToEnd).CopyTo(data);
                bytesRead += readToEnd;
                readPosition += readToEnd;
                readPosition %= buffer.Length;

                if (bytesRead < count)
                {
                    // must have wrapped round. Read from start
                    Debug.Assert(readPosition == 0);
                    var remaining = count - bytesRead;
                    buffer.AsSpan(readPosition, remaining).CopyTo(data.Slice(bytesRead));
                    readPosition += remaining;
                    bytesRead = count;
                }

                byteCount -= bytesRead;
                Debug.Assert(byteCount >= 0);
                return bytesRead;
            }
        }

        /// <summary>
        /// Maximum length of this circular buffer
        /// </summary>
        public int MaxLength => buffer.Length;

        /// <summary>
        /// Number of bytes currently stored in the circular buffer
        /// </summary>
        public int Count
        {
            get
            {
                lock (lockObject)
                {
                    return byteCount;
                }
            }
        }

        /// <summary>
        /// Resets the buffer
        /// </summary>
        public void Reset()
        {
            lock (lockObject)
            {
                ResetInner();
            }
        }

        private void ResetInner()
        {
            byteCount = 0;
            readPosition = 0;
            writePosition = 0;
        }

        /// <summary>
        /// Advances the buffer, discarding bytes
        /// </summary>
        /// <param name="count">Bytes to advance</param>
        public void Advance(int count)
        {
            lock (lockObject)
            {
                if (count >= byteCount)
                {
                    ResetInner();
                }
                else
                {
                    byteCount -= count;
                    readPosition += count;
                    readPosition %= MaxLength;
                }
            }
        }
    }
}
