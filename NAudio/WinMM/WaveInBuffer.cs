using System;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// A buffer of Wave samples
    /// </summary>
    public class WaveInBuffer : IDisposable
    {
        private static readonly int WaveHeaderSize = Marshal.SizeOf<WaveHeader>();
        private readonly WaveHeader header;
        private readonly Int32 bufferSize; // allocated bytes, may not be the same as bytes read
        private readonly byte[] buffer;
        private GCHandle hBuffer;
        private IntPtr waveInHandle;
        private GCHandle hHeader; // we need to pin the header structure
        private GCHandle hThis; // for the user callback

        /// <summary>
        /// creates a new wavebuffer
        /// </summary>
        /// <param name="waveInHandle">WaveIn device to write to</param>
        /// <param name="bufferSize">Buffer size in bytes</param>
        public WaveInBuffer(IntPtr waveInHandle, Int32 bufferSize)
        {
            this.bufferSize = bufferSize;
            this.buffer = new byte[bufferSize];
            this.hBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            this.waveInHandle = waveInHandle;

            header = new WaveHeader();
            hHeader = GCHandle.Alloc(header, GCHandleType.Pinned);
            header.dataBuffer = hBuffer.AddrOfPinnedObject();
            header.bufferLength = bufferSize;
            header.loops = 1;
            hThis = GCHandle.Alloc(this);
            header.userData = (IntPtr)hThis;

            MmException.Try(WaveInterop.waveInPrepareHeader(waveInHandle, header, WaveHeaderSize), "waveInPrepareHeader");
            //MmException.Try(WaveInterop.waveInAddBuffer(waveInHandle, header, WaveHeaderSize), "waveInAddBuffer");
        }

        /// <summary>
        /// Place this buffer back to record more audio
        /// </summary>
        public void Reuse()
        {
            // TEST: we might not actually need to bother unpreparing and repreparing
            MmException.Try(WaveInterop.waveInUnprepareHeader(waveInHandle, header, WaveHeaderSize), "waveUnprepareHeader");
            MmException.Try(WaveInterop.waveInPrepareHeader(waveInHandle, header, WaveHeaderSize), "waveInPrepareHeader");
            //System.Diagnostics.Debug.Assert(header.bytesRecorded == 0, "bytes recorded was not reset properly");
            MmException.Try(WaveInterop.waveInAddBuffer(waveInHandle, header, WaveHeaderSize), "waveInAddBuffer");
        }

        #region Dispose Pattern

        /// <summary>
        /// Finalizer for this wave buffer
        /// </summary>
        ~WaveInBuffer()
        {
            Dispose(false);
            System.Diagnostics.Debug.Assert(false, "WaveInBuffer was not disposed");
        }

        /// <summary>
        /// Releases resources held by this WaveBuffer
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// Releases resources held by this WaveBuffer
        /// </summary>
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
            }
            // free unmanaged resources
            if (waveInHandle != IntPtr.Zero)
            {
                WaveInterop.waveInUnprepareHeader(waveInHandle, header, WaveHeaderSize);
                waveInHandle = IntPtr.Zero;
            }
            if (hHeader.IsAllocated)
                hHeader.Free();
            if (hBuffer.IsAllocated)
                hBuffer.Free();
            if (hThis.IsAllocated)
                hThis.Free();

        }

        #endregion

        /// <summary>
        /// Provides access to the actual record buffer (for reading only)
        /// </summary>
        public byte[] Data
        {
            get
            {
                return buffer;
            }
        }

        /// <summary>
        /// Indicates whether the Done flag is set on this buffer
        /// </summary>
        public bool Done
        {
            get
            {
                return (header.flags & WaveHeaderFlags.Done) == WaveHeaderFlags.Done;
            }
        }


        /// <summary>
        /// Indicates whether the InQueue flag is set on this buffer
        /// </summary>
        public bool InQueue
        {
            get
            {
                return (header.flags & WaveHeaderFlags.InQueue) == WaveHeaderFlags.InQueue;
            }
        }

        /// <summary>
        /// Number of bytes recorded
        /// </summary>
        public int BytesRecorded
        {
            get
            {
                return header.bytesRecorded;
            }
        }

        /// <summary>
        /// The buffer size in bytes
        /// </summary>
        public Int32 BufferSize
        {
            get
            {
                return bufferSize;
            }
        }
    }
}
