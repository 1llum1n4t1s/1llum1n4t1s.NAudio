using System.IO;
using NAudio.Utils;

namespace NAudio.SoundFont
{
    class SampleHeaderBuilder : StructureBuilder<SampleHeader>
    {
        public override SampleHeader Read(BinaryReader br)
        {
            var sh = new SampleHeader();
            var s = br.ReadBytes(20);

            sh.SampleName = ByteEncoding.Instance.GetString(s, 0, s.Length);
            sh.Start = br.ReadUInt32();
            sh.End = br.ReadUInt32();
            sh.StartLoop = br.ReadUInt32();
            sh.EndLoop = br.ReadUInt32();
            sh.SampleRate = br.ReadUInt32();
            sh.OriginalPitch = br.ReadByte();
            sh.PitchCorrection = br.ReadSByte();
            sh.SampleLink = br.ReadUInt16();
            sh.SFSampleLink = (SFSampleLink)br.ReadUInt16();
            data.Add(sh);
            return sh;
        }

        public override void Write(BinaryWriter bw, SampleHeader sampleHeader)
        {
        }

        public override int Length => 46;

        internal void RemoveEOS()
        {
            // 細工された .sf2 で SHDR チャンクが空 (=末尾 EOS センチネル無し) のとき
            // RemoveAt(-1) で ArgumentOutOfRangeException が漏れるのを防ぎ、
            // 解析失敗として API 契約 (InvalidDataException) に揃える。
            if (data.Count == 0)
                throw new InvalidDataException("SoundFont SHDR チャンクに EOS 番兵がありません");
            data.RemoveAt(data.Count - 1);
        }

        public SampleHeader[] SampleHeaders => data.ToArray();
    }
}