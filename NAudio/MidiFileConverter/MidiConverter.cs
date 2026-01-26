using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NAudio.Midi;

namespace MarkHeath.MidiUtils
{
    class MidiConverter
    {
        public event EventHandler<ProgressEventArgs> Progress;
        int filesConverted;
        int filesCopied;
        int directoriesCreated;
        int errors;
        DateTime startTime;
        Properties.Settings settings;
        Regex ezdFileName;
        NamingRules namingRules;

        public MidiConverter(NamingRules namingRules)
        {
            settings = Properties.Settings.Default;
            this.namingRules = namingRules;
            ezdFileName = new Regex(namingRules.FilenameRegex);                
        }

        public void Start()
        {
            filesConverted = 0;
            filesCopied = 0;
            directoriesCreated = 0;
            errors = 0;
            startTime = DateTime.Now;
            LogInformation("{0} Beginning to Convert MIDI Files...", startTime);
            var productVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
            LogInformation("Processing using EZdrummer MIDI Converter v{0}", productVersion);
            LogInformation("Output MIDI type {0}", settings.OutputMidiType);
            LogInformation("Output Channel Number {0}", settings.OutputChannelNumber == -1 ? "Unchanged" : settings.OutputChannelNumber.ToString());

            // warn user if they are using hidden settings
            if (settings.RemoveSequencerSpecific)
            {
                LogWarning("Sequencer Specific Messages will be turned off");
            }
            if (settings.RemoveEmptyTracks)
            {
                LogWarning("Empty type 1 tracks will be removed");
            }
            if (!settings.RecreateEndTrackMarkers)
            {
                LogWarning("End track markers will be left where they are");
            }
            if (settings.TrimTextEvents)
            {
                LogWarning("Text events will have whitespace trimmed");
            }
            if (settings.AddNameMarker)
            {
                LogWarning("Name markers will be added");
            }
            if (settings.RemoveExtraTempoEvents)
            {
                LogWarning("Extra tempo events will be removed");
            }
            if (settings.RemoveExtraMarkers)
            {
                LogWarning("Extra markers will be removed");
            }

            ProcessFolder(settings.InputFolder, settings.OutputFolder, new string[0]);
            var timeTaken = DateTime.Now - startTime;
            LogInformation("Finished in {0}", timeTaken);
            LogInformation(Summary);

        }

        private string[] CreateNewContext(string[] oldContext, string newContextItem)
        {
            var newContext = new string[oldContext.Length + 1];
            for (var n = 0; n < oldContext.Length; n++)
            {
                newContext[n] = oldContext[n];
            }
            newContext[oldContext.Length] = newContextItem;
            return newContext;
        }

        private void ProcessFolder(string folder, string outputFolder, string[] context)
        {
            var midiFiles = Directory.GetFiles(folder);
            foreach (var midiFile in midiFiles)
            {
                try
                {
                    ProcessFile(midiFile, outputFolder, context);
                }
                catch (Exception e)
                {
                    LogError("Unexpected error processing file {0}", midiFile);
                    LogError(e.ToString());
                    errors++;
                }
            }

            var subfolders = Directory.GetDirectories(folder);
            foreach (var subfolder in subfolders)
            {
                var folderName = Path.GetFileName(subfolder);
                var newOutputFolder = Path.Combine(outputFolder, folderName);
                var newContext = CreateNewContext(context, folderName);

                if (!Directory.Exists(newOutputFolder))
                {                    
                    if (settings.VerboseOutput)
                    {
                        LogTrace("Creating folder {0}", newOutputFolder);
                    }
                    Directory.CreateDirectory(newOutputFolder);
                    directoriesCreated++;
                }

                ProcessFolder(subfolder, newOutputFolder, newContext);
            }
        }

        private void ProcessFile(string file, string outputFolder, string[] context)
        {
            var copy = false;
            var fileName = Path.GetFileName(file);
            var target = Path.Combine(outputFolder, fileName);

            if (Path.GetExtension(file).ToLower() == ".mid")
            {
                var midiFile = new MidiFile(file);
                ConvertMidi(midiFile, target, CreateNewContext(context, Path.GetFileNameWithoutExtension(file)));
                filesConverted++;
            }
            else
            {
                copy = true;
            }

            if (copy)
            {
                if (settings.VerboseOutput)
                {
                    LogTrace("Copying File {0} to {1}", fileName, target);
                }
                File.Copy(file, target);
                filesCopied++;
            }
        }


