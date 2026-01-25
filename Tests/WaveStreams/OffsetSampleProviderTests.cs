using System;
using System.Linq;
using NAudio.Wave.SampleProviders;
using NAudioTests.Utils;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// OffsetSampleProvider のディレイ・スキップ・Take・リードアウト・ブロックアラインのテスト。
    /// </summary>
    [TestFixture]
    public class OffsetSampleProviderTests
    {
        /// <summary>
        /// 既定ではソースがそのまま通過することを確認する。
        /// </summary>
        [Test]
        public void DefaultShouldPassStraightThrough()
        {
            var source = new TestSampleProvider(32000, 1);
            var osp = new OffsetSampleProvider(source);
            
            var expected = new float[] { 0, 1, 2, 3, 4, 5, 6 };
            osp.AssertReadsExpected(expected);
        }

        /// <summary>
        /// プリディレイ（サンプル数）を追加できることを確認する。
        /// </summary>
        [Test]
        public void CanAddPreDelay()
        {
            var source = new TestSampleProvider(32000, 1) {Position = 10};
            var osp = new OffsetSampleProvider(source) {DelayBySamples = 5};

            var expected = new float[] { 0, 0, 0, 0, 0, 10, 11, 12, 13, 14, 15 };
            osp.AssertReadsExpected(expected);
        }


        /// <summary>
        /// TimeSpan でプリディレイを追加できることを確認する。
        /// </summary>
        [Test]
        public void CanAddPreDelayUsingTimeSpan()
        {
            var source = new TestSampleProvider(100, 1) { Position = 10 };
            var osp = new OffsetSampleProvider(source) { DelayBy = TimeSpan.FromSeconds(1) };

            var expected = Enumerable.Range(0,100).Select(x => 0f)
                            .Concat(Enumerable.Range(10,10).Select(x => (float)x)).ToArray();
            osp.AssertReadsExpected(expected);
        }

        /// <summary>
        /// ステレオソースに TimeSpan でプリディレイを追加できることを確認する。
        /// </summary>
        [Test]
        public void CanAddPreDelayToStereoSourceUsingTimeSpan()
        {
            var source = new TestSampleProvider(100, 2) { Position = 10 };
            var osp = new OffsetSampleProvider(source) { DelayBy = TimeSpan.FromSeconds(1) };

            var expected = Enumerable.Range(0, 200).Select(x => 0f)
                            .Concat(Enumerable.Range(10, 10).Select(x => (float)x)).ToArray();
            osp.AssertReadsExpected(expected);
        }
        
        /// <summary>
        /// TimeSpan で設定したプリディレイが正しく返ることを確認する。
        /// </summary>
        [Test]
        public void SettingPreDelayUsingTimeSpanReturnsCorrectTimeSpan()
        {
            var source = new TestSampleProvider(100, 2) { Position = 10 };
            var osp = new OffsetSampleProvider(source) { DelayBy = TimeSpan.FromSeconds(2.5) };

            ClassicAssert.AreEqual(2500, (int) osp.DelayBy.TotalMilliseconds);
            ClassicAssert.AreEqual(500, osp.DelayBySamples);
        }

        /// <summary>
        /// SkipOverSamples で先頭をスキップできることを確認する。
        /// </summary>
        [Test]
        public void CanSkipOver()
        {
            var source = new TestSampleProvider(32000, 1);
            var osp = new OffsetSampleProvider(source) {SkipOverSamples = 17};

            var expected = new float[] { 17,18,19,20,21,22,23,24 };
            osp.AssertReadsExpected(expected);
        }

        /// <summary>
        /// TakeSamples で指定サンプル数だけ取得できることを確認する。
        /// </summary>
        [Test]
        public void CanTake()
        {
            var source = new TestSampleProvider(32000, 1);
            var osp = new OffsetSampleProvider(source) {TakeSamples = 7};

            var expected = new float[] { 0, 1, 2, 3, 4, 5, 6 };
            osp.AssertReadsExpected(expected, 10);
        }


        /// <summary>
        /// Take で 30 秒分だけ取得できることを確認する。
        /// </summary>
        [Test]
        public void CanTakeThirtySeconds()
        {
            var source = new TestSampleProvider(16000, 1);
            var osp = new OffsetSampleProvider(source) { Take = TimeSpan.FromSeconds(30) };
            var buffer = new float[16000];
            var totalRead = 0;
            while (true)
            {
                var read = osp.Read(buffer, 0, buffer.Length);
                totalRead += read;
                if (read == 0) break;
                ClassicAssert.IsTrue(totalRead <= 480000);

            }
            ClassicAssert.AreEqual(480000, totalRead);

        }

        /// <summary>
        /// リードアウト（末尾無音）を追加できることを確認する。
        /// </summary>
        [Test]
        public void CanAddLeadOut()
        {
            var source = new TestSampleProvider(32000, 1, 10);
            var osp = new OffsetSampleProvider(source) {LeadOutSamples = 5};

            var expected = new float[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0 };
            osp.AssertReadsExpected(expected, 100);
            var expected2 = new float[] { };
            osp.AssertReadsExpected(expected2, 100);
        }

        /// <summary>
        /// Take なしの場合、リードアウトはソース読み取り完了後にのみ始まることを確認する。
        /// </summary>
        [Test]
        public void LeadOutWithoutTakeOnlyBeginsAfterSourceIsCompletelyRead()
        {
            var source = new TestSampleProvider(32000, 1, 10);
            var osp = new OffsetSampleProvider(source) { LeadOutSamples = 5 };

            var expected = new float[] { 0, 1, 2, 3, 4, 5, 6 };
            osp.AssertReadsExpected(expected, 7);
            var expected2 = new float[] { 7, 8, 9, 0, 0, 0 };
            osp.AssertReadsExpected(expected2, 6);
            var expected3 = new float[] { 0, 0 };
            osp.AssertReadsExpected(expected3, 6);
        }

        /// <summary>
        /// WaveFormat がソースと同一であることを確認する。
        /// </summary>
        [Test]
        public void WaveFormatIsSampeAsSource()
        {
            var source = new TestSampleProvider(32000, 1, 10);
            var osp = new OffsetSampleProvider(source);
            ClassicAssert.AreEqual(source.WaveFormat, osp.WaveFormat);
        }


        /// <summary>
        /// プリディレイの内部状態が維持されることを確認する。
        /// </summary>
        [Test]
        public void MaintainsPredelayState()
        {
            var source = new TestSampleProvider(32000, 1) {Position = 10};
            var osp = new OffsetSampleProvider(source) {DelayBySamples = 10};

            var expected = new float[] {0, 0, 0, 0, 0,}; 
            osp.AssertReadsExpected(expected);
            var expected2 = new float[] {0, 0, 0, 0, 0,}; 
            osp.AssertReadsExpected(expected2);
            var expected3 = new float[] {10, 11, 12, 13, 14, 15}; 
            osp.AssertReadsExpected(expected3);
        }

        /// <summary>
        /// Take の直後にリードアウトを続けられることを確認する。
        /// </summary>
        [Test]
        public void CanFollowTakeWithLeadout()
        {
            var source = new TestSampleProvider(32000, 1) { Position = 10 };
            var osp = new OffsetSampleProvider(source) { TakeSamples = 10, LeadOutSamples = 5};

            
            var expected = new float[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 0, 0, 0, 0, 0 };
            osp.AssertReadsExpected(expected);
        }

        /// <summary>
        /// Take の読み取り状態が維持されることを確認する。
        /// </summary>
        [Test]
        public void MaintainsTakeState()
        {
            var source = new TestSampleProvider(32000, 1);
            var osp = new OffsetSampleProvider(source) {TakeSamples = 15};

            var expected = new float[] { 0, 1, 2, 3, 4, 5, 6, 7 };
            osp.AssertReadsExpected(expected);
            var expected2 = new float[] { 8, 9, 10, 11, 12, 13, 14 };
            osp.AssertReadsExpected(expected2, 20);
        }

        /// <summary>
        /// Read 呼び出し後に DelayBySamples を設定すると例外になることを確認する。
        /// </summary>
        [Test]
        public void CantSetDelayBySamplesAfterCallingRead()
        {
            var source = new TestSampleProvider(32000, 1);
            var osp = new OffsetSampleProvider(source);
            var buffer = new float[10];
            osp.Read(buffer, 0, buffer.Length);

            Assert.Throws<InvalidOperationException>(() => osp.DelayBySamples = 4);
        }

        /// <summary>
        /// Read 呼び出し後に LeadOutSamples を設定すると例外になることを確認する。
        /// </summary>
        [Test]
        public void CantSetLeadOutSamplesAfterCallingRead()
        {
            var source = new TestSampleProvider(32000, 1);
            var osp = new OffsetSampleProvider(source);
            var buffer = new float[10];
            osp.Read(buffer, 0, buffer.Length);

            Assert.Throws<InvalidOperationException>(() => osp.LeadOutSamples = 4);
        }

        /// <summary>
        /// Read 呼び出し後に SkipOverSamples を設定すると例外になることを確認する。
        /// </summary>
        [Test]
        public void CantSetSkipOverSamplesAfterCallingRead()
        {
            var source = new TestSampleProvider(32000, 1);
            var osp = new OffsetSampleProvider(source);
            var buffer = new float[10];
            osp.Read(buffer, 0, buffer.Length);

            Assert.Throws<InvalidOperationException>(() => osp.SkipOverSamples = 4);
        }

        /// <summary>
        /// Read 呼び出し後に TakeSamples を設定すると例外になることを確認する。
        /// </summary>
        [Test]
        public void CantSetTakeSamplesAfterCallingRead()
        {
            var source = new TestSampleProvider(32000, 1);
            var osp = new OffsetSampleProvider(source);
            var buffer = new float[10];
            osp.Read(buffer, 0, buffer.Length);

            Assert.Throws<InvalidOperationException>(() => osp.TakeSamples = 4);
        }

        /// <summary>
        /// ソース全体をスキップした場合に正しく空になることを確認する。
        /// </summary>
        [Test]
        public void HandlesSkipOverEntireSourceCorrectly()
        {
            var source = new TestSampleProvider(32000, 1, 10);
            var osp = new OffsetSampleProvider(source);
            osp.SkipOverSamples = 20;

            var expected = new float[] { };
            osp.AssertReadsExpected(expected, 20);
        }


        /// <summary>
        /// ブロック境界に揃わない DelayBySamples を設定すると例外になることを確認する。
        /// </summary>
        [Test]
        public void CantSetNonBlockAlignedDelayBySamples()
        {
            var source = new TestSampleProvider(32000, 2);
            var osp = new OffsetSampleProvider(source);

            var ex = Assert.Throws<ArgumentException>(() => osp.DelayBySamples = 3);
            ClassicAssert.That(ex.Message.Contains("DelayBySamples"));
        }

        /// <summary>
        /// ブロック境界に揃わない SkipOverSamples を設定すると例外になることを確認する。
        /// </summary>
        [Test]
        public void CantSetNonBlockAlignedSkipOverSamples()
        {
            var source = new TestSampleProvider(32000, 2);
            var osp = new OffsetSampleProvider(source);

            var ex = Assert.Throws<ArgumentException>(() => osp.SkipOverSamples = 3);
            ClassicAssert.That(ex.Message.Contains("SkipOverSamples"));
        }

        /// <summary>
        /// ブロック境界に揃わない TakeSamples を設定すると例外になることを確認する。
        /// </summary>
        [Test]
        public void CantSetNonBlockAlignedTakeSamples()
        {
            var source = new TestSampleProvider(32000, 2);
            var osp = new OffsetSampleProvider(source);

            var ex = Assert.Throws<ArgumentException>(() => osp.TakeSamples = 3);
            ClassicAssert.That(ex.Message.Contains("TakeSamples"));
        }


        /// <summary>
        /// ブロック境界に揃わない LeadOutSamples を設定すると例外になることを確認する。
        /// </summary>
        [Test]
        public void CantSetNonBlockAlignedLeadOutSamples()
        {
            var source = new TestSampleProvider(32000, 2);
            var osp = new OffsetSampleProvider(source);

            var ex = Assert.Throws<ArgumentException>(() => osp.LeadOutSamples = 3);
            ClassicAssert.That(ex.Message.Contains("LeadOutSamples"));
        }

        // TODO: Test that Read offset parameter is respected
    }
}
