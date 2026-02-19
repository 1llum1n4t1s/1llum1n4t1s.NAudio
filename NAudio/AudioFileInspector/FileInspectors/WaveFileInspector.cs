using System;
using System.Buffers.Binary;
using System.Text;
using NAudio.Wave;
using NAudio.Utils;
using System.ComponentModel.Composition;

namespace AudioFileInspector;

/// <summary>
/// WAV ファイル用インスペクター。
/// </summary>
[Export(typeof(IAudioFileInspector))]
public class WaveFileInspector : IAudioFileInspector
{
    /// <inheritdoc />
    public string FileExtension => ".wav";

    /// <inheritdoc />
    public string FileTypeDescription => "Wave File";

    /// <inheritdoc />
    public string Describe(string fileName)
    {
        var stringBuilder = new StringBuilder();
        using (var wf = new WaveFileReader(fileName))
        {
            stringBuilder.AppendFormat("{0} {1}Hz {2} channels {3} bits per sample\r\n",
                wf.WaveFormat.Encoding, wf.WaveFormat.SampleRate, wf.WaveFormat.Channels, wf.WaveFormat.BitsPerSample);
            stringBuilder.AppendFormat("Extra Size: {0} Block Align: {1} Average Bytes Per Second: {2}\r\n",
                wf.WaveFormat.ExtraSize, wf.WaveFormat.BlockAlign, wf.WaveFormat.AverageBytesPerSecond);
            stringBuilder.AppendFormat("WaveFormat: {0}\r\n", wf.WaveFormat);
            stringBuilder.AppendFormat("Length: {0} bytes: {1} \r\n", wf.Length, wf.TotalTime);
            foreach (var chunk in wf.ExtraChunks)
            {
                stringBuilder.AppendFormat("Chunk: {0}, length {1}\r\n", chunk.IdentifierAsString, chunk.Length);
                var data = wf.GetChunkData(chunk);
                DescribeChunk(chunk, stringBuilder, data);
            }
        }
        return stringBuilder.ToString();
    }

    private static void DescribeChunk(RiffChunk chunk, StringBuilder stringBuilder, byte[] data)
        {
            switch(chunk.IdentifierAsString)
            {
                case "strc":
                    DescribeStrc(stringBuilder, data);
                    break;
                case "bext":
                    DescribeBext(stringBuilder, data);
                    break;
                case "iXML":
                    stringBuilder.Append(UTF8Encoding.UTF8.GetString(data));
                    break;
                default:
                    {
                        if (ByteArrayExtensions.IsEntirelyNull(data))
                        {
                            stringBuilder.AppendFormat("{0} null bytes\r\n", data.Length);
                        }
                        else
                        {
                            stringBuilder.AppendFormat("{0}\r\n", ByteArrayExtensions.DescribeAsHex(data," ",32));
                        }
                    }
                    break;
            }
        }

        private static void DescribeBext(StringBuilder sb, byte[] data)
        {
            // bext minimum fixed size is 602 bytes before the variable-length Coding History
            if (data.Length < 602)
            {
                sb.AppendFormat("bext chunk too short ({0} bytes, expected at least 602)\r\n", data.Length);
                return;
            }
            var offset = 0;
            sb.AppendFormat("Description: {0}\r\n", ByteArrayExtensions.DecodeAsString(data, 0, 256, ASCIIEncoding.ASCII));
            offset += 256;
            sb.AppendFormat("Originator: {0}\r\n", ByteArrayExtensions.DecodeAsString(data, offset, 32, ASCIIEncoding.ASCII));
            offset += 32;
            sb.AppendFormat("Originator Reference: {0}\r\n", ByteArrayExtensions.DecodeAsString(data, offset, 32, ASCIIEncoding.ASCII));
            offset += 32;
            sb.AppendFormat("Origination Date: {0}\r\n", ByteArrayExtensions.DecodeAsString(data, offset, 10, ASCIIEncoding.ASCII));
            offset += 10;
            sb.AppendFormat("Origination Time: {0}\r\n", ByteArrayExtensions.DecodeAsString(data, offset, 8, ASCIIEncoding.ASCII));
            offset += 8;
            sb.AppendFormat("Time Reference Low: {0}\r\n", BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset)));
            offset += 4;
            sb.AppendFormat("Time Reference High: {0}\r\n", BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset)));
            offset += 4;
            sb.AppendFormat("Version: {0}\r\n", BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset)));
            offset += 2;
            sb.AppendFormat("SMPTE UMID: {0}\r\n", ByteArrayExtensions.DecodeAsString(data, offset, 64, Encoding.ASCII));
            offset += 64;
            sb.AppendFormat("Loudness Value: {0}\r\n", BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset)));
            offset += 2;
            sb.AppendFormat("Loudness Range: {0}\r\n", BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset)));
            offset += 2;
            sb.AppendFormat("Max True Peak Level: {0}\r\n", BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset)));
            offset += 2;
            sb.AppendFormat("Max Momentary Loudness: {0}\r\n", BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset)));
            offset += 2;
            sb.AppendFormat("Max short term loudness: {0}\r\n", BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset)));
            offset += 2;
            //byte[] reserved = 180 bytes;
            offset += 180;
            if (offset < data.Length)
                sb.AppendFormat("Coding History: {0}\r\n", ByteArrayExtensions.DecodeAsString(data, offset, data.Length - offset, Encoding.ASCII));
        }




        private static void DescribeStrc(StringBuilder stringBuilder, byte[] data)
        {
            // First 28 bytes are header
            if (data.Length < 28)
            {
                stringBuilder.AppendFormat("strc chunk too short ({0} bytes, expected at least 28)\r\n", data.Length);
                return;
            }
            var header1 = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(0)); // always 0x1C?
            var sliceCount = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(4));
            var header2 = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(8)); // 0x19 or 0x41?
            var header3 = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(12)); // 0x05 or 0x0A? (linked with header 2 - 0x41 0x05 go together and 0x19 0x0A go together)
            var header4 = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(16)); // always 1?
            var header5 = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(20)); // 0x00, 0x01 or 0x0A?
            var header6 = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(24)); // 0x02, 0x04. 0x05

            if (sliceCount < 0)
                sliceCount = 0;

            stringBuilder.AppendFormat("{0} slices. unknown: {1},{2},{3},{4},{5},{6}\r\n",
                sliceCount,header1,header2,header3,header4,header5,header6);

            var offset = 28;

            for (var slice = 0; slice < sliceCount; slice++)
            {
                if (offset + 32 > data.Length)
                {
                    stringBuilder.AppendFormat("strc chunk truncated at slice {0} (offset {1}, data length {2})\r\n", slice, offset, data.Length);
                    break;
                }
                var unknown1 = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset)); // 0 or 2
                var uniqueId1 = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset + 4)); // another unique ID - doesn't change?

                var samplePosition = BinaryPrimitives.ReadInt64LittleEndian(data.AsSpan(offset + 8));
                var samplePos2 = BinaryPrimitives.ReadInt64LittleEndian(data.AsSpan(offset + 16)); // is zero the first time through, equal to sample position next time round
                var unknown5 = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset + 24)); // large number first time through, zero second time through, not flags, not a float
                var uniqueId2 = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset + 28)); // always the same
                offset += 32;
                stringBuilder.AppendFormat("Pos: {2},{3} unknown: {0},{4}\r\n",
                    unknown1, uniqueId1, samplePosition, samplePos2, unknown5, uniqueId2);
            }
        }
    }
