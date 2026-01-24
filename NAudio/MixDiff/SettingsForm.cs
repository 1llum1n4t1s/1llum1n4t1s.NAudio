using System;
using System.Windows.Forms;
using NAudio.Wave;
using MarkHeath.AudioUtils.Properties;

namespace MarkHeath.AudioUtils
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            comboBoxOutputDevice.ValueMember = "DeviceNumber";
            comboBoxOutputDevice.DisplayMember = "DeviceName";
            comboBoxOutputDevice.Items.Add(new WaveOutComboItem("(Default)",-1));
            for (var n = 0; n < WaveOut.DeviceCount; n++)
            {
                var waveOutCaps = WaveOut.GetCapabilities(n);
                comboBoxOutputDevice.Items.Add(new WaveOutComboItem(waveOutCaps.ProductName, n));
            }
            var settings = Settings.Default;
            textBoxSkipBackSeconds.Text = settings.SkipBackSeconds.ToString();
            checkBoxUseAllSlots.Checked = settings.UseAllSlots;
            comboBoxOutputDevice.SelectedValue = settings.WaveOutDevice;
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            var settings = Settings.Default;
            
            var skipBackSeconds = 5;
            var parsed = Int32.TryParse(textBoxSkipBackSeconds.Text, out skipBackSeconds);
            if (!parsed || skipBackSeconds <= 0)
            {
                MessageBox.Show("Please enter a valid number of skip back seconds");
                textBoxSkipBackSeconds.Focus();
                return;
            }

            this.Close();
        }
    }

    class WaveOutComboItem
    {
        string deviceName;
        int deviceNumber;

        public WaveOutComboItem(string deviceName, int deviceNumber)
        {
            this.deviceName = deviceName;
            this.deviceNumber = deviceNumber;
        }

        public int DeviceNumber
        {
            get { return deviceNumber; }
        }

        public string DeviceName
        {
            get { return deviceName; }
        }
    }
}