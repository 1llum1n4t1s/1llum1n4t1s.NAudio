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
