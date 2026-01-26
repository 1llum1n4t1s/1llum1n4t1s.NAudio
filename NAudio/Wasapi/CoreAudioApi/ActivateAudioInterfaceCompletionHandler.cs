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
    /// Process Loopback 用。GetActivateResult で得たポインタから IAudioClient を明示的に QueryInterface してから RCW を作成する。
    /// IAudioClient2 ポインタを直接 GetObjectForIUnknown すると IAudioClient へのキャストが失敗する環境があるため。
    /// </summary>
    internal class ProcessLoopbackActivateCompletionHandler : IActivateAudioInterfaceCompletionHandler, IAgileObject
    {
        private static readonly Guid IID_IAudioClient = new Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
        private readonly Action<IAudioClient> initializeAction;
        private readonly TaskCompletionSource<IAudioClient> tcs = new TaskCompletionSource<IAudioClient>();

        public ProcessLoopbackActivateCompletionHandler(Action<IAudioClient> initializeAction)
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
            var iid = IID_IAudioClient;
            var iacPtr = IntPtr.Zero;
            try
            {
                var qhr = Marshal.QueryInterface(ptr, in iid, out iacPtr);
                if (qhr != 0 || iacPtr == IntPtr.Zero)
                {
                    tcs.TrySetException(Marshal.GetExceptionForHR(qhr, new IntPtr(-1)));
                    return;
                }
                var pAudioClient = (IAudioClient)Marshal.GetTypedObjectForIUnknown(iacPtr, typeof(IAudioClient));
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
                if (iacPtr != IntPtr.Zero)
                    Marshal.Release(iacPtr);
                Marshal.Release(ptr);
            }
        }

        public TaskAwaiter<IAudioClient> GetAwaiter()
        {
            return tcs.Task.GetAwaiter();
        }
    }
}
