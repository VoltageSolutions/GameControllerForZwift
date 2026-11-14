# Zwift LAN controller: findings and status

Branch: `zwift-lan-controller`. Live test date: 2026-11-12.

## 1. Goal

Let GameControllerForZwift act as a Zwift controller (shift, steer, menu navigation) over the LAN, without Bluetooth,
by emulating a Wahoo Direct Connect (DIRCON) device: an mDNS advertisement plus a TCP GATT-over-DIRCON server.

> **DIRCON = Wahoo Direct Connect**: Bluetooth LE GATT tunnelled over TCP. In code the types are called `DirectConnect*` (namespace `GameControllerForZwift.Network.DirectConnect`); this document and the protocol notes keep the short name "DirCon / DIRCON" where they describe the wire protocol or other projects' code.
>
> **Status (2026-11-13):** the code now offers one device presentation only, the Zwift Ride (`ZwiftRideProfile`). The KICKR, FTMS, Click, Play and `_zwift-protocol` experiments described below are historical: their code (and the unused `.proto` files) was removed and can be found in git history before this refactor.

Detail documents:

- [zwift-lan-protocol-notes.md](zwift-lan-protocol-notes.md) - wire format, GATT layout, handshake.
- Frame-by-frame analysis of the real-bike capture (local capture analysis, not in the repository).
- [zwift-lan-testing.md](zwift-lan-testing.md) - how to run the probe and test against Zwift.

## 2. Sources examined

| Source | Verdict |
|---|---|
| bikecontrol (Dart) | The LAN/DirCon emulator, mDNS and handshake code are closed source (`prop/` is stubbed in the public repo). Useful only for Zwift Ride button masks and keepalive hints. |
| openbikecontrol-protocol | A separate open protocol (`_openbikecontrol._tcp`). Zwift does not support it. Not useful. |
| qdomyos-zwift (`src/devices/dircon/*`) | A real open DirCon server. Source of the verified framing. |
| User's Wireshark capture, real KICKR BIKE SHIFT with Zwift | Ground truth. Stored locally in `docs/captures/` (not in git, contains private data). |

## 3. Protocol summary

Brief only; see the protocol notes for detail.

- **Discovery:** mDNS service `_wahoo-fitness-tnp._tcp`, TXT keys `ble-service-uuids`, `mac-address`, `serial-number`.
- **Framing:** 6-byte DirCon header: version (=1), msgId, seq, resp, length (uint16 big endian).
- **Messages 0x01-0x06:** discover services, discover characteristics, read, write, subscribe (0x05), notification (0x06).
- **Sequence numbers:** a response echoes the request seq; notifications use seq 0.
- **Notifications:** only send after the client has subscribed (0x05); queue earlier ones.
- **Zwift behaviour:** it pipelines discovery, subscribe and RideOn requests in a single burst.
- **Handshake:** Zwift writes `"RideOn" 02 04` (current Zwift; the capture had `02 03`) to characteristic
  `00000003-19CA-4651-86E5-FA29DCDD09D1`. The reply is `"RideOn" 02 02` on `00000004-...`, sent only after 0004 is subscribed.
- **Follow-ups (answered from the capture):** `00 08 xx` -> `0x3c` info; `41 08 05` -> `3e 08 41 10 02`; `0x3f` setup -> `40 08 00 10 nn`.

### Root causes of the original branch's failure

1. Hard-coded RideOn `02 03` reply instead of matching the client.
2. Reply possibly sent before the client had subscribed.
3. Wrong characteristic property flags.
4. Incorrect seq handling.
5. mDNS A record used the querier's IP rather than the host's.
6. Mismatched button masks.

## 4. Live test results (ZwiftLanProbe)

### 4.1 Discovery and handshake

| Test | Result |
|---|---|
| Advertise with exact real-bike (KICKR) TXT | Zwift lists the device under Controls (and under Resistance/Power). Handshake completes. |
| Custom name ("Voltage Emulator") with KICKR advert | Name is displayed correctly. |
| Advertise FTMS-style TXT, or FC82-only TXT | Zwift does not list the device at all, although it does browse `_wahoo-fitness-tnp`. |
| Bisect: `--advert kickr` with `--gatt full` and `--gatt nowahoo` | Both pair. `--gatt minimal` not yet tested. |

### 4.2 Menu navigation (works)

Ride keypad frames are `0x23` plus protobuf field 1, an inverted uint32 bitmap. Masks below are the BikeControl table (with the old placeholder labels), which is the correct one; see "Button table confirmed" below for Zwift labels:

