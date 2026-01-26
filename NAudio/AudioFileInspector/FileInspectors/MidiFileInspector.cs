using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using NAudio.Midi;
using System.ComponentModel.Composition;

namespace AudioFileInspector
{
    [Export(typeof(IAudioFileInspector))]
    public class MidiFileInspector : IAudioFileInspector
    {
        #region IAudioFileInspector Members

        public string FileExtension
        {
            get { return ".mid"; }
        }

        public string FileTypeDescription
        {
            get { return "Standard MIDI File"; }
        }

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
                    if(!MidiEvent.IsNoteOff(midiEvent))
                    {
                        sb.AppendFormat("{0} {1}\r\n", ToMBT(midiEvent.AbsoluteTime, mf.DeltaTicksPerQuarterNote, timeSignature), midiEvent);
                    }
                }
            }
            return sb.ToString();
        }

        private string ToMBT(long eventTime, int ticksPerQuarterNote, TimeSignatureEvent timeSignature)
        {
            var beatsPerBar = timeSignature == null ? 4 : timeSignature.Numerator;
            var ticksPerBar = timeSignature == null ? ticksPerQuarterNote * 4 : (timeSignature.Numerator * ticksPerQuarterNote * 4) / (1 << timeSignature.Denominator);
            var ticksPerBeat = ticksPerBar / beatsPerBar;
            var bar = 1 + (eventTime / ticksPerBar);
            var beat = 1 + ((eventTime % ticksPerBar) / ticksPerBeat);
            var tick = eventTime % ticksPerBeat;
            return String.Format("{0}:{1}:{2}", bar, beat, tick);
        }

        /// <summary>
        /// Find the number of beats per measure
        /// (for now assume just one TimeSignature per MIDI track)
        /// </summary>
        private int FindBeatsPerMeasure(IEnumerable<MidiEvent> midiEvents)
        {
            var beatsPerMeasure = 4;
            foreach (var midiEvent in midiEvents)
            {
                if (midiEvent is TimeSignatureEvent tse)
                {
                    beatsPerMeasure = tse.Numerator;
                }
            }
            return beatsPerMeasure;
        }


        #endregion
    }
}
