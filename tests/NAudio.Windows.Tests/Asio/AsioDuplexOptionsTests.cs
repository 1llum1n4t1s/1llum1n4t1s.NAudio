using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Windows.Tests.Asio;

[TestFixture]
[Category("UnitTest")]
public class AsioDuplexOptionsTests
{
    [Test]
    public void Defaults_AreSentinelNulls()
    {
        var options = new AsioDuplexOptions();
        Assert.Multiple(() =>
        {
            Assert.That(options.InputChannels, Is.Null);
            Assert.That(options.OutputChannels, Is.Null);
            Assert.That(options.SampleRate, Is.Null, "SampleRate == null signals 'use driver's current rate'.");
            Assert.That(options.BufferSize, Is.Null, "BufferSize == null signals 'use driver's preferred size'.");
            Assert.That(options.Processor, Is.Null);
        });
    }

}
