using NUnit.Framework;
using NUnit.Framework.Legacy;
using NAudio.Wave.SampleProviders;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// FadeInOutSampleProvider のフェードイン/アウト・WaveFormat のテスト。
    /// </summary>
    [TestFixture]
    public class FadeInOutSampleProviderTests
    {
        /// <summary>
        /// フェードインが適用されることを確認する。
        /// </summary>
        [Test]
        public void CanFadeIn()
        {
            var source = new TestSampleProvider(10, 1); // 10 samples per second
            source.UseConstValue = true;
            source.ConstValue = 100;
            var fade = new FadeInOutSampleProvider(source);
            fade.BeginFadeIn(1000);
            var buffer = new float[20];
            var read = fade.Read(buffer, 0, 20);
            ClassicAssert.AreEqual(20, read);
            ClassicAssert.AreEqual(0, buffer[0]); // start of fade-in
            ClassicAssert.AreEqual(50, buffer[5]); // half-way
            ClassicAssert.AreEqual(100, buffer[10]); // fully fade in
            ClassicAssert.AreEqual(100, buffer[15]); // fully fade in
        }

        /// <summary>
        /// フェードアウトが適用されることを確認する。
        /// </summary>
        [Test]
        public void CanFadeOut()
        {
            var source = new TestSampleProvider(10, 1); // 10 samples per second
            source.UseConstValue = true;
            source.ConstValue = 100;
            var fade = new FadeInOutSampleProvider(source);
            fade.BeginFadeOut(1000);
            var buffer = new float[20];
            var read = fade.Read(buffer, 0, 20);
            ClassicAssert.AreEqual(20, read);
            ClassicAssert.AreEqual(100, buffer[0]); // start of fade-out
            ClassicAssert.AreEqual(50, buffer[5]); // half-way
            ClassicAssert.AreEqual(0, buffer[10]); // fully fade out
            ClassicAssert.AreEqual(0, buffer[15]); // fully fade out
        }

        /// <summary>
        /// フェード期間が 1 回の Read より長い場合も正しく補間されることを確認する。
        /// </summary>
        [Test]
        public void FadeDurationCanBeLongerThanOneRead()
        {
            var source = new TestSampleProvider(10, 1); // 10 samples per second
            source.UseConstValue = true;
            source.ConstValue = 100;
            var fade = new FadeInOutSampleProvider(source);
            fade.BeginFadeIn(1000);
            var buffer = new float[4];
            var read = fade.Read(buffer, 0, 4);
            ClassicAssert.AreEqual(4, read);
            ClassicAssert.AreEqual(0, buffer[0]); // start of fade-in
            ClassicAssert.AreEqual(10, buffer[1]);
            ClassicAssert.AreEqual(20, buffer[2], 0.0001);
            ClassicAssert.AreEqual(30, buffer[3], 0.0001);

            read = fade.Read(buffer, 0, 4);
            ClassicAssert.AreEqual(4, read);
            ClassicAssert.AreEqual(40, buffer[0], 0.0001);
            ClassicAssert.AreEqual(50, buffer[1], 0.0001);
            ClassicAssert.AreEqual(60, buffer[2], 0.0001);
            ClassicAssert.AreEqual(70, buffer[3], 0.0001);

            read = fade.Read(buffer, 0, 4);
            ClassicAssert.AreEqual(4, read);
            ClassicAssert.AreEqual(80, buffer[0], 0.0001);
            ClassicAssert.AreEqual(90, buffer[1], 0.0001);
            ClassicAssert.AreEqual(100, buffer[2], 0.0001);
            ClassicAssert.AreEqual(100, buffer[3]);
        }

        /// <summary>
        /// WaveFormat がソースの WaveFormat を返すことを確認する。
        /// </summary>
        [Test]
        public void WaveFormatReturnsSourceWaveFormat()
        {
            var source = new TestSampleProvider(10, 1); // 10 samples per second
            var fade = new FadeInOutSampleProvider(source);
            ClassicAssert.AreSame(source.WaveFormat, fade.WaveFormat);
        }

        /// <summary>
        /// ステレオのサンプルペアでもフェードが正しく動作することを確認する。
        /// </summary>
        [Test]
        public void FadeWorksOverSamplePairs()
        {
            var source = new TestSampleProvider(10, 2); // 10 samples per second
            source.UseConstValue = true;
            source.ConstValue = 100;
            var fade = new FadeInOutSampleProvider(source);
            fade.BeginFadeIn(1000);
            var buffer = new float[20];
            var read = fade.Read(buffer, 0, 20);
            ClassicAssert.AreEqual(20, read);
            ClassicAssert.AreEqual(0, buffer[0]); // start of fade-in
            ClassicAssert.AreEqual(0, buffer[1]); // start of fade-in
            ClassicAssert.AreEqual(50, buffer[10]); // half-way
            ClassicAssert.AreEqual(50, buffer[11]); // half-way
            ClassicAssert.AreEqual(90, buffer[18], 0.0001); // fully fade in
            ClassicAssert.AreEqual(90, buffer[19], 0.0001); // fully fade in
        }

        /// <summary>
        /// フェードアウト後はバッファがゼロになることを確認する。
        /// </summary>
        [Test]
        public void BufferIsZeroedAfterFadeOut()
        {
            var source = new TestSampleProvider(10, 1); // 10 samples per second
            source.UseConstValue = true;
            source.ConstValue = 100;
            var fade = new FadeInOutSampleProvider(source);
            fade.BeginFadeOut(1000);
            var buffer = new float[20];
            var read = fade.Read(buffer, 0, 20);
            ClassicAssert.AreEqual(20, read);
            ClassicAssert.AreEqual(100, buffer[0]); // start of fade-in
            ClassicAssert.AreEqual(50, buffer[5]); // half-way
            ClassicAssert.AreEqual(0, buffer[10]); // half-way
            read = fade.Read(buffer, 0, 20);
            ClassicAssert.AreEqual(20, read);
            ClassicAssert.AreEqual(0, buffer[0]);
        }
    }
}
