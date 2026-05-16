# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

1llum1n4t1s.NAudio は [NAudio](https://github.com/naudio/NAudio) のフォーク。.NET 10 対応、プロセスループバックキャプチャ (Process Loopback Capture) の追加、多数のバグ修正を含む。Windows x64 専用のオーディオライブラリ。NuGet パッケージ ID は `1llum1n4t1s.NAudio`。

詳細なリリース履歴は `RELEASE_NOTES.md` を参照 (Keep a Changelog 形式)。

## ビルド・テストコマンド

```bash
# ビルド (Debug)
rtk dotnet build NAudio/NAudio.csproj

# ビルド (Release、NuGet パッケージ生成含む)
rtk dotnet build NAudio/NAudio.csproj -c Release

# テスト全体 (Integration 含む、実機デバイス必要)
rtk dotnet test Tests/NAudioTests.csproj

# 単体テストのみ (CI / 通常の動作確認はこちら)
rtk dotnet test Tests/NAudioTests.csproj --filter "TestCategory!=IntegrationTest"

# 特定テストクラス実行 (例: ActivateAudioInterfaceCompletionHandlerTests)
rtk dotnet test Tests/NAudioTests.csproj --filter "FullyQualifiedName~ActivateAudioInterfaceCompletionHandlerTests"
```

`IntegrationTest` カテゴリのテストは以下の環境変数でテストデータを指定:
- `NAUDIO_TEST_WAV` (単一 WAV) / `NAUDIO_TEST_WAV_DIR` (フォルダ)
- `NAUDIO_TEST_MP3` (単一 MP3) / `NAUDIO_TEST_MP3_DIR` (フォルダ)
- `NAUDIO_TEST_AAC` (単一 AAC)
- `NAUDIO_TEST_AIFF_DIR` (AIFF フォルダ)
- `NAUDIO_TEST_GSM610_WAV` (GSM610 WAV 入力)

未設定時は `ClassicAssert.Ignore` で skip される。

## ソリューション構成

- `NAudio.slnx` — Solution (XML 形式)
- `NAudio/NAudio.csproj` — メインライブラリ (NuGet `1llum1n4t1s.NAudio`)
- `Tests/NAudioTests.csproj` — テスト (NUnit 4.6 + Moq 4.20、`InternalsVisibleTo("NAudioTests")` 付与済)

> WPF サンプルアプリ (旧 AudioFileInspector / MidiFileConverter / MixDiff) は議題 1 採用で退役済。Process Loopback の動作確認用 UI は `Tests/Wasapi/ProcessLoopbackCaptureTestWindow.xaml(.cs)` を参照。

## ビルド環境

- **TFM**: `net10.0-windows10.0.20348.0` (x64 only)
- **MSBuild SDK**: `MSBuild.Sdk.Extras 3.0.44` (`global.json` で固定)
- **.NET SDK**: `10.0.100` + `rollForward: latestFeature` (`global.json` で固定)
- **AllowUnsafeBlocks**: 有効 (DSP / WaveBuffer 等)
- **UseWPF**: 有効 (`NAudio.Wpf.Gui.*` の WPF コントロール群のため、外せない)
- **バージョン**: `Directory.Build.props` の `<Version>` で一元管理。バージョン変更時は README の PackageReference / RELEASE_NOTES.md も同期する

## アーキテクチャ

### コアインターフェース (Provider / Stream パターン)

オーディオ処理のチェインを構築するための Provider / Stream パターンが中心設計:

- **`IWaveProvider`** (`Core/Wave/WaveOutputs/IWaveProvider.cs`) — byte[] ベース
- **`ISampleProvider`** (`Core/Wave/WaveOutputs/IWaveProviderFloat.cs`) — float[] ベース
- **`IWavePlayer`** (`Core/Wave/WaveOutputs/IWavePlayer.cs`) — 再生デバイス抽象 (`WasapiOut` / `WaveOutEvent` / `AsioOut` / `DirectSoundOut`)
- **`IWaveIn`** (`Core/Wave/WaveInputs/IWaveIn.cs`) — 録音デバイス抽象 (`WasapiCapture` / `WaveInEvent`)

典型的なチェイン: `AudioFileReader` → `SampleProvider` (加工) → `IWavePlayer` (出力)

### COM Lifecycle 統一方針 (重要)

WASAPI / MediaFoundation / DMO の COM ラッパークラスは、以下の方針で統一されている。新規 COM ラッパーを追加する場合も同じパターンを適用すること。

1. **`IDisposable` 実装**: `Dispose()` で `Marshal.ReleaseComObject` を呼ぶ
2. **`disposed` フラグ**: 冒頭で早期 return、二重 Dispose を防ぐ
3. **`GC.SuppressFinalize` は `if (!disposed)` の中で呼ぶ**
4. **ファイナライザは COM オブジェクトに触らない**: `Debug.WriteLine` で警告のみ
   - GC スレッド (MTA) から STA バインド COM を Release すると `RPC_E_WRONG_THREAD` や AccessViolation
   - CLR の RCW 内部 finalizer に最終解放を委ねる
5. **親が子 COM ラッパーを保持する場合**: 親の `Dispose(disposing=true)` で子も Dispose、`disposing=false` (finalizer 経由) では子に触らない

この方針が適用されているクラス:
- `MediaType` / `PropertyStore` / `MMDevice` / `AudioMeterInformation` / `DeviceTopology`
- `AudioSessionControl` / `AudioClient` / `Part`
- `AudioRenderClient` / `AudioCaptureClient` / `AudioClockClient` / `AudioStreamVolume` (警告 finalizer のみ追加)

### Process Loopback Capture (フォーク独自機能)

`WasapiCapture.CreateForProcessCaptureAsync(processId, includeProcessTree)` で特定プロセスの音声をキャプチャ。

**絶対に守る制約** (詳細は `Docs/ProcessLoopbackCapture.md`):
- UI スレッド (STA) + 非 null の `SynchronizationContext` で `await` する
- `ConfigureAwait(false)` を**付けてはいけない**
- `StartRecording()` は `await` 継続と同じスレッドから呼ぶ
- 違反すると `E_NOINTERFACE` / 無音 / `-1/0/1` のプレースホルダー値だけが返る症状になる

CI でリグレッション検知するため `Tests/Wasapi/ActivateAudioInterfaceCompletionHandlerTests.cs` で Mock test を回している (F-004 リグレッション保護: `Marshal.GetExceptionForHR(S_FALSE)` が null を返す前提、COMException フォールバックの動作確認)。

### ディレクトリ構成 (NAudio/ 配下)

| パス | 内容 |
|------|------|
| `Core/Wave/SampleProviders/` | ミキシング / フェード / リサンプリング等の SampleProvider |
| `Core/Wave/WaveStreams/` | WaveFileReader / Mp3FileReaderBase / Mp3FileReader 等 |
| `Core/Wave/WaveFormats/` | WaveFormat 定義群 |
| `Core/Dsp/` | FFT / BiQuad / SmbPitchShifter / WdlResampler 等 |
| `Core/Codecs/` | A-law / Mu-law / G.722 |
| `Core/FileFormats/` | MP3 / SoundFont / Wav (RIFF chunk) |
| `Midi/` | MIDI ファイル I/O、イベント管理 |
| `Wasapi/` | WASAPI、ProcessLoopback (フォーク独自) |
| `Wasapi/CoreAudioApi/` | Core Audio API + ActivateAudioInterfaceCompletionHandler |
| `Wasapi/MediaFoundation/` | Media Foundation エンコード / デコード |
| `Wasapi/Dmo/` | DirectX Media Objects (DMO エフェクト) |
| `Asio/` | ASIO ドライバーサポート (低遅延 / プロオーディオ用途) |
| `WinMM/` | レガシー Windows Multimedia API (WaveOut / WaveIn / ACM / Mixer) |
| `Wpf/` | WPF GUI コンポーネント (`NAudio.Gui.*`) |
| `Extras/` | AudioPlaybackEngine / Equalizer / SampleAggregator 等のユーティリティ |

## テスト構成

- 場所: `Tests/` 配下、サブフォルダで分類
- フレームワーク: NUnit 4.6 + Moq 4.20
- カテゴリ: `[Category("IntegrationTest")]` でオーディオデバイス / 環境依存ファイルを要するテストを分離 (CI 除外)
- `Tests/App.xaml` は **WPF Application のエントリーポイント**で、Process Loopback の手動 UI テスト用。これがあるため `Tests/NAudioTests.csproj` は `<OutputType>WinExe</OutputType>` が必須 (Library 化すると `MC1002: Library project file cannot specify ApplicationDefinition element` でビルド失敗する)

## コーディング規約

- コメント・コミットメッセージは日本語
- 公開 API の変更は既存利用者への影響を必ず考慮 (NuGet 公開済、`drop-in replacement` を謳う方針)
- 新機能は Provider / Stream パターンに従って実装
- ドキュメント XML 生成有効 (`GenerateDocumentationFile=true`) — public メンバーには XML コメント必須

## CI/CD (`.github/workflows/publish.yml`)

- トリガー: `release/**` ブランチへの push + `workflow_dispatch`
- 主要ステップ: checkout → setup-dotnet (10.0.x) → cache NuGet → `dotnet test --filter "TestCategory!=IntegrationTest"` → `dotnet build -c Release` → `publish.ps1`
- セキュリティ: `permissions: contents: read` 明示 / `timeout-minutes: 30` / `concurrency` でジョブ並走防止
- NuGet 公開には `NUGET_API_KEY` シークレットが必要

`publish.ps1` は `Directory.Build.props` の `<Version>` を XML 解析で抽出し、`NAudio/bin/x64/Release/1llum1n4t1s.NAudio.<version>.nupkg` をピンポイントで push する (Debug ビルド残骸の誤公開防止)。`$apiKey` が未設定なら `$env:NUGET_API_KEY` をフォールバックで取得する (PowerShell の子スクリプトスコープ問題への対応)。

## 過去の事故と落とし穴

過去の `/rere` レビュー & 修正セッションで判明した、機械的に触ると壊れる箇所:

1. **`Tests/NAudioTests.csproj` の `<OutputType>WinExe</OutputType>` は撤去禁止** — `Tests/App.xaml` が ApplicationDefinition のため、Library 化すると MC1002 / BG1003 でビルド失敗
2. **ASIO は退役しない** — 過去に議題 2 採用で `NAudio/Asio/` を一度削除したが、プロオーディオ層を保護するため復活させた経緯がある (RELEASE_NOTES.md の `[Reverted]` 参照)
3. **MediaType / PropertyStore 等の finalizer に COM 解放を戻さない** — 過去の commit で警告のみ方針に統一済。逆に「ファイナライザで `Marshal.ReleaseComObject` を呼ぶ」コードを見つけても、それは STA-COM 違反 / 二重解放のバグなので元に戻すこと
4. **`Directory.Build.props` の `CleanBinObjBeforeRestore` は復活させない** — MSBuild incremental を破壊する。stale `obj/` が問題なら `dotnet clean` を明示呼出
5. **`GenerateIcon` Target の `Inputs/Outputs` は外さない** — 外すと毎ビルド `powershell.exe` 起動で incremental が効かなくなる
6. **`UseWPF=true` をライブラリから外さない** — `NAudio/Wpf/Gui/` が WPF コントロールを持っており、外すとビルド失敗
7. **「機能を削る」「公開済みを引き下げる」修正はユーザー判断必須** — レビュー時は `business-call` 判定で議題提示のみに留め、勝手に削除しない

## 関連ドキュメント

- `README.md` — ユーザー向け概要 + Breaking Changes セクション
- `RELEASE_NOTES.md` — フォーク独自の変更履歴 (Keep a Changelog 形式)
- `Docs/ProcessLoopbackCapture.md` — Process Loopback の STA 制約と切り分けチェックリスト
- `Docs/OutputDeviceTypes.md` — 各 IWavePlayer (WaveOut / Wasapi / DirectSound / Asio) の使い分け
