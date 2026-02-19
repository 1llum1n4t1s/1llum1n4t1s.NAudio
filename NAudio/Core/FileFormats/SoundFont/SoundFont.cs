using System;
using System.IO;

namespace NAudio.SoundFont
{
    /// <summary>
    /// Represents a SoundFont
    /// </summary>
    public class SoundFont
    {
        private InfoChunk info;
        private PresetsChunk presetsChunk;
        private SampleDataChunk sampleData;

        /// <summary>
        /// Loads a SoundFont from a file
        /// </summary>
        /// <param name="fileName">Filename of the SoundFont</param>
        public SoundFont(string fileName) :
            this(new FileStream(fileName, FileMode.Open, FileAccess.Read))
        {
        }

        /// <summary>
        /// Loads a SoundFont from a stream
        /// </summary>
        /// <param name="sfFile">stream</param>
        public SoundFont(Stream sfFile)
        {
            using (sfFile) // a bit ugly, done to get Win store to compile
            {
                var riff = RiffChunk.GetTopLevelChunk(new BinaryReader(sfFile));
                if (riff.ChunkID == "RIFF")
                {
                    var formHeader = riff.ReadChunkID();
                    if (formHeader != "sfbk")
                    {
                        throw new InvalidDataException($"Not a SoundFont ({formHeader})");
                    }
                    var list = riff.GetNextSubChunk();
                    if (list.ChunkID == "LIST")
                    {
                        //RiffChunk r = list.GetNextSubChunk();
                        info = new InfoChunk(list);

                        var r = riff.GetNextSubChunk();
                        sampleData = new SampleDataChunk(r);

                        r = riff.GetNextSubChunk();
                        presetsChunk = new PresetsChunk(r);
                    }
                    else
                    {
                        throw new InvalidDataException($"Not info list found ({list.ChunkID})");
                    }
                }
                else
                {
                    throw new InvalidDataException("Not a RIFF file");
                }
            }
        }

        /// <summary>
        /// The File Info Chunk
        /// </summary>
        public InfoChunk FileInfo => info;

        /// <summary>
        /// The Presets
        /// </summary>
        public Preset[] Presets => presetsChunk.Presets;

        /// <summary>
        /// The Instruments
        /// </summary>
        public Instrument[] Instruments => presetsChunk.Instruments;

        /// <summary>
        /// The Sample Headers
        /// </summary>
        public SampleHeader[] SampleHeaders => presetsChunk.SampleHeaders;

        /// <summary>
        /// The Sample Data
        /// </summary>
        public byte[] SampleData => sampleData.SampleData;

        /// <summary>
        /// <see cref="Object.ToString"/>
        /// </summary>
        public override string ToString()
        {
            return $"Info Chunk:\r\n{info}\r\nPresets Chunk:\r\n{presetsChunk}";
        }

        // TODO: save / save as function
    }
}
