using System;
using NAudio.Wave;

namespace NAudio.Extras
{
    /// <summary>
    /// Loopable WaveStream
    /// </summary>
    public class LoopStream : WaveStream
    {
        readonly WaveStream sourceStream;

        /// <summary>
        /// Creates a new Loop stream
        /// </summary>
        public LoopStream(WaveStream source)
        {
            sourceStream = source;
            EnableLooping = true;
        }

        /// <summary>
        /// Whether to enable looping. When false, the stream behaves like the source stream.
        /// </summary>
        public bool EnableLooping { get; set; }

        /// <summary>
        /// The WaveFormat of this stream
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get { return sourceStream.WaveFormat; }
        }

        /// <summary>
        /// Length in bytes of this stream (effectively infinite when looping)
        /// </summary>
        public override long Length
        {
            get { return EnableLooping ? long.MaxValue / 32 : sourceStream.Length; }
        }

        /// <summary>
        /// Position within this stream in bytes
        /// </summary>
        public override long Position
        {
            get
            {
                return sourceStream.Position;
            }
            set
            {
                value = Math.Max(0, value);
                sourceStream.Position = value - (value % sourceStream.BlockAlign);
            }
        }

        /// <summary>
        /// Always has data available when looping
        /// </summary>
        public override bool HasData(int count)
        {
            return EnableLooping || sourceStream.HasData(count);
        }

        /// <summary>
        /// Read data from this stream
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = 0;
            while (read < count)
            {
                var required = count - read;
                var readThisTime = sourceStream.Read(buffer, offset + read, required);
                if (readThisTime == 0)
                {
                    if (!EnableLooping)
                        break;

                    sourceStream.Position = 0;
                    readThisTime = sourceStream.Read(buffer, offset + read, required);
                    if (readThisTime == 0)
                        break;
                }

                if (EnableLooping && sourceStream.Position >= sourceStream.Length)
                {
                    sourceStream.Position = 0;
                }
                read += readThisTime;
            }
            return read;
        }

        /// <summary>
        /// Dispose this WaveStream (disposes the source)
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                sourceStream.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
