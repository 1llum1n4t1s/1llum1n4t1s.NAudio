using NAudio.Wave;
using NUnit.Framework;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NAudio.Windows.Tests.Wave;

[TestFixture]
[Category("UnitTest")]
public class WorkerThreadLifecycleTests
{
    [Test]
    public void WaveOutDisposeWaitsForWorkerThread()
    {
        AssertDisposeWaitsForWorker(new WaveOut(), "playbackThread");
    }

    [Test]
    public void WaveInDisposeWaitsForWorkerThread()
    {
        AssertDisposeWaitsForWorker(new WaveIn(), "captureThread");
    }

    [Test]
    public void WaveInDoesNotReviveCaptureAfterStopRequestedBeforeWorkerStarts()
    {
        var waveIn = new WaveIn();
        var type = typeof(WaveIn);
        var stateField = type.GetField("captureState", BindingFlags.Instance | BindingFlags.NonPublic);
        var buffersField = type.GetField("buffers", BindingFlags.Instance | BindingFlags.NonPublic);
        var callbackField = type.GetField("callbackEvent", BindingFlags.Instance | BindingFlags.NonPublic);
        var doRecording = type.GetMethod("DoRecording", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.Multiple(() =>
        {
            Assert.That(stateField, Is.Not.Null);
            Assert.That(buffersField, Is.Not.Null);
            Assert.That(callbackField, Is.Not.Null);
            Assert.That(doRecording, Is.Not.Null);
        });

        stateField.SetValue(waveIn, CaptureState.Stopping);
        buffersField.SetValue(waveIn,
            Array.CreateInstance(buffersField.FieldType.GetElementType()!, 0));
        var callbackEvent = (AutoResetEvent)callbackField.GetValue(waveIn)!;
        var recordingTask = Task.Run(() => doRecording.Invoke(waveIn, null));

        try
        {
            var stopWasOverwritten = SpinWait.SpinUntil(
                () => (CaptureState)stateField.GetValue(waveIn)! == CaptureState.Capturing,
                TimeSpan.FromMilliseconds(250));

            Assert.Multiple(() =>
            {
                Assert.That(stopWasOverwritten, Is.False,
                    "The worker revived capture after StopRecording requested shutdown.");
                Assert.That(recordingTask.Wait(TimeSpan.FromSeconds(2)), Is.True,
                    "The worker did not exit after observing the pending stop request.");
                Assert.That(stateField.GetValue(waveIn), Is.EqualTo(CaptureState.Stopping));
            });
        }
        finally
        {
            stateField.SetValue(waveIn, CaptureState.Stopped);
            callbackEvent.Set();
            recordingTask.Wait(TimeSpan.FromSeconds(2));
            waveIn.Dispose();
        }
    }

    private static void AssertDisposeWaitsForWorker(IDisposable owner, string fieldName)
    {
        using var workerStarted = new ManualResetEventSlim();
        using var releaseWorker = new ManualResetEventSlim();
        using var disposeStarted = new ManualResetEventSlim();
        var worker = new Thread(() =>
        {
            workerStarted.Set();
            releaseWorker.Wait();
        })
        { IsBackground = true };

        var field = owner.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(owner, worker);
        worker.Start();
        Assert.That(workerStarted.Wait(TimeSpan.FromSeconds(2)), Is.True);

        var disposeTask = Task.Factory.StartNew(() =>
        {
            disposeStarted.Set();
            owner.Dispose();
        }, default, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        try
        {
            Assert.That(disposeStarted.Wait(TimeSpan.FromSeconds(2)), Is.True);
            Assert.That(disposeTask.Wait(TimeSpan.FromMilliseconds(100)), Is.False,
                "Dispose returned while the worker still owned lifecycle resources");
        }
        finally
        {
            releaseWorker.Set();
        }

        Assert.That(disposeTask.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(worker.IsAlive, Is.False);
    }
}
