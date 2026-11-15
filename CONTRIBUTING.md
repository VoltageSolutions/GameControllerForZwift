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

## Build and test

Build with the .NET 10 SDK on Windows (`dotnet build GameControllerForZwift.sln`). See [Build and test](https://VoltageSolutions.github.io/GameControllerForZwift/build/build-and-test/) for the full steps and [Architecture](https://VoltageSolutions.github.io/GameControllerForZwift/build/architecture/) for how the code is laid out.

## What a pull request needs

CI runs on every pull request and all checks must pass: build with no warnings, formatting, tests (none skipped), and coverage. If you change code that goes to `main`, bump `<Version>` in `Directory.Build.props`. If you change behavior that riders or protocol readers rely on, update the matching docs page. The details are in [Contributing](https://VoltageSolutions.github.io/GameControllerForZwift/build/contributing/).
