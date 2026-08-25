# Working in 1llum1n4t1s.NAudio

This file gives coding agents the conventions for contributing to the 1llum1n4t1s.NAudio fork. Humans should read [README.md](README.md) and [Docs/Architecture/](Docs/Architecture/) instead.

## Orientation

- **Repository layout.** Projects are grouped by role: `src/` holds the shipping libraries (the NuGet packages), `tests/` holds the test, benchmark and diagnostic projects (including `NAudio.Benchmarks`, `NAudioAotSmokeTest`, `MfStressTest`), and `samples/` holds the runnable demo/tool apps (`NAudioDemo`, `NAudioWpfDemo`, `NAudioConsoleTest`, `AudioFileInspector`, `MidiFileConverter`, `MixDiff`) plus their `SampleData/`. DocFX lives entirely under `docfx/` (`docfx.json`, `index.md`, `toc.yml`, `templates/`, generated `api/`); tutorial Markdown stays in `Docs/`.
- **The fork's NAudio 3 development happens on `main`.** Sync upstream changes through a descriptive temporary branch such as `codex/upstream-sync`, verify the fork-specific compatibility and Native AOT tests, and then integrate that branch into `main`. Use `release/x.y.z` only for an explicitly requested release.
- **Preserve upstream history.** Keep `upstream/main` as a merge parent when synchronizing the fork so future upstream updates remain traceable. Use a normal fast-forward push from the local `main`; force-push only after the maintainer explicitly approves the exact history rewrite.
- **Architecture docs** in [Docs/Architecture/](Docs/Architecture/) are the source of truth for cross-cutting decisions:
  - [ReleaseStrategy.md](Docs/Architecture/ReleaseStrategy.md) — release/branch/version flow
  - [NAudio3AssemblyLayoutPlan.md](Docs/Architecture/NAudio3AssemblyLayoutPlan.md) — package structure
  - [MODERNIZATION.md](Docs/Architecture/MODERNIZATION.md) — modernisation phases

## Changelog

When you make a **user-visible fork change** — new public API, behaviour change, bug fix, deprecation, packaging change — add a concise Japanese bullet to the matching category under `## [Unreleased]` in [CHANGELOG.md](CHANGELOG.md). Keep upstream history in `RELEASE_NOTES.md` unchanged. Match the style of existing entries:

- Describe the concrete user-visible result
- Mention the GitHub PR or issue number if known: `(#1234)`
- One line, no prose paragraphs — the maintainer will edit at release time

Purely internal refactors, test-only changes, dependency bumps with no observable effect, and docs/comment fixes can omit a changelog entry. When impact is uncertain, add the entry for the maintainer to curate.

## Documentation site

Tutorials live in [Docs/](Docs/) as Markdown and are published to a DocFX site on GitHub Pages by [.github/workflows/docs.yml](.github/workflows/docs.yml); the API reference is generated automatically from the source XML doc comments. When you **add a new tutorial**, also add an entry for it in [Docs/toc.yml](Docs/toc.yml) so it appears in the sidebar — a CI check fails the build if any `Docs/*.md` is missing from the TOC (an unlisted page still builds but is orphaned from the navigation). Internal `Docs/Architecture/` docs are excluded from the published site.

## PR labelling

When opening a PR (where you have permission), apply one of: `breaking`, `enhancement`, `bug`, `documentation`. These categorize GitHub's supplemental generated notes; `CHANGELOG.md` remains the canonical user-facing history. Use `release-notes-skip` for PRs that should not appear in generated notes.

## Versioning

Package versions are centralised in [Directory.Build.props](Directory.Build.props) as `<VersionPrefix>`. Do **not** add a per-csproj `<Version>` to NAudio packages — they're meant to stay in lockstep. The tool/sample apps (MixDiff, AudioFileInspector, MidiFileConverter) keep their own explicit `<Version>` and are exempt.

## Language

Write code comments and commit messages in Japanese unless an upstream file already establishes a local English convention. Public API XML documentation should follow the surrounding project style so generated documentation remains consistent.

## Building & testing on Linux

Some cloud and CI environments start without a .NET SDK on the PATH — this isn't a property of the repo but of the host. The default Anthropic cloud-agent sandbox is one example, and other infrastructure (GitHub Copilot agents, fresh CI runners, etc.) may differ now or in the future. So **first check whether `dotnet` is available**; only install it if it isn't. On Debian/Ubuntu, install .NET 10 via apt: add Microsoft's feed with `wget -q https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O /tmp/ms.deb && sudo dpkg -i /tmp/ms.deb && sudo apt-get update`, then `sudo apt-get install -y dotnet-sdk-10.0` (the SDK builds the `net9.0` libraries fine).

`NAudio.Core.Tests` only references the cross-platform projects (`NAudio.Core`, `NAudio.Midi`), so it builds and runs on Linux/macOS without any extra flags. Build and run the cross-platform tests with:

```
dotnet build tests/NAudio.Core.Tests/NAudio.Core.Tests.csproj
dotnet test --project tests/NAudio.Core.Tests/NAudio.Core.Tests.csproj --filter "TestCategory!=IntegrationTest"
```

(`NAudio.Windows.Tests` references the `NAudio` meta-package and the Windows backends, so it targets a Windows TFM — building it on Linux still needs `-p:EnableWindowsTargeting=true`, and it can only run on Windows. Tests that exercise the meta-package's `AudioFileReader` live there for this reason.)

The `TestCategory!=IntegrationTest` filter skips tests needing real audio hardware; note that .NET 10's `dotnet test` wants `--project` rather than a positional path.

## CI

Every PR runs build + test on `windows-latest` via [.github/workflows/build.yml](.github/workflows/build.yml). Tests requiring real audio hardware should be marked with `TestCategory=IntegrationTest` so they're filtered out of the headless run.
