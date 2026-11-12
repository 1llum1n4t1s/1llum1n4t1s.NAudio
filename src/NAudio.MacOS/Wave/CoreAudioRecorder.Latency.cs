using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

public partial class CoreAudioRecorder
{
    /// <summary>
    /// Latency between the HAL's "now" stamp and the input-data stamp of a capture callback,
    /// in seconds, or -1 when it cannot be computed (callers then fall back to the
    /// device-reported latency). Both stamps must be valid on the same time base; subtracting
    /// a missing stamp, or a sample time from a host time, would yield an absolute time rather
    /// than a latency. Kept free of the I/O procedure state so it can be unit tested.
    /// </summary>
    /// <param name="now">The <c>inNow</c> stamp passed to the I/O procedure.</param>
    /// <param name="input">The <c>inInputTime</c> stamp passed to the I/O procedure.</param>
    /// <param name="sampleRate">The stream's virtual-format sample rate.</param>
    internal static double ComputeCaptureLatencySeconds(in AudioTimeStamp now, in AudioTimeStamp input, double sampleRate)
    {
        if (now.mFlags == AudioTimeStampFlags.kAudioTimeStampNothingValid ||
            input.mFlags == AudioTimeStampFlags.kAudioTimeStampNothingValid)
        {
            return -1d;
        }

        if (now.mFlags.HasFlag(AudioTimeStampFlags.kAudioTimeStampSampleTimeValid) &&
            input.mFlags.HasFlag(AudioTimeStampFlags.kAudioTimeStampSampleTimeValid) &&
            sampleRate > 0)
        {
            return (now.mSampleTime - input.mSampleTime) / sampleRate;
        }

        if (now.mFlags.HasFlag(AudioTimeStampFlags.kAudioTimeStampHostTimeValid) &&
            input.mFlags.HasFlag(AudioTimeStampFlags.kAudioTimeStampHostTimeValid))
        {
            return CoreAudioFunctions.ConvertHostTimeToSeconds(now.mHostTime) -
                CoreAudioFunctions.ConvertHostTimeToSeconds(input.mHostTime);
        }

        // no common time base
        return -1d;
    }
}
