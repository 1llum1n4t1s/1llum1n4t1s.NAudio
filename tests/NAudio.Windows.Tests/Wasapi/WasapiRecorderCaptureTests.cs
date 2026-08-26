using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using NUnit.Framework;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NAudio.Windows.Tests.Wasapi;

[TestFixture]
[Category("UnitTest")]
public class WasapiRecorderCaptureTests
{
    private const int EFail = unchecked((int)0x80004005);
    private static readonly WaveFormat Format = new(48000, 16, 2);

    [Test]
    public async Task CaptureAsyncReleasesNativeBufferBeforeYieldingPacket()
    {
        byte[] source = { 1, 2, 3, 4, 5, 6, 7, 8 };
        using var capture = new FakeCaptureClient(source, frames: 2, default);
        using var recorder = CreateRecorder(new FakeAudioClient(), capture);
        await using var enumerator = recorder.CaptureAsync().GetAsyncEnumerator();

        Assert.That(await enumerator.MoveNextAsync(), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(capture.ReleaseCount, Is.EqualTo(1));
            Assert.That(enumerator.Current.Data.ToArray(), Is.EqualTo(source));
            Assert.That(enumerator.Current.DevicePosition, Is.EqualTo(123));
            Assert.That(enumerator.Current.QPCPosition, Is.EqualTo(456));
        });
    }

    [Test]
    public async Task CaptureAsyncYieldsZeroFilledSilentPacket()
    {
        byte[] undefinedNativeData = Enumerable.Repeat((byte)0x7F, 8).ToArray();
        using var capture = new FakeCaptureClient(undefinedNativeData, frames: 2,
            AudioClientBufferFlags.Silent);
        using var recorder = CreateRecorder(new FakeAudioClient(), capture);
        await using var enumerator = recorder.CaptureAsync().GetAsyncEnumerator();

        Assert.That(await enumerator.MoveNextAsync(), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(capture.ReleaseCount, Is.EqualTo(1));
            Assert.That(enumerator.Current.Data.Length, Is.EqualTo(undefinedNativeData.Length));
            Assert.That(enumerator.Current.Data.ToArray(), Is.All.Zero);
            Assert.That(enumerator.Current.Flags.HasFlag(AudioClientBufferFlags.Silent), Is.True);
        });
    }

    [Test]
    public void RecorderStartFailureRestoresStoppedState()
    {
        using var recorder = CreateRecorder(new FakeAudioClient { InitializeResult = EFail });

        Assert.Throws<CoreAudioException>(() => recorder.StartRecording());
        Assert.That(recorder.CaptureState, Is.EqualTo(CaptureState.Stopped));
    }

    [Test]
    public async Task RecorderDisposeDuringStartDoesNotHang()
    {
        using var startEntered = new ManualResetEventSlim();
        using var continueStart = new ManualResetEventSlim();
        using var client = new FakeCaptureClient(Array.Empty<byte>(), frames: 0, default);
        var audioClient = new FakeAudioClient
        {
            StartEntered = startEntered,
            ContinueStart = continueStart
        };
        var recorder = CreateRecorder(audioClient, client);

        recorder.StartRecording();
        Assert.That(startEntered.Wait(TimeSpan.FromSeconds(2)), Is.True,
            "Capture thread did not enter IAudioClient.Start().");

        var disposeTask = Task.Run(recorder.Dispose);
        Assert.That(SpinWait.SpinUntil(
                () => recorder.CaptureState == CaptureState.Stopping,
                TimeSpan.FromSeconds(2)),
            Is.True,
            "Dispose did not request capture shutdown.");

        continueStart.Set();
        var completedBeforeCleanup = await Task.WhenAny(
            disposeTask,
            Task.Delay(TimeSpan.FromSeconds(2))) == disposeTask;

        if (!completedBeforeCleanup)
        {
            // Let the buggy implementation exit so the regression test itself does not leak a
            // capture thread after recording the failed assertion.
            recorder.StopRecording();
            await Task.WhenAny(disposeTask, Task.Delay(TimeSpan.FromSeconds(2)));
        }

        Assert.That(completedBeforeCleanup, Is.True,
            "Dispose hung because the capture thread overwrote Stopping with Capturing.");
    }

    [Test]
    public async Task RecorderStopDuringAsyncStartCompletesEnumeration()
    {
        using var startEntered = new ManualResetEventSlim();
        using var continueStart = new ManualResetEventSlim();
        using var client = new FakeCaptureClient(Array.Empty<byte>(), frames: 0, default);
        var recorder = CreateRecorder(new FakeAudioClient
        {
            StartEntered = startEntered,
            ContinueStart = continueStart
        }, client);
        await using var enumerator = recorder.CaptureAsync().GetAsyncEnumerator();

        var moveNextTask = Task.Run(async () => await enumerator.MoveNextAsync());
        Assert.That(startEntered.Wait(TimeSpan.FromSeconds(2)), Is.True,
            "Async capture did not enter IAudioClient.Start().");

        recorder.StopRecording();
        continueStart.Set();
        var completedBeforeCleanup = await Task.WhenAny(
            moveNextTask,
            Task.Delay(TimeSpan.FromSeconds(2))) == moveNextTask;

        if (!completedBeforeCleanup)
        {
            recorder.StopRecording();
            await Task.WhenAny(moveNextTask, Task.Delay(TimeSpan.FromSeconds(2)));
        }

        Assert.Multiple(() =>
        {
            Assert.That(completedBeforeCleanup, Is.True,
                "CaptureAsync ignored the stop requested while IAudioClient.Start was in progress.");
            Assert.That(moveNextTask.IsCompletedSuccessfully, Is.True);
            Assert.That(moveNextTask.Result, Is.False);
        });

        await recorder.DisposeAsync();
    }

    [Test]
    public async Task RecorderDisposeDuringAsyncCaptureDoesNotReleaseResourcesEarly()
    {
        using var capture = new FakeCaptureClient(Array.Empty<byte>(), frames: 0, default);
        var recorder = CreateRecorder(new FakeAudioClient(), capture,
            useEventSync: false, bufferMilliseconds: 500);
        await using var enumerator = recorder.CaptureAsync().GetAsyncEnumerator();

        var moveNextTask = enumerator.MoveNextAsync().AsTask();
        Assert.That(SpinWait.SpinUntil(
                () => recorder.CaptureState == CaptureState.Capturing,
                TimeSpan.FromSeconds(2)),
            Is.True,
            "Async capture did not enter the capture loop.");

        recorder.Dispose();

        Assert.That(await moveNextTask.WaitAsync(TimeSpan.FromSeconds(2)), Is.False);
    }

    [Test]
    public async Task RecorderDisposeAsyncDuringAsyncCaptureWaitsForSafeShutdown()
    {
        using var capture = new FakeCaptureClient(Array.Empty<byte>(), frames: 0, default);
        var recorder = CreateRecorder(new FakeAudioClient(), capture,
            useEventSync: false, bufferMilliseconds: 500);
        await using var enumerator = recorder.CaptureAsync().GetAsyncEnumerator();

        var moveNextTask = enumerator.MoveNextAsync().AsTask();
        Assert.That(SpinWait.SpinUntil(
                () => recorder.CaptureState == CaptureState.Capturing,
                TimeSpan.FromSeconds(2)),
            Is.True,
            "Async capture did not enter the capture loop.");

        var disposeTask = recorder.DisposeAsync().AsTask();

        Assert.That(await moveNextTask.WaitAsync(TimeSpan.FromSeconds(2)), Is.False);
        await disposeTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task RecorderDisposeAsyncWhileIteratorIsYieldingDoesNotDeadlock()
    {
        using var capture = new FakeCaptureClient(new byte[] { 1, 2, 3, 4 },
            frames: 1, default);
        var recorder = CreateRecorder(new FakeAudioClient(), capture,
            useEventSync: false, bufferMilliseconds: 1);
        await using var enumerator = recorder.CaptureAsync().GetAsyncEnumerator();

        Assert.That(await enumerator.MoveNextAsync(), Is.True);

        await recorder.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
        Assert.That(await enumerator.MoveNextAsync(), Is.False);
    }

    [Test]
    public async Task RecorderAsyncInitializationFailureRestoresStoppedState()
    {
        using var recorder = CreateRecorder(new FakeAudioClient { InitializeResult = EFail });
        await using var enumerator = recorder.CaptureAsync().GetAsyncEnumerator();

        Assert.ThrowsAsync<CoreAudioException>(async () => await enumerator.MoveNextAsync());
        Assert.That(recorder.CaptureState, Is.EqualTo(CaptureState.Stopped));
    }

    [Test]
    public void LegacyCaptureStartFailureRestoresStoppedState()
    {
        using var client = new AudioClient(new FakeAudioClient { InitializeResult = EFail });
#pragma warning disable CS0618 // This test protects the legacy WasapiCapture lifecycle.
        using var capture = new WasapiCapture(client, useEventSync: false,
            audioBufferMillisecondsLength: 1, isProcessLoopback: false);

        Assert.Throws<CoreAudioException>(() => capture.StartRecording());
        Assert.That(capture.CaptureState, Is.EqualTo(CaptureState.Stopped));
#pragma warning restore CS0618
    }

    [Test]
    public void LegacyCaptureReportsPacketFlagsAndCumulativeCounts()
    {
        byte[] undefinedNativeData = Enumerable.Repeat((byte)0x7F, 8).ToArray();
        using var nativeCapture = new FakeCaptureClient(undefinedNativeData, frames: 2,
            AudioClientBufferFlags.Silent | AudioClientBufferFlags.DataDiscontinuity);
        using var client = new AudioClient(new FakeAudioClient(), nativeCapture);
#pragma warning disable CS0618 // This test protects the legacy fork diagnostic API.
        using var capture = new WasapiCapture(client, useEventSync: true,
            audioBufferMillisecondsLength: 1, isProcessLoopback: false);
        using var received = new ManualResetEventSlim();
        WasapiCapturePacketEventArgs packet = null;
        capture.CapturePacketReceived += (_, args) =>
        {
            packet = args;
            received.Set();
        };

        capture.StartRecording();

        Assert.That(received.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(packet, Is.Not.Null);
            Assert.That(packet.FramesAvailable, Is.EqualTo(2));
            Assert.That(packet.IsSilent, Is.True);
            Assert.That(packet.BufferFlags.HasFlag(AudioClientBufferFlags.DataDiscontinuity), Is.True);
            Assert.That(capture.TotalPacketCount, Is.EqualTo(1));
            Assert.That(capture.SilentPacketCount, Is.EqualTo(1));
        });
#pragma warning restore CS0618
    }

    [Test]
    public void LegacyCaptureDoesNotReviveAfterStopRequestedBeforeWorkerStarts()
    {
        using var client = new AudioClient(new FakeAudioClient());
#pragma warning disable CS0618 // This test protects the legacy WasapiCapture lifecycle.
        using var capture = new WasapiCapture(client, useEventSync: false,
            audioBufferMillisecondsLength: 1, isProcessLoopback: false);
        var type = typeof(WasapiCapture);
        var stateField = type.GetField("captureState",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var tryTransition = type.GetMethod("TryTransitionToCapturing",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        Assert.Multiple(() =>
        {
            Assert.That(stateField, Is.Not.Null);
            Assert.That(tryTransition, Is.Not.Null);
        });

        stateField.SetValue(capture, CaptureState.Starting);
        capture.StopRecording();
        var transitioned = (bool)tryTransition.Invoke(capture, null)!;

        Assert.Multiple(() =>
        {
            Assert.That(transitioned, Is.False);
            Assert.That(capture.CaptureState, Is.EqualTo(CaptureState.Stopping));
        });
#pragma warning restore CS0618
    }

    [Test]
    public void LegacyDataAvailableHandlerCanDisposeCaptureWithoutJoiningItself()
    {
        byte[] data = { 1, 2, 3, 4 };
        using var nativeCapture = new FakeCaptureClient(data, frames: 1, default);
        using var client = new AudioClient(new FakeAudioClient(), nativeCapture);
#pragma warning disable CS0618 // This test protects the legacy WasapiCapture lifecycle.
        using var capture = new WasapiCapture(client, useEventSync: true,
            audioBufferMillisecondsLength: 1, isProcessLoopback: false);
        using var disposeReturned = new ManualResetEventSlim();
        using var stopped = new ManualResetEventSlim();
        Exception stoppedException = null;
        capture.DataAvailable += (_, _) =>
        {
            capture.Dispose();
            disposeReturned.Set();
        };
        capture.RecordingStopped += (_, args) =>
        {
            stoppedException = args.Exception;
            stopped.Set();
        };

        capture.StartRecording();

        Assert.That(disposeReturned.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(stopped.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(nativeCapture.ReleaseCount, Is.EqualTo(1));
            Assert.That(stoppedException, Is.Null);
        });
#pragma warning restore CS0618
    }

    [Test]
    public void RecordingStoppedHandlerCanDisposeRecorderWithoutJoiningItself()
    {
        using var nativeCapture = new FakeCaptureClient(Array.Empty<byte>(), frames: 0, default)
        {
            GetNextPacketSizeResult = EFail
        };
        using var recorder = CreateRecorder(new FakeAudioClient(), nativeCapture);
        using var stopped = new ManualResetEventSlim();
        recorder.RecordingStopped += (_, _) =>
        {
            recorder.Dispose();
            stopped.Set();
        };

        recorder.StartRecording();

        Assert.That(stopped.Wait(TimeSpan.FromSeconds(2)), Is.True);
    }

    [Test]
    public void DataAvailableHandlerDefersRecorderDisposalUntilBufferRelease()
    {
        byte[] data = { 1, 2, 3, 4 };
        using var nativeCapture = new FakeCaptureClient(data, frames: 1, default);
        using var recorder = CreateRecorder(new FakeAudioClient(), nativeCapture);
        using var disposeReturned = new ManualResetEventSlim();
        using var stopped = new ManualResetEventSlim();
        Exception stoppedException = null;
        recorder.DataAvailable += (_, _, _, _) =>
        {
            recorder.Dispose();
            disposeReturned.Set();
        };
        recorder.RecordingStopped += (_, args) =>
        {
            stoppedException = args.Exception;
            stopped.Set();
        };

        recorder.StartRecording();

        Assert.That(disposeReturned.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(stopped.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(nativeCapture.ReleaseCount, Is.EqualTo(1));
            Assert.That(stoppedException, Is.Null);
        });
    }

    private static WasapiRecorder CreateRecorder(FakeAudioClient client,
        FakeCaptureClient capture = null, bool useEventSync = true,
        int bufferMilliseconds = 1)
    {
        var audioClient = capture == null
            ? new AudioClient(client)
            : new AudioClient(client, capture);
        return new WasapiRecorder(audioClient, useEventSync,
            bufferMilliseconds, Format, mmcssTaskName: null);
    }

    private sealed class FakeAudioClient : IAudioClient
    {
        public int InitializeResult { get; init; }

        public int Initialize(AudioClientShareMode shareMode, AudioClientStreamFlags streamFlags,
            long hnsBufferDuration, long hnsPeriodicity, IntPtr pFormat, in Guid audioSessionGuid)
            => InitializeResult;

        public int GetBufferSize(out uint bufferSize) { bufferSize = 0; return 0; }
        public int GetStreamLatency(out long latency) { latency = 0; return 0; }
        public int GetCurrentPadding(out int currentPadding) { currentPadding = 0; return 0; }
        public int IsFormatSupported(AudioClientShareMode shareMode, IntPtr pFormat,
            out IntPtr closestMatchFormat)
        { closestMatchFormat = IntPtr.Zero; return 0; }
        public int GetMixFormat(out IntPtr deviceFormatPointer) { deviceFormatPointer = IntPtr.Zero; return EFail; }
        public int GetDevicePeriod(out long defaultDevicePeriod, out long minimumDevicePeriod)
        { defaultDevicePeriod = 0; minimumDevicePeriod = 0; return 0; }
        public ManualResetEventSlim StartEntered { get; init; }
        public ManualResetEventSlim ContinueStart { get; init; }

        public int Start()
        {
            StartEntered?.Set();
            ContinueStart?.Wait();
            return 0;
        }
        public int Stop() => 0;
        public int Reset() => 0;
        public int SetEventHandle(IntPtr eventHandle) => 0;
        public int GetService(in Guid interfaceId, out IntPtr interfacePointer)
        { interfacePointer = IntPtr.Zero; return EFail; }
    }

    private sealed class FakeCaptureClient : IAudioCaptureClient, IDisposable
    {
        private readonly IntPtr buffer;
        private readonly int frames;
        private readonly AudioClientBufferFlags flags;
        private bool released;

        public FakeCaptureClient(byte[] data, int frames, AudioClientBufferFlags flags)
        {
            this.frames = frames;
            this.flags = flags;
            buffer = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, buffer, data.Length);
        }

        public int ReleaseCount { get; private set; }
        public int GetNextPacketSizeResult { get; init; }

        public int GetBuffer(out IntPtr dataBuffer, out int numFramesToRead,
            out AudioClientBufferFlags bufferFlags, out long devicePosition, out long qpcPosition)
        {
            dataBuffer = buffer;
            numFramesToRead = frames;
            bufferFlags = flags;
            devicePosition = 123;
            qpcPosition = 456;
            return 0;
        }

        public int ReleaseBuffer(int numFramesRead)
        {
            released = true;
            ReleaseCount++;
            return 0;
        }

        public int GetNextPacketSize(out int numFramesInNextPacket)
        {
            numFramesInNextPacket = released ? 0 : frames;
            return GetNextPacketSizeResult;
        }

        public void Dispose() => Marshal.FreeHGlobal(buffer);
    }
}
