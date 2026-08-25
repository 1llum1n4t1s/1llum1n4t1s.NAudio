using System.Collections.Generic;
using System.IO;
namespace NAudio.SoundFont;


/// <summary>
/// base class for structures that can read themselves
/// </summary>
internal abstract class StructureBuilder<T>
{
    protected List<T> data;

    public StructureBuilder()
    {
        Reset();
    }

    public abstract T Read(BinaryReader br);
    public abstract void Write(BinaryWriter bw, T o);
    public abstract int Length { get; }

    public void Reset()
    {
        data = new List<T>();
    }

    protected static int ValidateRange(
        int start, int endExclusive, int availableCount, string description)
    {
        if (start < 0 || endExclusive < start || endExclusive > availableCount)
        {
            throw new InvalidDataException(
                $"Invalid SoundFont {description} range [{start}, {endExclusive}) for {availableCount} records.");
        }

        return endExclusive - start;
    }

    protected void RemoveTerminalRecord(string description)
    {
        if (data.Count == 0)
            throw new InvalidDataException($"Missing required SoundFont {description} terminal record.");

        data.RemoveAt(data.Count - 1);
    }

    public T[] Data => data.ToArray();
}
