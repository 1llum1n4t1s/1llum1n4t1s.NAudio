using System;
using System.IO;
using System.Text;

namespace NAudio.SoundFont
{
    /// <summary>
    /// Instrument Builder
    /// </summary>
    internal class InstrumentBuilder : StructureBuilder<Instrument>
    {
        private Instrument lastInstrument = null;

        public override Instrument Read(BinaryReader br)
        {
            var i = new Instrument();
            var s = Encoding.UTF8.GetString(br.ReadBytes(20), 0, 20);
            if (s.IndexOf('\0') >= 0)
            {
                s = s.Substring(0, s.IndexOf('\0'));
            }
            i.Name = s;
            i.startInstrumentZoneIndex = br.ReadUInt16();
            if (lastInstrument != null)
            {
                // 細工された .sf2 で startInstrumentZoneIndex が単調増加でない場合、
                // (ushort)(start-1) が 65535 にラップして、後段 LoadZones で巨大配列確保。
                if (i.startInstrumentZoneIndex < lastInstrument.startInstrumentZoneIndex)
                {
                    throw new InvalidDataException(
                        $"SoundFont Instrument の startInstrumentZoneIndex が単調増加していません ({lastInstrument.startInstrumentZoneIndex} → {i.startInstrumentZoneIndex})");
                }
                lastInstrument.endInstrumentZoneIndex = (ushort)(i.startInstrumentZoneIndex - 1);
            }
            data.Add(i);
            lastInstrument = i;
            return i;
        }

        public override void Write(BinaryWriter bw, Instrument instrument)
        {
        }

        public override int Length => 22;

        public void LoadZones(Zone[] zones)
        {
            // 細工された .sf2 で INST チャンクが空のときの RemoveAt(-1) を防ぐ
            if (data.Count == 0)
                throw new InvalidDataException("SoundFont INST チャンクに EOI 番兵がありません");
            // don't do the last preset, which is simply EOP
            for (var instrument = 0; instrument < data.Count - 1; instrument++)
            {
                var i = data[instrument];
                i.Zones = new Zone[i.endInstrumentZoneIndex - i.startInstrumentZoneIndex + 1];
                Array.Copy(zones, i.startInstrumentZoneIndex, i.Zones, 0, i.Zones.Length);
            }
            // we can get rid of the EOP record now
            data.RemoveAt(data.Count - 1);
        }

        public Instrument[] Instruments => data.ToArray();
    }
}