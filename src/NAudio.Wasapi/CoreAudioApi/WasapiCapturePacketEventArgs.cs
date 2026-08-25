using System;

namespace NAudio.CoreAudioApi;

/// <summary>
/// Provides diagnostic information about one packet returned by a WASAPI capture client.
/// </summary>
public class WasapiCapturePacketEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="WasapiCapturePacketEventArgs"/>.
    /// </summary>
    /// <param name="bufferFlags">Flags returned by <c>IAudioCaptureClient.GetBuffer</c>.</param>
    /// <param name="framesAvailable">The number of audio frames in the packet.</param>
    public WasapiCapturePacketEventArgs(AudioClientBufferFlags bufferFlags, int framesAvailable)
    {
        BufferFlags = bufferFlags;
        FramesAvailable = framesAvailable;
    }

    /// <summary>
    /// Gets the flags returned by <c>IAudioCaptureClient.GetBuffer</c>.
    /// </summary>
    public AudioClientBufferFlags BufferFlags { get; }

    /// <summary>
    /// Gets the number of audio frames in the packet.
    /// </summary>
    public int FramesAvailable { get; }

    /// <summary>
    /// Gets whether Windows marked the packet as silent.
    /// </summary>
    public bool IsSilent => (BufferFlags & AudioClientBufferFlags.Silent) != 0;
}
