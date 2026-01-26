using System;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using NAudio.Wave;
using System.Diagnostics;
using Moq;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// MultiplexingWaveProvider の入出力チャンネル・例外・Read・24bit/IEEE のテスト。
    /// </summary>
    [TestFixture]
    public class MultiplexingWaveProviderTests
    {
        /// <summary>
        /// null 入力で ArgumentNullException がスローされることを確認する。
        /// </summary>
        [Test]
        public void NullInputsShouldThrowException()
        {
            Assert.Throws<ArgumentNullException>(() => new MultiplexingWaveProvider(null, 1));
        }

        /// <summary>
        /// 入力 0 で ArgumentException がスローされることを確認する。
        /// </summary>
        [Test]
        public void ZeroInputsShouldThrowException()
        {
            Assert.Throws<ArgumentException>(() => new MultiplexingWaveProvider(new IWaveProvider[] { }, 1));
        }

        /// <summary>
        /// 出力 0 で ArgumentException がスローされることを確認する。
        /// </summary>
        [Test]
        public void ZeroOutputsShouldThrowException()
        {
            var input1 = new Mock<IWaveProvider>();
            Assert.Throws<ArgumentException>(() => new MultiplexingWaveProvider(new[] { input1.Object }, 0));
        }

        /// <summary>
        /// 無効な WaveFormat（GSM 等）で ArgumentException がスローされることを確認する。
        /// </summary>
        [Test]
        public void InvalidWaveFormatShouldThowException()
        {
            var input1 = new Mock<IWaveProvider>();
            input1.Setup(x => x.WaveFormat).Returns(new Gsm610WaveFormat());
            Assert.Throws<ArgumentException>(() => new MultiplexingWaveProvider(new[] { input1.Object }, 1));
        }

        /// <summary>
        /// 1 入 1 出で WaveFormat がコピーされることを確認する。
        /// </summary>
        [Test]
        public void OneInOneOutShouldCopyWaveFormat()
        {
            var input1 = new Mock<IWaveProvider>();
            var inputWaveFormat = new WaveFormat(32000, 16, 1);
            input1.Setup(x => x.WaveFormat).Returns(inputWaveFormat);
            var mp = new MultiplexingWaveProvider(new[] { input1.Object }, 1);
            ClassicAssert.AreEqual(inputWaveFormat, mp.WaveFormat);
        }

        /// <summary>
        /// 1 入 2 出でステレオ WaveFormat になることを確認する。
        /// </summary>
        [Test]
        public void OneInTwoOutShouldCopyWaveFormatButBeStereo()
        {
            var input1 = new Mock<IWaveProvider>();
            var inputWaveFormat = new WaveFormat(32000, 16, 1);
            input1.Setup(x => x.WaveFormat).Returns(inputWaveFormat);
            var mp = new MultiplexingWaveProvider(new[] { input1.Object }, 2);
            var expectedOutputWaveFormat = new WaveFormat(32000, 16, 2);
            ClassicAssert.AreEqual(expectedOutputWaveFormat, mp.WaveFormat);
        }

        /// <summary>
        /// 1 入 1 出で Read が入力通り返すことを確認する。
        /// </summary>
        [Test]
        public void OneInOneOutShouldCopyInReadMethod()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 1));
            byte[] expected = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var mp = new MultiplexingWaveProvider(new IWaveProvider[] { input1 }, 1);
            var buffer = new byte[10];
            var read = mp.Read(buffer, 0, 10);
            ClassicAssert.AreEqual(10, read);
            ClassicAssert.AreEqual(expected, buffer);
        }


        /// <summary>
        /// 1 入 2 出でモノラルがステレオに複製されることを確認する。
        /// </summary>
        [Test]
        public void OneInTwoOutShouldConvertMonoToStereo()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 1));
            // 16 bit so left right pairs
            byte[] expected = { 0, 1, 0, 1, 2, 3, 2, 3, 4, 5, 4, 5, 6, 7, 6, 7, 8, 9, 8, 9 };
            var mp = new MultiplexingWaveProvider(new IWaveProvider[] { input1 }, 2);
            var buffer = new byte[20];
            var read = mp.Read(buffer, 0, 20);
            ClassicAssert.AreEqual(20, read);
            ClassicAssert.AreEqual(expected, buffer);
        }

        /// <summary>
        /// 2 入 1 出で左チャンネルが選ばれることを確認する。
        /// </summary>
        [Test]
        public void TwoInOneOutShouldSelectLeftChannel()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 2));
            // 16 bit so left right pairs
            byte[] expected = { 0, 1, 4, 5, 8, 9, 12, 13, 16, 17 };
            var mp = new MultiplexingWaveProvider(new IWaveProvider[] { input1 }, 1);
            var buffer = new byte[10];
            var read = mp.Read(buffer, 0, 10);
            ClassicAssert.AreEqual(10, read);
            ClassicAssert.AreEqual(expected, buffer);
        }

        /// <summary>
        /// ConnectInputToOutput で右チャンネルを選択できることを確認する。
        /// </summary>
        [Test]
        public void TwoInOneOutShouldCanBeConfiguredToSelectRightChannel()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 2));
            // 16 bit so left right pairs
            byte[] expected = { 2, 3, 6, 7, 10, 11, 14, 15, 18, 19 };
            var mp = new MultiplexingWaveProvider(new IWaveProvider[] { input1 }, 1);
            mp.ConnectInputToOutput(1, 0);
            var buffer = new byte[10];
            var read = mp.Read(buffer, 0, 10);
            ClassicAssert.AreEqual(10, read);
            ClassicAssert.AreEqual(expected, buffer);
        }

        /// <summary>
        /// ステレオ 1 入 2 出でそのままコピーされることを確認する。
        /// </summary>
        [Test]
        public void StereoInTwoOutShouldCopyStereo()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 2));
            // 4 bytes per pair of samples
            byte[] expected = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
            var mp = new MultiplexingWaveProvider(new IWaveProvider[] { input1 }, 2);
            var buffer = new byte[12];
            var read = mp.Read(buffer, 0, 12);
            ClassicAssert.AreEqual(12, read);
            ClassicAssert.AreEqual(expected, buffer);
        }

        /// <summary>
        /// モノ 2 入 2 出でインターリーブされたステレオになることを確認する。
        /// </summary>
        [Test]
        public void TwoMonoInTwoOutShouldCreateStereo()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 1));
            var input2 = new TestWaveProvider(new WaveFormat(32000, 16, 1)) { Position = 100 };
            // 4 bytes per pair of samples
            byte[] expected = { 0, 1, 100, 101, 2, 3, 102, 103, 4, 5, 104, 105, };
            var mp = new MultiplexingWaveProvider(new IWaveProvider[] { input1, input2 }, 2);
            var buffer = new byte[expected.Length];
            var read = mp.Read(buffer, 0, expected.Length);
            ClassicAssert.AreEqual(expected.Length, read);
            ClassicAssert.AreEqual(expected, buffer);
        }

        /// <summary>
        /// ConnectInputToOutput で左右を入れ替えられることを確認する。
        /// </summary>
        [Test]
        public void StereoInTwoOutCanBeConfiguredToSwapLeftAndRight()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 2));
            // 4 bytes per pair of samples
            byte[] expected = { 2, 3, 0, 1, 6, 7, 4, 5, 10, 11, 8, 9, };
            var mp = new MultiplexingWaveProvider(new IWaveProvider[] { input1 }, 2);
            mp.ConnectInputToOutput(0, 1);
            mp.ConnectInputToOutput(1, 0);
            var buffer = new byte[12];
            var read = mp.Read(buffer, 0, 12);
            ClassicAssert.AreEqual(12, read);
            ClassicAssert.AreEqual(expected, buffer);
        }

        /// <summary>
        /// ConnectInputToOutput を呼び出せることを確認する。
        /// </summary>
        [Test]
        public void HasConnectInputToOutputMethod()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 2));
            var mp = new MultiplexingWaveProvider(new IWaveProvider[] { input1 }, 1);
            mp.ConnectInputToOutput(1, 0);
        }

        /// <summary>
        /// 無効な入力チャンネルで ConnectInputToOutput が例外をスローすることを確認する。
        /// </summary>
        [Test]
        public void ConnectInputToOutputThrowsExceptionForInvalidInput()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 2));
            var mp = new MultiplexingWaveProvider(new IWaveProvider[] { input1 }, 1);
            Assert.Throws<ArgumentException>(() => mp.ConnectInputToOutput(2, 0));
        }

        /// <summary>
        /// 無効な出力チャンネルで ConnectInputToOutput が例外をスローすることを確認する。
        /// </summary>
        [Test]
        public void ConnectInputToOutputThrowsExceptionForInvalidOutput()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 2));
            var mp = new MultiplexingWaveProvider(new IWaveProvider[] { input1 }, 1);
            Assert.Throws<ArgumentException>(() => mp.ConnectInputToOutput(1, 1));
        }

        /// <summary>
        /// InputChannelCount が正しいことを確認する。
        /// </summary>
        [Test]
        public void InputChannelCountIsCorrect()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 2));
            var input2 = new TestWaveProvider(new WaveFormat(32000, 16, 1));
            var mp = new MultiplexingWaveProvider(new IWaveProvider[] { input1, input2 }, 1);
            ClassicAssert.AreEqual(3, mp.InputChannelCount);
        }

        /// <summary>
        /// OutputChannelCount が正しいことを確認する。
        /// </summary>
        [Test]
        public void OutputChannelCountIsCorrect()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 1));
            var mp = new MultiplexingWaveProvider(new IWaveProvider[] { input1 }, 3);
            ClassicAssert.AreEqual(3, mp.OutputChannelCount);
        }

        /// <summary>
        /// 入力のサンプルレートが異なると ArgumentException がスローされることを確認する。
        /// </summary>
        [Test]
        public void ThrowsExceptionIfSampleRatesDiffer()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 2));
            var input2 = new TestWaveProvider(new WaveFormat(44100, 16, 1));
            Assert.Throws<ArgumentException>(() => new MultiplexingWaveProvider(new IWaveProvider[] { input1, input2 }, 1));
        }

        /// <summary>
        /// 入力のビット深度が異なると ArgumentException がスローされることを確認する。
        /// </summary>
        [Test]
        public void ThrowsExceptionIfBitDepthsDiffer()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 2));
            var input2 = new TestWaveProvider(new WaveFormat(32000, 24, 1));
            Assert.Throws<ArgumentException>(() => new MultiplexingWaveProvider(new IWaveProvider[] { input1, input2 }, 1));
        }

        /// <summary>
        /// 単一入力が終端に達すると Read が 0 を返すことを確認する。
        /// </summary>
        [Test]
        public void ReadReturnsZeroIfSingleInputHasReachedEnd()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 1), 0);
            byte[] expected = { };
            var mp = new MultiplexingWaveProvider(new IWaveProvider[] { input1 }, 1);
            var buffer = new byte[10];
            var read = mp.Read(buffer, 0, 10);
            ClassicAssert.AreEqual(0, read);
        }

        /// <summary>
        /// 一方の入力のみ終了した場合に Read が要求数を返すことを確認する。
        /// </summary>
        [Test]
        public void ReadReturnsCountIfOneInputHasEndedButTheOtherHasnt()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 1), 0);
            var input2 = new TestWaveProvider(new WaveFormat(32000, 16, 1));
            byte[] expected = { };
            var mp = new MultiplexingWaveProvider(new IWaveProvider[] { input1, input2 }, 1);
            var buffer = new byte[10];
            var read = mp.Read(buffer, 0, 10);
            ClassicAssert.AreEqual(10, read);
        }

        /// <summary>
        /// 単一コンストラクタが入力チャンネル合計を使うことを確認する。
        /// </summary>
        [Test]
        public void SingleInputConstructorUsesTotalOfInputChannels()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 8, 2));
            var input2 = new TestWaveProvider(new WaveFormat(32000, 8, 1));
            byte[] expected = { 0,1,0,2,3,1,4,5,2};
            var mp = new MultiplexingWaveProvider(new IWaveProvider[] { input1, input2 });
            ClassicAssert.AreEqual(3, mp.WaveFormat.Channels);
            var buffer = new byte[9];
            var read = mp.Read(buffer, 0, 9);
            ClassicAssert.AreEqual(9, read);
            ClassicAssert.AreEqual(buffer,expected);
        }

        /// <summary>
        /// 入力が不足した分がバッファでゼロ埋めされることを確認する。
        /// </summary>
        [Test]
        public void ShouldZeroOutBufferIfInputStopsShort()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 1), 6);
            byte[] expected = { 0, 1, 2, 3, 4, 5, 0, 0, 0, 0 };
            var mp = new MultiplexingWaveProvider(new IWaveProvider[] { input1 }, 1);
            var buffer = new byte[10];
            for (var n = 0; n < buffer.Length; n++)
            {
                buffer[n] = 0xFF;
            }
            var read = mp.Read(buffer, 0, buffer.Length);
            ClassicAssert.AreEqual(6, read);
            ClassicAssert.AreEqual(expected, buffer);
        }

        /// <summary>
        /// 24bit オーディオが正しく処理されることを確認する。
        /// </summary>
        [Test]
        public void CorrectlyHandles24BitAudio()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 24, 1));
            byte[] expected = { 0, 1, 2, 0, 1, 2, 3, 4, 5, 3, 4, 5, 6, 7, 8, 6, 7, 8, 9, 10, 11, 9, 10, 11 };
            var mp = new MultiplexingWaveProvider(new IWaveProvider[] { input1 }, 2);
            var buffer = new byte[expected.Length];
            var read = mp.Read(buffer, 0, expected.Length);
            ClassicAssert.AreEqual(expected.Length, read);
            ClassicAssert.AreEqual(expected, buffer);
        }

        /// <summary>
        /// IEEE float が正しく処理されることを確認する。
        /// </summary>
        [Test]
        public void CorrectlyHandlesIeeeFloat()
        {
            var input1 = new TestWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(32000, 1));
            byte[] expected = { 0, 1, 2, 3, 0, 1, 2, 3, 4, 5, 6, 7, 4, 5, 6, 7, 8, 9, 10, 11, 8, 9, 10, 11, };
            var mp = new MultiplexingWaveProvider(new IWaveProvider[] { input1 }, 2);
            var buffer = new byte[expected.Length];
            var read = mp.Read(buffer, 0, expected.Length);
            ClassicAssert.AreEqual(expected.Length, read);
            ClassicAssert.AreEqual(expected, buffer);
        }

        /// <summary>
        /// IEEE float 入力で出力 WaveFormat が正しく設定されることを確認する。
        /// </summary>
        [Test]
        public void CorrectOutputFormatIsSetForIeeeFloat()
        {
            var input1 = new TestWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(32000, 1));
            byte[] expected = { 0, 1, 2, 3, 0, 1, 2, 3, 4, 5, 6, 7, 4, 5, 6, 7, 8, 9, 10, 11, 8, 9, 10, 11, };
            var mp = new MultiplexingWaveProvider(new IWaveProvider[] { input1 }, 2);
            ClassicAssert.AreEqual(WaveFormatEncoding.IeeeFloat, mp.WaveFormat.Encoding);
        }

        /// <summary>
        /// マルチプレックス処理のパフォーマンス計測用メソッド。
        /// </summary>
        public void PerformanceTest()
        {
            var waveFormat = new WaveFormat(32000, 16, 1);
            var input1 = new TestWaveProvider(waveFormat);
            var input2 = new TestWaveProvider(waveFormat);
            var input3 = new TestWaveProvider(waveFormat);
            var input4 = new TestWaveProvider(waveFormat);
            var mp = new MultiplexingWaveProvider(new IWaveProvider[] { input1, input2, input3, input4 }, 4);
            mp.ConnectInputToOutput(0, 3);
            mp.ConnectInputToOutput(1, 2);
            mp.ConnectInputToOutput(2, 1);
            mp.ConnectInputToOutput(3, 0);

            var buffer = new byte[waveFormat.AverageBytesPerSecond];
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
