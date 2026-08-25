# CLAUDE.md

This file provides guidance to Claude Code and other coding agents working in this repository.

## プロジェクト概要

1llum1n4t1s.NAudio は [NAudio](https://github.com/naudio/NAudio) のフォーク。.NET 10 対応、プロセスループバックキャプチャ (Process Loopback Capture) の追加、多数のバグ修正を含む。Windows x64 専用のオーディオライブラリ。NuGet パッケージ ID は `1llum1n4t1s.NAudio`。

詳細なリリース履歴は `RELEASE_NOTES.md` を参照 (Keep a Changelog 形式)。

## ビルド・テストコマンド

```bash
# ビルド (Debug)
dotnet build NAudio/NAudio.csproj

# ビルド (Release、NuGet パッケージ生成含む)
dotnet build NAudio/NAudio.csproj -c Release

# テスト全体 (Integration 含む、実機デバイス必要)
dotnet test Tests/NAudioTests.csproj

# 単体テストのみ (CI / 通常の動作確認はこちら)
dotnet test Tests/NAudioTests.csproj --filter "TestCategory!=IntegrationTest"

# 特定テストクラス実行 (例: ActivateAudioInterfaceCompletionHandlerTests)
dotnet test Tests/NAudioTests.csproj --filter "FullyQualifiedName~ActivateAudioInterfaceCompletionHandlerTests"
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
- `Tests/NAudioTests.csproj` — 単体テスト (`Microsoft.NET.Test.Sdk` + NUnit 4.6 + NUnit3TestAdapter + Moq 4.20)。`dotnet test` で実行する通常のテストプロジェクト (Library)。
- `ProcessLoopbackTestApp/ProcessLoopbackTestApp.csproj` — Process Loopback 手動 UI 動作確認用の WPF アプリ (WinExe)。`App.xaml` + `ProcessLoopbackCaptureTestWindow.xaml(.cs)`。

> WPF サンプルアプリ (旧 AudioFileInspector / MidiFileConverter / MixDiff) は議題 1 採用で退役済。Process Loopback の動作確認用 UI は `ProcessLoopbackTestApp/ProcessLoopbackCaptureTestWindow.xaml(.cs)` を参照。

## ビルド環境

- **TFM**: `net10.0-windows10.0.20348.0` (x64 only)
- **MSBuild SDK**: `MSBuild.Sdk.Extras 3.0.44` (`global.json` で固定)
- **.NET SDK**: `10.0.100` + `rollForward: latestFeature` (`global.json` で固定)
- **AllowUnsafeBlocks**: 有効 (DSP / WaveBuffer 等)
- **UseWPF**: 有効 (`NAudio.Wpf.Gui.*` の WPF コントロール群のため維持する)
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

WASAPI / MediaFoundation / DMO の COM ラッパークラスは、以下の方針で統一されている。新規 COM ラッパーを追加する場合も同じパターンを適用する:

1. **`IDisposable` 実装**: `Dispose()` で `Marshal.ReleaseComObject` を呼ぶ
2. **`disposed` フラグ**: 冒頭で早期 return し、二重 Dispose を防ぐ
3. **`GC.SuppressFinalize` は `if (!disposed)` の中で呼ぶ**
4. **ファイナライザは COM オブジェクトに触らず `Debug.WriteLine` で警告のみ出す**: GC スレッド (MTA) から STA バインド COM を Release すると `RPC_E_WRONG_THREAD` や AccessViolation になるため、最終解放は CLR の RCW 内部 finalizer に委ねる
5. **親が子 COM ラッパーを保持する場合**: 親の `Dispose(disposing=true)` で子も Dispose し、`disposing=false` (finalizer 経由) では子に触らない

この方針が適用されているクラス:
- `MediaType` / `PropertyStore` / `MMDevice` / `AudioMeterInformation` / `DeviceTopology`
- `AudioSessionControl` / `AudioClient` / `Part`
- `AudioRenderClient` / `AudioCaptureClient` / `AudioClockClient` / `AudioStreamVolume` (警告 finalizer のみ追加)
- `AudioSessionManager` (COM 操作は `Dispose(disposing=true)` 経由のみ、finalizer は警告のみ。元は finalizer から `UnregisterSessionNotification` / `ReleaseComObject` を直接呼ぶ方針違反だったが統一済)

### Process Loopback Capture (フォーク独自機能)

`WasapiCapture.CreateForProcessCaptureAsync(processId, includeProcessTree)` で特定プロセスの音声をキャプチャ。

**絶対に守る制約** (詳細は `Docs/ProcessLoopbackCapture.md`):
- UI スレッド (STA) + 非 null の `SynchronizationContext` で `await` する
- `await` は `ConfigureAwait` を付けずそのまま待つ。`ConfigureAwait(false)` を付けると STA コンテキストを失い、`E_NOINTERFACE` / 無音 / `-1/0/1` のプレースホルダー値だけが返る症状になる
- `StartRecording()` は `await` 継続と同じスレッドから呼ぶ
- `ProcessLoopbackMode` enum は Windows 公式 `PROCESS_LOOPBACK_MODE` 準拠で **`IncludeTargetProcessTree=0` / `ExcludeTargetProcessTree=1`** を維持する。`AudioClientProcessLoopbackParams` が blittable 構造体で enum 値が生のままネイティブへ渡るため、値を入れ替えると `includeProcessTree` の意味が反転する (v1.0.44 で逆定義バグを修正済)

CI でリグレッション検知するため `Tests/Wasapi/ActivateAudioInterfaceCompletionHandlerTests.cs` で Mock test を回している (F-004 リグレッション保護: `Marshal.GetExceptionForHR(S_FALSE)` が null を返す前提、COMException フォールバックの動作確認)。HRESULT→例外変換は 3 つの CompletionHandler 実装 (`ActivateAudioInterfaceCompletionHandler<T>` / `...Handler1` / `ProcessLoopbackActivateCompletionHandler`) で `ActivateAudioInterfaceResult.ToException(hr)` に集約している。この集約を維持し、分散コピペには戻さない (`GetExceptionForHR(S_FALSE)` の null を `TrySetException` に渡すと hang するため COMException フォールバックが必須)。

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
- **`Tests/NAudioTests.csproj` は `Microsoft.NET.Test.Sdk` を持つ通常テストプロジェクト (Library) として維持する**。これが無いと `dotnet test` は VSTest ターゲットを持たず、テストを 1 件も実行せず exit 0 で終わる (NUnit3TestAdapter だけでは不十分)。
- WPF 手動 UI テスト (`App.xaml` + `ProcessLoopbackCaptureTestWindow`) は **`ProcessLoopbackTestApp` プロジェクトへ分離したまま維持する**。同居させると WPF `ApplicationDefinition` の Main と `Microsoft.NET.Test.Sdk` の Main が衝突 (`CS0017`) し、Test.Sdk を入れられず `dotnet test` が機能しなくなるため。
- `UseWPF=true` は `ProcessLoopbackDeadlockTests` が WPF `Dispatcher` を使うため Tests でも有効 (ただし `ApplicationDefinition` を持たないので Main 衝突は起きない)。

## コーディング規約

- コメント・コミットメッセージは日本語
- 公開 API の変更は既存利用者への影響を必ず考慮する (NuGet 公開済、`drop-in replacement` を謳う方針)
- 新機能は Provider / Stream パターンに従って実装する
- ドキュメント XML 生成有効 (`GenerateDocumentationFile=true`) — public メンバーには XML コメント必須

## CI/CD (`.github/workflows/publish.yml`)

- トリガー: `release/**` ブランチへの push + `workflow_dispatch`
- 主要ステップ: checkout → setup-dotnet (10.0.x) → cache NuGet → `dotnet test --filter "TestCategory!=IntegrationTest"` → `dotnet build -c Release` → `publish.ps1`
- セキュリティ: `permissions: contents: read` 明示 / `timeout-minutes: 30` / `concurrency` でジョブ並走防止
- NuGet 公開には `NUGET_API_KEY` シークレットが必要

`publish.ps1` は `Directory.Build.props` の `<Version>` を XML 解析で抽出し、`NAudio/bin/x64/Release/1llum1n4t1s.NAudio.<version>.nupkg` をピンポイントで push する (Debug ビルド残骸の誤公開防止)。`$apiKey` が未設定なら `$env:NUGET_API_KEY` をフォールバックで取得する (PowerShell の子スクリプトスコープ問題への対応)。

## 過去の事故と落とし穴

過去の `/rere` レビュー & 修正セッションで判明した、機械的に触ると壊れる箇所。以下は「維持すべき状態」と「触ると起きる事故」をセットで示す:

1. **テストプロジェクトと WPF 手動 UI テストは分離して維持する** — `Tests/NAudioTests.csproj` は `Microsoft.NET.Test.Sdk` を持つ通常テストプロジェクト (Library)、WPF UI (`App.xaml` 等) は `ProcessLoopbackTestApp` (WinExe)。両者を 1 プロジェクトに同居させると `ApplicationDefinition` の Main と Test.Sdk の Main が衝突 (`CS0017`) し、Test.Sdk を外す→`dotnet test` が 0 件実行 (テスト未実行) に陥る。過去にこの状態でテストが一切走っていなかった事故がある (分離して 202 件が走るよう修復済み)
2. **ASIO (`NAudio/Asio/`) は維持する** — プロオーディオ層を保護するため削除しない。過去に議題 2 採用で一度削除したが復活させた経緯がある (RELEASE_NOTES.md の `[Reverted]` 参照)
3. **MediaType / PropertyStore 等の finalizer は「警告のみ」方針を維持する** — finalizer で `Marshal.ReleaseComObject` を呼ぶコードを見つけたら、それは STA-COM 違反 / 二重解放のバグなので警告のみへ戻す
4. **`Directory.Build.props` の `CleanBinObjBeforeRestore` は無効のまま維持する** — 有効化すると MSBuild incremental を破壊する。stale `obj/` が問題なら `dotnet clean` を明示呼出する
5. **`GenerateIcon` Target の `Inputs/Outputs` は維持する** — 外すと毎ビルド `powershell.exe` 起動で incremental が効かなくなる
6. **`UseWPF=true` はライブラリで維持する** — `NAudio/Wpf/Gui/` が WPF コントロールを持つため、外すとビルド失敗する
7. **「機能を削る」「公開済みを引き下げる」修正はユーザー判断に委ねる** — レビュー時は `business-call` 判定で議題提示に留め、勝手に削除しない
8. **`ProcessLoopbackMode` enum の値は維持する** — 公式 `PROCESS_LOOPBACK_MODE` 準拠で `IncludeTargetProcessTree=0` / `ExcludeTargetProcessTree=1`。blittable 構造体で生値がネイティブへ渡るため、値を反転させると `CreateForProcessCaptureAsync` の `includeProcessTree` 指定が逆転する (v1.0.44 で修正、再発防止コメントを `Wasapi/CoreAudioApi/AudioClientStreamFlags.cs` に記載済)
9. **`gh` コマンドは origin を既定にしてから使う** — このリポジトリは `upstream` (naudio/NAudio) remote を持つフォークなので、デフォルト未設定だと `gh` が upstream を誤参照して run/PR が空や `404 naudio/NAudio` になる。`gh repo set-default 1llum1n4t1s/1llum1n4t1s.NAudio` で origin を既定に固定する (`.git/config` にローカル保存されるが、別マシンへ clone した直後は再設定が必要)
10. **利用者向け診断ログは `Trace.WriteLine` を使う** — NuGet パッケージは `dotnet build -c Release` で生成されるため、`#if DEBUG` ブロックや `[Conditional("DEBUG")]` が付く `Debug.WriteLine` は利用者の手元で完全に消える。Process Loopback の STA / `SynchronizationContext` 違反警告のように利用者が本番で見るべき診断は `System.Diagnostics.Trace.WriteLine` を使う。一方、COM ラッパー finalizer の「Dispose 漏れ」警告は開発者向けなので `Debug.WriteLine` のままにする

## 関連ドキュメント

- `README.md` — ユーザー向け概要 + Breaking Changes セクション
- `RELEASE_NOTES.md` — フォーク独自の変更履歴 (Keep a Changelog 形式)
- `Docs/ProcessLoopbackCapture.md` — Process Loopback の STA 制約と切り分けチェックリスト
- `Docs/OutputDeviceTypes.md` — 各 IWavePlayer (WaveOut / Wasapi / DirectSound / Asio) の使い分け
