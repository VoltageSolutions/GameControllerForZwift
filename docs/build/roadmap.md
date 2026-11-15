# Roadmap

These are the next big pieces of work. They are plans, not promises, and the order may change. Ideas and help are welcome on the [Issues page](https://github.com/VoltageSolutions/GameControllerForZwift/issues).

## A refreshed user interface

A calmer, more guided main page.

## Save mapping changes

Today mapping changes in the app apply right away but are not saved, so they reset to the defaults when the app restarts. The goal is to save them to your user profile.

## An on-screen controller view

A picture of your controller that lights up as you press buttons.

## A Zwift simulator for end-to-end tests

Today the network code is covered by unit and integration tests, and the final check is a live ride in Zwift. A small Zwift simulator would play Zwift's side of the conversation: discover the device over mDNS, connect, subscribe, send the RideOn handshake and the device information requests, and then check the button frames. That would let CI test the whole path from a simulated gamepad to the bytes Zwift receives.

## Better logging

- One place that configures all log providers (Event Log and file).
- Consistent categories, levels and structured messages across the app.
- Log retention, so old log files are cleaned up.
- A clear rule for what is logged by default and what only at Debug level.

## Code signing for releases

Release builds are not digitally signed yet, so Windows SmartScreen warns on first run. Code signing is in progress.

## Release notes for each release

Short, readable notes on what changed in every release.

## A screenshot or short GIF in the README

Show the app and a shifting demo right on the project page.

## A simpler install

Fewer steps between downloading the app and riding.

## Issue and pull request templates

Templates that ask for the details I need, such as the app version, the controller and the log file.
