using System.IO;

namespace NAudio.SoundFont
{
    internal class GeneratorBuilder : StructureBuilder<Generator>
    {
        public override Generator Read(BinaryReader br)
        {
            var g = new Generator();
            g.GeneratorType = (GeneratorEnum)br.ReadUInt16();
            g.UInt16Amount = br.ReadUInt16();
            data.Add(g);
            return g;
        }

        public override void Write(BinaryWriter bw, Generator o)
        {
            //Zone z = (Zone) o;
            //bw.Write(p.---);
        }

        public override int Length => 4;

        public Generator[] Generators => data.ToArray();

        public void Load(Instrument[] instruments)
        {
            foreach (var g in Generators)
            {
                if (g.GeneratorType == GeneratorEnum.Instrument)
                {
                    // 細工された .sf2 で UInt16Amount (最大 65535) が
                    // instruments 配列長を超えていると IndexOutOfRangeException
                    if (g.UInt16Amount >= instruments.Length)
                    {
                        throw new System.IO.InvalidDataException(
                            $"SoundFont Generator の Instrument リンク先 ({g.UInt16Amount}) が範囲外です (Instrument 数: {instruments.Length})");
                    }
                    g.Instrument = instruments[g.UInt16Amount];
                }
            }
        }

        public void Load(SampleHeader[] sampleHeaders)
        {
            foreach (var g in Generators)
            {
                if (g.GeneratorType == GeneratorEnum.SampleID)
                {
                    // 同上 (SampleID リンク先のレンジチェック)
                    if (g.UInt16Amount >= sampleHeaders.Length)
                    {
                        throw new System.IO.InvalidDataException(
                            $"SoundFont Generator の SampleHeader リンク先 ({g.UInt16Amount}) が範囲外です (SampleHeader 数: {sampleHeaders.Length})");
                    }
                    g.SampleHeader = sampleHeaders[g.UInt16Amount];
                }
            }
        }
    }
}