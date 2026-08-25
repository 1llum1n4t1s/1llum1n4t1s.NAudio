using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using NUnit.Framework;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NAudio.Windows.Tests.Wasapi;

[TestFixture]
public class ProcessLoopbackCompatibilityTests
{
    [Test]
    public void ProcessLoopbackModeValuesMatchWindowsSdk()
    {
        Assert.That((int)ProcessLoopbackMode.IncludeTargetProcessTree, Is.EqualTo(0));
        Assert.That((int)ProcessLoopbackMode.ExcludeTargetProcessTree, Is.EqualTo(1));
    }

    [Test]
    public void PacketEventArgsExposeSilentFlagAndFrameCount()
    {
        var args = new WasapiCapturePacketEventArgs(
            AudioClientBufferFlags.Silent | AudioClientBufferFlags.TimestampError,
            framesAvailable: 480);

        Assert.Multiple(() =>
        {
            Assert.That(args.BufferFlags.HasFlag(AudioClientBufferFlags.TimestampError), Is.True);
            Assert.That(args.FramesAvailable, Is.EqualTo(480));
            Assert.That(args.IsSilent, Is.True);
        });
    }

    [Test]
    public void NonZeroSuccessHResultStillProducesException()
    {
        var exception = ActivateAudioInterfaceResult.ToException(1); // S_FALSE

        Assert.That(exception, Is.InstanceOf<COMException>());
        Assert.That(exception.HResult, Is.EqualTo(1));
    }

    [Test]
    public void CompletionHandlerRunsContinuationsAsynchronouslyAndCanBeCanceled()
    {
        var handler = new ActivateAudioInterfaceCompletionHandler((IAudioClient _) => { });
        using var source = new CancellationTokenSource();
        source.Cancel();

        handler.Cancel(source.Token);

        Assert.That(handler.Completion.IsCanceled, Is.True);
        Assert.That(handler.Completion.CreationOptions.HasFlag(TaskCreationOptions.RunContinuationsAsynchronously),
            Is.True);
    }

    [Test]
    public void RecorderBuilderRejectsPreCanceledActivationWithoutCallingWindows()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var builder = new WasapiRecorderBuilder().WithProcessLoopback(1);

        Assert.Throws<OperationCanceledException>(() => builder.BuildAsync(source.Token));
    }

    [Test]
    public void PlayerBuilderRejectsPreCanceledActivationWithoutCallingWindows()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var builder = new WasapiPlayerBuilder();

        Assert.Throws<OperationCanceledException>(() => builder.BuildAsync(source.Token));
    }

    [Test]
    public void LegacyFactoryRejectsInvalidProcessIdBeforeActivation()
    {
#pragma warning disable CS0618 // This test intentionally protects the 1.x compatibility API.
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            WasapiCapture.CreateForProcessCaptureAsync(0, includeProcessTree: true));
#pragma warning restore CS0618
    }

    [Test]
    public void LegacyFactoryHonorsPreCanceledTokenWithoutCallingWindows()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();

#pragma warning disable CS0618 // This test intentionally protects the 1.x compatibility API.
        Assert.ThrowsAsync<OperationCanceledException>(() =>
            WasapiCapture.CreateForProcessCaptureAsync(1, includeProcessTree: true, source.Token));
#pragma warning restore CS0618
    }
}
