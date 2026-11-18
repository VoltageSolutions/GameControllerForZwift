# Build and test

!!! tldr "TL;DR"
    Install the .NET 10 SDK, clone the repo and run `dotnet build windows/GameControllerForZwift.sln` on Windows. The build downloads the native SDL2 library once and checks its SHA256 hash. Run the tests with `dotnet test`. Warnings are errors, and formatting is checked with `dotnet format`, so keep the build clean.

## What you need

- **Windows 10 or 11** for the full solution. The WPF app and its tests only build on Windows.
- **.NET 10 SDK.** `windows/global.json` asks for SDK `10.0.100` with `rollForward: latestFeature`, so any newer 10.0 feature band works.
- An editor such as Visual Studio 2026, Visual Studio Code or Rider. Visual Studio needs the ".NET desktop development" workload for WPF.

The `Core`, `Network` and `Gamepad` libraries, their tests and `ZwiftLanProbe` also build on Linux and macOS. CI uses that for the formatting check.

Everything that belongs to the app lives in `windows/`: the solution, `global.json`, `Directory.Build.props`, `build/`, `src/`, `tests/` and `tools/`. The commands on this page run from the repo root.

## Build

```powershell
git clone https://github.com/VoltageSolutions/GameControllerForZwift.git
cd GameControllerForZwift
dotnet build windows/GameControllerForZwift.sln
```

Run the app:

```powershell
dotnet run --project windows/src/Presentation/GameControllerForZwift
```

### SDL2 download

The app reads controllers through the native SDL2 library. `windows/build/Sdl2.targets` (imported by the `Gamepad` project) takes care of it:

1. On Windows only, before the build needs it, it downloads `SDL2-2.30.10-win32-x64.zip` from the official SDL GitHub releases into `windows/.cache/sdl2/`.
2. It checks the zip's **SHA256** hash against the value in the targets file. If the hash does not match, the zip is deleted and the build fails.
3. It unzips `SDL2.dll` and copies it next to the app in both build and publish output.

The download happens once. Later builds reuse `windows/.cache/sdl2/SDL2.dll`.

!!! tip "Building offline"
    Put `SDL2.dll` (version 2.30.10, win32-x64) into `windows/.cache/sdl2/` yourself and the build skips the download.

## Tests

| Project | Kind | What it covers | Runs on |
|---|---|---|---|
| `GameControllerForZwift.Core.UnitTests` | Unit | Mapping, profiles, JSON converters, analog values, `DataIntegrator` | Any OS |
| `GameControllerForZwift.Gamepad.UnitTests` | Unit | SDL power level mapping, USB device lookup | Any OS |
| `GameControllerForZwift.Network.UnitTests` | Unit | Direct Connect framing and sessions, UUIDs, keypad frames, analog input, mDNS profile, replies that match the known-good Zwift Ride presentation | Any OS |
| `GameControllerForZwift.Network.IntegrationTests` | Integration | `MdnsAdvertiser` and `ZwiftRideOutputService` over real sockets | Any OS |
| `GameControllerForZwift.UnitTests` | Unit (WPF) | App startup, view models, converters, controls, navigation | Windows |
| `GameControllerForZwift.UITests` | UI | Drives the real app with Appium and WinAppDriver (About and Settings pages) | Windows, with WinAppDriver and Appium installed |

The tests use xUnit and NSubstitute. Run them all with:

```powershell
dotnet test windows/GameControllerForZwift.sln
```

Or run one project:

```powershell
dotnet test windows/tests/GameControllerForZwift.Network.UnitTests
```

!!! note "UI tests need extra tools"
    The UI tests need WinAppDriver and an Appium server with the Windows driver. CI installs both. Locally, you can skip that project and run the others.

## Code quality

`windows/Directory.Build.props` applies to every project:

| Setting | Effect |
|---|---|
| `AnalysisLevel` = `latest-recommended` | The recommended set of .NET code analyzers is on. |
| `EnableNETAnalyzers` = `true` | Analyzers run during the build. |
| `TreatWarningsAsErrors` = `true` | Any warning fails the build. |

`.editorconfig` only adjusts analyzer severities. For example, test projects may use underscores in test method names. Formatting follows the `dotnet format` defaults. Check it before you push:

```powershell
dotnet format windows/src/Infrastructure/GameControllerForZwift.Network --verify-no-changes --severity error
```

The `Lint` job in Windows CI runs this check on `Core`, `Network`, `Gamepad`, the portable test projects and `ZwiftLanProbe`.

## ARM smoke test

The `ARM smoke test` job in Windows CI runs the x64 builds from the `Build` job (framework-dependent and standalone) on a `windows-11-arm` runner. `.github/scripts/smoke-test-app.ps1` starts the app under x64 emulation and checks that its log shows SDL2 initialized and devices enumerated. The `Build` job also uploads both builds as the `app-win-x64` and `app-win-x64-standalone` artifacts (kept for 14 days), so you can copy them to a real ARM device.

## Workflows

The GitHub Actions workflows live in `.github/workflows/`.

| Workflow | File | When it runs | What it does |
|---|---|---|---|
| Windows CI | `windows-ci.yml` | Every pull request | Builds and publishes the app, runs the unit, integration and UI tests, checks formatting, the version, CodeQL and vulnerable packages, and uploads coverage. |
| Windows CD | `windows-cd.yml` | Push to `main` (not for docs-only changes) | Publishes both zips, creates the GitHub release and opens the next release branch. |
| Windows CodeQL and Coverage | `windows-codeql-and-coverage.yml` | Push to `main` (not for docs-only changes) | Refreshes the CodeQL alerts and Codecov coverage that pull requests compare against. |
| Docs CI | `docs-ci.yml` | Pull requests that change the docs | Builds the docs site with `--strict`. |
| Docs CD | `docs-cd.yml` | Push to `main` that changes the docs | Builds and deploys the docs site to GitHub Pages. |
| Release Branch | `release-branch.yml` | A `release/vX.Y.Z` branch is created | Sets `<Version>` in `windows/Directory.Build.props` to match the branch name. |

## Optional: the build watcher

`windows/tools/dev/build-watcher.ps1` is a small helper for driving builds on a Windows machine from somewhere else, for example a remote session that can write files but has no shell there. Start it from the repo root:

```powershell
powershell -ExecutionPolicy Bypass -File windows\tools\dev\build-watcher.ps1
```

It checks every 2 seconds for a file called `.build-request` in the repo root.

- An empty request runs `build`, `test-network` and `test-unit`.
- Otherwise list one keyword per line: `build`, `test`, `test-network`, `test-unit`, `clean`, or a custom line that starts with `dotnet `.

While it works it renames the request to `.build-running`. It writes one log per step (`build.log`, `test-network.log`, `test-unit.log`, or `step-N.log` for custom lines) and finishes with `.build-done`, which lists the exit code and duration of each step. A failed build stops the sequence. The marker files are ignored by git.
