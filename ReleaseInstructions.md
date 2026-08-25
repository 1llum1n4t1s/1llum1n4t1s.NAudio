# 1llum1n4t1s.NAudio release instructions

fork の preview / final package を GitHub Actions から公開する手順です。公開操作は maintainer が
明示的に release を開始した場合だけ実行します。upstream の設計経緯は
[Docs/Architecture/ReleaseStrategy.md](Docs/Architecture/ReleaseStrategy.md) に残しています。

## 前提

- GitHub CLI `gh` が `1llum1n4t1s/1llum1n4t1s.NAudio` へ認証済み
- repository secret `NUGET_API_KEY` に fork package だけを公開できる scoped key を設定済み
- `main` の build / test が成功している
- `Directory.Build.props` の `VersionPrefix` と [CHANGELOG.md](CHANGELOG.md) が同期している

## Preview

連番 preview を作る場合:

```powershell
gh workflow run release.yml --repo 1llum1n4t1s/1llum1n4t1s.NAudio
```

`<VersionPrefix>-preview.<run_number>` が生成されます。named milestone は `milestone` を渡します。

```powershell
gh workflow run release.yml `
  --repo 1llum1n4t1s/1llum1n4t1s.NAudio `
  -f milestone=rc.1
```

結果は [fork の Actions](https://github.com/1llum1n4t1s/1llum1n4t1s.NAudio/actions/workflows/release.yml)
で確認します。

## Final release

### 1. Pre-flight

1. `Directory.Build.props` の `VersionPrefix` を release version にします。
2. `CHANGELOG.md` の `## [Unreleased]` を `## [x.y.z] - YYYY-MM-DD` に変更します。
3. その上へ新しい空の `## [Unreleased]` section を追加します。
4. Release build、非 Integration test、Native AOT smoke、13 package の pack を確認します。
5. 通常の protected-branch flow で `main` へ取り込みます。

### 2. Tag

`main` を同期し、`VersionPrefix` と同じ tag を push します。

```powershell
git fetch origin
git switch main
git pull --ff-only
git tag v4.0.0
git push origin v4.0.0
```

release workflow は次を実行します。

- tag と `VersionPrefix` の一致を検証
- `CHANGELOG.md` の matching version section と35,000文字上限を検証
- `1llum1n4t1s.NAudio.*` の13 package と symbol package を生成
- `NUGET_API_KEY` で NuGet.org へ公開
- `1llum1n4t1s.NAudio x.y.z` の GitHub Release を作成

### 3. 次 version

release 後は `VersionPrefix` を次の development version へ進め、`CHANGELOG.md` の
`## [Unreleased]` へ以後の変更を記録します。

## Package 一覧

release workflow が公開する project は次の13個です。

- `NAudio.Core`
- `NAudio.Midi`
- `NAudio.WinMM`
- `NAudio.Wasapi`
- `NAudio.Asio`
- `NAudio.Dmo`
- `NAudio.WinForms`
- `NAudio.Vst3`
- `NAudio.Alsa`
- `NAudio.SoundFile`
- `NAudio.Sampler`
- `NAudio.Extras`
- `NAudio` meta-package

NuGet 上ではすべて `1llum1n4t1s.` prefix が付きます。project を追加した場合は
[release workflow](.github/workflows/release.yml) の `Pack` list も同じ変更で更新します。

## Troubleshooting

- **`CHANGELOG.md has no ... section`**: tag version と同じ `## [x.y.z] - YYYY-MM-DD` を追加します
- **tag と `VersionPrefix` が不一致**: version を修正した commit に tag を作り直します
- **NuGet authentication error**: repository secret `NUGET_API_KEY` の期限、scope、package ownership を確認します
- **package が不足する**: workflow の `Pack` list と `artifacts/*.nupkg` を照合します
- **35,000文字超過**: changelog は利用者向け要点へ絞り、詳細は commit / PR へ残します

## 関連ファイル

- [CHANGELOG.md](CHANGELOG.md)
- [.github/workflows/release.yml](.github/workflows/release.yml)
- [.github/workflows/build.yml](.github/workflows/build.yml)
- [Docs/Architecture/ReleaseStrategy.md](Docs/Architecture/ReleaseStrategy.md)
