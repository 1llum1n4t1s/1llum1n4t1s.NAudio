using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using NAudio.Utils;

namespace NAudio.Wave
{
    /// <summary>
    /// This class writes audio data to a .aif file on disk
    /// </summary>
    public class AiffFileWriter : Stream
    {
        private Stream outStream;
        private BinaryWriter writer;
        private long dataSizePos;
        private long commSampleCountPos;
        private long dataChunkSize = 8;
        private WaveFormat format;
        private string filename;

        /// <summary>
        /// Creates an Aiff file by reading all the data from a WaveProvider
        /// BEWARE: the WaveProvider MUST return 0 from its Read method when it is finished,
        /// or the Aiff File will grow indefinitely.
        /// </summary>
        /// <param name="filename">The filename to use</param>
        /// <param name="sourceProvider">The source WaveProvider</param>
        public static void CreateAiffFile(string filename, WaveStream sourceProvider)
        {
            using (var writer = new AiffFileWriter(filename, sourceProvider.WaveFormat))
            {
                var buffer = new byte[16384];

                while (sourceProvider.Position < sourceProvider.Length)
                {
                    var count = Math.Min((int)(sourceProvider.Length - sourceProvider.Position), buffer.Length);
                    var bytesRead = sourceProvider.Read(buffer, 0, count);

                    if (bytesRead == 0)
                    {
                        // end of source provider
                        break;
                    }

                    writer.Write(buffer, 0, bytesRead);
                }
            }
        }

        /// <summary>
        /// AiffFileWriter that actually writes to a stream
        /// </summary>
        /// <param name="outStream">Stream to be written to</param>
        /// <param name="format">Wave format to use</param>
        public AiffFileWriter(Stream outStream, WaveFormat format)
        {
            this.outStream = outStream;
            this.format = format;
            this.writer = new BinaryWriter(outStream, System.Text.Encoding.UTF8);
            this.writer.Write(System.Text.Encoding.UTF8.GetBytes("FORM"));
            this.writer.Write((int)0); // placeholder
            this.writer.Write(System.Text.Encoding.UTF8.GetBytes("AIFF"));

            CreateCommChunk();
            WriteSsndChunkHeader();
        }

        /// <summary>
        /// Creates a new AiffFileWriter
        /// </summary>
        /// <param name="filename">The filename to write to</param>
        /// <param name="format">The Wave Format of the output data</param>
        public AiffFileWriter(string filename, WaveFormat format)
            : this(new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read), format)
        {
            this.filename = filename;
        }

        private void WriteSsndChunkHeader()
        {
            this.writer.Write(System.Text.Encoding.UTF8.GetBytes("SSND"));
            dataSizePos = this.outStream.Position;
            this.writer.Write(SwapEndian((int)0));  // placeholder (big-endian)
            this.writer.Write(SwapEndian((int)0));  // zero offset (big-endian)
            this.writer.Write(SwapEndian((int)0));  // zero blockSize (big-endian)
        }

