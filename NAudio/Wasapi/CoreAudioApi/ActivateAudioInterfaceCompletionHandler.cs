using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi.Interfaces;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace NAudio.Wasapi.CoreAudioApi
{
    internal class ActivateAudioInterfaceCompletionHandler<T> :
    IActivateAudioInterfaceCompletionHandler, IAgileObject
    {
        private Action<T> initializeAction;
        private TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

        public ActivateAudioInterfaceCompletionHandler(
            Action<T> initializeAction)
        {
            this.initializeAction = initializeAction;
        }

        public void ActivateCompleted(IActivateAudioInterfaceAsyncOperation activateOperation)
        {
            activateOperation.GetActivateResult(out var hr, out var ptr);
            if (hr != 0)
            {
                // HRESULT→例外変換は共通ヘルパーへ集約 (F-004 リグレッション保護の核心)
                tcs.TrySetException(ActivateAudioInterfaceResult.ToException(hr));
                if (ptr != IntPtr.Zero) Marshal.Release(ptr);
                return;
            }
            try
            {
                var pAudioClient = (T)Marshal.GetObjectForIUnknown(ptr);
                try
                {
                    initializeAction(pAudioClient);
                    tcs.SetResult(pAudioClient);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }
            finally
            {
                // GetActivateResult が返した IUnknown ptr は AddRef 済み。
                // GetObjectForIUnknown が RCW 用にさらに AddRef するため、
                // ここで元の ptr の参照を Release しないと COM オブジェクトがリークする。
                // (1版 / ProcessLoopback 版は既に Release してるのに Generic 版だけ漏れていた)
                Marshal.Release(ptr);
            }
        }


        public TaskAwaiter<T> GetAwaiter()
        {
            return tcs.Task.GetAwaiter();
        }
    }

    /// <summary>
    /// Process Loopback 用。GetActivateResult で得た IUnknown ポインタをそのまま返す。
    /// RCW をコールバック（多くの場合 MTA）で作ると STA で使うときに E_NOINTERFACE になるため、
    /// 呼び出し側の STA（UI）スレッドで GetTypedObjectForIUnknown と WasapiCapture 構築を行う。
    /// </summary>
    internal class ProcessLoopbackActivateCompletionHandler : IActivateAudioInterfaceCompletionHandler, IAgileObject
    {
        private readonly TaskCompletionSource<IntPtr> tcs = new TaskCompletionSource<IntPtr>();

        /// <summary>活性化完了を待つ Task。CancellationToken 対応の待機のため公開する。</summary>
        public Task<IntPtr> Task => tcs.Task;

        public void ActivateCompleted(IActivateAudioInterfaceAsyncOperation activateOperation)
        {
            activateOperation.GetActivateResult(out var hr, out var ptr);
            if (hr != 0)
            {
                // HRESULT→例外変換は共通ヘルパーへ集約 (F-004 リグレッション保護の核心)
                tcs.TrySetException(ActivateAudioInterfaceResult.ToException(hr));
                if (ptr != IntPtr.Zero) Marshal.Release(ptr);
                return;
            }
            // 既にキャンセル済み(TrySetResult が false)なら ptr を誰も受け取らないので Release してリークを防ぐ。
            if (!tcs.TrySetResult(ptr) && ptr != IntPtr.Zero)
                Marshal.Release(ptr);
        }

        /// <summary>呼び出し側のキャンセルで待機を打ち切る。後続の ActivateCompleted で ptr は Release される。</summary>
        public void Cancel() => tcs.TrySetCanceled();

        public TaskAwaiter<IntPtr> GetAwaiter()
        {
            return tcs.Task.GetAwaiter();
        }
    }

    /// <summary>
    /// ActivateAudioInterfaceAsync の HRESULT を例外へ変換する共通ヘルパー。
    /// 各 CompletionHandler 実装でエラー経路を共有し、F-004 修正の分散コピペを防ぐ。
    /// </summary>
    internal static class ActivateAudioInterfaceResult
    {
        /// <summary>
        /// HRESULT を例外に変換する。Marshal.GetExceptionForHR は S_FALSE(1) 等の成功扱い HRESULT で
        /// null を返す仕様のため、null を TrySetException に渡すと ArgumentNullException が漏れて
        /// await CreateForProcessCaptureAsync(...) が永遠に hang する。COMException でフォールバックし、
        /// HRESULT が必ず例外として伝搬するようにする (F-004 リグレッション保護)。
        /// </summary>
        public static Exception ToException(int hr)
        {
            return Marshal.GetExceptionForHR(hr, new IntPtr(-1))
                   ?? new COMException($"ActivateAudioInterfaceAsync failed (HRESULT 0x{hr:X8})", hr);
        }
    }
}
