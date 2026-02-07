using System.IO;

namespace NAudio.SoundFont
{
    class SampleDataChunk
    {
        public SampleDataChunk(RiffChunk chunk)
        {
            var header = chunk.ReadChunkID();
            if (header != "sdta")
            {
                throw new InvalidDataException($"Not a sample data chunk ({header})");
            }
            RiffChunk c;
            while ((c = chunk.GetNextSubChunk()) != null)
            {
                if (c.ChunkID == "smpl")
                {
                    SampleData = c.GetData();
                }
            }
        }

        public byte[] SampleData { get; private set; }
    }

}