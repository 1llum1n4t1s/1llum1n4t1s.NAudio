# 1llum1n4t1s.NAudio

[![Build](https://github.com/1llum1n4t1s/1llum1n4t1s.NAudio/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/1llum1n4t1s/1llum1n4t1s.NAudio/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/1llum1n4t1s.NAudio)](https://www.nuget.org/packages/1llum1n4t1s.NAudio/)
[![License](https://img.shields.io/github/license/1llum1n4t1s/1llum1n4t1s.NAudio)](LICENSE)

1llum1n4t1s.NAudio は、[NAudio](https://github.com/naudio/NAudio) をベースに
Process Loopback Capture と Native AOT 対応を強化したフォークです。assembly 名と
`NAudio.*` namespace は本家との互換性を維持し、NuGet package ID だけを
`1llum1n4t1s.NAudio.*` に分離しています。

> [!IMPORTANT]
> 現在の stable 版は 4.x 系です。旧 1.x 系とは対象 framework・package 構成・
> Process Loopback の推奨 API が異なるため、更新時は「1.x からの移行」を確認してください。

## 特徴

- NAudio 3 の分割 package、cross-platform core、Span ベース API を採用
- 特定プロセスまたはプロセスツリーだけを録音する Process Loopback Capture
- WASAPI / Media Foundation の source-generated COM interop と Native AOT 対応
- WASAPI 非同期 activation のキャンセル、HRESULT 変換、COM lifetime の堅牢化
- WASAPI、WaveOut / WaveIn、ASIO、DirectSound、MIDI、DSP、各種 audio file をサポート

## 動作要件

| 用途 | 要件 |
| --- | --- |
| 共通 API / file・DSP・MIDI | .NET 10 以降 |
| WASAPI / Media Foundation | Windows |
| Process Loopback Capture | Windows 10 version 2004（build 19041）以降 |
| このリポジトリでの Native AOT 検証 | Windows x64 / `win-x64` |

Linux では `1llum1n4t1s.NAudio.Alsa`、cross-platform file I/O では
`1llum1n4t1s.NAudio.SoundFile` を利用できます。macOS 向けの出力 backend はありません。

## インストール

通常の Windows アプリでは、Windows backend 一式を参照する meta-package が簡単です。

```powershell
dotnet add package 1llum1n4t1s.NAudio --version 4.0.2
```

```xml
<PackageReference Include="1llum1n4t1s.NAudio" Version="4.0.2" />
```

Native AOT アプリでは必要な package だけを参照してください。Process Loopback と
Media Foundation が目的なら `1llum1n4t1s.NAudio.Wasapi` が `NAudio.Core` を推移参照します。

```xml
<PackageReference Include="1llum1n4t1s.NAudio.Wasapi" Version="4.0.2" />
```

## 再生

```csharp
using NAudio.Wave;

using var audioFile = new AudioFileReader("music.mp3");
using var player = new WasapiPlayerBuilder().Build();

player.Init(audioFile);
player.Play();

while (player.PlaybackState == PlaybackState.Playing)
{
    await Task.Delay(100);
}
```

## Process Loopback Capture

新規コードでは `WasapiRecorderBuilder` を使用します。対象プロセスと子プロセスを含める場合は
`IncludeTargetProcessTree`、対象プロセスツリー以外を録音する場合は
`ExcludeTargetProcessTree` を指定します。

```csharp
using NAudio.CoreAudioApi;
using NAudio.Wave;

using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

await using var recorder = await new WasapiRecorderBuilder()
    .WithProcessLoopback(
        (uint)targetProcessId,
        ProcessLoopbackMode.IncludeTargetProcessTree)
    .BuildAsync(cancellationSource.Token);

recorder.DataAvailable += (buffer, flags, devicePosition, qpcPosition) =>
{
    // buffer は callback 中だけ有効な ReadOnlySpan<byte>。ここで同期的に消費する。
    ProcessAudio(buffer);
};

recorder.StartRecording();
await Task.Delay(TimeSpan.FromSeconds(10), cancellationSource.Token);
recorder.StopRecording();
```

Process Loopback の virtual device は mix format を公開しません。format を省略した場合、
`WasapiRecorderBuilder` は 44.1 kHz stereo IEEE float を使用します。必要なら
`WithFormat(...)` で明示してください。対象プロセスが音声を render していない間は
`DataAvailable` が発生しません。

### 1.x 互換 API

旧 fork の呼び出し元は移行期間中、次の API を継続利用できます。

```csharp
using var capture = await WasapiCapture.CreateForProcessCaptureAsync(
    targetProcessId,
    includeProcessTree: true,
    cancellationToken);
```

この互換 API は従来と同じ 48 kHz / 16-bit / stereo を使用します。4.x 実装は
source-generated COM を使うため、1.x で必要だった STA UI thread と非 null の
`SynchronizationContext` は不要です。新規コードには `WasapiRecorderBuilder` を推奨します。

旧 fork の診断 API `CapturePacketReceived`、`TotalPacketCount`、`SilentPacketCount` も
`WasapiCapture` に維持しています。Windows が返した `AUDCLNT_BUFFERFLAGS_SILENT` と
パケット数を確認できるため、Process Loopback の無音と後段処理で生じた無音を切り分けられます。

## Native AOT

Native AOT アプリは Windows version を含む TFM と RID を指定します。次は、このリポジトリの
smoke test と同じ最小構成です。

```xml
<PropertyGroup>
  <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
  <PublishAot>true</PublishAot>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <BuiltInComInteropSupport>false</BuiltInComInteropSupport>
</PropertyGroup>
```

```powershell
dotnet publish -c Release -r win-x64
```

`BuiltInComInteropSupport=false` は必須ではありませんが、reflection-based COM interop に
依存せず source-generated COM だけで動作することを検証できます。本リポジトリでは Native AOT
EXE を実行し、Process Loopback の activation と録音開始・停止、Core Audio callback、
Media Foundation encode/decode/resample、日本語パスのM4A file decode、DirectSound playback を
確認しています。`MediaFoundationReader` は Native AOT のメインプロセス内で利用できるため、
M4A decode を非AOT helper processへ分離する必要はありません。

## Package 構成

| NuGet package | 主な内容 | Platform |
| --- | --- | --- |
| `1llum1n4t1s.NAudio` | Windows backend をまとめる meta-package | cross-platform / Windows |
| `1llum1n4t1s.NAudio.Core` | provider、WAV/AIFF、DSP、resampling、effects | cross-platform |
| `1llum1n4t1s.NAudio.Midi` | MIDI file と event、Windows では WinRT MIDI | cross-platform / Windows |
| `1llum1n4t1s.NAudio.Wasapi` | WASAPI、Process Loopback、Media Foundation | Windows |
| `1llum1n4t1s.NAudio.WinMM` | WaveOut / WaveIn、ACM、legacy MIDI | Windows |
| `1llum1n4t1s.NAudio.Asio` | ASIO playback / capture | Windows |
| `1llum1n4t1s.NAudio.Dmo` | DMO、DirectSound、DMO MP3 decoder / resampler | Windows |
| `1llum1n4t1s.NAudio.WinForms` | WinForms control と window callback | Windows |
| `1llum1n4t1s.NAudio.Sampler` | SoundFont / SFZ software sampler | cross-platform |
| `1llum1n4t1s.NAudio.SoundFile` | libsndfile による FLAC / Ogg / Opus / MP3 等 | cross-platform |
| `1llum1n4t1s.NAudio.Alsa` | ALSA playback / capture | Linux |
| `1llum1n4t1s.NAudio.Vst3` | VST 3 host | Windows |
| `1llum1n4t1s.NAudio.Extras` | playback engine などの補助 API | cross-platform / Windows |

Native AOT では meta-package を一律に参照するより、使用する backend package だけを選ぶと
trim/AOT warning と配布サイズを抑えられます。

## 1.x からの移行

4.x は NAudio 3 を基点に再構築した major migration であり、1.x の完全な drop-in replacement
ではありません。

- 単一 assembly から複数 package / assembly へ分割されています
- 最小 TFM は .NET 10 で、core package は Windows 以外でも利用できます
- `IWaveProvider` / `ISampleProvider` など一部 API は Span ベースになっています
- Process Loopback は `WasapiRecorderBuilder.BuildAsync` が推奨経路です
- `includeProcessTree: true` は対象プロセスと子プロセスを含み、`false` は除外します
- 旧 fork の差分は upstream 3 で置換済みかを確認し、必要なものだけを再実装しています

移行前に [NAudio 2 → 3 migration guide](Docs/MigratingFromNAudio2.md) と
[CHANGELOG.md](CHANGELOG.md) を確認し、実アプリの audio device を使う Integration Test を
実施してください。

## トラブルシュート

- Process Loopback で `Build()` を呼ぶと失敗します。非同期 activation のため `BuildAsync()` を使います
- `DataAvailable` が来ない場合は、対象 PID と Process Tree mode、対象が実際に音声を render しているかを確認します
- `CA1416` が出る場合は、Windows 10 build 19041 以降を TFM または実行時 guard で保証します
- AOT warning が meta-package 由来の場合は、`NAudio.Wasapi` など必要な package の直接参照へ絞ります
- system 全体を録音したい場合は Process Loopback ではなく `WithLoopbackCapture()` を使います

## ドキュメント

- [Process Loopback / WasapiRecorder](Docs/WasapiRecorder.md)
- [NAudio 2 から 3 への移行](Docs/MigratingFromNAudio2.md)
- [出力 API の選び方](Docs/OutputDeviceTypes.md)
- [fork の変更履歴](CHANGELOG.md)
- [upstream NAudio documentation](https://naudio.github.io/NAudio/)

## Upstream とライセンス

本プロジェクトは Mark Heath 氏と contributors による
[NAudio](https://github.com/naudio/NAudio) を基にしています。fork 固有の問題は
[このリポジトリの Issues](https://github.com/1llum1n4t1s/1llum1n4t1s.NAudio/issues) へ、
upstream 一般の仕様は本家 documentation も参照してください。

ライセンスは [MIT License](LICENSE) です。
