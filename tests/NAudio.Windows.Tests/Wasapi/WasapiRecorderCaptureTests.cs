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
        FakeCaptureClient capture = null)
    {
        var audioClient = capture == null
            ? new AudioClient(client)
            : new AudioClient(client, capture);
        return new WasapiRecorder(audioClient, useEventSync: true,
            bufferMilliseconds: 1, Format, mmcssTaskName: null);
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
        public int Start() => 0;
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
