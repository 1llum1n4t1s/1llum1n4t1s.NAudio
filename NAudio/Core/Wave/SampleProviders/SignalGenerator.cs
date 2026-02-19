using System;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Signal Generator
    /// Sin, Square, Triangle, SawTooth, White Noise, Pink Noise, Sweep.
    /// </summary>
    /// <remarks>
    /// Posibility to change ISampleProvider
    /// Example :
    /// ---------
    /// WaveOut _waveOutGene = new WaveOut();
    /// WaveGenerator wg = new SignalGenerator();
    /// wg.Type = ...
    /// wg.Frequency = ...
    /// wg ...
    /// _waveOutGene.Init(wg);
    /// _waveOutGene.Play();
    /// </remarks>
    public class SignalGenerator : ISampleProvider
    {
        // Wave format
        private readonly WaveFormat waveFormat;

        // Random Number for the White Noise & Pink Noise Generator
        private readonly Random random = new Random();

        private readonly double[] pinkNoiseBuffer = new double[7];

        // Const Math
        private const double TwoPi = 2 * Math.PI;

        // Generator variable
        private long nSample;

        // Sweep Generator variable
        private double phi;

        /// <summary>
        /// Initializes a new instance for the Generator (Default :: 44.1Khz, 2 channels, Sinus, Frequency = 440, Gain = 1)
        /// </summary>
        public SignalGenerator()
            : this(44100, 2)
        {

        }

        /// <summary>
        /// Initializes a new instance for the Generator (UserDef SampleRate &amp; Channels)
        /// </summary>
        /// <param name="sampleRate">Desired sample rate</param>
        /// <param name="channel">Number of channels</param>
        public SignalGenerator(int sampleRate, int channel)
        {
            phi = 0;
            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channel);

            // Default
            Type = SignalGeneratorType.Sin;
            Frequency = 440.0;
            Gain = 1;
            PhaseReverse = new bool[channel];
            SweepLengthSecs = 2;
        }

        /// <summary>
        /// The waveformat of this WaveProvider (same as the source)
        /// </summary>
        public WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Frequency for the Generator. (20.0 - 20000.0 Hz)
        /// Sin, Square, Triangle, SawTooth, Sweep (Start Frequency).
        /// </summary>
        public double Frequency { get; set; }

        /// <summary>
        /// Return Log of Frequency Start (Read only)
        /// </summary>
        public double FrequencyLog => Math.Log(Frequency);

        /// <summary>
        /// End Frequency for the Sweep Generator. (Start Frequency in Frequency)
        /// </summary>
        public double FrequencyEnd { get; set; }

        /// <summary>
        /// Return Log of Frequency End (Read only)
        /// </summary>
        public double FrequencyEndLog => Math.Log(FrequencyEnd);

        /// <summary>
        /// Gain for the Generator. (0.0 to 1.0)
        /// </summary>
        public double Gain { get; set; }

        /// <summary>
        /// Channel PhaseReverse
        /// </summary>
        public bool[] PhaseReverse { get; }

        /// <summary>
        /// Type of Generator.
        /// </summary>
        public SignalGeneratorType Type { get; set; }

        /// <summary>
        /// Length Seconds for the Sweep Generator.
        /// </summary>
        public double SweepLengthSecs { get; set; }

        /// <summary>
        /// Reads from this provider.
        /// </summary>
        public int Read(float[] buffer, int offset, int count)
        {
            var outIndex = offset;

            // Generator current value
            double multiple;
            double sampleValue;
            double sampleSaw;

            // Pre-compute loop-invariant values
            var channels = waveFormat.Channels;
            var sampleRate = (double)waveFormat.SampleRate;
            var samplesPerChannel = count / channels;
            var sinMultiple = TwoPi * Frequency / sampleRate;
            var baseMultiple = 2 * Frequency / sampleRate;
            var sweepLengthSamples = SweepLengthSecs * sampleRate;
            var sweepFreqLogRange = FrequencyEndLog - FrequencyLog;
            var gain = Gain;

            // Complete Buffer
            for (var sampleCount = 0; sampleCount < samplesPerChannel; sampleCount++)
            {
                switch (Type)
                {
                    case SignalGeneratorType.Sin:

                        // Sinus Generator

                        sampleValue = gain * Math.Sin(nSample * sinMultiple);

                        nSample++;

                        break;


                    case SignalGeneratorType.Square:

                        // Square Generator

                        sampleSaw = ((nSample * baseMultiple) % 2) - 1;
                        sampleValue = sampleSaw >= 0 ? gain : -gain;

                        nSample++;
                        break;

                    case SignalGeneratorType.Triangle:

                        // Triangle Generator

                        sampleSaw = ((nSample * baseMultiple) % 2);
                        sampleValue = 2 * sampleSaw;
                        if (sampleValue > 1)
                            sampleValue = 2 - sampleValue;
                        if (sampleValue < -1)
                            sampleValue = -2 - sampleValue;

                        sampleValue *= gain;

                        nSample++;
                        break;

                    case SignalGeneratorType.SawTooth:

                        // SawTooth Generator

                        sampleSaw = ((nSample * baseMultiple) % 2) - 1;
                        sampleValue = gain * sampleSaw;

                        nSample++;
                        break;

                    case SignalGeneratorType.White:

                        // White Noise Generator
                        sampleValue = (gain * NextRandomTwo());
                        break;

                    case SignalGeneratorType.Pink:

                        // Pink Noise Generator

                        var white = NextRandomTwo();
                        pinkNoiseBuffer[0] = 0.99886*pinkNoiseBuffer[0] + white*0.0555179;
                        pinkNoiseBuffer[1] = 0.99332*pinkNoiseBuffer[1] + white*0.0750759;
                        pinkNoiseBuffer[2] = 0.96900*pinkNoiseBuffer[2] + white*0.1538520;
                        pinkNoiseBuffer[3] = 0.86650*pinkNoiseBuffer[3] + white*0.3104856;
                        pinkNoiseBuffer[4] = 0.55000*pinkNoiseBuffer[4] + white*0.5329522;
                        pinkNoiseBuffer[5] = -0.7616*pinkNoiseBuffer[5] - white*0.0168980;
                        var pink = pinkNoiseBuffer[0] + pinkNoiseBuffer[1] + pinkNoiseBuffer[2] + pinkNoiseBuffer[3] + pinkNoiseBuffer[4] + pinkNoiseBuffer[5] + pinkNoiseBuffer[6] + white*0.5362;
                        pinkNoiseBuffer[6] = white*0.115926;
                        sampleValue = (gain * (pink * 0.2));
                        break;

                    case SignalGeneratorType.Sweep:

                        // Sweep Generator
                        var f = Math.Exp(FrequencyLog + (nSample * sweepFreqLogRange) / sweepLengthSamples);

                        multiple = TwoPi * f / sampleRate;
                        phi += multiple;
                        sampleValue = gain * (Math.Sin(phi));
                        nSample++;
                        if (nSample > sweepLengthSamples)
                        {
                            nSample = 0;
                            phi = 0;
                        }
                        break;

                    default:
                        sampleValue = 0.0;
                        break;
                }

                // Phase Reverse Per Channel
                for (var i = 0; i < channels; i++)
                {
                    if (PhaseReverse[i])
                        buffer[outIndex++] = (float) -sampleValue;
                    else
                        buffer[outIndex++] = (float) sampleValue;
                }
            }
            return samplesPerChannel * channels;
        }

        /// <summary>
        /// Private :: Random for WhiteNoise &amp; Pink Noise (Value form -1 to 1)
        /// </summary>
        /// <returns>Random value from -1 to +1</returns>
        private double NextRandomTwo()
        {
            return 2*random.NextDouble() - 1;
        }

    }

    /// <summary>
    /// Signal Generator type
    /// </summary>
    public enum SignalGeneratorType
    {
        /// <summary>
        /// Pink noise
        /// </summary>
        Pink,
        /// <summary>
        /// White noise
        /// </summary>
        White,
        /// <summary>
        /// Sweep
        /// </summary>
        Sweep,
        /// <summary>
        /// Sine wave
        /// </summary>
        Sin,
        /// <summary>
        /// Square wave
        /// </summary>
        Square,
        /// <summary>
        /// Triangle Wave
        /// </summary>
        Triangle,
        /// <summary>
        /// Sawtooth wave
        /// </summary>
        SawTooth,
    }

}
