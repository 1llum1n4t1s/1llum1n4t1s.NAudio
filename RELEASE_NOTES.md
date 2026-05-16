# RELEASE NOTES — 1llum1n4t1s.NAudio

[1llum1n4t1s.NAudio](https://github.com/1llum1n4t1s/1llum1n4t1s.NAudio) のリリースノート。upstream NAudio に対するフォーク独自の変更を記録します。

形式: [Keep a Changelog](https://keepachangelog.com/ja/1.1.0/) に準拠。バージョニングは [Semantic Versioning](https://semver.org/lang/ja/) を採用。

---

## [Unreleased] — 1.0.43 候補

> Claude Code による 2 ラウンドの `/rere` レビューで指摘された問題を修正中。

### Added
- **Process Loopback リグレッション保護用 CI Test 追加** (`Tests/Wasapi/ActivateAudioInterfaceCompletionHandlerTests.cs`)
  - `Marshal.GetExceptionForHR(S_FALSE)` が `null` を返す前提を継続的に検証
  - `ProcessLoopbackActivateCompletionHandler` の `ActivateCompleted` 3 ケース (S_OK / E_FAIL / S_FALSE)
- `Part` クラスに `IDisposable` を実装 (`DeviceTopology` リーク解消)
- `AudioRenderClient` / `AudioCaptureClient` / `AudioClockClient` / `AudioStreamVolume` に警告 finalizer 追加 (Dispose 漏れ検知)

### Changed
- **`README.md` 整備**: 「Breaking Changes from upstream NAudio」セクションを追加。「drop-in replacement」 → 「near drop-in replacement (except ASIO)」に修正。Process Loopback の STA / `ConfigureAwait(false)` 禁止の警告を追記。
- **`NAudio.csproj`**:
  - `<Description>` をフォーク特性を反映した内容に変更 (NuGet ページ整合)
  - `<PackageTags>` に `wasapi process-loopback` を追加
  - `<InternalsVisibleTo Include="NAudioTests" />` を追加 (テストハーネス整備)
- **CI/CD 強化** (`publish.yml` / `publish.ps1`):
  - `permissions: contents: read` 明示
  - `timeout-minutes: 30` 追加
  - `concurrency` で release ブランチ並走防止
  - `actions/cache@v4` で NuGet パッケージキャッシュ
  - `dotnet test` ステップ追加 (壊れたコード公開防止)
  - `<Version>` 突き合わせで Debug 残骸の誤 push 防止
  - `$apiKey` フォールバック (`$env:NUGET_API_KEY` で確実に届く)
- **ビルド最適化**:
  - `Directory.Build.props` から `CleanBinObjBeforeRestore` 撤去 (MSBuild incremental 復活)
  - `GenerateIcon` Target に `Inputs`/`Outputs`/`Condition` 追加 (毎ビルド PowerShell 起動を回避)
- **`global.json` に SDK ピン留め** (`10.0.100`, `rollForward: latestFeature`, `allowPrerelease: false`)
- `Docs/PlaySineWave.md`: 存在しない `SignalGeneratorType.PinkNoise/WhiteNoise` → 実装通り `Pink/White` に修正
- `Docs/ProcessLoopbackCapture.md`: 参照サンプル `MinimalProcessLoopbackWpf` → 実在する `Tests/Wasapi/ProcessLoopbackCaptureTestWindow.xaml.cs` に変更
- `Docs/OutputDeviceTypes.md` / `Docs/EnumerateOutputDevices.md` / `Docs/Resampling.md`: ASIO 退役注記
- `Tests/`: 個人パス (`C:\Users\Mark\...`, `C:\Users\mheath\...`) を環境変数指定方式 (`NAUDIO_TEST_*`) に置換
- `Tests/NAudioTests.csproj`: `<OutputType>WinExe</OutputType>` 削除 (暗黙 Library 化)、`<IsPackable>false</IsPackable>` 追加
- `MixingWaveProvider32` に `[Obsolete]` 付与 (新規利用抑制)
- `NAudio.Utils.NativeMethods` に `[EditorBrowsable(EditorBrowsableState.Never)]` 付与
- `AudioSessionControl` / `MediaType` / `PropertyStore` / `MMDevice` / `AudioMeterInformation` / `DeviceTopology` / `AudioClient` の `Dispose` / finalizer パターン統一 (二重解放 + GC スレッド COM 呼出を構造的に解消)
- `MidiEventCollection.PrepareForExport` の EndTrack 削除ループを O(n²) → O(n) (2-pointer write-index 方式)
- `Equalizer` の `BiQuadFilter[,]` を jagged 配列化 + ローカルキャッシュ (リアルタイム EQ で 5-15% CPU 削減)
- `WaveChannel32` の `Sample` event 88,200 回/sec 発火パスでローカルキャプチャ
- `BufferedWaveProvider.AddSamples` に double-checked locking (並行 producer 対応)
- `WasapiCapture.RaiseRecordingStopped`: handler 未購読時に Debug.WriteLine で警告
- `MediaFoundationEncoder.SelectMediaType` / `GetEncodeBitrates` で未選択 MediaType を明示 Dispose (確定リーク解消)

### Fixed
- **入力検証 DoS 対策** (悪意ある `.wav` / `.mp3` / `.mid` / `.sf2` でのクラッシュ防止):
  - `XingHeader.LoadXingHeader`: `frame.RawData` の境界チェック追加 (OOB Read 解消)
  - `Id3v2Tag.ReadTag`: synchsafe `dataLength` を残りストリーム長と突き合わせ (256MB OOM 解消)
  - `WaveFormat.ReadWaveFormat`: `channels` / `sampleRate` / `blockAlign` / `bitsPerSample` の sanity check (DivByZero 解消)
  - `SoundFont` 各 Builder: 空チャンクで `RemoveAt(-1)` ガード、`UInt16Amount` レンジチェック、`startIndex` 単調性チェック
  - `MidiFile`: SysexEvent のチャンク境界早期検出
  - `MetaEvent.ReadMetaEvent`: `length` サニタイズ
  - `CueList`: `cueCount` 巨大値 OOM + ラベル走査 KeyNotFoundException 解消
  - `WaveFileChunkReader.ReadDs64Chunk`: chunkSize<24 で負 ReadBytes 解消
  - `SoundFont/RiffChunk`: `(int)ChunkSize` uint→int 負キャスト解消
- **COM ライフサイクル**:
  - `MediaType` の二重 `ReleaseComObject` + GC スレッド COM 呼出を解消
  - `MMDevice.Dispose` で子 COM ラッパー (`PropertyStore` / `AudioMeterInformation` / `DeviceTopology`) を確実に Dispose
  - `AudioClient` finalizer から managed 操作削除 (finalizer 順序非決定性への対応)
  - `AudioPlaybackEngine.Dispose` で `mixer` も Dispose (ArrayPool バッファ浮遊解消)
  - `ActivateAudioInterfaceCompletionHandler` の `Marshal.GetExceptionForHR` null フォールバック (Process Loopback hang バグ解消)
  - `Marshal.Release(ptr)` 漏れ修正 (Generic 版 CompletionHandler の COM ptr リーク解消)
  - `AudioSessionControl` の finalizer / Dispose を統一方針に揃え、`AudioMeterInformation` 子 Dispose も追加
  - `Part` に `IDisposable` 実装 (`DeviceTopology` リーク解消)
  - `AudioRenderClient` / `AudioCaptureClient` / `AudioClockClient` / `AudioStreamVolume` の Dispose 漏れ検知用 finalizer 追加
- **状態整合性**:
  - `WasapiOut.PlayThread` catch (Exception) で `playbackState=Stopped` を明示
  - `AsioOut.Dispose` を `Stop()` 経由化 (`PlaybackStopped` イベント発火統一)
  - `AsioOut.Stop` の二重実行ガード
  - `WasapiCapture`: 空 `DataAvailable` イベント発火を抑止

### Removed
- **ASIO サポート全体** (`NAudio/Asio/` 13 ファイル) — 議題 2 (Round 1) の判断
  - `AsioOut` / `AsioDriver` / `AsioDriverExt` 等
  - 代替: WASAPI exclusive mode (`WasapiOut(... AudioClientShareMode.Exclusive ...)`)
  - 関連ドキュメント `Docs/AsioPlayback.md` / `Docs/AsioRecording.md` も削除
- **WPF サンプルアプリ 3 つ** — 議題 1 (Round 1) の判断
  - `NAudio/AudioFileInspector/` (516 ファイル相当)
  - `NAudio/MidiFileConverter/`
  - `NAudio/MixDiff/`
  - Process Loopback の動作確認は `Tests/Wasapi/ProcessLoopbackCaptureTestWindow.xaml.cs` を参照
- `NAudio/Changes.xml` — 2010 年止まりの上流由来歴史ファイル
- `NAudio.slnx` の dangling reference 5 件削除 (`naudio-logo.png` / `readme.txt` / `Docs/AsioPlayback.md` / `Docs/AsioRecording.md`)
- `Mp3FileReader.cs` を `NAudio/` 直下 → `NAudio/Core/Wave/WaveStreams/` に物理移動 (namespace 不変、配置正規化)

---

## [1.0.42] — 2026-03-10

### Changed
- `MidiEvent` に `stackalloc` を導入してヒープ割当削減
- `ResamplerDmoStream` の出力バッファ配列をキャッシュ
- `SampleAggregator` のハミング窓を事前計算テーブル化
- `Equalizer` ホットループの modulo 演算除去
- `AudioClient` の GUID 定数を `static readonly` に抽出
- `MediaBuffer` の `GC.SuppressFinalize` 位置修正
- `MediaType` / `PropertyStore` に `IDisposable` 実装で COM リーク修正
- DMO エフェクトでタイポ修正、`First()` → `FirstOrDefault()`
- `MidiFile` の NoteOff 検索を末尾から逆方向に変更 (パフォーマンス改善)

---

## 1.0.x までの履歴

1.0.41 以前については [git log](https://github.com/1llum1n4t1s/1llum1n4t1s.NAudio/commits/main) を参照してください。本フォークは upstream [NAudio](https://github.com/naudio/NAudio) の機能を基盤としており、それ以前の歴史は upstream 側のリリースノートを参照することを推奨します。
