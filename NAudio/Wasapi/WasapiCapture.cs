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
        private long silentPacketCount;
        private long totalPacketCount;

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
        /// 診断用。これまでに受信したキャプチャパケットの総数。
        /// </summary>
        public long TotalPacketCount => Interlocked.Read(ref totalPacketCount);

        /// <summary>
        /// 診断用。これまでに受信した SILENT フラグ付きパケット数。
        /// Process Loopback で <see cref="TotalPacketCount"/> に対しこの値が増え続ける場合、
        /// OS が SILENT を返している（対象プロセスが無音 / PID・プロセスツリー指定ミス）可能性が高い。
        /// イベント購読なしでもポーリングで無音原因を切り分けられる。
        /// </summary>
        public long SilentPacketCount => Interlocked.Read(ref silentPacketCount);

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
            // NuGet 配布物は Release ビルドのため、#if DEBUG + Debug.WriteLine だと
            // この最重要の初期化失敗診断が利用者の手元で完全に消える。
            // Release でも残る Trace.WriteLine を使い、STA/SynchronizationContext 違反を検知可能にする。
            if (isProcessLoopback && syncContext == null)
                System.Diagnostics.Trace.WriteLine("WasapiCapture (Process Loopback): SynchronizationContext.Current is null. Call CreateForProcessCaptureAsync and StartRecording from an STA thread (e.g. UI thread) with a synchronization context to avoid COM errors or invalid audio.");
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
        public static Task<WasapiCapture> CreateForProcessCaptureAsync(int processId, bool includeProcessTree)
            => CreateForProcessCaptureAsync(processId, includeProcessTree, CancellationToken.None);

        /// <summary>
        /// 2 引数版 <c>CreateForProcessCaptureAsync(processId, includeProcessTree)</c> の CancellationToken 対応版。
        /// COM 活性化のコールバック (ActivateCompleted) が来ない経路 (OS 非対応 SKU・対象プロセス消滅等) で
        /// 待機を打ち切れる。STA 制約は同じ (await 継続と同じ UI スレッドで使うこと)。
        /// </summary>
        public static async Task<WasapiCapture> CreateForProcessCaptureAsync(int processId, bool includeProcessTree, CancellationToken cancellationToken)
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
                        using var ctReg = cancellationToken.Register(() => icbh.Cancel());
                        var ptr = await icbh.Task.WaitAsync(cancellationToken);
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
            using (var devices = new MMDeviceEnumerator())
            {
                return devices.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
            }
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
                bufferSize = FALLBACK_BUFFER_LENGTH * bytesPerFrame;
            }

            recordBuffer = new byte[bufferSize];

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
        /// For Process Loopback instances (created via <c>CreateForProcessCaptureAsync</c>), call this method
        /// from the same thread that awaited <c>CreateForProcessCaptureAsync</c> (typically the UI thread).
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
                // Process Loopback の client.Stop() は STA(UI スレッド)からしか呼べないが、
                // ここ(CaptureThread)から syncContext.Send で同期実行すると、UI スレッドが
                // Dispose の captureThread.Join() でブロック中の場合に相互デッドロックする。
                // よって Process Loopback の Stop は Dispose(UI スレッド)側へ委譲し、ここでは行わない。
                // 通常 WASAPI キャプチャ(!isProcessLoopback)は従来どおりこのスレッドで Stop する。
                if (!(isProcessLoopback && syncContext != null))
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
            // captureThread の null 化は所有者(Dispose)に集約する。CaptureThread 自身が
            // null を書くと、Dispose 側の「!=null 判定 → Join」の間で null になり NRE になる競合があった。
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
            if (handler == null)
            {
                // 利用者が RecordingStopped を購読していないと、Process Loopback の
                // COM 例外 (E_NOINTERFACE 等) が完全に握り潰されて「録音が止まったが理由不明」になる。
                // Debug ビルドでは少なくとも開発者が原因にたどり着けるようログに出す。
                if (e != null)
                    System.Diagnostics.Debug.WriteLine(
                        $"WasapiCapture: RecordingStopped (no handler subscribed) で例外を抑止: {e}");
                return;
            }
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
                totalPacketCount++;
                if ((flags & AudioClientBufferFlags.Silent) == AudioClientBufferFlags.Silent)
                    silentPacketCount++;
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
            // packetSize=0 で while を一度も実行しなかった場合 (無音区間連続など) でも
            // 旧実装は bytesRecorded=0 のイベントを毎ループ発火していた。
            // ユーザーが「データが来た」と誤認してファイル書き込み等を行うと
            // ゴミデータ混入の原因になるため、空の場合はイベントを発火しない。
            if (recordBufferOffset > 0)
                DataAvailable?.Invoke(this, new WaveInEventArgs(recordBuffer, recordBufferOffset));
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (captureState == CaptureState.Stopped && audioClient == null)
                return; // already disposed

            StopRecording();
            // captureThread をローカルへ退避してから Join する (CaptureThread 側の自己 null 化を
            // やめたので、ここで null チェックと Join の間に他スレッドが介入する競合は無い)。
            var threadToJoin = captureThread;
            if (threadToJoin != null)
            {
                threadToJoin.Join();
                captureThread = null;
            }
            // Process Loopback の Stop は CaptureThread から委譲され、ここ(Dispose を呼んだ UI スレッド)で実行する
            // (CaptureThread から syncContext.Send で Stop すると Join とデッドロックするため)。
            // Stop は STA 必須なので Dispose も Process Loopback では UI スレッドから呼ぶこと。
            if (isProcessLoopback && syncContext != null && audioClient != null)
            {
                try { audioClient.Stop(); }
                catch { /* 既に停止/解放済みでも無視 (後続の Dispose で COM 解放される) */ }
            }
            if (audioClient != null)
            {
                audioClient.Dispose();
                audioClient = null;
            }
            if (frameEventWaitHandle != null)
            {
                frameEventWaitHandle.Dispose();
                frameEventWaitHandle = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}
