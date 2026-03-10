# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

1llum1n4t1s.NAudioは[NAudio](https://github.com/naudio/NAudio)のフォーク。.NET 10対応、プロセスループバックキャプチャの追加、多数のバグ修正を含む。Windows x64専用のオーディオライブラリ。

## ビルド・テストコマンド

```bash
# ビルド
rtk dotnet build NAudio/NAudio.csproj

# テスト全体実行
rtk dotnet test Tests/NAudioTests.csproj

# 単体テストのみ（IntegrationTestを除外）
rtk dotnet test Tests/NAudioTests.csproj --filter "TestCategory!=IntegrationTest"

# 特定テスト実行
rtk dotnet test Tests/NAudioTests.csproj --filter "FullyQualifiedName~MidiFileTests"

# リリースビルド（NuGetパッケージ生成含む）
rtk dotnet build NAudio/NAudio.csproj -c Release
```

## ソリューション構成

- **NAudio.slnx** — ソリューションファイル（XML形式）
- **NAudio/NAudio.csproj** — メインライブラリ（NuGetパッケージ: `1llum1n4t1s.NAudio`）
- **Tests/NAudioTests.csproj** — テスト（NUnit 4.4 + Moq）
- **NAudio/AudioFileInspector**, **MidiFileConverter**, **MixDiff** — WPFサンプルアプリ

## ビルド環境

- **ターゲット**: `net10.0-windows10.0.20348.0`（x64のみ）
- **SDK**: MSBuild.Sdk.Extras 3.0.44（`global.json`）
- **UnsafeBlocks**: 有効
- **バージョン**: `Directory.Build.props`で一元管理。バージョン変更時はREADMEのPackageReferenceも同期すること

## アーキテクチャ

### コアインターフェース（Providerパターン）

オーディオ処理のチェインを構築するためのProvider/Streamパターンが中心設計:

- **`IWaveProvider`** (`Core/Wave/WaveOutputs/IWaveProvider.cs`) — byte[]ベースのオーディオデータ提供
- **`ISampleProvider`** (`Core/Wave/WaveOutputs/IWaveProviderFloat.cs`) — float[]ベースのサンプルデータ提供
- **`IWavePlayer`** (`Core/Wave/WaveOutputs/IWavePlayer.cs`) — オーディオ再生デバイス抽象化
- **`IWaveIn`** (`Core/Wave/WaveInputs/IWaveIn.cs`) — オーディオ録音デバイス抽象化

典型的な処理チェイン: `AudioFileReader` → `SampleProvider`(加工) → `IWavePlayer`(出力)

### ディレクトリ構成（NAudio/配下）

| パス | 内容 |
|------|------|
| `Core/Wave/SampleProviders/` | ミキシング、フェード、リサンプリング等のサンプル加工 |
| `Core/Wave/WaveStreams/` | ストリームベースのオーディオ読み書き |
| `Core/Wave/WaveFormats/` | WaveFormat定義群 |
| `Core/Dsp/` | FFT、フィルタ、ピッチシフト等のDSPアルゴリズム |
| `Core/Codecs/` | A-law、Mu-law、G.722コーデック |
| `Core/FileFormats/` | MP3、SoundFont等のファイル形式 |
| `Midi/` | MIDIファイルI/O、イベント管理 |
| `Wasapi/` | WASAPI (Windows Audio Session API) |
| `Wasapi/CoreAudioApi/` | Core Audio APIインターフェース |
| `Wasapi/MediaFoundation/` | Media Foundationエンコード/デコード |
| `Wasapi/Dmo/` | DirectX Media Objects (DMOエフェクト) |
| `Asio/` | ASIOドライバーサポート |
| `WinMM/` | レガシーWindows Multimedia API |
| `Wpf/` | WPF用GUIコンポーネント |

### テスト構成

テストは`Tests/`配下にサブフォルダで分類。`[Category("IntegrationTest")]`が付いたテストはオーディオデバイスやファイルI/Oに依存するため、CI環境では除外する。

## コーディング規約

- コメント・コミットメッセージは日本語
- 公開APIの変更は既存ユーザーへの影響を考慮すること
- 新機能はNAudioの既存パターン（Provider/Streamパターン）に従って実装
- ドキュメントXML生成が有効 — publicメンバーにはXMLコメントを付ける

## CI/CD

- `release/**`ブランチへのpushまたはworkflow_dispatchでNuGet公開（`.github/workflows/publish.yml`）
- NuGet公開には`NUGET_API_KEY`シークレットが必要
