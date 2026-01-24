using System.Text;
using NAudio.SoundFont;
using System.ComponentModel.Composition;

namespace AudioFileInspector
{
    [Export(typeof(IAudioFileInspector))]
    public class SoundFontInspector : IAudioFileInspector
    {
        #region IAudioFileInspector Members

        public string FileExtension
        {
            get { return ".sf2"; }
        }

        public string FileTypeDescription
        {
            get { return "SoundFont File"; }
        }

        public string Describe(string fileName)
        {
			var sf = new SoundFont(fileName);
            var stringBuilder = new StringBuilder();
			stringBuilder.AppendFormat("{0}\r\n",sf.FileInfo);
			stringBuilder.Append("Presets\r\n");
			foreach(var p in sf.Presets)
			{
				stringBuilder.AppendFormat("{0}\r\n",p);
				foreach(var z in p.Zones)
				{
					stringBuilder.AppendFormat("   {0}\r\n",z);
					foreach(var g in z.Generators)
					{
						stringBuilder.AppendFormat("      {0}\r\n",g);
					}
					foreach(var m in z.Modulators)
					{
						stringBuilder.AppendFormat("      {0}\r\n",m);
					}
				}
			}

			stringBuilder.Append("Instruments\r\n");
			foreach(var i in sf.Instruments)
			{
				stringBuilder.AppendFormat("{0}\r\n",i);
				foreach(var z in i.Zones)
				{
					stringBuilder.AppendFormat("   {0}\r\n",z);
					foreach(var g in z.Generators)
					{
						stringBuilder.AppendFormat("      {0}\r\n",g);
					}
					foreach(var m in z.Modulators)
					{
						stringBuilder.AppendFormat("      {0}\r\n",m);
					}
				}
			}
            return stringBuilder.ToString();
        }

        #endregion
    }
}
