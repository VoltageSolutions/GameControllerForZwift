# Additional learnings

!!! tldr "TL;DR"
    Getting to a working Zwift Ride presentation taught us a lot about how Zwift sees network devices. The identity a device claims shapes how Zwift uses it, and Zwift understands several other controller formats too. This page collects those findings so that anyone can pick them up and build on them. Each item ends with ideas for where it could go next.

## Identity shapes how Zwift uses a device

A device that presents itself like a Wahoo KICKR bike, with a trainer role in its device information, is treated by Zwift as a smart bike that manages its own gears. Zwift then shows gears in "front-rear" form (for example 1-1 to 1-5). The device reports gear changes itself with `0x42` gear report messages:

| Message | Meaning |
|---|---|
| `42 32 02 08 <front>` | Current front gear |
| `42 32 02 20 <rear>` | Current rear gear |

This works well when the same device is also the trainer: it shifts, reports the new gear, and Zwift sends resistance updates back. When a separate trainer is the Resistance device, a pure controller identity (the Zwift Ride) is the one that fits, which is why the app uses it.

**Where this could go next:** a "smart bike" mode for riders whose trainer has no virtual shifting. The app could act as the Power Source and Resistance device, run its own gear table, report gears with `0x42`, and pass power and resistance through to the real trainer.

## Zwift Click and Zwift Play use simpler frames

Zwift supports other controllers with their own button formats:

| Controller | Opcode | Format |
|---|---|---|
| Zwift Click | `0x37` | A short frame where one byte goes to 0 for "plus" and another for "minus". |
| Zwift Play | `0x07` | A per-side frame for the left and right Play controllers. |

These are documented by the community projects listed in the [credits](credits.md). The Zwift Ride format covers more inputs (D-pad, four face buttons, analog paddles), so it is the best match for a full game controller.

**Where this could go next:** a "Click mode" for very simple inputs such as a single foot pedal or a two-button remote, where a smaller identity may be a better fit than a full Ride.

## Power data belongs to the trainer

The app's GATT tree includes a Fitness Machine service, but a device presenting as a Zwift Ride is used for its controls only. Zwift does not take power or cadence from it, so the rider's trainer stays the Power Source. That is exactly the split riders want.

**Where this could go next:** if a future mode needs to relay power (for example the smart bike idea above), it would use a trainer-style identity rather than the Ride identity.

## Zwift also browses `_zwift-protocol._tcp`

Besides `_wahoo-fitness-tnp._tcp`, Zwift sends mDNS queries for a second service type, `_zwift-protocol._tcp`. We have not seen a public description of what Zwift expects behind it.

**Where this could go next:** watch for any public documentation of this service. It may be a more direct way for third-party devices to talk to Zwift on the local network.

## Different devices describe themselves differently

The device information page (data object id 0, see [Device information](zwift-protocol.md#device-information)) is where a device says what it is. Comparing replies is very instructive:

| Device | protocolVersion | deviceCapabilities (type, caps) | Follow-up requests from Zwift |
|---|---|---|---|
| KICKR-style smart bike | 514 (`0x0202`) | (1, 36), (2, 0): trainer and user input | Controller input config (1024), battery (771), gear index config (528) |
| Smart trainer with Zwift virtual shifting | 515 (`0x0203`) | (1, 209), (8, 1), (4, 0): trainer, Wi-Fi, sensor relay | Client/server config (16), sensor relay page (1280), sensor relay configuration (`0x44`) |
| The app, presenting as a Zwift Ride | 515 (`0x0203`) | (2, 1): user input only | Client/server config (16), controller input config (1024), battery (771). Other requests, such as 1280, get no reply. |

Device types from Zwift's protocol: 1 = cycling trainer, 2 = user input device, 3 = treadmill, 4 = sensor relay, 5 = heart rate monitor, 6 = power meter, 7 = cadence sensor, 8 = Wi-Fi.

Two patterns stand out:

- For the smart bike and the trainer, `protocolVersion` matches the two bytes after `RideOn` in their handshake reply (514 with `02 02`, 515 with `02 03`). The Ride presentation replies with `02 00`.
- Zwift picks its follow-up questions from the capability list, so the list is a good place to look when a new device type behaves unexpectedly.

**Where this could go next:** the meaning of each capability bit is still open. A small, careful study of these values would help every project that builds Zwift-compatible devices.