        private void ConvertMidi(MidiFile midiFile, string target, string[] context)
        {
            var fileNameWithoutExtension = context[context.Length - 1];
            string name = null;
            long endTrackTime = -1;
            if (settings.UseFileName)
            {
                name = fileNameWithoutExtension;
            }
            if (settings.ApplyNamingRules)
            {
                if (ezdFileName.Match(fileNameWithoutExtension).Success)
                {
                    name = CreateEzdName(context);
                }
            }

            var outputFileType = midiFile.FileFormat;
            int outputTrackCount;
            if (settings.OutputMidiType == OutputMidiType.Type0)
            {
                outputFileType = 0;
            }
            else if (settings.OutputMidiType == OutputMidiType.Type1)
            {
                outputFileType = 1;
            }

            if (outputFileType == 0)
            {
                outputTrackCount = 1;
            }
            else
            {
                if (midiFile.FileFormat == 0)
                    outputTrackCount = 2;
                else
                    outputTrackCount = Math.Max(midiFile.Tracks,2); // at least two tracks because we'll move notes onto track 1 always
            }


            var events = new MidiEventCollection(outputFileType, midiFile.DeltaTicksPerQuarterNote);
            for (var track = 0; track < outputTrackCount; track++)
            {
                events.AddTrack();
            }
            if (name != null)
            {
                for (var track = 0; track < outputTrackCount; track++)
                {
                    events[track].Add(new TextEvent(name, MetaEventType.SequenceTrackName, 0));
                }
                if (settings.AddNameMarker)
                {
                    events[0].Add(new TextEvent(name, MetaEventType.Marker, 0));
                }
            }

            foreach (var midiEvent in midiFile.Events[0])
            {
                if (settings.OutputChannelNumber != -1)
                    midiEvent.Channel = settings.OutputChannelNumber;
                if (midiEvent is MetaEvent metaEvent)
                {
                    var exclude = false;
                    switch (metaEvent.MetaEventType)
                    {
                        case MetaEventType.SequenceTrackName:
                            if (name != null)
                            {
                                exclude = true;
                            }
                            break;
                        case MetaEventType.SequencerSpecific:
                            exclude = settings.RemoveSequencerSpecific;
                            break;
                        case MetaEventType.EndTrack:
                            exclude = settings.RecreateEndTrackMarkers;
                            endTrackTime = metaEvent.AbsoluteTime;
                            break;
                        case MetaEventType.SetTempo:
                            if (metaEvent.AbsoluteTime != 0 && settings.RemoveExtraTempoEvents)
                            {
                                LogWarning("Removing a tempo event ({0}bpm) at {1} from {2}", ((TempoEvent)metaEvent).Tempo, metaEvent.AbsoluteTime, target);
                                exclude = true;
                            }
                            break;
                        case MetaEventType.TextEvent:
                            if (settings.TrimTextEvents)
                            {
                                var textEvent = (TextEvent)midiEvent;
                                textEvent.Text = textEvent.Text.Trim();
                                if (textEvent.Text.Length == 0)
                                {
                                    exclude = true;
                                }
                            }
                            break;
                        case MetaEventType.Marker:
                            if (settings.AddNameMarker && midiEvent.AbsoluteTime == 0)
                            {
                                exclude = true;
                            }
                            if (settings.RemoveExtraMarkers && midiEvent.AbsoluteTime > 0)
                            {
                                LogWarning("Removing a marker ({0}) at {1} from {2}", ((TextEvent)metaEvent).Text, metaEvent.AbsoluteTime, target);                                
                                exclude = true;
                            }
                            break;
                    }
                    if (!exclude)
                    {
                        events[0].Add(midiEvent);
                    }
                }
                else
                {
                    if (outputFileType == 1)
                        events[1].Add(midiEvent);
                    else
                        events[0].Add(midiEvent);
                }
            }

            // now do track 1 (Groove Monkee)                
            for (var inputTrack = 1; inputTrack < midiFile.Tracks; inputTrack++)
            {
                int outputTrack;
                if(outputFileType == 1)
                    outputTrack = inputTrack;
                else
                    outputTrack = 0;

                foreach (var midiEvent in midiFile.Events[inputTrack])
                {                    
                    if (settings.OutputChannelNumber != -1)
                        midiEvent.Channel = settings.OutputChannelNumber;
                    var exclude = false;
                    if (midiEvent is MetaEvent metaEvent)
                    {
                        switch (metaEvent.MetaEventType)
                        {
                            case MetaEventType.SequenceTrackName:
                                if (name != null)
                                {
                                    exclude = true;
                                }
                                break;
                            case MetaEventType.SequencerSpecific:
                                exclude = settings.RemoveSequencerSpecific;
                                break;
                            case MetaEventType.EndTrack:
                                exclude = settings.RecreateEndTrackMarkers;
                                break;
                        }
                    }
                    if (!exclude)
                    {
                        events[outputTrack].Add(midiEvent);
                    }
                }
                if(outputFileType == 1 && settings.RecreateEndTrackMarkers)
                {
                    AppendEndMarker(events[outputTrack]);
                }
            }

            if (settings.RecreateEndTrackMarkers)
            {
                if (outputFileType == 1)
                {
                    // make sure track 1 has an end track marker
                    AppendEndMarker(events[1]);
                }
                // make sure that track zero has an end track marker
                AppendEndMarker(events[0]);
            }
            else
            {
                // if we are converting type 0 to type 1 without recreating end markers,
                // then we still need to add an end marker to track 1
                if (midiFile.FileFormat == 0)
                {
                    // use the time we got from track 0 as the end track time for track 1
                    if (endTrackTime == -1)
                    {
                        LogError("Error adding track 1 end marker");
                        // make it a valid MIDI file anyway
                        AppendEndMarker(events[1]);
                    }
                    else
                    {
                        events[1].Add(new MetaEvent(MetaEventType.EndTrack, 0, endTrackTime));
                    }
                }
            }

            if (settings.VerboseOutput)
            {
                LogTrace("Processing {0}: {1}", name, target);
            }

            if (settings.RemoveEmptyTracks)
            {
                var newList = new MidiEventCollection(events.MidiFileType, events.DeltaTicksPerQuarterNote);
                
                var removed = 0;
                for (var track = 0; track < events.Tracks; track++)
                {
                    var trackEvents = events[track];
                    if (track < 2)
                    {
                        newList.AddTrack(events[track]);
                    }
                    else
                    {
                        if(HasNotes(trackEvents))
                        {
                            newList.AddTrack(trackEvents);
                        }
                        else
                        {
                            removed++;
                        }
                    }

                }
                if (removed > 0)
                {
                    events = newList;
                    LogWarning("Removed {0} empty tracks from {1} ({2} remain)", removed, target, events.Tracks);
                }
            }
            MidiFile.Export(target, events);
        }

