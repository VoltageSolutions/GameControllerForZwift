# AGENTS.md

Guidance for AI coding agents (Codex, Copilot, Cursor, Gemini CLI, Jules, Claude Code and others) working in this repo. Humans should read [CONTRIBUTING.md](CONTRIBUTING.md) and the [docs site](https://VoltageSolutions.github.io/GameControllerForZwift/).

## What this is

Game Controller for Zwift is a Windows WPF app (.NET 10) that turns a game controller into a Zwift Ride controller over the local network. The app reads the controller with SDL2, maps inputs to Zwift functions, and talks to Zwift over mDNS and Wahoo Direct Connect. Everything that belongs to the app (solution, `global.json`, `Directory.Build.props`, source, tests and tools) lives in `windows/`. Docs, workflows and repo-wide config stay at the root.

| Project | Path | Role |
|---|---|---|
| `GameControllerForZwift.Core` | `windows/src/Application/` | Domain types, mapping, saved configurations, `DataIntegrator`. No UI or network code. |
| `GameControllerForZwift.Gamepad` | `windows/src/Infrastructure/` | Reads controllers through SDL2. |
| `GameControllerForZwift.Network` | `windows/src/Infrastructure/` | mDNS advertising, Direct Connect server, Zwift Ride protocol. |
| `GameControllerForZwift` | `windows/src/Presentation/` | The WPF app: pages, view models, dialogs, dependency injection. |
| Tests | `windows/tests/` | Unit, integration and UI tests (xUnit, NSubstitute). |
| `ZwiftLanProbe` | `windows/tools/ZwiftLanProbe/` | Console tool that runs the network side on its own. |

## Build, test and format

The full solution builds on Windows only. Core, Gamepad, Network, their tests and the probe also build on Linux and macOS.

```powershell
dotnet build windows/GameControllerForZwift.sln
dotnet test windows/tests/GameControllerForZwift.Core.UnitTests
dotnet test windows/tests/GameControllerForZwift.Gamepad.UnitTests
dotnet test windows/tests/GameControllerForZwift.Network.UnitTests
dotnet test windows/tests/GameControllerForZwift.Network.IntegrationTests
dotnet test windows/tests/GameControllerForZwift.UnitTests
dotnet format windows/src/Application/GameControllerForZwift.Core --verify-no-changes --severity error
```

- `GameControllerForZwift.UnitTests` (WPF) needs Windows.
- `GameControllerForZwift.UITests` needs WinAppDriver and Appium. CI runs it. Skip it locally.
- Run `dotnet format <project> --verify-no-changes --severity error` on any project you change. CI checks Core, Network, Gamepad, the portable test projects and ZwiftLanProbe.
- Docs build: `pip install -r docs/requirements.txt`, then `zensical build --clean --strict`. It must pass with no issues.
- Workflows in `.github/workflows/`: `windows-ci.yml` (Windows CI, pull requests), `windows-cd.yml` (Windows CD, release on `main`), `windows-codeql-and-coverage.yml` (baseline for `main`), `docs-ci.yml` (Docs CI), `docs-cd.yml` (Docs CD) and `release-branch.yml` (sets the version on new release branches).

To render every screen and state to PNG files, I run the opt-in screenshot catalog. It does nothing unless `GCFZ_SCREENSHOTS` names an output folder, and CI leaves it out. Open `index.md` in the folder first.

```powershell
$env:GCFZ_SCREENSHOTS = "$PWD\.shots"
dotnet test windows/tests/GameControllerForZwift.UnitTests --filter Category=Screenshots
```

## Source of truth

- Read `docs/spec/` before you change behavior. Each rule has an ID such as `CFG-015`.
- Reference the requirement IDs in tests (for example in the test name or a comment).
- When behavior changes, update the spec in the same change.
- Protocol and network details are in `docs/how-it-works/`. Layout and project roles are in `docs/build/architecture.md`.

## Conventions

- `TreatWarningsAsErrors` is on and analyzers are at `latest-recommended`. Leave zero warnings.
- The version lives only in `windows/Directory.Build.props`. Do not change it by hand. Release branches are `release/vX.Y.Z`, and `.github/scripts/version.sh` automates the version.
- The repo stores LF. Windows working trees use CRLF. `*.sh` files always stay LF (see `.gitattributes`).
- Comments, XML docs and docs pages describe what the code does now. Do not write history ("previously", "the old ...", "used to", "for now"), because git has it. Do not write future plans, because the roadmap has them, and only when requested.
- Codecov targets: patch coverage of at least 60% of the changed lines, and project coverage of at least 40%. Codecov ignores `windows/tests/`, `windows/tools/`, generated files and `obj/`/`bin/`, but counts `*.xaml.cs`. CI collects coverage from the UnitTests, Core, Gamepad and Network unit test projects. Measure coverage and check it before you call work ready.
- Docs voice: first person singular ("I", "me"), US spelling, active voice, serial (Oxford) comma in lists of three or more, short sentences, no em dashes. Pages start with a TL;DR callout.
- UI text goes in `windows/src/Presentation/GameControllerForZwift/Resources/Strings.resx`, never in XAML or code (`docs/spec/ux/localization.md`). XAML uses `{loc:Loc Key}`, code uses `Localizer.Get`. Give every string a translator comment.
- Keep private data out of the repo: no machine names, IP addresses, MAC addresses, serial numbers or local user paths. Packet captures stay in the git-ignored `docs/captures/`.
- When an AI helped with a commit, end the message with a `Co-Authored-By` trailer. Do not add links to AI sessions.

## Branch flow

- Feature branches open pull requests into the current `release/vX.Y.Z` branch, not `main`.
- A release branch is merged into `main` by pull request, and that publishes the release.
- Dependabot pull requests are re-pointed to the release branch.
- Do not push or open pull requests unless asked.
- A pull request that changes the UI embeds screenshots in its description: each changed screen in light and dark mode, plus the states or window sizes that matter, about six. Upload them as GitHub attachments in the description, never as files in the repository or on a branch.
