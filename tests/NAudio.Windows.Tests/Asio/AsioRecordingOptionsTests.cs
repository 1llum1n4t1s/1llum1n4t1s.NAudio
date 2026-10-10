using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Windows.Tests.Asio;

[TestFixture]
[Category("UnitTest")]
public class AsioRecordingOptionsTests
{
    [Test]
    public void SampleRate_DefaultsToNull()
    {
        var options = new AsioRecordingOptions();
        Assert.That(options.SampleRate, Is.Null, "SampleRate == null signals 'use driver's current rate'.");
    }

    [Test]
    public void BufferSize_DefaultsToNull()
    {
        var options = new AsioRecordingOptions();
        Assert.That(options.BufferSize, Is.Null, "BufferSize == null signals 'use driver's preferred size'.");
    }

}
