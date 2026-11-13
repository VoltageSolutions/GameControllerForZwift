# Zwift LAN controller: findings and status

Branch: `zwift-lan-controller`. Live test date: 2026-11-12.

## 1. Goal

Let GameControllerForZwift act as a Zwift controller (shift, steer, menu navigation) over the LAN, without Bluetooth,
by emulating a Wahoo Direct Connect (DirCon) device: an mDNS advertisement plus a TCP GATT-over-DirCon server.

Detail documents:

- [zwift-lan-protocol-notes.md](zwift-lan-protocol-notes.md) - wire format, GATT layout, handshake.
- [captures/kickr-bike-capture-analysis.md](captures/kickr-bike-capture-analysis.md) (local only, not in git) - frame-by-frame analysis of the real-bike capture.
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
5. Wire the winning configuration into the WPF app (`NetworkService` and `CompositeOutputService` already exist; the current default is kickr/full) and add a Settings toggle.
6. Clean up: stale Zeroconf reference in the integration tests; the `.proto` files in `Network/Protos` are unused.

## 6. Code map

| Area | Location |
|---|---|
| DirCon protocol | `src/Infrastructure/GameControllerForZwift.Network/Dircon/`: `DirconMessage`, `BleUuid`, `GattModel` / `KickrBikeProfile`, `DeviceProfile` (`AdvertKind`, `GattKind`), `ZwiftControlProtocol`, `DirconSession`, `DirconServer`, `RideSimulator`, `DeviceIdentity`, `ZwiftRideButton` |
| mDNS | `Network/Mdns/MdnsAdvertiser` |
| Logging | `Network/Logging/FileLoggerProvider` (logs in `<exe dir>/logs`) |
| Core abstractions | `GameControllerForZwift.Core`: `IZwiftControllerOutput` / `ZwiftControllerState` (implemented by `NetworkService`), `ZwiftFunction` (see "App simplification") |
| Probe tool | `tools/ZwiftLanProbe` |
| Tests | `tests/GameControllerForZwift.Network.UnitTests` (capture replay tests) |

Build status: the solution builds and all tests pass as of the last build. Also fixed a pre-existing SDL2 PostBuild
path problem (`$(SolutionDir)` is undefined when building single projects).

### Probe CLI

```
ZwiftLanProbe [--name "<name>"] [--profile kickr|ftms|zwift] [--advert kickr|ftms|zwift]
              [--gatt full|nowahoo|minimal] [--manufacturer "<s>"] [--no-gearing] [--port N]
              [--masks bikecontrol|makinolo] [--max-gear N] [--analog-max N] [--verbose]
```

Interactive keys:

| Key | Action |
|---|---|
| l u r d, arrows | Left / Up / Right / Down (Enter = A, Backspace = B) |
| a b y z | A, B, Y, Z buttons |
| `.` `,` | Left shift up / down |
| `]` `[` | Right shift up / down |
| p o / P O | PowerUp and OnOff, left / right |
| h j | Hold Left / Right 2 s (one frame) |
| n N | Hold Left / Right 2 s, frame repeated every 100 ms |
| k K | Left / Right analog brake paddle (field 3) |
| `#` | Press raw bit index 0-31 |
| w / W | Toggle simulated ride (100 W / 80 rpm) / custom `watts,rpm` |
| g G | Gear up / down via `0x42` reports (unless `--no-gearing`) |
| c C | Zwift Click v1 plus / minus (`0x37`) |
| v V | Zwift Play right / left paddle (`0x07`) |
| s ? q | Status / help / quit |

Note: `ZwiftRideButton` now holds the confirmed table with Zwift labels (see "Button table confirmed"); the probe keeps the Makinolo table selectable with `--masks makinolo` for reference only.

## Breakthrough (2026-11-13)

Running the probe with `--preset bcride` (now the probe default, name `Voltage Emulator`) makes Zwift recognise us as a Zwift Ride. In its Ride tutorial the following were verified:

- Menus: Up/Down/Left/Right, A, B work.
- Steering works with the paddle analog entries of the `0x23` frame, not with the Left/Right button bits. Frame: field 1 = inverted button bitmap, field 3 repeated `{1: location, 2: zigzag sint32}`; location 0 = left paddle, 1 = right paddle, range -100..100. Steer = paddles outward: left -100 (steer left), right +100 (steer right).
- Braking = paddles inward: left +100, right -100.
- Frames are resent every 100 ms while held, followed by a neutral frame (buttons released, both paddles 0).
- Shifting was first observed working with `0x2000` (up) and `0x200` (down); the explanation is in "Button table confirmed" below (those were RS2 and LS2 in the correct table).

### Button table confirmed

