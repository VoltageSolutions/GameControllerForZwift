# Roadmap

These are the next big pieces of work. They are plans, not promises, and the order may change. Ideas and help are welcome on the [Issues page](https://github.com/VoltageSolutions/GameControllerForZwift/issues).

## A fresh user experience

The app works, but its main page is a long list of drop-downs. The plan is a calmer, guided experience:

- A short first-run guide: pick a controller, pair with Zwift, test a button.
- An always-visible status for the controller (with battery) and for the Zwift connection.
- A simpler mapping view built around the Zwift functions, with sensible defaults and a reset.

The research behind this is in [UX concepts](design/ux-concepts.md).

## On-screen gamepad visualization

Show a picture of your controller that lights up as you press buttons, moves with the sticks and fills with the triggers. Clicking a part of the picture would let you map it. The art would match your controller family (Xbox-style, PlayStation-style, Switch-style) without using any brand logos.

The research and plan are in [Gamepad visualization](design/gamepad-visualization.md).

## A Zwift simulator for end-to-end tests

Today the network code is covered by unit and integration tests, and the final check is a live ride in Zwift. A small Zwift simulator would play Zwift's side of the conversation: discover the device over mDNS, connect, subscribe, send the RideOn handshake and the device information requests, and then check the button frames. That would let CI test the whole path from a simulated gamepad to the bytes Zwift receives.

## Better logging

- One place that configures all log providers (Event Log and file).
- Consistent categories, levels and structured messages across the app.
- Log retention, so old log files are cleaned up.
- A clear rule for what is logged by default and what only at Debug level.

## Docs move to Zensical

This site uses Material for MkDocs, which reaches end of life in May 2027. The plan is to move to Zensical, its successor from the same team. Pages are plain Markdown with Mermaid diagrams, so the move should be mostly configuration.
