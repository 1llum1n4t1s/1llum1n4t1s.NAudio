using System.Text;
using NAudio.SoundFont;
using System.ComponentModel.Composition;

namespace AudioFileInspector;

/// <summary>
/// SoundFont (.sf2) ファイル用インスペクター。
/// </summary>
[Export(typeof(IAudioFileInspector))]
public class SoundFontInspector : IAudioFileInspector
{
    /// <inheritdoc />
    public string FileExtension => ".sf2";

    /// <inheritdoc />
    public string FileTypeDescription => "SoundFont File";

    /// <inheritdoc />
    public string Describe(string fileName)
    {
        var sf = new SoundFont(fileName);
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendFormat("{0}\r\n", sf.FileInfo);
        stringBuilder.Append("Presets\r\n");
        foreach (var p in sf.Presets)
        {
            stringBuilder.AppendFormat("{0}\r\n", p);
            foreach (var z in p.Zones)
            {
                stringBuilder.AppendFormat("   {0}\r\n", z);
                foreach (var g in z.Generators)
                    stringBuilder.AppendFormat("      {0}\r\n", g);
                foreach (var m in z.Modulators)
                    stringBuilder.AppendFormat("      {0}\r\n", m);
            }
        }
        stringBuilder.Append("Instruments\r\n");
        foreach (var i in sf.Instruments)
        {
            stringBuilder.AppendFormat("{0}\r\n", i);
            foreach (var z in i.Zones)
            {
                stringBuilder.AppendFormat("   {0}\r\n", z);
                foreach (var g in z.Generators)
                    stringBuilder.AppendFormat("      {0}\r\n", g);
                foreach (var m in z.Modulators)
                    stringBuilder.AppendFormat("      {0}\r\n", m);
            }
        }
        return stringBuilder.ToString();
    }
}
