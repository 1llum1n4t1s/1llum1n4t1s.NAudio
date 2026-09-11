using NAudio.CoreAudioApi;
using NAudio.Wave;
using NUnit.Framework;
using System;

namespace NAudio.Windows.Tests.Wasapi;

[TestFixture]
public class WasapiPublicApiCompatibilityTests
{
    [TestCase(typeof(AudioClient), typeof(AudioClientShareMode), typeof(WaveFormat))]
    [TestCase(typeof(WasapiPlayer), typeof(WaveFormat), null)]
    public void IsFormatSupportedKeepsWaveFormatExtensibleOverload(
        Type ownerType,
        Type firstParameterType,
        Type secondParameterType)
    {
        var parameterTypes = secondParameterType == null
            ? new[] { firstParameterType, typeof(WaveFormatExtensible).MakeByRefType() }
            : new[] { firstParameterType, secondParameterType, typeof(WaveFormatExtensible).MakeByRefType() };

        Assert.That(ownerType.GetMethod("IsFormatSupported", parameterTypes), Is.Not.Null);
    }

    [TestCase(typeof(AudioClient), typeof(AudioClientShareMode), typeof(WaveFormat))]
    [TestCase(typeof(WasapiPlayer), typeof(WaveFormat), null)]
    public void ClosestMatchApiCanReturnActualWaveFormatType(
        Type ownerType,
        Type firstParameterType,
        Type secondParameterType)
    {
        var parameterTypes = secondParameterType == null
            ? new[] { firstParameterType, typeof(WaveFormat).MakeByRefType() }
            : new[] { firstParameterType, secondParameterType, typeof(WaveFormat).MakeByRefType() };

        Assert.That(ownerType.GetMethod("IsFormatSupportedWithClosestMatch", parameterTypes), Is.Not.Null);
    }
}
