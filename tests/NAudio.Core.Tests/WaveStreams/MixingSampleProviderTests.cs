using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NUnit.Framework;

namespace NAudio.Core.Tests.WaveStreams;

[TestFixture]
public class MixingSampleProviderTests
{
    [Test]
    public void WithNoInputsFirstReadReturnsNoSamples()
    {
        var msp = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
        var buffer = new float[1000];
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(0));
    }

    [Test]
    public void WithReadFullySetNoInputsReturnsSampleCountRequested()
    {
        var msp = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
        msp.ReadFully = true;
        var buffer = new float[1000];
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(buffer.Length));
    }

    [Test]
    public void WithOneInputReadsToTheEnd()
    {
        var input1 = new TestSampleProvider(44100, 2, 2000);
        var msp = new MixingSampleProvider([input1]);
        var buffer = new float[1000];
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(buffer.Length));
        // randomly check one value
        Assert.That(buffer[567], Is.EqualTo(567));
    }

    [Test]
    public void WithOneInputReturnsSamplesReadIfNotEnoughToFullyRead()
    {
        var input1 = new TestSampleProvider(44100, 2, 800);
        var msp = new MixingSampleProvider([input1]);
        var buffer = new float[1000];
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(800));
        // randomly check one value
        Assert.That(buffer[567], Is.EqualTo(567));
    }

    [Test]
    public void FullyReadCausesPartialBufferToBeZeroedOut()
    {
        var input1 = new TestSampleProvider(44100, 2, 800);
        var msp = new MixingSampleProvider([input1]);
        msp.ReadFully = true;
        // buffer of 1000 floats of value 9999
        var buffer = Enumerable.Range(1, 1000).Select(n => 9999f).ToArray();

        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(buffer.Length));
        // check we get 800 samples, followed by zeroed out data
        Assert.That(buffer[567], Is.EqualTo(567f));
        Assert.That(buffer[799], Is.EqualTo(799f));
        Assert.That(buffer[800], Is.EqualTo(0));
        Assert.That(buffer[999], Is.EqualTo(0));
    }

    [Test]
    public void AddingSameMixerInputTwiceThrows()
    {
        var input = new TestSampleProvider(44100, 2, 2000);
        var msp = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
        msp.AddMixerInput(input);
        Assert.Throws<ArgumentException>(() => msp.AddMixerInput(input));
        Assert.That(msp.MixerInputs.Count(), Is.EqualTo(1));
    }

    [Test]
    public void RejectedMixerInputDoesNotMutateMixer()
    {
        var msp = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
        var mismatched = new TestSampleProvider(48000, 2, 100);

        Assert.Throws<ArgumentException>(() => msp.AddMixerInput(mismatched));
        Assert.That(msp.MixerInputs, Is.Empty);
    }

    [Test]
    public void NullMixerInputDoesNotMutateMixer()
    {
        var msp = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));

        Assert.Throws<ArgumentNullException>(() => msp.AddMixerInput(null));
        Assert.That(msp.MixerInputs, Is.Empty);
    }

    [Test]
    public void MixerInputEndedInvoked()
    {
        var input1 = new TestSampleProvider(44100, 2, 8000);
        var input2 = new TestSampleProvider(44100, 2, 800);
        var msp = new MixingSampleProvider([input1, input2]);
        ISampleProvider endedInput = null;
        msp.MixerInputEnded += (s, a) =>
        {
            Assert.That(endedInput, Is.Null);
            endedInput = a.SampleProvider;
        };
        // buffer of 1000 floats of value 9999
        var buffer = Enumerable.Range(1, 1000).Select(n => 9999f).ToArray();

        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(buffer.Length));
        Assert.That(endedInput, Is.SameAs(input2));
        Assert.That(msp.MixerInputs.Count(), Is.EqualTo(1));
    }

    [Test]
    public void MixerInputEndedCanReenterMixerAfterInputWasRemoved()
    {
        var input = new TestSampleProvider(44100, 2, 10);
        var msp = new MixingSampleProvider([input]);
        bool inputWasAlreadyRemoved = false;
        msp.MixerInputEnded += (_, a) =>
        {
            inputWasAlreadyRemoved = !msp.MixerInputs.Contains(a.SampleProvider);
            msp.RemoveAllMixerInputs();
        };

        Assert.DoesNotThrow(() => msp.Read(new float[100].AsSpan()));
        Assert.That(inputWasAlreadyRemoved, Is.True);
        Assert.That(msp.MixerInputs, Is.Empty);
    }

    [Test]
    public void MixerInputCanRemoveItselfDuringRead()
    {
        MixingSampleProvider mixer = null;
        CallbackSampleProvider input = null;
        input = new CallbackSampleProvider(() => mixer.RemoveMixerInput(input), samplesToReturn: 0);
        mixer = new MixingSampleProvider([input]);

        Assert.DoesNotThrow(() => mixer.Read(new float[100]));
        Assert.That(mixer.MixerInputs, Is.Empty);
    }

    [Test]
    [CancelAfter(10000)]
    public void AddMixerInputDoesNotWaitForAnotherInputRead()
    {
        using var readEntered = new ManualResetEventSlim();
        using var releaseRead = new ManualResetEventSlim();
        using var addStarted = new ManualResetEventSlim();
        var blockingInput = new CallbackSampleProvider(() =>
        {
            readEntered.Set();
            releaseRead.Wait(TimeSpan.FromSeconds(5));
        }, samplesToReturn: 100);
        var mixer = new MixingSampleProvider([blockingInput]);
        var additionalInput = new TestSampleProvider(44100, 2, 100);
        var readTask = Task.Run(() => mixer.Read(new float[100]));
        Assert.That(readEntered.Wait(TimeSpan.FromSeconds(2)), Is.True);
        var addTask = Task.Run(() =>
        {
            addStarted.Set();
            mixer.AddMixerInput(additionalInput);
        });

        try
        {
            Assert.That(addStarted.Wait(TimeSpan.FromSeconds(2)), Is.True);
            Assert.That(addTask.Wait(TimeSpan.FromMilliseconds(500)), Is.True,
                "AddMixerInput was blocked by an unrelated input Read.");
        }
        finally
        {
            releaseRead.Set();
            Task.WaitAll([readTask, addTask], TimeSpan.FromSeconds(5));
        }
    }

    private sealed class CallbackSampleProvider : ISampleProvider
    {
        private readonly Action onRead;
        private readonly int samplesToReturn;

        public CallbackSampleProvider(Action onRead, int samplesToReturn)
        {
            this.onRead = onRead;
            this.samplesToReturn = samplesToReturn;
        }

        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

        public int Read(Span<float> buffer)
        {
            onRead();
            int count = Math.Min(samplesToReturn, buffer.Length);
            buffer[..count].Clear();
            return count;
        }
    }

    // ── CompleteAdding ────────────────────────────────────────────────────

    [TestCase(true)]
    [TestCase(false)]
    public void AfterCompleteAddingMixerEndsWithShortReadThenZero(bool readFully)
    {
        var input1 = new TestSampleProvider(44100, 2, 800);
        var input2 = new TestSampleProvider(44100, 2, 2500);
        var msp = new MixingSampleProvider([input1, input2]) { ReadFully = readFully };
        msp.CompleteAdding();
        Assert.That(msp.IsAddingCompleted, Is.True);

        var buffer = Enumerable.Repeat(9999f, 1000).ToArray();
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(1000)); // input1 ends here
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(1000));
        buffer.AsSpan().Fill(9999f);
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(500));
        Assert.That(buffer[499], Is.EqualTo(2499));
        // the short read is not padded
        Assert.That(buffer.Skip(500), Is.All.EqualTo(9999f));
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(0));
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(0));
    }

    [Test]
    public void ReadFullyMixerStillPadsBeforeCompleteAdding()
    {
        var msp = new MixingSampleProvider([new TestSampleProvider(44100, 2, 800)]) { ReadFully = true };
        var buffer = new float[1000];
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(1000));
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(1000));
        msp.CompleteAdding();
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(0));
    }

    [Test]
    public void CompleteAddingWithNoInputsReturnsZeroEvenWithReadFully()
    {
        var msp = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)) { ReadFully = true };
        msp.CompleteAdding();
        Assert.That(msp.Read(new float[1000].AsSpan()), Is.EqualTo(0));
    }

    [Test]
    public void AddMixerInputAfterCompleteAddingThrows()
    {
        var msp = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
        msp.AddMixerInput(new TestSampleProvider(44100, 2, 800));
        msp.CompleteAdding();
        Assert.Throws<InvalidOperationException>(() => msp.AddMixerInput(new TestSampleProvider(44100, 2, 800)));
        Assert.That(msp.MixerInputs.Count(), Is.EqualTo(1));
    }

    [Test]
    public void CompletedBufferedWaveProviderInputEndsCompletedMixer()
    {
        var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        var bwp = new BufferedWaveProvider(format);
        var msp = new MixingSampleProvider(format) { ReadFully = true };
        msp.AddMixerInput(bwp.ToSampleProvider());
        msp.CompleteAdding();

        var buffer = new float[1000];
        // before the producer completes, an empty buffer is padded and the chain keeps going
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(1000));
        bwp.AddSamples(new byte[1500 * 4]);
        bwp.CompleteAdding();
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(1000));
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(500));
        Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(0));
    }

    [Test]
    public void ConcurrentlyAddedInputsAreAllReadBeforeMixerEnds()
    {
        var msp = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)) { ReadFully = true };
        var inputs = Enumerable.Range(0, 200).Select(n => new TestSampleProvider(44100, 2, 100 + n * 37)).ToArray();

        var adder = Task.Run(() =>
        {
            foreach (var input in inputs)
            {
                msp.AddMixerInput(input);
            }
            msp.CompleteAdding();
        });
        var reader = Task.Run(() =>
        {
            var buffer = new float[256];
            while (msp.Read(buffer.AsSpan()) > 0) { }
        });

        Assert.That(Task.WaitAll([adder, reader], TimeSpan.FromSeconds(30)), Is.True, "mixer did not end");
        Assert.That(inputs.Select(i => i.Position), Is.EqualTo(inputs.Select(i => 100 + Array.IndexOf(inputs, i) * 37)));
        Assert.That(msp.MixerInputs, Is.Empty);
    }
}
