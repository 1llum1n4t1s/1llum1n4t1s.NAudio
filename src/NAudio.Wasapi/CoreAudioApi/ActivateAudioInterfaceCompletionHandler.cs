using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;

namespace NAudio.CoreAudioApi;

[GeneratedComClass]
internal partial class ActivateAudioInterfaceCompletionHandler :
IActivateAudioInterfaceCompletionHandler, IAgileObject
{
    private readonly Action<IAudioClient> initializeAction;
    private readonly TaskCompletionSource<IAudioClient> tcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ActivateAudioInterfaceCompletionHandler(
        Action<IAudioClient> initializeAction)
    {
        this.initializeAction = initializeAction;
    }

    public void ActivateCompleted(IntPtr activateOperationPtr)
    {
        IActivateAudioInterfaceAsyncOperation activateOperation = null;
        IAudioClient audioClient = null;
        IntPtr unkPtr = IntPtr.Zero;
        bool ownershipTransferred = false;

        // activateOperationPtr is a borrowed callback parameter — we don't own it.
        // GetOrCreateObjectForComInstance (UniqueInstance) takes its own QI'd ref,
        // which we must FinalRelease before returning to keep ref counts balanced.
        try
        {
            activateOperation = (IActivateAudioInterfaceAsyncOperation)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                activateOperationPtr, CreateObjectFlags.UniqueInstance);

            // First get the activation results, and see if anything bad happened then
            activateOperation.GetActivateResult(out int hr, out unkPtr);
            if (hr != 0)
            {
                tcs.TrySetException(ActivateAudioInterfaceResult.ToException(hr));
                return;
            }
            if (unkPtr == IntPtr.Zero)
            {
                const int E_POINTER = unchecked((int)0x80004003);
                tcs.TrySetException(ActivateAudioInterfaceResult.ToException(E_POINTER));
                return;
            }

            // Wrap as the base IAudioClient: the process-loopback virtual device returns a client
            // that does NOT support IAudioClient2, so casting to IAudioClient2 here would throw.
            // Callers that need IAudioClient2 features QI for it from the returned client.
            try
            {
                audioClient = (IAudioClient)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                    unkPtr, CreateObjectFlags.UniqueInstance);
            }
            finally
            {
                Marshal.Release(unkPtr);
                unkPtr = IntPtr.Zero;
            }

            // Next try to call the client's (synchronous, blocking) initialization method.
            initializeAction(audioClient);

            // Cancellation can win while the native operation is still completing. In that case
            // no caller receives the COM wrapper, so release it here rather than leaking it.
            ownershipTransferred = tcs.TrySetResult(audioClient);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }
        finally
        {
            if (unkPtr != IntPtr.Zero)
            {
                Marshal.Release(unkPtr);
            }
            if (!ownershipTransferred && (object)audioClient is ComObject audioClientComObject)
            {
                audioClientComObject.FinalRelease();
            }
            if ((object)activateOperation is ComObject co)
            {
                co.FinalRelease();
            }
        }
    }

    public TaskAwaiter<IAudioClient> GetAwaiter()
    {
        return tcs.Task.GetAwaiter();
    }

    /// <summary>
    /// Cancels the managed wait. The native operation may still complete; its callback releases
    /// any COM result that can no longer be delivered to a caller.
    /// </summary>
    public void Cancel(CancellationToken cancellationToken)
    {
        tcs.TrySetCanceled(cancellationToken);
    }

    /// <summary>
    /// The underlying activation task. Await this with <c>ConfigureAwait(false)</c> when the
    /// caller may be on a thread with a synchronization context (e.g. WPF/WinForms) to avoid
    /// marshalling the continuation back onto a thread that might be blocked.
    /// </summary>
    public Task<IAudioClient> Completion => tcs.Task;
}

internal static class ActivateAudioInterfaceResult
{
    public static Exception ToException(int hr)
    {
        // GetExceptionForHR returns null for non-failing success codes such as S_FALSE. The
        // activation contract treats every non-zero result as unsuccessful, so always provide an
        // exception and never leave the completion task unresolved.
        return Marshal.GetExceptionForHR(hr, new IntPtr(-1))
            ?? new AudioInterfaceActivationException(hr);
    }
}

internal sealed class AudioInterfaceActivationException : COMException
{
    public AudioInterfaceActivationException(int hr)
        : base($"ActivateAudioInterfaceAsync failed (HRESULT 0x{hr:X8})", hr)
    {
    }
}
