using System;

namespace MarkHeath.MidiUtils
{
    /// <summary>
    /// Progress Event Arguments
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        /// <summary>
        /// New progress event arguments
        /// </summary>
        /// <param name="messageType">The message type</param>
        /// <param name="message">The message</param>
        public ProgressEventArgs(ProgressMessageType messageType, string message)
        {
            Message = message;
            MessageType = messageType;
        }

        /// <summary>
        /// New progress event arguments
        /// </summary>
        /// <param name="messageType">The message type</param>
        /// <param name="message">the message format string</param>
        /// <param name="args">format arguments</param>
        public ProgressEventArgs(ProgressMessageType messageType, string message, params object[] args)
        {
            MessageType = messageType;
            Message = string.Format(message, args);
        }

        /// <summary>
        /// The message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The message type
        /// </summary>
        public ProgressMessageType MessageType { get; }
    }

    /// <summary>
    /// Progress Message Type
    /// </summary>
    public enum ProgressMessageType
    {
        /// <summary>
        /// Trace
        /// </summary>
        Trace,
        /// <summary>
        /// Information
        /// </summary>
        Information,
        /// <summary>
        /// Warning
        /// </summary>
        Warning,
        /// <summary>
        /// Error
        /// </summary>
        Error,
    }
}
