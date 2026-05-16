using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using NAudio.Wasapi.CoreAudioApi;
using NAudio.Wasapi.CoreAudioApi.Interfaces;

namespace NAudioTests.Wasapi
{
    /// <summary>
    /// F-004 (Marshal.GetExceptionForHR null フォールバック) のリグレッション保護用ユニットテスト。
    /// Round 2 レビューで「Process Loopback の致命バグ修正を守るテストが CI 完全カバーゼロ」と
    /// 指摘されたため新規追加。
    ///
    /// テスト対象は internal クラス ProcessLoopbackActivateCompletionHandler。
    /// NAudio.csproj に &lt;InternalsVisibleTo Include="NAudioTests" /&gt; を追加してアクセス可能にしている。
    /// </summary>
    [TestFixture]
    public class ActivateAudioInterfaceCompletionHandlerTests
    {
        /// <summary>
        /// テスト用の手書き Mock。GetActivateResult が指定した hr / ptr をそのまま返す。
        /// Moq では COM ComImport interface のモック作成が技術的に不安定なので手書きにする。
        /// </summary>
        private class FakeActivateOperation : IActivateAudioInterfaceAsyncOperation
        {
            public int ResultHr { get; set; }
            public IntPtr ResultPtr { get; set; }

            public void GetActivateResult(out int activateResult, out IntPtr activatedInterface)
            {
                activateResult = ResultHr;
                activatedInterface = ResultPtr;
            }
        }

        /// <summary>
        /// hr=S_OK + 有効な ptr → Task が IntPtr で完了することを確認。
        /// 正常系の基本動作。
        /// </summary>
        [Test]
        public void ProcessLoopbackHandler_OnSuccess_TaskCompletesWithPtr()
        {
            var handler = new ProcessLoopbackActivateCompletionHandler();
            var expectedPtr = new IntPtr(0x12345);
            var fakeOp = new FakeActivateOperation { ResultHr = 0, ResultPtr = expectedPtr };

            handler.ActivateCompleted(fakeOp);

            var awaiter = handler.GetAwaiter();
            ClassicAssert.IsTrue(awaiter.IsCompleted, "Task should complete synchronously");
            ClassicAssert.AreEqual(expectedPtr, awaiter.GetResult());
        }

        /// <summary>
        /// hr=E_FAIL (0x80004005) → Task.Exception に COMException が入ることを確認。
        /// Marshal.GetExceptionForHR が正規 HRESULT を COMException に変換する通常パス。
        /// </summary>
        [Test]
        public void ProcessLoopbackHandler_OnEFail_TaskFaultsWithCOMException()
        {
            var handler = new ProcessLoopbackActivateCompletionHandler();
            var fakeOp = new FakeActivateOperation
            {
                ResultHr = unchecked((int)0x80004005), // E_FAIL
                ResultPtr = IntPtr.Zero
            };

            handler.ActivateCompleted(fakeOp);

            var awaiter = handler.GetAwaiter();
            ClassicAssert.IsTrue(awaiter.IsCompleted);
            var ex = Assert.Throws<COMException>(() => awaiter.GetResult());
            ClassicAssert.AreEqual(unchecked((int)0x80004005), ex.HResult,
                "COMException.HResult が E_FAIL を保持しているはず");
        }

        /// <summary>
        /// hr=S_FALSE (1) は Marshal.GetExceptionForHR が null を返す代表的なケース。
        /// 旧コードは tcs.TrySetException(null) で ArgumentNullException が漏れて
        /// Task が永遠に未完了 (hang) になっていた。
        /// F-004 修正は `?? new COMException(...)` フォールバックでこれを解消した。
        /// 本テストはそのリグレッション (= 再びフォールバックが消える事故) を検知する。
        /// </summary>
        [Test]
        public void ProcessLoopbackHandler_OnSFalse_AppliesCOMExceptionFallback_F004Regression()
        {
            var handler = new ProcessLoopbackActivateCompletionHandler();
            // S_FALSE (1) は Marshal.GetExceptionForHR が null を返す HRESULT の代表。
            var fakeOp = new FakeActivateOperation { ResultHr = 1, ResultPtr = IntPtr.Zero };

            handler.ActivateCompleted(fakeOp);

            var awaiter = handler.GetAwaiter();
            ClassicAssert.IsTrue(awaiter.IsCompleted,
                "Task should NOT hang (F-004 regression check). Old code threw ArgumentNullException via TrySetException(null) which left tcs incomplete.");

            // フォールバック COMException は HResult を保持する
            var ex = Assert.Throws<COMException>(() => awaiter.GetResult());
            ClassicAssert.AreEqual(1, ex.HResult, "Fallback COMException should carry the original HRESULT (S_FALSE=1)");
        }

        /// <summary>
        /// `Marshal.GetExceptionForHR(1, IntPtr(-1))` が **本当に null を返す** ことを確認する。
        /// .NET BCL 仕様変更で挙動が変わるとフォールバック自体が不要になる (or 別問題が起きる) ため、
        /// プラットフォーム前提を継続的に検証する低レベルテスト。
        /// </summary>
        [Test]
        public void Marshal_GetExceptionForHR_ReturnsNullForSFalse()
        {
            // S_FALSE (1) は成功扱いなので、Marshal.GetExceptionForHR は null を返す仕様。
            // この前提が崩れたら ProcessLoopbackHandler 側のフォールバックロジックを見直す必要あり。
            var ex = Marshal.GetExceptionForHR(1, new IntPtr(-1));
            ClassicAssert.IsNull(ex, "Marshal.GetExceptionForHR(S_FALSE) must return null on this platform (BCL invariant assumed by F-004 fix)");
        }
    }
}
