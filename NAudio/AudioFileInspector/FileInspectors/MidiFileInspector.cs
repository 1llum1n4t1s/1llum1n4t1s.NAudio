using System.Linq;
using System.Collections.Generic;
using System.Text;
using NAudio.Midi;
using System.ComponentModel.Composition;

namespace AudioFileInspector;

/// <summary>
/// Standard MIDI ファイル用インスペクター。
/// </summary>
[Export(typeof(IAudioFileInspector))]
public class MidiFileInspector : IAudioFileInspector
{
    /// <inheritdoc />
    public string FileExtension => ".mid";

    /// <inheritdoc />
    public string FileTypeDescription => "Standard MIDI File";

    /// <inheritdoc />
    public string Describe(string fileName)
    {
        var mf = new MidiFile(fileName, false);
        var sb = new StringBuilder();
        sb.AppendFormat("Format {0}, Tracks {1}, Delta Ticks Per Quarter Note {2}\r\n",
            mf.FileFormat, mf.Tracks, mf.DeltaTicksPerQuarterNote);
        var timeSignature = mf.Events[0].OfType<TimeSignatureEvent>().FirstOrDefault();
        for (var n = 0; n < mf.Tracks; n++)
        {
            foreach (var midiEvent in mf.Events[n])
            {
                if (!MidiEvent.IsNoteOff(midiEvent))
                    sb.AppendFormat("{0} {1}\r\n", ToMBT(midiEvent.AbsoluteTime, mf.DeltaTicksPerQuarterNote, timeSignature), midiEvent);
            }
        }
        return sb.ToString();
    }

    private string ToMBT(long eventTime, int ticksPerQuarterNote, TimeSignatureEvent timeSignature)
    {
        if (ticksPerQuarterNote <= 0)
            return $"0:0:{eventTime}";
        var beatsPerBar = timeSignature == null ? 4 : timeSignature.Numerator;
        if (beatsPerBar <= 0)
            beatsPerBar = 4;
        var denominator = timeSignature?.Denominator ?? 2;
        if (denominator < 0 || denominator > 30)
            denominator = 2;
        var ticksPerBar = timeSignature == null ? ticksPerQuarterNote * 4 : (timeSignature.Numerator * ticksPerQuarterNote * 4) / (1 << denominator);
        if (ticksPerBar <= 0)
            ticksPerBar = ticksPerQuarterNote * 4;
        var ticksPerBeat = ticksPerBar / beatsPerBar;
        if (ticksPerBeat <= 0)
            ticksPerBeat = 1;
        var bar = 1 + (eventTime / ticksPerBar);
        var beat = 1 + ((eventTime % ticksPerBar) / ticksPerBeat);
        var tick = eventTime % ticksPerBeat;
        return $"{bar}:{beat}:{tick}";
    }

    /// <summary>
    /// 拍子の拍数を取得する（1 トラックに 1 つの TimeSignature を想定）。
    /// </summary>
    private int FindBeatsPerMeasure(IEnumerable<MidiEvent> midiEvents)
    {
        var beatsPerMeasure = 4;
        foreach (var midiEvent in midiEvents)
        {
            if (midiEvent is TimeSignatureEvent tse)
                beatsPerMeasure = tse.Numerator;
        }
        return beatsPerMeasure;
    }
}
