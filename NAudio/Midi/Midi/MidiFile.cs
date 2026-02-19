using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Collections.Generic;
using NAudio.Utils;

namespace NAudio.Midi 
{
    /// <summary>
    /// Class able to read a MIDI file
    /// </summary>
    public class MidiFile 
    {
        private readonly MidiEventCollection events;
        private readonly ushort fileFormat;
        //private ushort tracks;
        private readonly ushort deltaTicksPerQuarterNote;
        private readonly bool strictChecking;

        /// <summary>
        /// Opens a MIDI file for reading
        /// </summary>
        /// <param name="filename">Name of MIDI file</param>
        public MidiFile(string filename)
            : this(filename,true)
        {
        }

        /// <summary>
        /// MIDI File format
        /// </summary>
        public int FileFormat => fileFormat;

        /// <summary>
        /// Opens a MIDI file for reading
        /// </summary>
        /// <param name="filename">Name of MIDI file</param>
        /// <param name="strictChecking">If true will error on non-paired note events</param>
        public MidiFile(string filename, bool strictChecking) :
            this(File.OpenRead(filename), strictChecking, true)
        {
        }

        /// <summary>
        /// Opens a MIDI file stream for reading
        /// </summary>
        /// <param name="inputStream">The input stream containing a MIDI file</param>
        /// <param name="strictChecking">If true will error on non-paired note events</param>
        public MidiFile(Stream inputStream, bool strictChecking) :
            this(inputStream, strictChecking, false)
        {
        }

        private MidiFile(Stream inputStream, bool strictChecking, bool ownInputStream)
        {
            this.strictChecking = strictChecking;
            
            var br = new BinaryReader(inputStream);
            try 
            {
                var chunkHeader = Encoding.UTF8.GetString(br.ReadBytes(4));
                if(chunkHeader != "MThd") 
                {
                    throw new FormatException("Not a MIDI file - header chunk missing");
                }
                var chunkSize = SwapUInt32(br.ReadUInt32());
                
                if(chunkSize != 6) 
                {
                    throw new FormatException("Unexpected header chunk length");
                }
                // 0 = single track, 1 = multi-track synchronous, 2 = multi-track asynchronous
                fileFormat = SwapUInt16(br.ReadUInt16());
                if (fileFormat > 2)
                {
                    throw new FormatException($"Unsupported MIDI file format: {fileFormat}");
                }
                int tracks = SwapUInt16(br.ReadUInt16());
                if (tracks == 0)
                {
                    throw new FormatException("MIDI file has no tracks");
                }
                if (fileFormat == 0 && tracks != 1)
                {
                    throw new FormatException($"MIDI file format 0 must have exactly 1 track, but has {tracks}");
                }
                deltaTicksPerQuarterNote = SwapUInt16(br.ReadUInt16());
                if (deltaTicksPerQuarterNote == 0)
                {
                    throw new FormatException("MIDI file has zero delta ticks per quarter note");
                }

                events = new MidiEventCollection((fileFormat == 0) ? 0 : 1, deltaTicksPerQuarterNote);
                for (var n = 0; n < tracks; n++)
                {
                    events.AddTrack();
                }
                
                long absoluteTime = 0;
                
                for(var track = 0; track < tracks; track++) 
                {
                    if(fileFormat != 0)
                    {
                        absoluteTime = 0;
                    }
                    chunkHeader = Encoding.UTF8.GetString(br.ReadBytes(4));
                    if(chunkHeader != "MTrk") 
                    {
                        throw new FormatException("Invalid chunk header");
                    }
                    chunkSize = SwapUInt32(br.ReadUInt32());

                    var startPos = br.BaseStream.Position;
                    var endPos = startPos + chunkSize;
                    if (endPos > br.BaseStream.Length)
                    {
                        throw new FormatException(
                            $"Track {track} chunk size {chunkSize} exceeds stream length (position {startPos}, stream length {br.BaseStream.Length})");
                    }
                    MidiEvent me = null;
                    var outstandingNoteOns = new List<NoteOnEvent>();
                    while(br.BaseStream.Position < startPos + chunkSize) 
                    {
                        try
                        {
                            me = MidiEvent.ReadNextEvent(br, me);
                        }
                        catch (InvalidDataException)
                        {
                            if (strictChecking) throw;
                            continue;
                        }
                        catch (FormatException)
                        {
                            if (strictChecking) throw;
                            continue;
                        }

                        absoluteTime += me.DeltaTime;
                        me.AbsoluteTime = absoluteTime;
                        events[track].Add(me);
                        if (me.CommandCode == MidiCommandCode.NoteOn) 
                        {
                            var ne = (NoteEvent) me;
                            if(ne.Velocity > 0) 
                            {
                                outstandingNoteOns.Add((NoteOnEvent) ne);
                            }
                            else 
                            {
                                // don't remove the note offs, even though
                                // they are annoying
                                // events[track].Remove(me);
                                FindNoteOn(ne,outstandingNoteOns);
                            }
                        }
                        else if(me.CommandCode == MidiCommandCode.NoteOff) 
                        {
                            FindNoteOn((NoteEvent) me,outstandingNoteOns);
                        }
                        else if(me.CommandCode == MidiCommandCode.MetaEvent) 
                        {
                            var metaEvent = (MetaEvent) me;
                            if(metaEvent.MetaEventType == MetaEventType.EndTrack) 
                            {
                                //break;
                                // some dodgy MIDI files have an event after end track
                                if (strictChecking)
                                {
                                    if (br.BaseStream.Position < startPos + chunkSize)
                                    {
                                        throw new FormatException(
                                            $"End Track event was not the last MIDI event on track {track}");
                                    }
                                }
                            }
                        }
                    }
                    if(outstandingNoteOns.Count > 0) 
                    {
                        if (strictChecking)
                        {
                            throw new FormatException(
                                $"Note ons without note offs {outstandingNoteOns.Count} (file format {fileFormat})");
                        }
                    }
                    if(br.BaseStream.Position != startPos + chunkSize) 
                    {
                        throw new FormatException($"Read too far {chunkSize}+{startPos}!={br.BaseStream.Position}");
                    }
                }
            }
            finally
            {
                if (ownInputStream)
                {
                    br.Dispose();
                }
            }
        }

