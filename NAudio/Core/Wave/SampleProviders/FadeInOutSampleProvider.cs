using System;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Sample Provider to allow fading in and out
    /// </summary>
    public class FadeInOutSampleProvider : ISampleProvider
    {
        enum FadeState
        {
            Silence,
            FadingIn,
            FullVolume,
            FadingOut,
        }

        private readonly object lockObject = new object();
        private readonly ISampleProvider source;
        private int fadeSamplePosition;
        private int fadeSampleCount;
        private FadeState fadeState;

        /// <summary>
        /// Creates a new FadeInOutSampleProvider
        /// </summary>
        /// <param name="source">The source stream with the audio to be faded in or out</param>
        /// <param name="initiallySilent">If true, we start faded out</param>
        public FadeInOutSampleProvider(ISampleProvider source, bool initiallySilent = false)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            fadeState = initiallySilent ? FadeState.Silence : FadeState.FullVolume;
        }

        /// <summary>
        /// Requests that a fade-in begins (will start on the next call to Read)
        /// </summary>
        /// <param name="fadeDurationInMilliseconds">Duration of fade in milliseconds</param>
        public void BeginFadeIn(double fadeDurationInMilliseconds)
        {
            if (fadeDurationInMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(fadeDurationInMilliseconds), "Must be non-negative");
            lock (lockObject)
            {
                fadeSamplePosition = 0;
                fadeSampleCount = (int)((fadeDurationInMilliseconds * source.WaveFormat.SampleRate) / 1000);
                if (fadeSampleCount <= 0)
                {
                    fadeState = FadeState.FullVolume;
                }
                else
                {
                    fadeState = FadeState.FadingIn;
                }
            }
        }

        /// <summary>
        /// Requests that a fade-out begins (will start on the next call to Read)
        /// </summary>
        /// <param name="fadeDurationInMilliseconds">Duration of fade in milliseconds</param>
        public void BeginFadeOut(double fadeDurationInMilliseconds)
        {
            if (fadeDurationInMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(fadeDurationInMilliseconds), "Must be non-negative");
            lock (lockObject)
            {
                fadeSamplePosition = 0;
                fadeSampleCount = (int)((fadeDurationInMilliseconds * source.WaveFormat.SampleRate) / 1000);
                if (fadeSampleCount <= 0)
                {
                    fadeState = FadeState.Silence;
                }
                else
                {
                    fadeState = FadeState.FadingOut;
                }
            }
        }

        /// <summary>
        /// Reads samples from this sample provider
        /// </summary>
        /// <param name="buffer">Buffer to read into</param>
        /// <param name="offset">Offset within buffer to write to</param>
        /// <param name="count">Number of samples desired</param>
        /// <returns>Number of samples read</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            var sourceSamplesRead = source.Read(buffer, offset, count);
            lock (lockObject)
            {
                if (fadeState == FadeState.FadingIn)
                {
                    FadeIn(buffer, offset, sourceSamplesRead);
                }
                else if (fadeState == FadeState.FadingOut)
                {
                    FadeOut(buffer, offset, sourceSamplesRead);
                }
                else if (fadeState == FadeState.Silence)
                {
                    ClearBuffer(buffer, offset, count);
                }
            }
            return sourceSamplesRead;
        }

        private static void ClearBuffer(float[] buffer, int offset, int count)
        {
            Array.Clear(buffer, offset, count);
        }

        private void FadeOut(float[] buffer, int offset, int sourceSamplesRead)
        {
            var sample = 0;
            var channels = source.WaveFormat.Channels;
            var invFadeCount = 1.0f / fadeSampleCount;
            while (sample < sourceSamplesRead)
            {
                var multiplier = 1.0f - (fadeSamplePosition * invFadeCount);
                if (multiplier < 0f) multiplier = 0f;
                for (var ch = 0; ch < channels; ch++)
                {
                    buffer[offset + sample++] *= multiplier;
                }
                fadeSamplePosition++;
                if (fadeSamplePosition >= fadeSampleCount)
                {
                    fadeState = FadeState.Silence;
                    // clear out the end
                    ClearBuffer(buffer, sample + offset, sourceSamplesRead - sample);
                    break;
                }
            }
        }

        private void FadeIn(float[] buffer, int offset, int sourceSamplesRead)
        {
            var sample = 0;
            var channels = source.WaveFormat.Channels;
            var invFadeCount = 1.0f / fadeSampleCount;
            while (sample < sourceSamplesRead)
            {
                var multiplier = fadeSamplePosition * invFadeCount;
                if (multiplier > 1.0f) multiplier = 1.0f;
                for (var ch = 0; ch < channels; ch++)
                {
                    buffer[offset + sample++] *= multiplier;
                }
                fadeSamplePosition++;
                if (fadeSamplePosition >= fadeSampleCount)
                {
                    fadeState = FadeState.FullVolume;
                    // no need to multiply any more
                    break;
                }
            }
        }

        /// <summary>
        /// WaveFormat of this SampleProvider
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return source.WaveFormat; }
        }
    }
}
