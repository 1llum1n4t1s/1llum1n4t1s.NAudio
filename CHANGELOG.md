# Changelog

1llum1n4t1s.NAudio の fork 固有変更を記録します。形式は
[Keep a Changelog](https://keepachangelog.com/ja/1.1.0/)、versioning は
[Semantic Versioning](https://semver.org/lang/ja/) に準拠します。

本家 NAudio の変更履歴は [RELEASE_NOTES.md](RELEASE_NOTES.md) に残し、fork の NuGet package と
GitHub Release にはこの `CHANGELOG.md` を使用します。

## [Unreleased]

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

### Fixed

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

### Compatibility

- 1.x 互換 factory は 48 kHz / 16-bit / stereo format を維持します。
- 3.x は package / assembly 分割と upstream API modernization を含むため、1.x からの major migration です。
- 旧 fork の全 commit を機械的には移植せず、upstream 3 で置換済みかを確認して必要な差分だけを
  再実装しています。

### Validation

- Release solution build: warning 0 / error 0。
- 非 Integration test: 2,056 passed / 30 skipped / 0 failed。
- Grok audit 回帰 test: 12 passed / 0 failed。
- `win-x64` Native AOT publish と native EXE 実行に成功しました。
- Native AOT 上で Process Loopback、Core Audio callback、Media FoundationのM4A file decode、
  DirectSound を確認しました。
- 13個の NuGet package を pack し、内部依存がすべて `1llum1n4t1s.NAudio.*` を参照することを確認しました。

## Legacy 1.x history

1.0.49 以前の詳細な履歴は、移行前の
[RELEASE_NOTES.md](https://github.com/1llum1n4t1s/1llum1n4t1s.NAudio/blob/71007e4fd85d2de6cccb3ededed9a02871c889b4/RELEASE_NOTES.md)
を参照してください。

[Unreleased]: https://github.com/1llum1n4t1s/1llum1n4t1s.NAudio/compare/71007e4fd85d2de6cccb3ededed9a02871c889b4...HEAD
