# Contributing

Thank you for helping. Ideas, bug reports and pull requests are all welcome.

## Ideas, Issues, or Bugs

Post to the [Issues page](https://github.com/VoltageSolutions/GameControllerForZwift/issues). For a bug, include what you did, what you expected, what happened, and the app's log file (see [Find and share the log file](https://VoltageSolutions.github.io/GameControllerForZwift/use/troubleshooting/#find-and-share-the-log-file)).

## Code

This project follows a modified version of Git Flow where `main` always represents the latest version.

- Version branches are named `release/vX.Y.Z` (for example `release/v0.2.0`).
- Open pull requests against the current release branch, not `main`. If you are not sure which one it is, ask in your pull request.
- Once a release branch is tested, I merge it to `main`, which creates the release.
- Dependabot pull requests are re-pointed to the release branch.

## Versions and release branches

- The version lives in one place: `<Version>` in `Directory.Build.props`.
- After each release, the branch for the next minor version (for example `release/v0.4.0` after `v0.3.0`) is opened automatically, with the version already set.
- For a patch or major release, I create `release/vX.Y.Z` by hand from `main`, and the version is set for me. It must be higher than the latest release.
- When I go to a new major version, I delete the unused minor branch that was opened automatically.
- The Version Check on pull requests enforces all of this, so you normally do not change the version yourself.

## Build and test

Build with the .NET 10 SDK on Windows (`dotnet build GameControllerForZwift.sln`). See [Build and test](https://VoltageSolutions.github.io/GameControllerForZwift/build/build-and-test/) for the full steps and [Architecture](https://VoltageSolutions.github.io/GameControllerForZwift/build/architecture/) for how the code is laid out.

## What a pull request needs

CI runs on every pull request and all checks must pass: build with no warnings, formatting, tests (none skipped), and coverage. The version in `Directory.Build.props` must match the release branch (see above). If you change behavior that riders or protocol readers rely on, update the matching docs page. Behavior is described in `docs/spec/`, so update the spec too. AI coding agents should read [AGENTS.md](AGENTS.md). The details are in [Contributing](https://VoltageSolutions.github.io/GameControllerForZwift/build/contributing/).
