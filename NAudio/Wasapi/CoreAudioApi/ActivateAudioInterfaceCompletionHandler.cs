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
                tcs.TrySetException(Marshal.GetExceptionForHR(hr, new IntPtr(-1)));
                return;
            }
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
                tcs.TrySetException(Marshal.GetExceptionForHR(hr, new IntPtr(-1)));
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
                tcs.TrySetException(Marshal.GetExceptionForHR(hr, new IntPtr(-1)));
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