        private bool HasNotes(IList<MidiEvent> midiEvents)
        {
            return midiEvents.Any(midiEvent => midiEvent.CommandCode == MidiCommandCode.NoteOn);
        }

        private bool IsEndTrack(MidiEvent midiEvent)
        {
            var meta = midiEvent as MetaEvent;
            return meta?.MetaEventType == MetaEventType.EndTrack;
        }

        private void AppendEndMarker(IList<MidiEvent> eventList)
        {
            long absoluteTime = 0;
            
            if (eventList.Count > 0)
                absoluteTime = eventList[eventList.Count - 1].AbsoluteTime;
            
            if (!IsEndTrack(eventList.LastOrDefault()))
                eventList.Add(new MetaEvent(MetaEventType.EndTrack, 0, absoluteTime));
        }

        private string CreateEzdName(string[] context)
        {
            var name = new StringBuilder();
            var contextLevels = Math.Min(namingRules.ContextDepth, context.Length);
            for (var n = 0; n < contextLevels; n++)
            {
                var filtered = ApplyNameFilters(context[context.Length - contextLevels + n]);
                if (filtered.Length > 0)
                {
                    name.Append(filtered);

                    if (n != contextLevels - 1)
                        name.Append(namingRules.ContextSeparator);
                }
            }
            return name.ToString();
        }

        private string ApplyNameFilters(string name)
        {
            foreach (var rule in namingRules.Rules)
            {
                name = Regex.Replace(name, rule.Regex, rule.Replacement);
            }
            return name;
        }

        private void LogTrace(string message, params object[] args)
        {
            OnProgress(this, new ProgressEventArgs(ProgressMessageType.Trace,
                message, args));
        }

        private void LogInformation(string message, params object[] args)
        {
            OnProgress(this, new ProgressEventArgs(ProgressMessageType.Information,
                message, args));
        }

        private void LogWarning(string message, params object[] args)
        {
            OnProgress(this, new ProgressEventArgs(ProgressMessageType.Warning,
                message, args));
        }

        private void LogError(string message, params object[] args)
        {
            OnProgress(this, new ProgressEventArgs(ProgressMessageType.Error,
                message, args));
        }

        protected void OnProgress(object sender, ProgressEventArgs args)
        {
            if (Progress != null)
            {
                Progress(sender, args);
            }
        }

        public string Summary
        {
            get
            {
                return String.Format("Files Converted {0}\r\nFiles Copied {1}\r\nFolders Created {2}\r\nErrors {3}", filesConverted, filesCopied, directoriesCreated, errors);
            }
        }
    }
}
