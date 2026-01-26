using System;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// 診断用。WASAPI キャプチャで GetBuffer から返されたパケットごとのフラグ情報。
    /// Process Loopback で無音になる原因切り分け（AUDCLNT_BUFFERFLAGS_SILENT が常に立っているか等）に利用できる。
    /// </summary>
    public class WasapiCapturePacketEventArgs : EventArgs
    {
        /// <summary>
        /// IAudioCaptureClient.GetBuffer で返されたバッファフラグ。
        /// </summary>
        public AudioClientBufferFlags BufferFlags { get; }

        /// <summary>
        /// このパケットのフレーム数。
        /// </summary>
        public int FramesAvailable { get; }

        /// <summary>
        /// SILENT フラグが立っている場合は true。この場合 NAudio は Array.Clear でゼロを書き込む。
        /// </summary>
        public bool IsSilent => (BufferFlags & AudioClientBufferFlags.Silent) == AudioClientBufferFlags.Silent;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="bufferFlags">バッファフラグ。</param>
        /// <param name="framesAvailable">フレーム数。</param>
        public WasapiCapturePacketEventArgs(AudioClientBufferFlags bufferFlags, int framesAvailable)
        {
            BufferFlags = bufferFlags;
            FramesAvailable = framesAvailable;
        }
    }
}
