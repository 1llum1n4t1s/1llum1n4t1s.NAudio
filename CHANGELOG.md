# Changelog

1llum1n4t1s.NAudio の fork 固有変更を記録します。形式は
[Keep a Changelog](https://keepachangelog.com/ja/1.1.0/)、versioning は
[Semantic Versioning](https://semver.org/lang/ja/) に準拠します。

本家 NAudio の変更履歴は [RELEASE_NOTES.md](RELEASE_NOTES.md) に残し、fork の NuGet package と
GitHub Release にはこの `CHANGELOG.md` を使用します。

## [Unreleased]

## [4.0.2] - 2026-08-28

### Fixed

- `AudioFileReader(Stream)` が WAVEFORMATEXTENSIBLE の PCM / IEEE float WAV を不要な ACM 変換へ渡す問題を修正しました。
- `AudioFileReader` の初期化失敗時に、内部で作成した reader とファイルハンドルを解放しない問題を修正しました。
- `WaveMixerStream32` で入力が Read 中に自身を削除すると列挙例外になり、最長入力の削除後に AutoStop 読み込みが範囲外になる問題を修正しました。
- `WaveMixerStream32` の最初の入力を並行追加した際に、異なる形式を同時に受け入れる競合を修正しました。
- `WaveFormat.MarshalFromPtr` が宣言された拡張データ長を越えて native memory を読み取る問題を修正しました。
- `WaveFormatConversionStream` が 2 GiB を超える音源の長さとシーク位置を 32 bit に切り詰める問題を修正しました。

## [4.0.1] - 2026-08-26

### Changed

- 全shipping package、sample、test、CIの最小target frameworkを.NET 10へ統一しました。

### Fixed

- `WasapiRecorder` の開始処理中に停止・破棄すると、capture threadが停止要求を
  `Capturing` で上書きして`Dispose` / `DisposeAsync`が完了しない競合を修正しました。
- `WaveIn` と旧 `WasapiCapture` の開始直後に停止・破棄すると停止要求が上書きされる競合を修正しました。
- `WasapiRecorder.CaptureAsync` の実行中に破棄するとWASAPI資源が早期解放される競合を修正しました。
- 32,767 byteを超えるWAV `fmt`拡張データで後続chunkを正しく読めない問題を修正しました。

## [4.0.0] - 2026-08-26

### Added

- NAudio 3 の source-generated COM interop 上に、1.x 互換の
  `WasapiCapture.CreateForProcessCaptureAsync(int, bool)` を再実装しました。
- `WasapiCapture.CreateForProcessCaptureAsync`、`AudioClient` の非同期 activation、
  `WasapiRecorderBuilder.BuildAsync`、`WasapiPlayerBuilder.BuildAsync` に
  `CancellationToken` overload を追加しました。
- Process Loopback mode、HRESULT fallback、キャンセル、無効 PID を検証する単体テストを追加しました。
- Native AOT smoke test に Process Loopback の activation と録音開始・停止を追加しました。
- 旧 fork の `WasapiCapture.CapturePacketReceived`、`TotalPacketCount`、`SilentPacketCount` と
  `WasapiCapturePacketEventArgs` を再実装しました。
- Native AOT smoke test に、日本語ファイル名を含むM4Aのencode・file decode・WAV出力を追加しました。
- `SoundFont(Stream, bool leaveOpen)` を追加し、呼び出し元が stream ownership を選べるようにしました。

### Changed

- fork を upstream NAudio `main` の commit
  `97cb1c886d762c6985c2a58335d0d62790f1b0ff`（3.0.2 development line）上へ再構築しました。
- 単一 package 構成を NAudio 3 の13 package 構成へ移行し、NuGet ID を
  `1llum1n4t1s.NAudio.*` に統一しました。assembly 名と `NAudio.*` namespace は維持します。
- 最小 TFM を .NET 9 とし、core / MIDI / sampler / file package を cross-platform 化した
  upstream 3 の構成を採用しました。
- Process Loopback の推奨 API を
  `WasapiRecorderBuilder.WithProcessLoopback(...).BuildAsync(...)` に変更しました。
- Process Loopback activation は STA thread と `SynchronizationContext` に依存しない
  source-generated COM 経路へ変更しました。
- release workflow の NuGet 認証を fork の `NUGET_API_KEY` repository secret に変更し、
  公式 NAudio package ID へ公開できない構成にしました。
- README を fork 利用者向けに全面刷新し、変更履歴の正本を `CHANGELOG.md` へ移しました。
- NuGet の直接依存を最新安定版へ更新し、`System.Numerics.Tensors` と
  `System.ComponentModel.Composition` を10系へ移行しました。
- GitHub Actions を Node.js 24 対応の `checkout` v7、`setup-dotnet` v6、
  `upload-artifact` v7、`upload-pages-artifact` v5、`deploy-pages` v5へ更新しました。
- NuGet と GitHub Actions を毎週監視する Dependabot 設定を追加しました。

### Fixed

- capture callback 内から `WasapiCapture` / `WasapiRecorder` を破棄したとき、worker thread が
  自分自身を待機する deadlock と、WASAPI buffer 解放前の native resource 破棄を防止しました。
- Standard MIDI File の SysEx event を VLQ length に従って読み書きし、`F7` continuation と
  終端されていない packet を保持できるようにしました。
- 128 bytes 未満の短い有効な MP3 で ID3v1 tag 探索が負の位置へ seek する問題を修正しました。
- filename から不正な AIFF を開いたとき、constructor の例外経路で file handle が leak する問題を
  修正しました。
- RF64 の `ds64` chunk の最小長、stream 境界、table 長を検証し、不正な長さによる過大確保を
  防止するとともに、宣言された data 長を実際に読み取れる stream 範囲へ制限しました。
- `AudioFileReader` が stream 入力の RF64/WAVE を Media Foundation ではなく
  `WaveFileReader` で開くようにしました。
- `Marshal.GetExceptionForHR(S_FALSE)` が `null` を返したとき activation Task が完了せず
  hang する問題を、常に `COMException` を返す共通変換で修正しました。
- activation のキャンセルと native callback が競合したとき、呼び出し元へ渡らない COM object が
  leak する問題を修正しました。
- activation 初期化または `AudioClient` constructor が失敗したときの COM ownership を明確化し、
  二重解放と解放漏れを防止しました。
- `ProcessLoopbackMode` の native 値を Windows SDK と同じ
  `IncludeTargetProcessTree = 0` / `ExcludeTargetProcessTree = 1` として回帰テストしました。
- `WasapiRecorder.CaptureAsync` が WASAPI buffer を `yield` 中も保持し、silent packet を欠落させる
  問題を修正しました。
- WASAPI 初期化失敗後に capture state が `Starting` のまま残る問題を修正しました。
- `WaveOut` / `WaveIn` の破棄・再初期化時に worker thread の終了を待たず、buffer や native handle と
  競合する問題を修正しました。
- WASAPI の停止通知ハンドラー内から `Dispose` したとき、capture / playback worker が自分自身を
  `Join` して deadlock する問題を修正しました。
- track 冒頭の不正な MIDI running status を、`NullReferenceException` ではなく `FormatException` として
  報告するようにしました。
- `WaveFileWriter.WriteSample` / `WriteSamples` でも通常 RIFF の 4 GiB 上限を一貫して検査するようにしました。
- `MixingSampleProvider` が形式不一致や `null` の入力を内部リストへ残す問題を修正しました。
  入力の `Read` と `MixerInputEnded` は source list の lock 外で実行し、callback からの自己削除による
  リスト破損と、別 thread からの入力追加が外部 `Read` の完了待ちになる問題も防止しました。
- AIFF / AIFC の短すぎる `COMM` chunk を後続 chunk まで読み越す問題と、奇数長 `SSND` の
  pad byte を音声データとして数える問題を修正しました。
- `WaveFileReader` / `AiffFileReader` が負の `Position` を受け入れ、container header を音声として
  読める状態になる問題を修正しました。
- 17 bytes の WAV `fmt` chunk で `cbSize` を後続 chunk から読み越す問題を修正しました。
- SoundFont の必須 INFO / sample data / preset data chunk が欠落したとき、
  `NullReferenceException` ではなく `InvalidDataException` で原因を報告するようにしました。
- SoundFont の必須 `pdta` chunk、zone 範囲、instrument / sample 参照を読み込み時に検証し、
  壊れた SF2 を `IndexOutOfRangeException`、`OverflowException`、過大確保ではなく
  `InvalidDataException` として拒否するようにしました。

### Compatibility

- 1.x 互換 factory は 48 kHz / 16-bit / stereo format を維持します。
- 3.x は package / assembly 分割と upstream API modernization を含むため、1.x からの major migration です。
- 旧 fork の全 commit を機械的には移植せず、upstream 3 で置換済みかを確認して必要な差分だけを
  再実装しています。

### Validation

- Release solution build: warning 0 / error 0。
- 非 Integration test: 2,098 passed / 30 skipped / 0 failed。
- Grok audit 対象 test: 80 passed / 1 skipped / 0 failed。
- `win-x64` Native AOT publish と native EXE 実行に成功しました。
- Native AOT 上で Process Loopback、Core Audio callback、Media FoundationのM4A file decode、
  DirectSound を確認しました。
- 13個の NuGet package を pack し、内部依存がすべて `1llum1n4t1s.NAudio.*` を参照することを確認しました。

## Legacy 1.x history

1.0.49 以前の詳細な履歴は、移行前の
[RELEASE_NOTES.md](https://github.com/1llum1n4t1s/1llum1n4t1s.NAudio/blob/71007e4fd85d2de6cccb3ededed9a02871c889b4/RELEASE_NOTES.md)
を参照してください。

[Unreleased]: https://github.com/1llum1n4t1s/1llum1n4t1s.NAudio/compare/v4.0.2...HEAD
[4.0.2]: https://github.com/1llum1n4t1s/1llum1n4t1s.NAudio/compare/v4.0.1...v4.0.2
[4.0.1]: https://github.com/1llum1n4t1s/1llum1n4t1s.NAudio/compare/v4.0.0...v4.0.1
[4.0.0]: https://github.com/1llum1n4t1s/1llum1n4t1s.NAudio/compare/71007e4fd85d2de6cccb3ededed9a02871c889b4...v4.0.0
