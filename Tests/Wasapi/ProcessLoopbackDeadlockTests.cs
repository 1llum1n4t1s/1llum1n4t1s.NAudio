using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using NAudio.CoreAudioApi;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NAudioTests.Wasapi
{
    /// <summary>
    /// #B1-001 回帰テスト。Process Loopback を UI 相当 (STA + 非 null SynchronizationContext) スレッドで
    /// 開始 → 停止 (Dispose) したとき、CaptureThread の syncContext.Send(client.Stop()) と
    /// Dispose 側の captureThread.Join() が相互待ちでデッドロックしないことを検証する。
    ///
    /// 修正前は Dispose() を呼ぶと UI スレッドが Join でブロックし、CaptureThread の停止 Send が
    /// 処理されずに永久ハングしていた。修正後は Stop を Dispose(UI スレッド)側へ委譲したため完了する。
    ///
    /// 実デバイス / COM 活性化に依存するため IntegrationTest。CI では除外され、実機で手動実行する。
    /// </summary>
    [TestFixture]
    public class ProcessLoopbackDeadlockTests
    {
        /// <summary>
        /// UI 相当 (STA + 非 null SynchronizationContext) スレッドで Process Loopback を
        /// 開始 → 停止 (Dispose) しても #B1-001 のデッドロックが起きないことを検証する。
        /// </summary>
        [Test]
        [Category("IntegrationTest")]
        [Apartment(ApartmentState.STA)]
        public void Dispose_OnUiThread_DoesNotDeadlock()
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            // WPF の UI スレッドと同じ「STA + DispatcherSynchronizationContext」状況を再現する。
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));

            Exception error = null;
            var cycleCompleted = false;

            dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    var capture = await WasapiCapture.CreateForProcessCaptureAsync(
                        Process.GetCurrentProcess().Id, includeProcessTree: false);
                    capture.StartRecording();
                    await Task.Delay(500);
                    capture.Dispose(); // 修正前はここで Join × syncContext.Send が相互待ちしてデッドロック
                    cycleCompleted = true;
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    dispatcher.InvokeShutdown();
                }
            });

            // ウォッチドッグ: デッドロック時に 20 秒でメッセージポンプを強制停止し、テストをハングさせない。
            var watchdog = new Thread(() =>
            {
                Thread.Sleep(20000);
                try { dispatcher.InvokeShutdown(); } catch { /* 既に shutdown 済みなら無視 */ }
            })
            { IsBackground = true };
            watchdog.Start();

            // メッセージポンプ。正常時は Dispose 完了 → InvokeShutdown で即座に抜ける。
            // デッドロック時は Dispose の Join で止まり、ウォッチドッグの InvokeShutdown でのみ抜ける。
            Dispatcher.Run();

            if (!cycleCompleted && error != null &&
                (error is COMException || error is InvalidOperationException || error is NotSupportedException))
            {
                // Process Loopback 非対応環境 / 活性化失敗。デッドロック検証自体は成立しないが、ハングはしていない。
                Assert.Ignore($"Process Loopback 活性化に失敗（環境依存の可能性）: {error.GetType().Name}: {error.Message}");
            }

            ClassicAssert.IsTrue(cycleCompleted,
                "UI スレッドでの Process Loopback 開始→停止が 20 秒以内に完了しなかった（#B1-001 デッドロックの疑い）");
        }
    }
}
