using System;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Extras
{
    /// <summary>
    /// Basic example of a multi-band eq
    /// uses the same settings for both channels in stereo audio
    /// Call Update after you've updated the bands
    /// Potentially to be added to NAudio in a future version
    /// </summary>
    public class Equalizer : ISampleProvider
    {
        private readonly ISampleProvider sourceProvider;
        private readonly EqualizerBand[] bands;
        // 2 次元配列 BiQuadFilter[,] は JIT 境界 check 二重発火 + 仮想呼出のオーバヘッド大。
        // jagged 配列 BiQuadFilter[][] にして、ホットループでチャンネル別配列を
        // ローカル変数にキャッシュできるよう変更 (波形は完全に同じ計算順序)。
        private readonly BiQuadFilter[][] filters;
        private readonly int channels;
        private readonly int bandCount;
        private volatile bool updated;

        /// <summary>
        /// Creates a new Equalizer
        /// </summary>
        public Equalizer(ISampleProvider sourceProvider, EqualizerBand[] bands)
        {
            this.sourceProvider = sourceProvider ?? throw new ArgumentNullException(nameof(sourceProvider));
            this.bands = bands ?? throw new ArgumentNullException(nameof(bands));
            if (bands.Length == 0) throw new ArgumentException("Must provide at least one equalizer band", nameof(bands));
            channels = sourceProvider.WaveFormat.Channels;
            bandCount = bands.Length;
            filters = new BiQuadFilter[channels][];
            for (var c = 0; c < channels; c++)
            {
                filters[c] = new BiQuadFilter[bandCount];
            }
            CreateFilters();
        }

        private void CreateFilters()
        {
            for (var bandIndex = 0; bandIndex < bandCount; bandIndex++)
            {
                var band = bands[bandIndex];
                for (var n = 0; n < channels; n++)
                {
                    if (filters[n][bandIndex] == null)
                        filters[n][bandIndex] = BiQuadFilter.PeakingEQ(sourceProvider.WaveFormat.SampleRate, band.Frequency, band.Bandwidth, band.Gain);
                    else
                        filters[n][bandIndex].SetPeakingEq(sourceProvider.WaveFormat.SampleRate, band.Frequency, band.Bandwidth, band.Gain);
                }
            }
        }

        /// <summary>
        /// Update the equalizer settings
        /// </summary>
        public void Update()
        {
            updated = true;
        }

        /// <summary>
        /// Gets the WaveFormat of this Sample Provider
        /// </summary>
        public WaveFormat WaveFormat => sourceProvider.WaveFormat;

        /// <summary>
        /// Reads samples from this Sample Provider
        /// </summary>
        public int Read(float[] buffer, int offset, int count)
        {
            var samplesRead = sourceProvider.Read(buffer, offset, count);

            if (updated)
            {
                CreateFilters();
                updated = false;
            }

            var ch = 0;
            for (var n = 0; n < samplesRead; n++)
            {
                // チャンネル別フィルタ配列をローカル変数にキャッシュして
                // jagged の 1 次元 bound check 1 回 + サンプル値レジスタ保持に最適化。
                var chFilters = filters[ch];
                var sample = buffer[offset + n];
                for (var band = 0; band < bandCount; band++)
                {
                    sample = chFilters[band].Transform(sample);
                }
                buffer[offset + n] = sample;
                if (++ch >= channels) ch = 0;
            }
            return samplesRead;
        }
    }
}