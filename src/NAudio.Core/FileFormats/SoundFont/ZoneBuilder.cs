using System;
using System.IO;

namespace NAudio.SoundFont;

internal class ZoneBuilder : StructureBuilder<Zone>
{
    private Zone lastZone = null;

    public override Zone Read(BinaryReader br)
    {
        Zone z = new Zone();
        z.generatorIndex = br.ReadUInt16();
        z.modulatorIndex = br.ReadUInt16();
        if (lastZone != null)
        {
            if (z.generatorIndex < lastZone.generatorIndex || z.modulatorIndex < lastZone.modulatorIndex)
                throw new InvalidDataException("SoundFont zone generator and modulator indices must not decrease.");

            lastZone.generatorCount = (ushort)(z.generatorIndex - lastZone.generatorIndex);
            lastZone.modulatorCount = (ushort)(z.modulatorIndex - lastZone.modulatorIndex);
        }
        data.Add(z);
        lastZone = z;
        return z;
    }

    public override void Write(BinaryWriter bw, Zone zone)
    {
        //bw.Write(p.---);
    }

    public void Load(Modulator[] modulators, Generator[] generators)
    {
        if (data.Count == 0)
            throw new InvalidDataException("Missing required SoundFont bag terminal record.");

        Zone terminal = data[^1];
        ValidateRange(terminal.generatorIndex, terminal.generatorIndex,
            generators.Length, "zone generator terminal");
        ValidateRange(terminal.modulatorIndex, terminal.modulatorIndex,
            modulators.Length, "zone modulator terminal");

        // The final bag is terminal. Successive indices define exclusive ranges.
        for (int zone = 0; zone < data.Count - 1; zone++)
        {
            Zone z = data[zone];
            Zone next = data[zone + 1];
            z.generatorCount = (ushort)ValidateRange(
                z.generatorIndex, next.generatorIndex, generators.Length, "zone generator");
            z.modulatorCount = (ushort)ValidateRange(
                z.modulatorIndex, next.modulatorIndex, modulators.Length, "zone modulator");
            z.Generators = new Generator[z.generatorCount];
            Array.Copy(generators, z.generatorIndex, z.Generators, 0, z.generatorCount);
            z.Modulators = new Modulator[z.modulatorCount];
            Array.Copy(modulators, z.modulatorIndex, z.Modulators, 0, z.modulatorCount);
        }
        RemoveTerminalRecord("bag");
    }

    public Zone[] Zones => data.ToArray();

    public override int Length => 4;
}
