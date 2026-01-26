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
            var pAudioClient = (IAudioClient)Marshal.GetObjectForIUnknown(ptr);
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


        public TaskAwaiter<IAudioClient> GetAwaiter()
        {
            return tcs.Task.GetAwaiter();
        }
    }
}