| Button | Mask | Button | Mask |
|---|---|---|---|
| Left | 0x1 | ShiftUpL | 0x100 |
| Up | 0x2 | ShiftDnL | 0x200 |
| Right | 0x4 | PowerUpL | 0x400 |
| Down | 0x8 | OnOffL | 0x800 |
| A | 0x10 | ShiftUpR | 0x1000 |
| B | 0x20 | ShiftDnR | 0x2000 |
| Y | 0x40 | PowerUpR | 0x4000 |
| Z | 0x80 | OnOffR | 0x8000 |

Result: Up/Down/Left/Right menu navigation, A = select and B = back all work.

### 4.3 Shifting when the emulated KICKR is the Resistance device (works, via gear reports)

- Zwift shows KICKR-style gears as "front-rear" (1-1 ... 1-5).
- Gear changes work **only** through `0x42` gear reports: `42 32 02 08 <front>` and `42 32 02 20 <rear>`.
- This matches a real KICKR BIKE, which shifts on the device and reports the resulting gear; Zwift then writes `0x3f` messages to the trainer.
- Simulated power and cadence (100 W / 80 rpm via 2A63, 2AD2 and `0x03`) works.

### 4.4 Shifting when a different trainer is Resistance (does not work so far)

Setup: the user's Jetblack Victory (LAN, supports Zwift virtual shifting, works with a real Zwift Click v1) is the
Resistance device and our emulator is Controls only.

- Zwift shows single-number gears.
- None of our shift formats changed gear: Ride shift bits, `0x42` gear reports, Click v1 `0x37` frames, Play `0x07` paddle frames.
- Steering (Left/Right, single and repeated-hold) and brake (analog paddles, field 3) showed no visible effect.
- **Caveat:** the user was not pedalling. Whether Zwift ignores shifting and steering at zero cadence is the open question.
  The control test (real Click, no pedalling) is pending.

### 4.5 Other observations

- Zwift also browses `_zwift-protocol._tcp`. We found no implementation of it anywhere; a possible future lead.
- **Known issue:** after quitting and restarting the probe, Zwift fails to reconnect until Zwift itself is restarted.
- Windows Firewall prompts on first run; allowing it was sufficient (see the testing doc for explicit rules).

## 5. Next steps (ordered)

1. Control test with a real Zwift Click and no pedalling, to learn whether Zwift ignores shifting at zero cadence.
2. Pedalling test with the probe as Controls only (`--gatt nowahoo`, then `minimal`), trying all shift formats plus steering and brake.
3. If Zwift never accepts controller shifting while another trainer is Resistance, either:
   - bridge mode: our app is Zwift's Resistance/power device and forwards power and resistance to the real trainer (bikecontrol has a "proxy" concept), or
   - investigate `_zwift-protocol._tcp`.
4. Fix the reconnect issue.
5. Done: the winning configuration (the Zwift Ride profile) is wired into the WPF app through `ZwiftRideOutputService`; there is no Settings toggle.
6. Done: the unused `.proto` files and the experiment code were removed.

## 6. Code map

| Area | Location |
|---|---|
| Direct Connect (DIRCON) | `src/Infrastructure/GameControllerForZwift.Network/DirectConnect/`: `DirectConnectMessage` (+ `DirectConnectMessageId`, `DirectConnectResponseCode`), `DirectConnectSession`, `DirectConnectServer`, `BleUuid`, `GattModel` |
| Zwift Ride device | `Network/ZwiftRide/`: `ZwiftRideProfile` (advertisement, manufacturer data, GATT tree), `ZwiftControlProtocol` (handshake, replies, keypad frames), `ZwiftRideButton`, `DeviceIdentity`, `IZwiftRideLink` |
| mDNS | `Network/Mdns/MdnsAdvertiser` |
| Output service | `Network/Output/ZwiftRideOutputService` (implements `IOutputService`: owns the server + mDNS, the 100 ms send loop, button map, battery) and `PaddleMath` |
| Logging | `src/Presentation/GameControllerForZwift/Logging/FileLoggerProvider` (app logs in `<exe dir>/logs`, next to the Event Log provider). The Network library only depends on `Microsoft.Extensions.Logging.Abstractions` |
| Core abstractions | `GameControllerForZwift.Core`: `IOutputService` / `OutputState` (implemented by `ZwiftRideOutputService`), `ZwiftFunction` (see "App simplification") |
| Probe tool | `tools/ZwiftLanProbe` |
| Tests | `tests/GameControllerForZwift.Network.UnitTests` (capture replay tests), `tests/GameControllerForZwift.Network.IntegrationTests` |

Build status: the solution builds and all tests pass as of the last build. Also fixed a pre-existing SDL2 PostBuild
path problem (`$(SolutionDir)` is undefined when building single projects).

