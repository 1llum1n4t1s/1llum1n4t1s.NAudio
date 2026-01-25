using NUnit.Framework.Legacy;
using NAudio.Wave;

namespace NAudioTests.Utils
{
    /// <summary>
    /// <see cref="ISampleProvider" /> のテスト用ヘルパー。
    /// </summary>
    public static class SampleProviderTestHelpers
    {
        /// <summary>
        /// サンプルプロバイダが expected と同じサンプルを返すことを検証する。
        /// </summary>
        /// <param name="sampleProvider">検証対象のサンプルプロバイダ。</param>
        /// <param name="expected">期待するサンプル配列。</param>
        public static void AssertReadsExpected(this ISampleProvider sampleProvider, float[] expected)
        {
            AssertReadsExpected(sampleProvider, expected, expected.Length);
        }

        /// <summary>
        /// サンプルプロバイダが expected と同じサンプルを返すことを、指定読み込みサイズで検証する。
        /// </summary>
        /// <param name="sampleProvider">検証対象のサンプルプロバイダ。</param>
        /// <param name="expected">期待するサンプル配列。</param>
        /// <param name="readSize">1 回の Read で読み込むサンプル数。</param>
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