        /// <summary>
        /// The collection of events in this MIDI file
        /// </summary>
        public MidiEventCollection Events => events;

        /// <summary>
        /// Number of tracks in this MIDI file
        /// </summary>
        public int Tracks => events.Tracks;

        /// <summary>
        /// Delta Ticks Per Quarter Note
        /// </summary>
        public int DeltaTicksPerQuarterNote => deltaTicksPerQuarterNote;

        private void FindNoteOn(NoteEvent offEvent, List<NoteOnEvent> outstandingNoteOns)
        {
            var found = false;
            // Search from the end: most recent NoteOn is the most likely match,
            // improving performance when outstandingNoteOns is large.
            for (var i = outstandingNoteOns.Count - 1; i >= 0; i--)
            {
                var noteOnEvent = outstandingNoteOns[i];
                if ((noteOnEvent.Channel == offEvent.Channel) && (noteOnEvent.NoteNumber == offEvent.NoteNumber))
                {
                    noteOnEvent.OffEvent = offEvent;
                    outstandingNoteOns.RemoveAt(i);
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                if (strictChecking)
                {
                    throw new FormatException($"Got an off without an on {offEvent}");
                }
            }
        }
        
        private static uint SwapUInt32(uint i)
        {
            return BinaryPrimitives.ReverseEndianness(i);
        }

        private static ushort SwapUInt16(ushort i)
        {
            return BinaryPrimitives.ReverseEndianness(i);
        }
        
        /// <summary>
        /// Describes the MIDI file
        /// </summary>
        /// <returns>A string describing the MIDI file and its events</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"Format {fileFormat}, Tracks {Tracks}, Delta Ticks Per Quarter Note {deltaTicksPerQuarterNote}\r\n");
            for (var n = 0; n < Tracks; n++)
            {
                foreach (var midiEvent in events[n])
                {
                    sb.Append(midiEvent).Append("\r\n");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Exports a MIDI file
        /// </summary>
        /// <param name="filename">Filename to export to</param>
        /// <param name="events">Events to export</param>
        public static void Export(string filename, MidiEventCollection events)
        {
            if (events.MidiFileType == 0 && events.Tracks > 1)
            {
                throw new ArgumentException("Can't export more than one track to a type 0 file");
            }
            using (var writer = new BinaryWriter(File.Create(filename)))
            {
                writer.Write(Encoding.UTF8.GetBytes("MThd"));
                writer.Write(SwapUInt32(6)); // chunk size
                writer.Write(SwapUInt16((ushort)events.MidiFileType));
                writer.Write(SwapUInt16((ushort)events.Tracks));
                writer.Write(SwapUInt16((ushort)events.DeltaTicksPerQuarterNote));

                for (var track = 0; track < events.Tracks; track++ )
                {
                    var eventList = events[track];

                    writer.Write(Encoding.UTF8.GetBytes("MTrk"));
                    var trackSizePosition = writer.BaseStream.Position;
                    writer.Write(SwapUInt32(0));

                    var absoluteTime = events.StartAbsoluteTime;

                    // use a stable sort to preserve ordering of MIDI events whose
                    // absolute times are the same
                    MergeSort.Sort(eventList, new MidiEventComparer());
                    if (eventList.Count > 0)
                    {
                        System.Diagnostics.Debug.Assert(MidiEvent.IsEndTrack(eventList[eventList.Count - 1]), "Exporting a track with a missing end track");
                    }
                    foreach (var midiEvent in eventList)
                    {
                        midiEvent.Export(ref absoluteTime, writer);
                    }

                    var trackChunkLength = (uint)(writer.BaseStream.Position - trackSizePosition) - 4;
                    writer.BaseStream.Position = trackSizePosition;
                    writer.Write(SwapUInt32(trackChunkLength));
                    writer.BaseStream.Position += trackChunkLength;
                }
            }
        }
    }
}