### Probe CLI

```
ZwiftLanProbe [--name "<name>"] [--port N] [--partial 1..100] [--hold-ms N] [--log-file <path>] [--verbose]
```

Interactive keys (the probe only offers the Zwift Ride presentation; `?` prints the table, full list in [zwift-lan-testing.md](zwift-lan-testing.md)):

| Key | Action |
|---|---|
| `=` `0` / `-` `9` | RS1 / RS2 (harder gear), LS1 / LS2 (easier gear) |
| `[` `]` | LB / RB |
| `q w e r` | Steer full left / partial left / partial right / full right |
| `a s d f` | Brake left full / left partial / right partial / right full |
| arrows, Enter, Backspace | Left / Up / Right / Down, A, B |
| `y` `Y` / `z` `Z` | Y tap / hold, Z tap / hold |
| `#` | Press raw bit index 0-31 |
| `@` | Analog lab (`loc,value[,ms[,buttonsHex]]`) |
| `i` `?` Ctrl+C | Status / help / quit |

Earlier experiment keys (simulated ride, 0x42 gear, Click and Play frames, `--zp-*`) are gone with the code they drove.

- Simulated power is not shown by Zwift in `bcride`; 2AD2 notifications are sent, suspect the 2ACC feature value.
- Partial steering and brake are untested.
- Reconnect after probe restart still fails.

## App simplification (2026-11-13)

Live test (Zwift on a tablet, app on a laptop): the emulated Zwift Ride works; steer, shift, D-pad holds and brake work, but even a light brake press gives a full stop. Decisions taken:

- Keyboard emulation is removed entirely (the Keyboard project and its integration tests, `CompositeOutputService`, `IOutputService`, `ILanOutputService`, `OutputOptions` / `OutputOptionsStore` / `settings.json`, the "Send to" card and the legacy `PerformActionAsync` / hold-time path in the old `NetworkService` (now `ZwiftRideOutputService`)). The emulated Zwift Ride is the only output and is always on; `DataIntegrator` only talks to `IOutputService`.
- `ZwiftFunction` is reduced to what a Zwift Ride provides: None, NavigateUp/Down/Left/Right (D-pad), Select (A), GoBack (B, Back/Menu), Powerup (Y; hold = skip workout block), RideOn (Z; hold = Ride On bombs), ShiftUp (RS1), ShiftDown (LS1), SteerLeft, SteerRight, Brake. `ZwiftRiderAction`, `ZwiftPlayerView` and the matching `InputMapping` properties and UI dropdowns are gone.
- Profile compatibility: `ZwiftFunctionJsonConverter` loads removed or unknown names (and unknown numbers) as None, `FTPBiasUp`/`FTPBiasDown` as ShiftUp/ShiftDown, and the legacy `RiderAction` function as RideOn when its `RiderAction` property was RideOn or absent (any other rider action loads as None). Legacy `PlayerView` / `RiderAction` JSON properties are ignored and never written.
- Default profile: D-pad = Navigate*, A = Select, B = GoBack, X = RideOn, Y = Powerup, LB = ShiftDown, RB = ShiftUp, left stick left/right = SteerLeft/SteerRight, left trigger = Brake.

### Brake scaling (no behaviour change)

Chain: SDL trigger axis (0..32767) -> `GetAnalogValue` = (raw - 1638) / (32767 - 1638), 0 inside the ~5% deadzone, clamped to 0..1 -> `OutputState.Brake` -> `PaddleMath.Compute` = round(100 x brake), written as ZL = +b and ZR = -b (inward). It is linear and not binary. Values sent for trigger travel of 10% / 30% / 60% (raw 3277 / 9830 / 19660): brake 0.053 / 0.263 / 0.579, i.e. ZL/ZR = 5/-5, 26/-26, 58/-58. So the app sends a proportional value; the full stop on a light press is most likely how Zwift reads the brake paddle (any inward value treated as full brake, or a steep curve). To verify, send fixed partial values with the probe (`--partial N` with the brake keys, default 50) and watch whether Zwift's braking changes; a dedicated `--brake-test` sweep (ramp ZL/ZR inward 5, 10, 25, 50, 75, 100 while riding) would be the next step and is not implemented yet.

## Future work

- Audit and improve logging app-wide: consistent categories and levels, structured messages, a single place that configures providers (Event Log and file), log retention, and a decision on what is logged by default versus at Debug.
- Bluetooth: 8BitDo SN30 Pro+ is not detected over Bluetooth (works over USB) - investigate SDL2 HIDAPI/Bluetooth hints and device mapping.
