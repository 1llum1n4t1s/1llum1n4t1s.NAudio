using System;
using System.IO;
using System.Text;

namespace NAudio.SoundFont;

internal class PresetBuilder : StructureBuilder<Preset>
{
    private Preset lastPreset = null;

    public override Preset Read(BinaryReader br)
    {
        Preset p = new Preset();
        string s = Encoding.UTF8.GetString(br.ReadBytes(20), 0, 20);
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
            if (p.startPresetZoneIndex < lastPreset.startPresetZoneIndex)
                throw new InvalidDataException("SoundFont preset zone indices must not decrease.");

            lastPreset.endPresetZoneIndex = p.startPresetZoneIndex == lastPreset.startPresetZoneIndex
                ? lastPreset.startPresetZoneIndex
                : (ushort)(p.startPresetZoneIndex - 1);
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
        if (data.Count == 0)
            throw new InvalidDataException("Missing required SoundFont phdr/EOP terminal record.");

        ValidateRange(data[^1].startPresetZoneIndex, data[^1].startPresetZoneIndex,
            presetZones.Length, "preset zone terminal");

        // The final record is EOP. Successive start indices define an exclusive range.
        for (int preset = 0; preset < data.Count - 1; preset++)
        {
            Preset p = data[preset];
            int endExclusive = data[preset + 1].startPresetZoneIndex;
            int count = ValidateRange(
                p.startPresetZoneIndex, endExclusive, presetZones.Length, "preset zone");
            p.Zones = new Zone[count];
            Array.Copy(presetZones, p.startPresetZoneIndex, p.Zones, 0, p.Zones.Length);
        }
        RemoveTerminalRecord("phdr/EOP");
    }

    public Preset[] Presets => data.ToArray();
}
