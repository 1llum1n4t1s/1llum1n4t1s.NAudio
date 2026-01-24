using NUnit.Framework.Legacy;
using NAudio.Wave;

namespace NAudioTests.Utils
{
    public static class SampleProviderTestHelpers
    {
        public static void AssertReadsExpected(this ISampleProvider sampleProvider, float[] expected)
        {
            AssertReadsExpected(sampleProvider, expected, expected.Length);
        }

        public static void AssertReadsExpected(this ISampleProvider sampleProvider, float[] expected, int readSize)
        {
            var buffer = new float[readSize];
            var read = sampleProvider.Read(buffer, 0, readSize);
            ClassicAssert.AreEqual(expected.Length, read, "Number of samples read");
            for (var n = 0; n < read; n++)
            {
                ClassicAssert.AreEqual(expected[n], buffer[n], $"Buffer at index {n}");
            }
        }
    }
}
