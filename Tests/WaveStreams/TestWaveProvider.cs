using System;
using NAudio.Wave;

namespace NAudioTests.WaveStreams
{
    class TestWaveProvider : IWaveProvider
    {
        private int length;
        public int ConstValue { get; set; }

        public TestWaveProvider(WaveFormat format, int lengthInBytes = Int32.MaxValue)
        {
            this.WaveFormat = format;
            this.length = lengthInBytes;
            this.ConstValue = -1;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            var n = 0;
            while (n < count && Position < length)
            {
                buffer[n + offset] = (ConstValue == -1) ? (byte)Position : (byte)ConstValue;
                n++; Position++;
            }
            return n;
        }

        public WaveFormat WaveFormat { get; set; }

        public int Position { get; set; }
    }
}
