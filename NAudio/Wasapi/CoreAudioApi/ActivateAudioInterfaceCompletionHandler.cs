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
                // Marshal.GetExceptionForHR は S_FALSE(1) 等の場合に null を返す仕様。
                // null を TrySetException に渡すと ArgumentNullException が漏れて
                // await CreateForProcessCaptureAsync(...) が永遠に hang する原因になる。
                // COMException でフォールバックして HRESULT が必ず例外として伝搬するようにする。
                var ex = Marshal.GetExceptionForHR(hr, new IntPtr(-1))
                         ?? new COMException($"ActivateAudioInterfaceAsync failed (HRESULT 0x{hr:X8})", hr);
                tcs.TrySetException(ex);
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


    internal class ActivateAudioInterfaceCompletionHandler1 :
    IActivateAudioInterfaceCompletionHandler, IAgileObject
    {
        private Action<IAudioClient> initializeAction;
        private TaskCompletionSource<IAudioClient> tcs = new TaskCompletionSource<IAudioClient>();

        public ActivateAudioInterfaceCompletionHandler1(
            Action<IAudioClient> initializeAction)
        {
            this.initializeAction = initializeAction;
        }

        public void ActivateCompleted(IActivateAudioInterfaceAsyncOperation activateOperation)
        {
            activateOperation.GetActivateResult(out var hr, out var ptr);
            if (hr != 0)
            {
                // Marshal.GetExceptionForHR が null を返した場合の COMException フォールバック
                // (Generic 版と同じ理由)
                var ex = Marshal.GetExceptionForHR(hr, new IntPtr(-1))
                         ?? new COMException($"ActivateAudioInterfaceAsync failed (HRESULT 0x{hr:X8})", hr);
                tcs.TrySetException(ex);
                if (ptr != IntPtr.Zero) Marshal.Release(ptr);
                return;
            }
            try
            {
                var pAudioClient = (IAudioClient)Marshal.GetTypedObjectForIUnknown(ptr, typeof(IAudioClient));
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
                Marshal.Release(ptr);
            }
        }


        public TaskAwaiter<IAudioClient> GetAwaiter()
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

        public void ActivateCompleted(IActivateAudioInterfaceAsyncOperation activateOperation)
        {
            activateOperation.GetActivateResult(out var hr, out var ptr);
            if (hr != 0)
            {
                // Marshal.GetExceptionForHR が null を返した場合の COMException フォールバック。
                // null を TrySetException に渡すと ArgumentNullException で Task hang する。
                var ex = Marshal.GetExceptionForHR(hr, new IntPtr(-1))
                         ?? new COMException($"ActivateAudioInterfaceAsync failed (HRESULT 0x{hr:X8})", hr);
                tcs.TrySetException(ex);
                if (ptr != IntPtr.Zero) Marshal.Release(ptr);
                return;
            }
            tcs.SetResult(ptr);
        }

        public TaskAwaiter<IntPtr> GetAwaiter()
        {
            return tcs.Task.GetAwaiter();
        }
    }
}
