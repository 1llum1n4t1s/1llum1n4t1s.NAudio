using NUnit.Framework;

using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;
using NAudio.Wave;

namespace NAudio.MacOS.Tests;

/// <summary>
/// Pins how CoreAudioRecorder.CurrentLatency derives a latency from the two HAL time stamps of
/// a capture callback. A missing stamp used to be treated as time 0, so the "latency" came
/// back as an absolute time.
/// </summary>
[TestFixture]
public sealed class CaptureLatencyTests
{
    private const double SampleRate = 48000;

    private static AudioTimeStamp SampleStamp(double sampleTime) => new()
    {
        mSampleTime = sampleTime,
        mFlags = AudioTimeStampFlags.kAudioTimeStampSampleTimeValid,
    };

    private static AudioTimeStamp HostStamp(ulong hostTime) => new()
    {
        mHostTime = hostTime,
        mFlags = AudioTimeStampFlags.kAudioTimeStampHostTimeValid,
    };

    private static readonly AudioTimeStamp Missing = new() { mFlags = AudioTimeStampFlags.kAudioTimeStampNothingValid };

    [Test]
    public void SampleTimesGiveTheirDifferenceInSeconds()
    {
        var latency = CoreAudioRecorder.ComputeCaptureLatencySeconds(
            SampleStamp(1_000_480), SampleStamp(1_000_000), SampleRate);
        Assert.That(latency, Is.EqualTo(0.01).Within(1e-9));
    }

    [Test]
    public void EitherStampMissingFallsBack()
    {
        // previously: one valid stamp minus 0 = an absolute time, e.g. ~20 s here
        Assert.That(CoreAudioRecorder.ComputeCaptureLatencySeconds(SampleStamp(1_000_000), Missing, SampleRate), Is.EqualTo(-1d));
        Assert.That(CoreAudioRecorder.ComputeCaptureLatencySeconds(Missing, SampleStamp(1_000_000), SampleRate), Is.EqualTo(-1d));
        Assert.That(CoreAudioRecorder.ComputeCaptureLatencySeconds(Missing, Missing, SampleRate), Is.EqualTo(-1d));
    }

    [Test]
    public void StampsWithoutACommonTimeBaseFallBack()
    {
        // a sample time and a host time are different units and cannot be subtracted
        Assert.That(CoreAudioRecorder.ComputeCaptureLatencySeconds(SampleStamp(1_000_000), HostStamp(5_000_000), SampleRate), Is.EqualTo(-1d));
        Assert.That(CoreAudioRecorder.ComputeCaptureLatencySeconds(HostStamp(5_000_000), SampleStamp(1_000_000), SampleRate), Is.EqualTo(-1d));
    }

    [Test]
    public void SampleTimesWithoutAUsableSampleRateFallBack()
    {
        // dividing by a zero rate used to give NaN, which TimeSpan.FromSeconds rejects
        Assert.That(CoreAudioRecorder.ComputeCaptureLatencySeconds(SampleStamp(1_000_480), SampleStamp(1_000_000), 0), Is.EqualTo(-1d));
    }

    [Test]
    public void HostTimesGiveTheirDifferenceInSeconds()
    {
        // host time conversion asks the HAL for the clock frequency, so this one needs macOS
        MacOSVerify.VerifyIsOSMacOSFloorAtLeast();
        double frequency = CoreAudioFunctions.HostClockFrequency;
        ulong ticks = (ulong)(frequency / 100); // ~10 ms
        var latency = CoreAudioRecorder.ComputeCaptureLatencySeconds(
            HostStamp(1_000_000_000 + ticks), HostStamp(1_000_000_000), SampleRate);
        Assert.That(latency, Is.EqualTo(ticks / frequency).Within(1e-9));
    }
}