The BikeControl table is correct; Makinolo's upper values are wrong for Zwift. `ZwiftRideButton` uses Zwift's labels: LEFT 0x1, UP 0x2, RIGHT 0x4, DOWN 0x8, A 0x10, B 0x20, Y 0x40 (power-up; hold = skip workout block), Z 0x80 (Ride On; hold = Ride On to 5 riders), LS1 0x100, LS2 0x200, LB 0x400, LPower 0x800, RS1 0x1000, RS2 0x2000, RB 0x4000, RPower 0x8000. In sequential shift style LS1/LS2 = easier gear and RS1/RS2 = harder gear. Analog: ZL = location 0, ZR = location 1; outward tilt steers (ZL negative = left, ZR positive = right), inward brakes (ZL positive, ZR negative), range -100..100. Source: support.zwift.com (shift-style-and-gear-range) plus live tests. See Test plan K in `zwift-lan-testing.md`.

## 7. Status 2026-11-13 (afternoon)

The probe default preset is now `bcride`, which presents as a Zwift Ride (a BikeControl clone):

- KICKR mDNS advert plus manufacturer data `4A0906B653` and the OBC uuid.
- GATT with FC82, `00000001-19CA`, `d273f680`, 1826, 180A and 180D.
- Device info "Zwift Ride": f1=515, f8=(2,1), f9=1, f10=8.
- RideOn reply `02 00`.

Zwift launches its Zwift Ride tutorial for this device.

**Verified**

- Menus: D-pad, A and B work.
- Steering: paddle outward (left loc0 -100 / right loc1 +100).
- Brake: paddle inward.
- Shifting in a real ride with the Victory as trainer: `0x2000` shifts up, `0x200` shifts down (`0x100` also shifts down).

**Open**

- Mask table: resolved, see "Button table confirmed".
- Y, Z, PowerUp and drop buttons are untested.
- Simulated power is not shown by Zwift in `bcride`; 2AD2 notifications are sent, suspect the 2ACC feature value.
- Partial steering and brake are untested.
- Reconnect after probe restart still fails.

## App simplification (2026-11-13)

Live test (Zwift on a tablet, app on a laptop): the emulated Zwift Ride works; steer, shift, D-pad holds and brake work, but even a light brake press gives a full stop. Decisions taken:

- Keyboard emulation is removed entirely (the Keyboard project and its integration tests, `CompositeOutputService`, `IOutputService`, `ILanOutputService`, `OutputOptions` / `OutputOptionsStore` / `settings.json`, the "Send to" card and the legacy `PerformActionAsync` / hold-time path in `NetworkService`). The emulated Zwift Ride is the only output and is always on; `DataIntegrator` only talks to `IZwiftControllerOutput`.
- `ZwiftFunction` is reduced to what a Zwift Ride provides: None, NavigateUp/Down/Left/Right (D-pad), Select (A), GoBack (B, Back/Menu), Powerup (Y; hold = skip workout block), RideOn (Z; hold = Ride On bombs), ShiftUp (RS1), ShiftDown (LS1), SteerLeft, SteerRight, Brake. `ZwiftRiderAction`, `ZwiftPlayerView` and the matching `InputMapping` properties and UI dropdowns are gone.
- Profile compatibility: `ZwiftFunctionJsonConverter` loads removed or unknown names (and unknown numbers) as None, `FTPBiasUp`/`FTPBiasDown` as ShiftUp/ShiftDown, and the legacy `RiderAction` function as RideOn when its `RiderAction` property was RideOn or absent (any other rider action loads as None). Legacy `PlayerView` / `RiderAction` JSON properties are ignored and never written.
- Default profile: D-pad = Navigate*, A = Select, B = GoBack, X = RideOn, Y = Powerup, LB = ShiftDown, RB = ShiftUp, left stick left/right = SteerLeft/SteerRight, left trigger = Brake.

### Brake scaling (no behaviour change)

Chain: SDL trigger axis (0..32767) -> `GetAnalogValue` = (raw - 1638) / (32767 - 1638), 0 inside the ~5% deadzone, clamped to 0..1 -> `ZwiftControllerState.Brake` -> `PaddleMath.Compute` = round(100 x brake), written as ZL = +b and ZR = -b (inward). It is linear and not binary. Values sent for trigger travel of 10% / 30% / 60% (raw 3277 / 9830 / 19660): brake 0.053 / 0.263 / 0.579, i.e. ZL/ZR = 5/-5, 26/-26, 58/-58. So the app sends a proportional value; the full stop on a light press is most likely how Zwift reads the brake paddle (any inward value treated as full brake, or a steep curve). To verify, send fixed partial values with the probe (`--partial N` with the brake keys, default 50) and watch whether Zwift's braking changes; a dedicated `--brake-test` sweep (ramp ZL/ZR inward 5, 10, 25, 50, 75, 100 while riding) would be the next step and is not implemented yet.
