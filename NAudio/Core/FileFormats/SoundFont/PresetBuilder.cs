using System;
using System.IO;
using System.Text;

namespace NAudio.SoundFont
{
    class PresetBuilder : StructureBuilder<Preset>
    {
        private Preset lastPreset = null;

        public override Preset Read(BinaryReader br)
        {
            var p = new Preset();
            var s = Encoding.UTF8.GetString(br.ReadBytes(20), 0, 20);
            if (s.IndexOf('\0') >= 0)
            {
                s = s.Substring(0, s.IndexOf('\0'));
            }
            p.Name = s;
            p.PatchNumber = br.ReadUInt16();
            p.Bank = br.ReadUInt16();
            p.startPresetZoneIndex = br.ReadUInt16();
            p.library = br.ReadUInt32();
            p.genre = br.ReadUInt32();
            p.morphology = br.ReadUInt32();
            if (lastPreset != null)
            {
                // 細工された .sf2 で startPresetZoneIndex が単調増加でない場合、
                // (ushort)(start-1) が 65535 にラップして、後段 LoadZones で
                // new Zone[65536] や Array.Copy で巨大配列確保 / ArgumentException 発生。
                if (p.startPresetZoneIndex < lastPreset.startPresetZoneIndex)
                {
                    throw new InvalidDataException(
                        $"SoundFont Preset の startPresetZoneIndex が単調増加していません ({lastPreset.startPresetZoneIndex} → {p.startPresetZoneIndex})");
                }
                lastPreset.endPresetZoneIndex = (ushort)(p.startPresetZoneIndex - 1);
            }
            data.Add(p);
            lastPreset = p;
            return p;
        }

        public override void Write(BinaryWriter bw, Preset preset)
        {
        }

        public override int Length => 38;

        public void LoadZones(Zone[] presetZones)
        {
            // 細工された .sf2 で PHDR チャンクが空のときの RemoveAt(-1) を防ぐ
            if (data.Count == 0)
                throw new InvalidDataException("SoundFont PHDR チャンクに EOP 番兵がありません");
            // don't do the last preset, which is simply EOP
            for (var preset = 0; preset < data.Count - 1; preset++)
            {
                var p = data[preset];
                p.Zones = new Zone[p.endPresetZoneIndex - p.startPresetZoneIndex + 1];
                Array.Copy(presetZones, p.startPresetZoneIndex, p.Zones, 0, p.Zones.Length);
            }
            // we can get rid of the EOP record now
            data.RemoveAt(data.Count - 1);
        }

        public Preset[] Presets => data.ToArray();
    }
}