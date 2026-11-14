# Roadmap

These are the next big pieces of work. They are plans, not promises, and the order may change. Ideas and help are welcome on the [Issues page](https://github.com/VoltageSolutions/GameControllerForZwift/issues).

## A refreshed user interface

A calmer, more guided main page.

## An on-screen controller view

A picture of your controller that lights up as you press buttons.

## A Zwift simulator for end-to-end tests

Today the network code is covered by unit and integration tests, and the final check is a live ride in Zwift. A small Zwift simulator would play Zwift's side of the conversation: discover the device over mDNS, connect, subscribe, send the RideOn handshake and the device information requests, and then check the button frames. That would let CI test the whole path from a simulated gamepad to the bytes Zwift receives.

## Better logging

- One place that configures all log providers (Event Log and file).
- Consistent categories, levels and structured messages across the app.
- Log retention, so old log files are cleaned up.
- A clear rule for what is logged by default and what only at Debug level.
