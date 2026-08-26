# 1llum1n4t1s.NAudio

[![Nuget](https://img.shields.io/nuget/v/1llum1n4t1s.NAudio)](https://www.nuget.org/packages/1llum1n4t1s.NAudio/)

1llum1n4t1s.NAudio is a fork of the open source .NET audio library written by [Mark Heath](https://markheath.net) and contributors. It keeps the upstream assembly names and `NAudio.*` namespaces while publishing every package under the `1llum1n4t1s.NAudio.*` ID family.

This is the main 1llum1n4t1s.NAudio meta-package. Installing it pulls in everything you need to play, record and manipulate audio on .NET, and adds `AudioFileReader` — the one-line "just open this audio file" reader that picks the right decoder for you — along with `Mp3FileReader`.

Requires `net10.0` or later. If you need .NET Framework or .NET Standard 2.0, use NAudio 2.x.

## What it pulls in

Always, on every target framework:

- **NAudio.Core** — the format-independent core: `WaveStream`, `IWaveProvider` / `ISampleProvider`, WAV and AIFF readers and writers, mixing, resampling, DSP, and the `NAudio.Effects` framework
- **NAudio.Midi** — the MIDI event model and Standard MIDI File reading and writing

Additionally, on a Windows target framework:

- **NAudio.Wasapi** — WASAPI playback, capture and loopback (`WasapiPlayer` / `WasapiRecorder`), plus Media Foundation codecs
- **NAudio.WinMM** — `WaveOut` / `WaveIn`, classic MIDI I/O, ACM codecs and mixer controls via `winmm.dll`
- **NAudio.Asio** — low-latency multichannel playback and capture through ASIO drivers
- **NAudio.Dmo** — DMO effects, the DMO MP3 decoder and resampler, and `DirectSoundOut`
- **NAudio.WinForms** — Windows Forms controls, and the window-callback `WaveOutWindow` / `WaveInWindow`

On a non-Windows target framework only the cross-platform pieces are referenced. If you only need a subset, reference the individual packages directly rather than this meta-package.

## Optional packages

These are not pulled in by the meta-package — add them explicitly if you want them:

| Package | Platform | What it gives you |
| --- | --- | --- |
| [NAudio.Sampler](https://www.nuget.org/packages/1llum1n4t1s.NAudio.Sampler/) | cross-platform | Polyphonic software sampler — SoundFont (`.sf2`), SFZ and single-sample instruments |
| [NAudio.SoundFile](https://www.nuget.org/packages/1llum1n4t1s.NAudio.SoundFile/) | cross-platform | Read *and write* WAV/AIFF/FLAC/Ogg-Vorbis/Opus/MP3 via libsndfile |
| [NAudio.Alsa](https://www.nuget.org/packages/1llum1n4t1s.NAudio.Alsa/) | Linux | `AlsaOut` / `AlsaIn` playback and capture via libasound |
| [NAudio.Vst3](https://www.nuget.org/packages/1llum1n4t1s.NAudio.Vst3/) | Windows | VST 3 plug-in hosting (preview) — effects and instruments |
| [NAudio.Extras](https://www.nuget.org/packages/1llum1n4t1s.NAudio.Extras/) | cross-platform (+ Windows extras) | Opinionated helpers — playback engine, capture mixing, ID3 tags |

## Getting started

```csharp
using NAudio.Wave;

using var audioFile = new AudioFileReader("myfile.mp3");
using var player = new WasapiPlayerBuilder().Build();
player.Init(audioFile);
player.Play();
while (player.PlaybackState == PlaybackState.Playing)
{
    Thread.Sleep(500);
}
```

- **[Upstream documentation site](https://naudio.github.io/NAudio/)** — tutorials and the full API reference
- **[Fork repository](https://github.com/1llum1n4t1s/1llum1n4t1s.NAudio)** — source, issues, and the demo applications (`NAudioDemo`, `NAudioWpfDemo`)
- **[Fork changelog](https://github.com/1llum1n4t1s/1llum1n4t1s.NAudio/blob/main/CHANGELOG.md)** — fork-specific fixes, compatibility notes, and release history
- **[Migrating from NAudio 2 to NAudio 3](https://github.com/1llum1n4t1s/1llum1n4t1s.NAudio/blob/main/Docs/MigratingFromNAudio2.md)** — every breaking change, with before/after code

## License

MIT. See the [fork repository](https://github.com/1llum1n4t1s/1llum1n4t1s.NAudio) for source and issues, and the [upstream project](https://github.com/naudio/NAudio) for the original project and contributors.
