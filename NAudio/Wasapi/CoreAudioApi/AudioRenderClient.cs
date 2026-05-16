using System;
using NAudio.CoreAudioApi.Interfaces;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Audio Render Client
    /// </summary>
    public class AudioRenderClient : IDisposable
    {
        IAudioRenderClient audioRenderClientInterface;

        internal AudioRenderClient(IAudioRenderClient audioRenderClientInterface)
        {
            this.audioRenderClientInterface = audioRenderClientInterface;
        }

        /// <summary>
        /// Gets a pointer to the buffer
        /// </summary>
        /// <param name="numFramesRequested">Number of frames requested</param>
        /// <returns>Pointer to the buffer</returns>
        public IntPtr GetBuffer(int numFramesRequested)
        {
            Marshal.ThrowExceptionForHR(audioRenderClientInterface.GetBuffer(numFramesRequested, out var bufferPointer));
            return bufferPointer;
        }

        /// <summary>
        /// Release buffer
        /// </summary>
        /// <param name="numFramesWritten">Number of frames written</param>
        /// <param name="bufferFlags">Buffer flags</param>
        public void ReleaseBuffer(int numFramesWritten,AudioClientBufferFlags bufferFlags)
        {
            Marshal.ThrowExceptionForHR(audioRenderClientInterface.ReleaseBuffer(numFramesWritten, bufferFlags));
        }

        /// <summary>
        /// Release the COM object
        /// </summary>
        public void Dispose()
        {
            if (audioRenderClientInterface != null)
            {
                // although GC would do this for us, we want it done now
                // to let us reopen WASAPI
                Marshal.ReleaseComObject(audioRenderClientInterface);
                audioRenderClientInterface = null;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// ファイナライザ。
        /// 前回 /rere で AudioClient finalizer から DisposeSubClients を削除した影響で、
        /// AudioClient を Dispose し忘れた場合に sub-client (本クラス) の COM オブジェクトが
        /// 解放されない経路ができていた。MediaType / PropertyStore と同じく「警告のみ」方針で
        /// Dispose 漏れを開発者に通知する。CLR の RCW 内部 finalizer による最終解放には
        /// 引き続き依存。
        /// </summary>
        ~AudioRenderClient()
        {
            if (audioRenderClientInterface != null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "WARNING: AudioRenderClient が Dispose されずに finalize された。AudioClient を using か Dispose() で明示解放してください。");
            }
        }
    }
}
