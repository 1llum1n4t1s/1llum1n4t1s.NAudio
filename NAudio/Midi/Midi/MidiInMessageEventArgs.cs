using System;

namespace NAudio.Midi
{
    /// <summary>
    /// MIDI In Message Information
    /// </summary>
    public class MidiInMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Create a new MIDI In Message EventArgs
        /// </summary>
        /// <param name="message"></param>
        /// <param name="timestamp"></param>
        public MidiInMessageEventArgs(int message, int timestamp)
        {
            this.RawMessage = message;
            this.Timestamp = timestamp;
            try
            {
                this.MidiEvent = MidiEvent.FromRawMessage(message);
            }
            catch (FormatException)
            {
                // MidiEvent.FromRawMessage throws FormatException for unsupported MIDI command codes
            }
            catch (ArgumentOutOfRangeException)
            {
                // MIDI event constructors may reject out-of-range data values
            }
        }

        /// <summary>
        /// The Raw message received from the MIDI In API
        /// </summary>
        public int RawMessage { get; private set; }

        /// <summary>
        /// The raw message interpreted as a MidiEvent
        /// </summary>
        public MidiEvent MidiEvent { get; private set; }

        /// <summary>
        /// The timestamp in milliseconds for this message
        /// </summary>
        public int Timestamp { get; private set; }
    }
}