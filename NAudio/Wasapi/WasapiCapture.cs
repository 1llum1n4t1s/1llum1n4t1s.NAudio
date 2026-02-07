using System;
using System.Threading;
using System.Runtime.InteropServices;
using NAudio.Wave;
using System.Threading.Tasks;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi;

// for consistency this should be in NAudio.Wave namespace, but left as it is for backwards compatibility
// ReSharper disable once CheckNamespace
namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Audio Capture using Wasapi
    /// See http://msdn.microsoft.com/en-us/library/dd370800%28VS.85%29.aspx
    /// </summary>
    public class WasapiCapture : IWaveIn
    {
        private const long ReftimesPerSec = 10000000;
        private const long ReftimesPerMillisec = 10000;
        private const int FALLBACK_BUFFER_LENGTH = 10000;
        private volatile CaptureState captureState;
        private byte[] recordBuffer;
        private Thread captureThread;
        private AudioClient audioClient;
        private int bytesPerFrame;
        private WaveFormat waveFormat;
        private bool initialized;
        private readonly SynchronizationContext syncContext;
        private readonly bool isUsingEventSync;
        private EventWaitHandle frameEventWaitHandle;
        private readonly int audioBufferMillisecondsLength;
        private AudioClientStreamFlags audioClientStreamFlags;
        private readonly bool isProcessLoopback;

        /// <summary>
        /// Indicates recorded data is available 
        /// </summary>
        public event EventHandler<WaveInEventArgs> DataAvailable;

        /// <summary>
        /// Indicates that all recorded data has now been received.
        /// </summary>
        public event EventHandler<StoppedEventArgs> RecordingStopped;

        /// <summary>
        /// 診断用。GetBuffer から返された各パケットのバッファフラグ（Silent 等）を通知する。
        /// Process Loopback で無音になる原因が OS の SILENT 返却かどうかの切り分けに利用できる。
        /// </summary>
        public event EventHandler<WasapiCapturePacketEventArgs> CapturePacketReceived;

        /// <summary>
        /// Initialises a new instance of the WASAPI capture class
        /// </summary>
        public WasapiCapture() : 
            this(GetDefaultCaptureDevice())
        {
        }

        /// <summary>
        /// Initialises a new instance of the WASAPI capture class
        /// </summary>
        /// <param name="captureDevice">Capture device to use</param>
        public WasapiCapture(MMDevice captureDevice)
            : this(captureDevice, false)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WasapiCapture"/> class.
        /// </summary>
        /// <param name="captureDevice">The capture device.</param>
        /// <param name="useEventSync">true if sync is done with event. false use sleep.</param>
        public WasapiCapture(MMDevice captureDevice, bool useEventSync) 
            : this(captureDevice, useEventSync, 100)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WasapiCapture" /> class.
        /// </summary>
        /// <param name="captureDevice">The capture device.</param>
        /// <param name="useEventSync">true if sync is done with event. false use sleep.</param>
        /// <param name="audioBufferMillisecondsLength">Length of the audio buffer in milliseconds. A lower value means lower latency but increased CPU usage.</param>
        public WasapiCapture(MMDevice captureDevice, bool useEventSync, int audioBufferMillisecondsLength)
            : this(captureDevice.AudioClient, useEventSync, audioBufferMillisecondsLength)
        {
            waveFormat = audioClient.MixFormat;
        }


        private WasapiCapture(AudioClient audioClient, bool useEventSync, int audioBufferMillisecondsLength)
            : this(audioClient, useEventSync, audioBufferMillisecondsLength, false)
        {
        }

        private WasapiCapture(AudioClient audioClient, bool useEventSync, int audioBufferMillisecondsLength, bool isProcessLoopback)
        {
            syncContext = SynchronizationContext.Current;
#if DEBUG
            if (isProcessLoopback && syncContext == null)
                System.Diagnostics.Debug.WriteLine("WasapiCapture (Process Loopback): SynchronizationContext.Current is null. Call CreateForProcessCaptureAsync and StartRecording from an STA thread (e.g. UI thread) with a synchronization context to avoid COM errors or invalid audio.");
#endif
            this.audioClient = audioClient;
            ShareMode = AudioClientShareMode.Shared;
            isUsingEventSync = useEventSync;
            this.audioBufferMillisecondsLength = audioBufferMillisecondsLength;
            this.isProcessLoopback = isProcessLoopback;
            // Process Loopback: LOOPBACK | AUTOCONVERTPCM でエンジン内部フォーマットから要求フォーマット(44.1k 16bit 2ch)へ変換を依頼。仮想デバイスが異なる内部フォーマットを持つ場合に必要。通常キャプチャは AUTOCONVERTPCM | SrcDefaultQuality。
            audioClientStreamFlags = isProcessLoopback
                ? AudioClientStreamFlags.Loopback | AudioClientStreamFlags.AutoConvertPcm
                : AudioClientStreamFlags.AutoConvertPcm | AudioClientStreamFlags.SrcDefaultQuality;
        }

        /// <summary>
        /// Creates a WasapiCapture instance for capturing audio from a specific process.
        /// </summary>
        /// <param name="processId">The process ID to capture audio from.</param>
        /// <param name="includeProcessTree">If true, includes the target process and its child processes; otherwise, excludes them.</param>
        /// <returns>A WasapiCapture instance configured for process audio capture.</returns>
        /// <remarks>
        /// Threading (Process Loopback): This method performs COM activation asynchronously; the resulting
        /// <see cref="WasapiCapture"/> and all its COM usage must be bound to a single STA thread (typically the UI thread).
        /// You must await this method from that STA thread and must not use ConfigureAwait(false) on this await
        /// (or on any await in the calling chain), so that the continuation runs on the same thread. That thread's
        /// <see cref="SynchronizationContext.Current"/> is captured and used for all IAudioClient/IAudioCaptureClient calls
        /// during capture. Call <see cref="StartRecording"/> from the same thread immediately after await. If you call
        /// from a thread with no SynchronizationContext (e.g. thread pool), Process Loopback may fail with E_NOINTERFACE
        /// or return invalid/placeholder audio.
        /// </remarks>
        public static async Task<WasapiCapture> CreateForProcessCaptureAsync(int processId, bool includeProcessTree)
        {
            // https://github.com/microsoft/Windows-classic-samples/blob/main/Samples/ApplicationLoopback/cpp/LoopbackCapture.cpp
            // 公式: GetActivateResult は IAudioClient のみを返す。IAudioClient2/3 へのキャストは E_NOINTERFACE の可能性あり。
            var activationParams = new AudioClientActivationParams
            {
                ActivationType = AudioClientActivationType.ProcessLoopback,
                ProcessLoopbackParams = new AudioClientProcessLoopbackParams
                {
                    ProcessLoopbackMode = includeProcessTree ? ProcessLoopbackMode.IncludeTargetProcessTree :
                        ProcessLoopbackMode.ExcludeTargetProcessTree,
                    TargetProcessId = (uint)processId
                }
            };
            var hBlobData = GCHandle.Alloc(activationParams, GCHandleType.Pinned);
            try
            {
                var data = hBlobData.AddrOfPinnedObject();
                var activateParams = new PropVariant
                {
                    vt = (short)VarEnum.VT_BLOB,
                    blobVal = new Blob
                    {
                        Length = Marshal.SizeOf(activationParams),
                        Data = data
                    }
                };
                const int processLoopbackBufferMs = 20;
                var icbh = new ProcessLoopbackActivateCompletionHandler();
                var hActivateParams = GCHandle.Alloc(activateParams, GCHandleType.Pinned);
                try
                {
                    NativeMethods.ActivateAudioInterfaceAsync(VIRTUAL_AUDIO_DEVICE_PROCESS_LOOPBACK, typeof(IAudioClient).GUID, hActivateParams.AddrOfPinnedObject(), icbh, out var activationOperation);
                    try
                    {
                        var ptr = await icbh;
                        try
                        {
                            var ac = (IAudioClient)Marshal.GetTypedObjectForIUnknown(ptr, typeof(IAudioClient));
                            var client = new AudioClient(ac);
                            var capture = new WasapiCapture(client, true, processLoopbackBufferMs, true);
                            // Process Loopback 仮想デバイスでは IAudioClient::GetMixFormat が E_NOTIMPL のため固定フォーマット。48kHz 16bit 2ch（44.1kHz だと無音になる環境あり）。
                            capture.WaveFormat = new WaveFormat(48000, 16, 2);
                            return capture;
                        }
                        finally
                        {
                            if (ptr != IntPtr.Zero)
                                Marshal.Release(ptr);
                        }
                    }
                    finally
                    {
                        if (activationOperation != null)
                            Marshal.ReleaseComObject(activationOperation);
                    }
                }
                finally
                {
                    hActivateParams.Free();
                }
            }
            finally
            {
                hBlobData.Free();
            }
        }
        
        private const string VIRTUAL_AUDIO_DEVICE_PROCESS_LOOPBACK = "VAD\\Process_Loopback";


        /// <summary>
        /// Share Mode - set before calling StartRecording
        /// </summary>
        public AudioClientShareMode ShareMode { get; set; }

        /// <summary>
        /// Current Capturing State
        /// </summary>
        public CaptureState CaptureState {  get { return captureState; } }

        /// <summary>
        /// Capturing wave format
        /// </summary>
        public virtual WaveFormat WaveFormat 
        {
            get
            {
                // for convenience, return a WAVEFORMATEX, instead of the real
                // WAVEFORMATEXTENSIBLE being used
                return waveFormat.AsStandardWaveFormat();
            }
            set { waveFormat = value; }
        }

        /// <summary>
        /// Gets the default audio capture device
        /// </summary>
        /// <returns>The default audio capture device</returns>
        public static MMDevice GetDefaultCaptureDevice()
        {
            var devices = new MMDeviceEnumerator();
            return devices.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
        }

        private void InitializeCaptureDevice()
        {
            if (initialized)
                return;

            var requestedDuration = ReftimesPerMillisec * audioBufferMillisecondsLength;

            var streamFlags = GetAudioClientStreamFlags();

            // If using EventSync, setup is specific with shareMode
            if (isUsingEventSync)
            {
                // Init Shared or Exclusive
                if (ShareMode == AudioClientShareMode.Shared)
                {
                    // With EventCallBack and Shared, both latencies must be set to 0
                    audioClient.Initialize(ShareMode, AudioClientStreamFlags.EventCallback | streamFlags, requestedDuration, 0,
                        waveFormat, Guid.Empty);
                }
                else
                {
                    // With EventCallBack and Exclusive, both latencies must equals
                    audioClient.Initialize(ShareMode, AudioClientStreamFlags.EventCallback | streamFlags, requestedDuration, requestedDuration,
                                        waveFormat, Guid.Empty);
                }

                // Create the Wait Event Handle
                frameEventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
                audioClient.SetEventHandle(frameEventWaitHandle.SafeWaitHandle.DangerousGetHandle());
            }
            else
            {
                // Normal setup for both sharedMode
                audioClient.Initialize(ShareMode,
                streamFlags,
                requestedDuration,
                0,
                waveFormat,
                Guid.Empty);
            }

            var bufferFrameCount = audioClient.BufferSize;
            bytesPerFrame = waveFormat.Channels * waveFormat.BitsPerSample / 8;
            var bufferSize = bufferFrameCount * bytesPerFrame;

            if (bufferSize < 1)
            {
                var fallbackSize = FALLBACK_BUFFER_LENGTH * bytesPerFrame;
                // System.Diagnostics.Debug.WriteLine("Buffer Size is faulted, The size is {0}, using fallback size instead {1}", bufferSize, fallbackSize);
                // Console.WriteLine("[!] Playback Buffer is Faulted");
                bufferSize = fallbackSize;
            }
            
            recordBuffer = new byte[bufferSize];
            
            //Debug.WriteLine(string.Format("record buffer size = {0}", this.recordBuffer.Length));

            initialized = true;
        }

        /// <summary>
        /// To allow overrides to specify different flags (e.g. loopback)
        /// </summary>
        protected virtual AudioClientStreamFlags GetAudioClientStreamFlags()
        {
            return audioClientStreamFlags;
        }

        /// <summary>
        /// Start Capturing
        /// </summary>
        /// <remarks>
        /// For Process Loopback instances (created via <see cref="CreateForProcessCaptureAsync"/>), call this method
        /// from the same thread that awaited <see cref="CreateForProcessCaptureAsync"/> (typically the UI thread).
        /// Do not call from a background or thread-pool thread.
        /// </remarks>
        public void StartRecording()
        {
            if (captureState != CaptureState.Stopped)
            {
                throw new InvalidOperationException("Previous recording still in progress");
            }
            captureState = CaptureState.Starting;
            InitializeCaptureDevice();
            captureThread = new Thread(() => CaptureThread(audioClient))
            {
                IsBackground = true,
            };
            captureThread.Start();
        }

        /// <summary>
        /// Stop Capturing (requests a stop, wait for RecordingStopped event to know it has finished)
        /// </summary>
        public void StopRecording()
        {
            if (captureState != CaptureState.Stopped)
                captureState = CaptureState.Stopping;
        }

        private void CaptureThread(AudioClient client)
        {
            Exception exception = null;
            try
            {
                DoRecording(client);
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                if (isProcessLoopback && syncContext != null)
                {
                    try
                    {
                        syncContext.Send(_ => client.Stop(), null);
                    }
                    catch (Exception stopEx)
                    {
                        exception = exception ?? stopEx;
                    }
                }
                else
                {
                    try
                    {
                        client.Stop();
                    }
                    catch (Exception stopEx)
                    {
                        exception = exception ?? stopEx;
                    }
                }
            }
            captureThread = null;
            captureState = CaptureState.Stopped;
            RaiseRecordingStopped(exception);
        }

        private void DoRecording(AudioClient client)
        {
            int bufferFrameCount;
            AudioCaptureClient capture;
            if (isProcessLoopback && syncContext != null)
            {
                var holder = new object[2];
                syncContext.Send(_ =>
                {
                    holder[0] = client.BufferSize;
                    holder[1] = client.AudioCaptureClient;
                }, null);
                bufferFrameCount = (int)holder[0];
                capture = (AudioCaptureClient)holder[1];
            }
            else
            {
                bufferFrameCount = client.BufferSize;
                capture = client.AudioCaptureClient;
            }
            if (bufferFrameCount < 1)
                bufferFrameCount = FALLBACK_BUFFER_LENGTH;
            var actualDuration = (long)((double)ReftimesPerSec * bufferFrameCount / waveFormat.SampleRate);
            var sleepMilliseconds = (int)(actualDuration / ReftimesPerMillisec / 2);
            var waitMilliseconds = (int)(3 * actualDuration / ReftimesPerMillisec);
            if (isProcessLoopback && syncContext != null)
                syncContext.Send(_ => client.Start(), null);
            else
                client.Start();
            if (captureState == CaptureState.Starting)
            {
                captureState = CaptureState.Capturing;
            }
            while (captureState == CaptureState.Capturing)
            {
                if (isUsingEventSync)
                {
                    frameEventWaitHandle.WaitOne(waitMilliseconds, false);
                }
                else
                {
                    Thread.Sleep(sleepMilliseconds);
                }
                if (captureState != CaptureState.Capturing)
                    break;

                if (isProcessLoopback && syncContext != null)
                    syncContext.Send(_ => ReadNextPacket(capture), null);
                else
                    ReadNextPacket(capture);
            }
        }

        private void RaiseRecordingStopped(Exception e)
        {
            var handler = RecordingStopped;
            if (handler == null) return;
            if (syncContext == null)
            {
                handler(this, new StoppedEventArgs(e));
            }
            else
            {
                syncContext.Post(state => handler(this, new StoppedEventArgs(e)), null);
            }
        }

        private void ReadNextPacket(AudioCaptureClient capture)
        {
            var packetSize = capture.GetNextPacketSize();
            var recordBufferOffset = 0;
            //Debug.WriteLine(string.Format("packet size: {0} samples", packetSize / 4));

            while (packetSize != 0)
            {
                var buffer = capture.GetBuffer(out var framesAvailable, out var flags);
                CapturePacketReceived?.Invoke(this, new WasapiCapturePacketEventArgs(flags, framesAvailable));

                var bytesAvailable = framesAvailable * bytesPerFrame;

                // apparently it is sometimes possible to read more frames than we were expecting?
                // fix suggested by Michael Feld:
                var spaceRemaining = Math.Max(0, recordBuffer.Length - recordBufferOffset);
                if (spaceRemaining < bytesAvailable)
                {
                    if (recordBufferOffset > 0)
                    {
                        DataAvailable?.Invoke(this, new WaveInEventArgs(recordBuffer, recordBufferOffset));
                        recordBufferOffset = 0;
                    }
                    if (bytesAvailable > recordBuffer.Length)
                    {
                        bytesAvailable = recordBuffer.Length;
                    }
                }

                // if not silence...
                if ((flags & AudioClientBufferFlags.Silent) != AudioClientBufferFlags.Silent)
                {
                    Marshal.Copy(buffer, recordBuffer, recordBufferOffset, bytesAvailable);
                }
                else
                {
                    Array.Clear(recordBuffer, recordBufferOffset, bytesAvailable);
                }
                recordBufferOffset += bytesAvailable;
                capture.ReleaseBuffer(framesAvailable);
                packetSize = capture.GetNextPacketSize();
            }
            DataAvailable?.Invoke(this, new WaveInEventArgs(recordBuffer, recordBufferOffset));
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            StopRecording();
            if (captureThread != null)
            {
                captureThread.Join();
                captureThread = null;
            }
            if (audioClient != null)
            {
                audioClient.Dispose();
                audioClient = null;
            }
        }
    }
}
