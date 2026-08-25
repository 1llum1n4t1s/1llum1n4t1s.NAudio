using System;
using System.IO;
using System.Text;

namespace NAudio.SoundFont;

/// <summary>
/// Instrument Builder
/// </summary>
internal class InstrumentBuilder : StructureBuilder<Instrument>
{
    private Instrument lastInstrument = null;

    public override Instrument Read(BinaryReader br)
    {
        Instrument i = new Instrument();
        string s = Encoding.UTF8.GetString(br.ReadBytes(20), 0, 20);
        if (s.IndexOf('\0') >= 0)
        {
            s = s.Substring(0, s.IndexOf('\0'));
        }
        i.Name = s;
        i.startInstrumentZoneIndex = br.ReadUInt16();
        if (lastInstrument != null)
        {
            if (i.startInstrumentZoneIndex < lastInstrument.startInstrumentZoneIndex)
                throw new InvalidDataException("SoundFont instrument zone indices must not decrease.");

            lastInstrument.endInstrumentZoneIndex = i.startInstrumentZoneIndex == lastInstrument.startInstrumentZoneIndex
                ? lastInstrument.startInstrumentZoneIndex
                : (ushort)(i.startInstrumentZoneIndex - 1);
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
        if (data.Count == 0)
            throw new InvalidDataException("Missing required SoundFont inst/EOI terminal record.");

        ValidateRange(data[^1].startInstrumentZoneIndex, data[^1].startInstrumentZoneIndex,
            zones.Length, "instrument zone terminal");

        // The final record is EOI. Successive start indices define an exclusive range.
        for (int instrument = 0; instrument < data.Count - 1; instrument++)
        {
            Instrument i = data[instrument];
            int endExclusive = data[instrument + 1].startInstrumentZoneIndex;
            int count = ValidateRange(
                i.startInstrumentZoneIndex, endExclusive, zones.Length, "instrument zone");
            i.Zones = new Zone[count];
            Array.Copy(zones, i.startInstrumentZoneIndex, i.Zones, 0, i.Zones.Length);
        }
        RemoveTerminalRecord("inst/EOI");
    }

    public Instrument[] Instruments => data.ToArray();
}