        private readonly byte[] swapBuf2 = new byte[2];
        private readonly byte[] swapBuf4 = new byte[4];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte[] SwapEndian(short n)
        {
            BinaryPrimitives.WriteInt16BigEndian(swapBuf2, n);
            return swapBuf2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte[] SwapEndian(int n)
        {
            BinaryPrimitives.WriteInt32BigEndian(swapBuf4, n);
            return swapBuf4;
        }

        private void CreateCommChunk()
        {
            this.writer.Write(System.Text.Encoding.UTF8.GetBytes("COMM"));
            this.writer.Write(SwapEndian((int)18));
            this.writer.Write(SwapEndian((short)format.Channels));
            commSampleCountPos = this.outStream.Position; ;
            this.writer.Write((int)0);  // placeholder for total number of samples
            this.writer.Write(SwapEndian((short)format.BitsPerSample));
            this.writer.Write(IEEE.ConvertToIeeeExtended(format.SampleRate));
        }

        /// <summary>
        /// The aiff file name or null if not applicable
        /// </summary>
        public string Filename
        {
            get { return filename; }
        }

        /// <summary>
        /// Number of bytes of audio in the data chunk
        /// </summary>
        public override long Length
        {
            get { return dataChunkSize; }
        }

        /// <summary>
        /// WaveFormat of this aiff file
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return format; }
        }

        /// <summary>
        /// Returns false: Cannot read from a AiffFileWriter
        /// </summary>
        public override bool CanRead
        {
            get { return false; }
        }

        /// <summary>
        /// Returns true: Can write to a AiffFileWriter
        /// </summary>
        public override bool CanWrite
        {
            get { return true; }
        }

        /// <summary>
        /// Returns false: Cannot seek within a AiffFileWriter
        /// </summary>
        public override bool CanSeek
        {
            get { return false; }
        }

        /// <summary>
        /// Read is not supported for a AiffFileWriter
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Cannot read from an AiffFileWriter");
        }

        /// <summary>
        /// Seek is not supported for a AiffFileWriter
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException("Cannot seek within an AiffFileWriter");
        }

        /// <summary>
        /// SetLength is not supported for AiffFileWriter
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            throw new InvalidOperationException("Cannot set length of an AiffFileWriter");
        }

        /// <summary>
        /// Gets the Position in the AiffFile (i.e. number of bytes written so far)
        /// </summary>
        public override long Position
        {
            get { return dataChunkSize; }
            set { throw new InvalidOperationException("Repositioning an AiffFileWriter is not supported"); }
        }

        /// <summary>
        /// Appends bytes to the AiffFile (assumes they are already in the correct format)
        /// </summary>
        /// <param name="data">the buffer containing the wave data</param>
        /// <param name="offset">the offset from which to start writing</param>
        /// <param name="count">the number of bytes to write</param>
        public override void Write(byte[] data, int offset, int count)
        {
            var swappedData = ArrayPool<byte>.Shared.Rent(count);
            try
            {
                var align = format.BitsPerSample / 8;

                for (var i = 0; i < count; i++)
                {
                    var sampleRelPos = i % align;
                    var sampleStart = (i / align) * align;
                    var pos = sampleStart + (align - sampleRelPos - 1);
                    swappedData[i] = data[offset + pos];
                }

                outStream.Write(swappedData, 0, count);
                dataChunkSize += count;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(swappedData);
            }
        }

        private byte[] value24 = new byte[3]; // keep this around to save us creating it every time

        /// <summary>
        /// Writes a single sample to the Aiff file
        /// </summary>
        /// <param name="sample">the sample to write (assumed floating point with 1.0f as max value)</param>
        public void WriteSample(float sample)
        {
            if (WaveFormat.BitsPerSample == 16)
            {
                writer.Write(SwapEndian(ClampToInt16(sample)));
                dataChunkSize += 2;
            }
            else if (WaveFormat.BitsPerSample == 24)
            {
                var clamped = ClampToFloat(sample);
                Span<byte> value = stackalloc byte[4];
                BinaryPrimitives.WriteInt32LittleEndian(value, (Int32)(Int32.MaxValue * (double)clamped));
                // Big-endian 24-bit: write the top 3 bytes in big-endian order
                value24[0] = value[3];
                value24[1] = value[2];
                value24[2] = value[1];
                writer.Write(value24);
                dataChunkSize += 3;
            }
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == NAudio.Wave.WaveFormatEncoding.Extensible)
            {
                var clamped = ClampToFloat(sample);
                writer.Write(SwapEndian((int)(Int32.MaxValue * (double)clamped)));
                dataChunkSize += 4;
            }
            else
            {
                throw new InvalidOperationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
            }
        }

        /// <summary>
        /// Writes 32 bit floating point samples to the Aiff file
        /// They will be converted to the appropriate bit depth depending on the WaveFormat of the AIF file
        /// </summary>
        /// <param name="samples">The buffer containing the floating point samples</param>
        /// <param name="offset">The offset from which to start writing</param>
        /// <param name="count">The number of floating point samples to write</param>
        public void WriteSamples(float[] samples, int offset, int count)
        {
            if (samples == null) throw new ArgumentNullException(nameof(samples));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "Must be non-negative");
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Must be non-negative");
            if (offset + count > samples.Length) throw new ArgumentException("offset + count exceeds buffer length");
            for (var n = 0; n < count; n++)
            {
                WriteSample(samples[offset + n]);
            }
        }

        /// <summary>
        /// Writes 16 bit samples to the Aiff file
        /// </summary>
        /// <param name="samples">The buffer containing the 16 bit samples</param>
        /// <param name="offset">The offset from which to start writing</param>
        /// <param name="count">The number of 16 bit samples to write</param>
        public void WriteSamples(short[] samples, int offset, int count)
        {
            if (samples == null) throw new ArgumentNullException(nameof(samples));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "Must be non-negative");
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Must be non-negative");
            if (offset + count > samples.Length) throw new ArgumentException("offset + count exceeds buffer length");
            // 16 bit PCM data
            if (WaveFormat.BitsPerSample == 16)
            {
                for (var sample = 0; sample < count; sample++)
                {
                    writer.Write(SwapEndian(samples[sample + offset]));
                }
                dataChunkSize += (count * 2);
            }
            // 24 bit PCM data
            else if (WaveFormat.BitsPerSample == 24)
            {
                Span<byte> value = stackalloc byte[4];
                for (var sample = 0; sample < count; sample++)
                {
                    // Shift 16-bit sample left by 8 bits for 16→24 bit conversion.
                    // WriteInt32LittleEndian stores as [byte0=LSB, byte1, byte2, byte3=MSB].
                    // For big-endian 24-bit output, write lower 3 bytes in reverse order.
                    BinaryPrimitives.WriteInt32LittleEndian(value, (int)samples[sample + offset] << 8);
                    value24[0] = value[2];
                    value24[1] = value[1];
                    value24[2] = value[0];
                    writer.Write(value24);
                }
                dataChunkSize += (count * 3);
            }
            // 32 bit PCM data
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.Extensible)
            {
                for (var sample = 0; sample < count; sample++)
                {
                    writer.Write(SwapEndian((int)samples[sample + offset] << 16));
                }
                dataChunkSize += (count * 4);
            }
            else
            {
                throw new InvalidOperationException("Only 16, 24 or 32 bit PCM audio data supported");
            }
        }

        /// <summary>
        /// Ensures data is written to disk
        /// </summary>
        public override void Flush()
        {
            writer.Flush();
        }

        #region IDisposable Members

        /// <summary>
        /// Actually performs the close,making sure the header contains the correct data
        /// </summary>
        /// <param name="disposing">True if called from <see>Dispose</see></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (outStream != null)
                {
                    try
                    {
                        UpdateHeader(writer);
                    }
                    finally
                    {
                        // in a finally block as we don't want the FileStream to run its disposer in
                        // the GC thread if the code above caused an IOException (e.g. due to disk full)
                        outStream.Dispose(); // will close the underlying base stream
                        outStream = null;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the header with file size information
        /// </summary>
        protected virtual void UpdateHeader(BinaryWriter writer)
        {
            this.Flush();
            writer.Seek(4, SeekOrigin.Begin);
            writer.Write(SwapEndian((int)(outStream.Length - 8)));
            UpdateCommChunk(writer);
            UpdateSsndChunk(writer);
        }

        private void UpdateCommChunk(BinaryWriter writer)
        {
            writer.Seek((int)commSampleCountPos, SeekOrigin.Begin);
            writer.Write(SwapEndian((int)((dataChunkSize - 8) * 8 / format.BitsPerSample / format.Channels)));
        }

        private void UpdateSsndChunk(BinaryWriter writer)
        {
            writer.Seek((int)dataSizePos, SeekOrigin.Begin);
            writer.Write(SwapEndian((int)dataChunkSize));
        }

        /// <summary>
        /// Finaliser - should only be called if the user forgot to close this AiffFileWriter
        /// </summary>
        ~AiffFileWriter()
        {
            System.Diagnostics.Debug.Assert(false, "AiffFileWriter was not disposed");
            Dispose(false);
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short ClampToInt16(float sample)
        {
            sample = (sample < -1.0f) ? -1.0f : (sample > 1.0f) ? 1.0f : sample;
            return (short)(sample * short.MaxValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ClampToFloat(float sample)
        {
            return (sample < -1.0f) ? -1.0f : (sample > 1.0f) ? 1.0f : sample;
        }
    }
}
