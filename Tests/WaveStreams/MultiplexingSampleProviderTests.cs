using System;
using NAudioTests.Utils;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System.Diagnostics;
using Moq;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// MultiplexingSampleProvider の入出力チャンネル・例外・Read のテスト。
    /// </summary>
    [TestFixture]
    public class MultiplexingSampleProviderTests
    {
        /// <summary>
        /// null 入力で ArgumentNullException がスローされることを確認する。
        /// </summary>
        [Test]
        public void NullInputsShouldThrowException()
        {
            Assert.Throws<ArgumentNullException>(() => new MultiplexingSampleProvider(null, 1));
        }

        /// <summary>
        /// 入力 0 で ArgumentException がスローされることを確認する。
        /// </summary>
        [Test]
        public void ZeroInputsShouldThrowException()
        {
            Assert.Throws<ArgumentException>(() => new MultiplexingSampleProvider(new ISampleProvider[] { }, 1));
        }

        /// <summary>
        /// 出力 0 で ArgumentException がスローされることを確認する。
        /// </summary>
        [Test]
        public void ZeroOutputsShouldThrowException()
        {
            var input1 = new Mock<ISampleProvider>();
            Assert.Throws<ArgumentException>(() => new MultiplexingSampleProvider(new ISampleProvider[] { input1.Object }, 0));
        }

        /// <summary>
        /// 無効な WaveFormat（16bit 等）で ArgumentException がスローされることを確認する。
        /// </summary>
        [Test]
        public void InvalidWaveFormatShouldThowException()
        {
            var input1 = new Mock<ISampleProvider>();
            input1.Setup(x => x.WaveFormat).Returns(new WaveFormat(32000, 16, 1));
            Assert.Throws<ArgumentException>(() => new MultiplexingSampleProvider(new ISampleProvider[] { input1.Object }, 1));
        }

        /// <summary>
        /// 1 入 1 出で WaveFormat がコピーされることを確認する。
        /// </summary>
        [Test]
        public void OneInOneOutShouldCopyWaveFormat()
        {
            var input1 = new Mock<ISampleProvider>();
            var inputWaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(32000, 1);
            input1.Setup(x => x.WaveFormat).Returns(inputWaveFormat);
            var mp = new MultiplexingSampleProvider(new ISampleProvider[] { input1.Object }, 1);
            ClassicAssert.AreEqual(inputWaveFormat, mp.WaveFormat);
        }

        /// <summary>
        /// 1 入 2 出で WaveFormat がコピーされステレオになることを確認する。
        /// </summary>
        [Test]
        public void OneInTwoOutShouldCopyWaveFormatButBeStereo()
        {
            var input1 = new Mock<ISampleProvider>();
            var inputWaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(32000, 1);
            input1.Setup(x => x.WaveFormat).Returns(inputWaveFormat);
            var mp = new MultiplexingSampleProvider(new ISampleProvider[] { input1.Object }, 2);
            var expectedOutputWaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(32000, 2);
            ClassicAssert.AreEqual(expectedOutputWaveFormat, mp.WaveFormat);
        }

        /// <summary>
        /// 1 入 1 出で Read が入力通り返すことを確認する。
        /// </summary>
        [Test]
        public void OneInOneOutShouldCopyInReadMethod()
        {
            var input1 = new TestSampleProvider(32000, 1);
            var expected = new float[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var mp = new MultiplexingSampleProvider(new ISampleProvider[] { input1 }, 1);
            mp.AssertReadsExpected(expected);
        }

        /// <summary>
        /// 1 入 2 出でモノラルがステレオに複製されることを確認する。
        /// </summary>
        [Test]
        public void OneInTwoOutShouldConvertMonoToStereo()
        {
            var input1 = new TestSampleProvider(32000, 1);
            var expected = new float[] { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9 };
            var mp = new MultiplexingSampleProvider(new ISampleProvider[] { input1 }, 2);
            mp.AssertReadsExpected(expected);
        }

        /// <summary>
        /// 2 入 1 出で左チャンネルが選ばれることを確認する。
        /// </summary>
        [Test]
        public void TwoInOneOutShouldSelectLeftChannel()
        {
            var input1 = new TestSampleProvider(32000, 2);
            var expected = new float[] { 0, 2, 4, 6, 8, 10, 12, 14, 16, 18 };
            var mp = new MultiplexingSampleProvider(new ISampleProvider[] { input1 }, 1);
            mp.AssertReadsExpected(expected);
        }

        /// <summary>
        /// ConnectInputToOutput で右チャンネルを選択できることを確認する。
        /// </summary>
        [Test]
        public void TwoInOneOutShouldCanBeConfiguredToSelectRightChannel()
        {
            var input1 = new TestSampleProvider(32000, 2);
            var expected = new float[] { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19 };
            var mp = new MultiplexingSampleProvider(new ISampleProvider[] { input1 }, 1);
            mp.ConnectInputToOutput(1, 0);
            mp.AssertReadsExpected(expected);
        }

        /// <summary>
        /// ステレオ 1 入 2 出でそのままコピーされることを確認する。
        /// </summary>
        [Test]
        public void StereoInTwoOutShouldCopyStereo()
        {
            var input1 = new TestSampleProvider(32000, 2);
            var expected = new float[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 };
            var mp = new MultiplexingSampleProvider(new ISampleProvider[] { input1 }, 2);
        }

        /// <summary>
        /// モノ 2 入 2 出でインターリーブされたステレオになることを確認する。
        /// </summary>
        [Test]
        public void TwoMonoInTwoOutShouldCreateStereo()
        {
            var input1 = new TestSampleProvider(32000, 1);
            var input2 = new TestSampleProvider(32000, 1) { Position = 100 };
            var expected = new float[] { 0, 100, 1, 101, 2, 102, 3, 103, 4, 104, 5, 105 };
            var mp = new MultiplexingSampleProvider(new ISampleProvider[] { input1, input2 }, 2);
            mp.AssertReadsExpected(expected);
        }

        /// <summary>
        /// ConnectInputToOutput で左右を入れ替えられることを確認する。
        /// </summary>
        [Test]
        public void StereoInTwoOutCanBeConfiguredToSwapLeftAndRight()
        {
            var input1 = new TestSampleProvider(32000, 2);
            var expected = new float[] { 1, 0, 3, 2, 5, 4, 7, 6, 9, 8, 11, 10 };
            var mp = new MultiplexingSampleProvider(new ISampleProvider[] { input1 }, 2);
            mp.ConnectInputToOutput(0, 1);
            mp.ConnectInputToOutput(1, 0);
            mp.AssertReadsExpected(expected);
        }

        /// <summary>
        /// ConnectInputToOutput を呼び出せることを確認する。
        /// </summary>
        [Test]
        public void HasConnectInputToOutputMethod()
        {
            var input1 = new TestSampleProvider(32000, 2);
            var mp = new MultiplexingSampleProvider(new ISampleProvider[] { input1 }, 1);
            mp.ConnectInputToOutput(1, 0);
        }

        /// <summary>
        /// 無効な入力チャンネルで ConnectInputToOutput が例外をスローすることを確認する。
        /// </summary>
        [Test]
        public void ConnectInputToOutputThrowsExceptionForInvalidInput()
        {
            var input1 = new TestSampleProvider(32000, 2);
            var mp = new MultiplexingSampleProvider(new ISampleProvider[] { input1 }, 1);
            Assert.Throws<ArgumentException>(() => mp.ConnectInputToOutput(2, 0));
        }

        /// <summary>
        /// 無効な出力チャンネルで ConnectInputToOutput が例外をスローすることを確認する。
        /// </summary>
        [Test]
        public void ConnectInputToOutputThrowsExceptionForInvalidOutput()
        {
            var input1 = new TestSampleProvider(32000, 2);
            var mp = new MultiplexingSampleProvider(new ISampleProvider[] { input1 }, 1);
            Assert.Throws<ArgumentException>(() => mp.ConnectInputToOutput(1, 1));
        }

        /// <summary>
        /// InputChannelCount が正しいことを確認する。
        /// </summary>
        [Test]
        public void InputChannelCountIsCorrect()
        {
            var input1 = new TestSampleProvider(32000, 2);
            var input2 = new TestSampleProvider(32000, 1);
            var mp = new MultiplexingSampleProvider(new ISampleProvider[] { input1, input2 }, 1);
            ClassicAssert.AreEqual(3, mp.InputChannelCount);
        }

        /// <summary>
        /// OutputChannelCount が正しいことを確認する。
        /// </summary>
        [Test]
        public void OutputChannelCountIsCorrect()
        {
            var input1 = new TestSampleProvider(32000, 1);
            var mp = new MultiplexingSampleProvider(new ISampleProvider[] { input1 }, 3);
            ClassicAssert.AreEqual(3, mp.OutputChannelCount);
        }

        /// <summary>
        /// 入力のサンプルレートが異なると ArgumentException がスローされることを確認する。
        /// </summary>
        [Test]
        public void ThrowsExceptionIfSampleRatesDiffer()
        {
            var input1 = new TestSampleProvider(32000, 2);
            var input2 = new TestSampleProvider(44100, 1);
            Assert.Throws<ArgumentException>(() => new MultiplexingSampleProvider(new ISampleProvider[] { input1, input2 }, 1));
        }

        /// <summary>
        /// 単一入力が終端に達すると Read が 0 を返すことを確認する。
        /// </summary>
        [Test]
        public void ReadReturnsZeroIfSingleInputHasReachedEnd()
        {
            var input1 = new TestSampleProvider(32000, 1, 0);
            var expected = new float[] { };
            var mp = new MultiplexingSampleProvider(new ISampleProvider[] { input1 }, 1);
            var buffer = new float[10];
            var read = mp.Read(buffer, 0, buffer.Length);
            ClassicAssert.AreEqual(0, read);
        }

        /// <summary>
        /// 一方の入力のみ終了した場合に Read が要求数を返すことを確認する。
        /// </summary>
        [Test]
        public void ReadReturnsCountIfOneInputHasEndedButTheOtherHasnt()
        {
            var input1 = new TestSampleProvider(32000, 1, 0);
            var input2 = new TestSampleProvider(32000, 1);
            var expected = new float[] { 0, 0, 0, 1, 0, 2, 0, 3, 0, 4, 0, 5, 0, 6, 0, 7 };
            var mp = new MultiplexingSampleProvider(new ISampleProvider[] { input1, input2 }, 2);
            mp.AssertReadsExpected(expected);
        }

        /// <summary>
        /// 入力が不足した分がバッファでゼロ埋めされることを確認する。
        /// </summary>
        [Test]
        public void ShouldZeroOutBufferIfInputStopsShort()
        {
            var input1 = new TestSampleProvider(32000, 1, 6);
            var expected = new float[] { 0, 1, 2, 3, 4, 5, 0, 0, 0, 0 };
            var mp = new MultiplexingSampleProvider(new ISampleProvider[] { input1 }, 1);
            var buffer = new float[10];
            for (var n = 0; n < buffer.Length; n++)
            {
                buffer[n] = 99;
            }
            var read = mp.Read(buffer, 0, buffer.Length);
            ClassicAssert.AreEqual(6, read);
            ClassicAssert.AreEqual(expected, buffer);
        }

        /// <summary>
        /// マルチプレックス処理のパフォーマンス計測用メソッド。
        /// </summary>
        public void PerformanceTest()
        {
            var input1 = new TestSampleProvider(32000, 1);
            var input2 = new TestSampleProvider(32000, 1);
            var input3 = new TestSampleProvider(32000, 1);
            var input4 = new TestSampleProvider(32000, 1);
            var mp = new MultiplexingSampleProvider(new ISampleProvider[] { input1, input2, input3, input4 }, 4);
            mp.ConnectInputToOutput(0, 3);
            mp.ConnectInputToOutput(1, 2);
            mp.ConnectInputToOutput(2, 1);
            mp.ConnectInputToOutput(3, 0);

            var buffer = new float[input1.WaveFormat.AverageBytesPerSecond / 4];
            var s = new Stopwatch();
            var duration = s.Time(() =>
            {
                // read one hour worth of audio
                for (var n = 0; n < 60 * 60; n++)
                {
                    mp.Read(buffer, 0, buffer.Length);
                }
            });
            Console.WriteLine("Performance test took {0}ms", duration);
        }
    }
}
