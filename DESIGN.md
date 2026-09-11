# 1llum1n4t1s.NAudio design

この文書は、4.x 系の現在の実装から確認できる構造と設計上の不変条件をまとめた正本です。
利用方法は [README.md](README.md)、エージェント向け作業規約と検証コマンドは
[AGENTS.md](AGENTS.md)、設計判断の詳細な経緯は [Docs/Architecture/](Docs/Architecture/) を参照してください。

## 目的と境界

1llum1n4t1s.NAudio は upstream NAudio 3 を基点とし、既存の `NAudio.*` namespace と assembly 名を
保ちながら、NuGet IDを `1llum1n4t1s.NAudio.*` に分離したaudio libraryです。managed audio処理、
file I/O、MIDI、samplerと、Windows・Linux・macOSのdevice backendを提供します。fork固有の中心機能は、
Process Loopback Captureと、WASAPI / Media Foundation / DirectSoundを含むNative AOT対応です。

UI applicationやaudio serviceそのものは提供範囲外です。`samples/` は利用例、`tests/` は単体・統合・
Native AOT検証、`docfx/` と `Docs/` は公開documentationを担います。

## 主要コンポーネント

| Package | 責務と境界 |
| --- | --- |
| `NAudio.Core` | provider interfaceと配列互換base class、wave format、managed file reader/writer、DSP、effects、SF2/SFZ parser |
| `NAudio.Midi` | MIDI file/event model。Windows legだけWinRT MIDI I/Oを追加 |
| `NAudio.Wasapi` | WASAPI、Core Audio、Media Foundation、Process Loopback。compile時は`net10.0`、実行時はWindows限定 |
| `NAudio.WinMM` | WaveOut/WaveIn、ACM、mixer、legacy MIDI |
| `NAudio.Dmo` | DMO、DirectSound、Windows codec/resampler |
| `NAudio.Asio` | ASIO playback/capture |
| `NAudio.WinForms` | WinForms controlとwindow callback。`NAudio.WinMM`に依存 |
| `NAudio.Vst3` | VST 3 host。CoreとMIDIに依存 |
| `NAudio.Alsa` | Linux ALSA playback/capture |
| `NAudio.SoundFile` | system libsndfileを使うcross-platform file I/O |
| `NAudio.MacOS` | Core Audio playback/captureとAudio Toolbox file I/O。API安定化まではpreviewのみ |
| `NAudio.Sampler` | SF2/SFZ/single-sampleのsoftware sampler。Core、MIDI、SoundFileに依存 |
| `NAudio` | CoreとMIDIを常に集約し、Windows TFMではWindows backendも集約するmeta-package |
| `NAudio.Extras` | playback engineなどの補助API。Windows legだけWASAPI機能を含む |

依存関係の基点は `NAudio.Core` です。platform package同士を横断する共通assemblyは増やさず、
OS固有interopを各backendへ閉じ込めています。meta-packageは利便性のための集約であり、Native AOTや
配布sizeを重視するconsumerは必要なbackend packageだけを直接参照します。

## データフロー

1. **Playback**: file/stream readerが`IWaveProvider`または`ISampleProvider`を生成し、converter、effect、
   resampler、`MixingSampleProvider`を必要に応じて接続して、`IWavePlayer`実装へ渡します。
2. **Capture**: device backendがnative bufferを取得し、callbackまたはasync APIでmanaged側へ渡します。
   consumerはcallback中にbufferを消費するか、writer/encoderへ転送します。
3. **Process Loopback**: `WasapiRecorderBuilder.WithProcessLoopback`または互換factoryがPIDとmodeを
   activation parametersへ格納し、`ActivateAudioInterfaceAsync`でvirtual endpointの`AudioClient`を
   作成します。成功後は通常のWASAPI capture loopへ合流します。
4. **Sampler**: SF2/SFZ parserがinstrument/regionを構築し、MIDI eventとsample dataをsampler engineへ
   入力します。出力は`ISampleProvider`として通常のmix/playback chainへ接続されます。
5. **Release**: root `VersionPrefix`とCHANGELOGを同期し、`v*` tagでGitHub Actionsを起動します。
   workflowは14 packageをbuild/test/packし、NuGetへpushした後にGitHub Releaseを作成します。

## 重要な不変条件

- 14個のshipping packageはroot [Directory.Build.props](Directory.Build.props) の`VersionPrefix`を共有します。
  `NAudio.MacOS`だけはAPI安定化まで常にpreview suffixを付け、sample/tool appは独立versionを持ちます。
- `NAudio.Core`は`net10.0`のcross-platform基盤です。OS固有device APIをCoreへ持ち込みません。
- `NAudio`と`NAudio.Extras`の`net10.0` / `net10.0-windows` /
  `net10.0-windows10.0.19041.0`の3 legを維持します。plain Windows TFM consumerをportable assetへ
  誤fallbackさせず、versioned legではWinRT MIDIを提供するためです。
- `NAudio.Wasapi`はsource-generated COMとP/Invokeでcompileできるため`net10.0`を使用し、
  `[SupportedOSPlatform("windows")]`でruntime境界を表します。
- `NAudio.MacOS`は`net10.0`でcompileし、`[SupportedOSPlatform("macos")]`でruntime境界を表します。
  stable meta-packageには含めず、preview packageを明示参照します。
- Process LoopbackはWindows 10 build 19041以降がruntime要件です。include/exclude modeのnative値、
  cancellation、HRESULT変換とCOM ownershipをmanaged/native境界で変えません。
- providerの`Read`はcaller supplied bufferへ書き込み、返したcountだけを有効とします。
  `WaveProviderBase` / `SampleProviderBase`は`Span<T>`をsource上で表現できない言語向けの配列bridgeであり、
  direct interface実装より1回多いcopyを許容します。
  mixerは外部providerの`Read`やevent callback中にsource list lockを保持しません。
- caller supplied streamのownershipはconstructorの`leaveOpen`契約に従います。COM/native resourceは
  明示的な`Dispose` / `IAsyncDisposable`経路で解放します。
- hardwareやnative libraryが必要なtestは`IntegrationTest`または環境依存skipとして通常CIから分離し、
  非Integration testがheadless CIの合否を決めます。

## 採用済み設計判断

- **package分割**: cross-platform consumerがWindows依存を引かないことを優先しました。代わりに、
  旧単一packageからの移行では参照packageを選び直す必要があります。
- **namespace/assembly名維持とNuGet ID分離**: source互換性を保ちつつ、upstream公式packageへの誤公開と
  dependency混同を防ぎます。
- **source-generated COM**: trimmingとNative AOTでreflection-based COM metadataが失われる問題を避けます。
  代わりにpointer ownership、CCW/RCWの向き、release回数を各interop境界で明示します。
- **Span APIと配列互換base classの併存**: C#の通常経路はbridge copyのない`Span<T>` interfaceを維持し、
  VB.NETやNAudio 2由来のproviderにはpooled arrayを使うbridgeを提供します。言語互換性と移行容易性の代わりに、
  bridge経路ではreadごとのcopyを受け入れます。
- **tag駆動のfinal release**: versionとsource SHAを一意にし、同じCHANGELOG本文をNuGetとGitHub Releaseで
  共有します。previewだけは`workflow_dispatch`でsuffixを付与します。
- **実device smokeをCIから分離**: headless runnerではaudio endpointとcallbackを保証できないため、CIは
  build/analyzerと非Integration testを正本とし、Native AOT executableのend-to-end実行は実機で行います。
