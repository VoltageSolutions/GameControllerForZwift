# Zwift LAN (DirCon) controller emulation - notes from `bikecontrol`

Source repo: `$HOME/mnt/bikecontrol` (Dart/Flutter, read-only). Paths below are relative to it.
Convention: `file:line` citations. **UNVERIFIED** = not in the repo, inferred or from general DirCon knowledge.

## 0. Critical caveat: the DirCon/TCP/mDNS core is NOT in this repo

The public checkout ships `prop_public/` with **stubs** for everything that touches the wire:

- `prop_public/lib/emulators/dircon_emulator.dart:1-5` - "This is a stub - contact me if you need the full implementation"; `startServer` only flips flags (`:92-100`), `boundPort` returns null (`:68`), `processCharacteristic` returns false (`:90`).
- `prop_public/lib/emulators/transporter/network_transporter.dart:1,11-14` - `sendCharacteristicNotification` and `handleIncomingData` are empty. (`NetworkTransporter` is the class that would implement the DirCon framing.)
- `prop_public/lib/utils/resilient_tcp_server.dart:1-5` - stub TCP server.
- `prop_public/lib/mdns/*.dart` - stub mDNS advertisers (OS `nsd`, in-process responder, dns_sd).
- `prop_public/lib/utils/shared.dart:5-15` - `SharedLogic.handleWriteRequest` (the RideOn handshake responder) returns null.
- `prop_public/lib/emulators/definitions/zwift_emulator_definition.dart:1`, `zwift_click_definition.dart:1-4` - stubs (the app even calls `ZwiftEmulatorDefinition(asRide: ...)` at `lib/bluetooth/devices/zwift/ftms_mdns_emulator.dart:26-29`, a parameter the stub lacks).
- `prop_public/lib/utils/constants.dart:1-4` - **UUIDs, handshake bytes and `BikeControlMdnsMarkers` are FAKE placeholders** (`...0000A1`, all-zero RIDE_ON, etc.). Do not use them.
- `prop/dircon/dircon_framing.dart` (`DirconMessage`, `DirconFrameAccumulator`) is imported by `test/bluetooth/devices/openbikecontrol/obc_mdns_first_notification_test.dart:7` but **does not exist anywhere in the checkout** (`prop_public/lib/dircon/` only contains `dircon_trainer_transport.dart`, also a stub).

So the exact 6-byte DirCon header layout, service/characteristic discovery payloads, the Zwift RideOn handshake reply and the Zwift-side mDNS TXT values are **not recoverable from this repo**. What the repo does give for certain:
real Zwift GATT UUIDs (`lib/.../zwift/constants.dart`), the Ride keypad protobuf and button bitmap, how press/release/keepalive frames are built, the mDNS service type, the OBC DirCon TXT record, the port numbers, and a few DirCon facts via tests. Everything else below is flagged UNVERIFIED.

## 1. mDNS advertisement

| Item | Value | Source |
|---|---|---|
| Service type | `_wahoo-fitness-tnp._tcp` (Wahoo "DirCon") | `lib/bluetooth/wifi_trainer_scanner.dart:40`; `lib/bluetooth/devices/openbikecontrol/obc_mdns_emulator.dart:219`; `integration_test/virtual_shifting_macos_test.dart:68` |
| Library | App-side: `AdvertisedService` + `ServiceAdvertiser` abstraction with 3 backends: OS responder via Flutter `nsd` plugin, an in-process mDNS responder (desktop+Android), and an iOS dns_sd bridge. Bodies stubbed. | `prop_public/lib/mdns/service_advertiser.dart:1-6,14-40,84-93`; `dnssd_service_advertiser.dart:1-5` |
| Instance name (Zwift LAN controller / trainer bridge) | `BikeControl`, or for a bridged trainer `BikeControl <trailing-id>` (e.g. "KICKR CORE 73D3" -> "BikeControl 73D3"); fallbacks `Bridge`, `Virtual Trainer`. Chosen so it does not contain / is not contained in the real trainer's name. Default stub name `BikeControl`. | `lib/bluetooth/devices/proxy/proxy_device.dart:576-588`; `prop_public/.../dircon_emulator.dart:88` |
| Instance name (OBC DirCon endpoint) | `BikeControl` | `obc_mdns_emulator.dart:218` |
| Port | Zwift/FTMS DirCon emulator instances are auto-assigned from **36868** upward (`_portNumberIterator = 36868`, `_ownPort = preferredPort ?? _portNumberIterator++`). "Standard Wahoo DirCon port" is **36866** (used only for TrainerRoad/"Other" via `preferStandardPort`; named apps such as Zwift keep the auto port). OBC endpoint prefers **36867** and walks up on contention. The SRV record carries the actually-bound port, so always advertise the bound port. | `dircon_emulator.dart:30,40,44-46`; `zwift_unlock.dart:27-29`; `test/bluetooth/proxy/trainerroad_dircon_port_test.dart:1-60`; `openbikecontrol_device.dart:68-73`; `obc_mdns_emulator.dart:195-197` |
| TXT `mac-address` | ASCII string (real value hidden behind `BikeControlMdnsMarkers.macAddress`, fake in public repo; looks like a UUID-ish/MAC string). Required: Tacx drops a peripheral without it. OBC variant: `00:00:00:00:00:01` placeholder (`obcMacAddress`). | `ftms_mdns_emulator.dart:106`; `proxy_device.dart:663-667`; `connection.dart:1176-1178`; `constants.dart(prop):44-49` |
| TXT `serial-number` | ASCII, **all digits, 9 chars, parsed by clients as uint64**: `mdnsSerialNumber(seed)` = `h=0; for c in seed.codeUnits: h=(h*31+c)&0x7fffffff; return h.toString().padLeft(9,'0')`. Missing serial makes MyWhoosh throw. OBC variant hardcodes `1234567890`. | `prop_public/lib/utils/constants.dart:64-73`; `ftms_mdns_emulator.dart:107-109`; `connection.dart:1176`; `integration_test/virtual_shifting_macos_test.dart:91-92`; `obc_mdns_emulator.dart:226` |
| TXT `ble-service-uuids` | Comma-separated list of service UUIDs of what the endpoint serves. Short forms appear as bare `1826` for Tacx or `0x1826` for Rouvy/Zwift/MyWhoosh/TPV ("Rouvy (and Zwift, MyWhoosh, TPV) parse the `0x` form and silently drop a bare one"). Trainer bridge advertises FTMS `1826` (test asserts it contains "1826"). OBC variant advertises the full 128-bit `d273f680-d548-419d-b9d1-fa0472345229`. **The exact string for a Zwift controller (FC82 / 0000fc82-0000-1000-8000-00805f9b34fb?) is not in the repo** - built inside the stubbed emulator from `BleDefinition.advertiseServiceUUIDs`/`serviceUUIDs` and `bareShortServiceUuids`. UNVERIFIED: use `0xFC82` (Zwift reads `0x` form) or the full 128-bit form. | `proxy_device.dart:649-654` (doc of `bareShortServiceUuidsFor`); `wifi_trainer_scanner.dart:195-213`; `obc_mdns_emulator.dart:225`; `dircon_emulator.dart:58-59` |
| TXT `manufacturer-data` | OBC variant only: marker constant `0000000000` (placeholder). Not seen on Zwift entries. | `obc_mdns_emulator.dart:228-230` |
| TXT `product-id` | Tacx only; not for Zwift. | `lib/utils/keymap/apps/tacx.dart:35` |
| Host label | In-process responder uses a stored random hostname label to avoid `BikeControl.local` collisions. | `obc_mdns_emulator.dart:204-216` |
| IPv4 only? | Rouvy requires an IPv4-only TCP listener (`forceIPv4`); Zwift not. Pick the real LAN interface IPv4, not VPN/virtual/link-local. | `proxy_device.dart:594-599` (`needsIPv4For`); `obc_mdns_emulator.dart:176-185` |

**Controller vs trainer classification.** The repo never shows a TXT flag. Classification is by *which services are served over the DirCon connection* (and advertised in `ble-service-uuids`):
- Trainer bridge = FTMS `1826` / CPS `1818` / etc. (`ProxyDevice.proxyServiceUUIDs`, `proxy_device.dart:42-50`).
- Zwift controller = the Zwift **Ride** custom service `0000fc82-0000-1000-8000-00805f9b34fb` ("adding the Zwift *Ride* service (`0000fc82-...`)" to the FitnessBikeDefinition, `proxy_device.dart:528-531`). It can ride alongside the trainer service set on the same endpoint when Zwift is the app (`_attachesZwiftController`, `proxy_device.dart:534-537`) or alone ("emulator starts standalone so Zwift sees the controller right away", `zwift_unlock.dart:261-263`).
- The service the Zwift controller definition exposes: `ZwiftEmulatorDefinition.serviceUUIDs = [ZWIFT_RIDE_CUSTOM_SERVICE_UUID]` with characteristics RX(write), ASYNC(notify), TX(notify) - stubbed but structure is real (`zwift_emulator_definition.dart:13-27`; the UUIDs there are fake placeholders, real ones in sec. 3).
- Rouvy: refuses to pair a trainer bridge that also carries a controller service; wants the controller on a separate endpoint (the "Click" server, instance name `BikeControl`) (`zwift_unlock.dart:263-268` (also :156)). Name collisions: two `_wahoo-fitness-tnp._tcp "BikeControl"` records collide and the app lists neither (`zwift_unlock.dart:60-66`); the bridge uses `BikeControl Sensors` in that case (`connection.dart:719-723`).
- The `emulatesZwiftRide` flag (true for Zwift) makes emulators present as a Ride, i.e. Ride keypad protobuf (`zwift.dart:10`; `ride_controller_press.dart:434`).

## 2. TCP server and DirCon framing

Port: see sec. 1. One client at a time is the norm (`ResilientTcpServer` "single-client TCP server with dual-stack listening, reconnect-supersede and port-fallback", `resilient_tcp_server.dart:1-5`, `allowConcurrentClients=false` default `:14`). Zwift considers the controller *connected* as soon as a TCP client connects in the OBC path (`obc_mdns_emulator.dart:365-372`); for the bridge, "the bridge reports the client as connected" upon TCP connect (`virtual_shifting_macos_test.dart:104-111`).

### Message IDs (real values, repo)
`lib/bluetooth/devices/zwift/ftms_mdns_emulator.dart:173-192` (identical copy in `rouvy_mdns_emulator.dart:160-179`; the prop stub adds `DC_MESSAGE_ECHO = 0x07` at `constants.dart:25`, UNVERIFIED if real):

| ID | Meaning |
|---|---|
| 0x01 | Discover Services |
| 0x02 | Discover Characteristics |
| 0x03 | Read Characteristic |
| 0x04 | Write Characteristic |
| 0x05 | Enable Characteristic Notifications |
| 0x06 | Characteristic Notification (unsolicited, server->client) |

Response codes (`ftms_mdns_emulator.dart:173-184`): 0 OK, 1 unknown message type, 2 unexpected error, 3 service not found, 4 characteristic not found, 5 operation not supported, 6 write failed invalid size, 7 unknown protocol version.

### Header layout
The repo's `DirconMessage(version, msgId, seqNum, respCode, body).encode()` and `DirconFrameAccumulator().addBytes(bytes)` (stream reassembler) exist only as test usage (`obc_mdns_first_notification_test.dart:56-80`), which confirms the field set `{version, msgId, seqNum, respCode, body}` and that `version: 1` is used in requests. The byte layout is **not in the repo**. Standard Wahoo DirCon (UNVERIFIED, from general DirCon knowledge, widely implemented identically):

```
offset 0  : uint8  version      (= 1)
offset 1  : uint8  msgId        (0x01..0x06 above)
offset 2  : uint8  seqNum       (client echoes request seq in the response; unsolicited notifications use 0 - UNVERIFIED)
offset 3  : uint8  respCode     (0 in requests and in notifications; result code in responses)
offset 4-5: uint16 length       BIG-endian, = number of body bytes after the header
offset 6.. : body
```

Body layouts (UNVERIFIED except where marked; UUIDs are **16 raw bytes, big-endian, in the canonical textual order**, no dashes - confirmed by the test helper `_uuidBytes` which hex-decodes the dashed string in order, `obc_mdns_first_notification_test.dart:14-17`):

- Discover services: req body empty; resp body = N x 16-byte service UUIDs.
- Discover characteristics: req body = 16-byte service UUID; resp body = service UUID (16) + N x [char UUID (16) + properties (1 byte bitmask: bit0 read, bit1 write, bit2 notify; bit3 indicate in some impls)].
- Read characteristic: req = char UUID(16); resp = char UUID(16) + value.
- Write characteristic: req body = char UUID(16) + data - CONFIRMED (`obc_mdns_first_notification_test.dart:54-61`: `body: [...appInfoUuid, ..._appInfo]`). Resp = write response frame with same msgId (0x04) (test asserts `frames[0].msgId == WRITE`, `:81`), body presumably = char UUID(16) (UNVERIFIED).
- Enable notifications: req = char UUID(16) + 1 byte (0/1); resp = char UUID(16) (+ maybe echo). UNVERIFIED.
- Notification (0x06): body = char UUID(16) + data - CONFIRMED (`obc_mdns_first_notification_test.dart:82-87`: `frames[1].body == [...uuidBytes(BUTTON_STATE_UUID), MSG_TYPE_BUTTON_STATE]`). respCode defaults to 0 = `DC_RC_REQUEST_COMPLETED_SUCCESSFULLY` (`lib/../prop_public/lib/emulators/ble_definition.dart:36-42`).
- Routing: the server is generic over a `BleDefinition` (services, `getCharacteristics(serviceUuid)`, `onWriteRequest`, `onReadRequest`, `onEnableNotificationRequest`, `sendCharacteristicNotification`) (`ble_definition.dart:10-47`), i.e. the TCP side is a literal tunnel of GATT operations. TCP data may arrive fragmented/coalesced: use a frame accumulator that buffers until `6+length` bytes.
- Incoming-data hook: `NetworkTransporter.handleIncomingData(List<int>)` is where parsing happens (`obc_mdns_emulator.dart:386-391`); outgoing via `sendCharacteristicNotification(charUuid, data)` (`:508-512`).
- UNVERIFIED: whether the Zwift endpoint answers an unsolicited "first notification" - for TrainingPeaks Virtual (OBC DirCon) a button-state frame naming no buttons is sent right after App Info, because the app's spinner waits for a first notification (`obc_mdns_emulator.dart:476-483`). For Zwift, the periodic keepalive (sec. 6) fills that role.

## 3. GATT model exposed

Real UUIDs (`lib/bluetooth/devices/zwift/constants.dart:200-206`):

| Name | UUID |
|---|---|
| Zwift custom service (Play/Click/legacy) | `00000001-19CA-4651-86E5-FA29DCDD09D1` (short `0001`) |
| Zwift **Ride** custom service | `0000fc82-0000-1000-8000-00805f9b34fb` (short `fc82`) |
| ASYNC (controller notifications; Zwift subscribes) | `00000002-19CA-4651-86E5-FA29DCDD09D1` |
| SYNC RX (Zwift writes commands here) | `00000003-19CA-4651-86E5-FA29DCDD09D1` |
| SYNC TX (responses; indicate) | `00000004-19CA-4651-86E5-FA29DCDD09D1` |
| Zwift BLE manufacturer id | 2378 = 0x094A (`constants.dart:208`) |
| Device-type byte in manufacturer data | Click 0x09; Play L 0x03 / R 0x02 / FW2 0x0E; **Ride R 0x07 / L 0x08**; Click v2 R 0x0A / L 0x0B ("unconfirmed") (`constants.dart:214-229`) |

**DirCon (LAN) path.** The Zwift controller definition serves the service `0000fc82-...` with three characteristics 00000003 (write), 00000002 (notify), 00000004 (notify) (`zwift_emulator_definition.dart:13-27`; structure only - UUID constants are fake there, but `ftms_mdns_emulator.dart:22` hardcodes the real ASYNC UUID `00000002-19CA-4651-86E5-FA29DCDD09D1` and that is what button frames are pushed on, `:145,152`). The real TX characteristic properties (notify vs indicate) are UNVERIFIED for the LAN path. In DirCon discovery you should return FC82 as the service UUID (alongside FTMS `1826`/CPS `1818` if you also act as a trainer - not required for shifting-only, see open questions).

**BLE path (for reference, fully visible)** `lib/bluetooth/devices/zwift/zwift_emulator.dart:203-268`:
- `180A` Device Information (not on Windows): 2A29 `BikeControl`, 2A25 `09-B48123283828F1337`, 2A27 `A.0`, 2A26 app version - all read-only.
- `180F` Battery: 2A19 read+notify; read handler returns `[100]` (`:153-160`).
- Zwift custom service `00000001-19CA-...`: `...0002` notify; `...0003` writeWithoutResponse; `...0004` read+indicate; `...0005` notify; `...0006` indicate+read+writeWithoutResponse+write.
- Advertised service UUID: `fc82`, local name `KICKR BIKE PRO 1337` (or `BikeControl` for Rouvy, which also advertises the OBC service `d273f680-...`) (`:288-302`). Note FC82 is advertised but the GATT service hosting the characteristics is `00000001-...` in this BLE code - UNVERIFIED whether Zwift accepts that; the LAN definition uses FC82 for the service.

**FTMS/power alongside.** When bridging a real trainer the shared emulator also serves FTMS 1826 / CPS 1818 / HR 180D (`proxy_device.dart:42-50`, `proxy_device.dart:950-960`); the Zwift controller is attached to that same composite only when the selected app is Zwift. For a pure controller port, FC82 only is what `zwift_unlock.dart:261-263` calls "standalone". Whether Zwift (as "Controller", not "Power Source") needs anything else is UNVERIFIED; INSTRUCTIONS_ZWIFT.md:1-3 says: choose BikeControl as **Power Source** (auto-selects it as Controller), then keep BikeControl as Controller and pick your trainer as Power Source.

## 4. Handshake

What the repo can show (client-side code, i.e. what real controllers do; the emulator must mimic the device):

- Zwift (central) writes to SYNC RX (`...0003`) `RIDE_ON = 52 69 64 65 4F 6E` ("RideOn", 6 bytes) (`constants.dart:231`; the app does exactly this to real controllers, `zwift_device.dart:138-146`). A real Zwift app is believed to append 2+ more bytes (header "RIDEON + 2", `constants.dart:234-235`: `REQUEST_START = [0x00, 0x09]`, comment says the 2 bytes "don't actually seem to matter") and possibly a public key - UNVERIFIED, the emulator's parse is stubbed (`shared.dart:8-10`). Safest: treat any write on `...0003` whose first 6 bytes are `RideOn` as the handshake request.
- Device replies on SYNC TX (`...0004`, via notify/indicate): `RideOn` + 2-byte start code + optional device public key. Start codes (`constants.dart:236-238`): Click `01 03`, Play `01 04`, Click v2 `02 03`. The device-side parser requires `RIDE_ON + code` as prefix and treats the rest as the public key (`zwift_device.dart:155-157,171-178`; test fake sends 16 bytes of 0x42, `emulated_peripherals.dart:288-296`). The emulator's real reply is produced by stubbed `SharedLogic.handleWriteRequest(char, value, asRide:)`; the call site checks `response == ZwiftConstants.RIDE_ON` to start the keepalive (`zwift_emulator.dart:179-197`), which suggests the reply is the bare 6-byte `RideOn` when emulating (and `RIDE_ON_FULL` is an 8-byte form `RideOn`+2 bytes, `prop constants.dart:39`). UNVERIFIED; implement: reply `52 69 64 65 4F 6E` (+ optionally `01 03`/`01 04`) on `...0004`.
- **Encryption: none in this repo.** No crypto/key exchange is implemented in the Zwift emulator or device parser (the public-key bytes are only logged, `zwift_device.dart:171-178`). SRAM AXS has crypto (`prop_public/lib/devices/sram/sram_axs_crypto.dart`) - unrelated. Ride V2 / Click V2 are "unlocked" by Zwift's app, with a vendor challenge replayed (opcode 0xFF VENDOR_MESSAGE, `bytes[1]==0x03`) - that concerns real hardware being bridged, not the emulator (`zwift_unlock.dart:183-191`, `zwift_unlock.dart:119-124` writes `FF 04 00`).
- Subsequent messages seen from a real Ride (all parsed in `zwift_ride.dart:150-238`): opcodes (`prop_public/lib/protocol/zp.pbenum.dart:19-56`): `0x23` CONTROLLER_NOTIFICATION (35), `0x19` BATTERY_NOTIF (25; payload `BatteryNotification`, field newPercLevel), `0x2A` LOG_DATA, `0x3C` GET_RESPONSE, `0x3E` STATUS_RESPONSE, `0x52` RIDE_ON (82), `0xFE` LOST_CONTROL, `0xFF` VENDOR_MESSAGE, `0x15` empty message (`constants.dart:243-254`), `0x1A` BatteryStatus. The app also writes (to the controller) `[0x12,0x12,0x08,0x0A,0x06,0x08,0x02,0x10,0x00,0x18]+0x20` as a vibrate pattern (`constants.dart:232`, `zwift_device.dart:257-265`) - what Zwift would write to *us* when it wants to buzz is UNVERIFIED; the emulator ignores writes other than the handshake (`zwift_emulator.dart:162-200`, stubs `onWriteRequest {}`).
- Battery: BLE emulator answers 2A19 reads with 100 (`zwift_emulator.dart:153-160`). Nothing in the LAN path.

## 5. Control data (button / keypad notification)

Frame carried in a GATT notification on ASYNC `00000002-19CA-4651-86E5-FA29DCDD09D1`:

```
byte 0     : opcode 0x23 (CONTROLLER_NOTIFICATION = 35)          zwift.pb enums, constants.dart:251
byte 1..   : protobuf RideKeyPadStatus (package de.jonasbark in the generated code, irrelevant on the wire)
   field 1 (tag 0x08) uint32 ButtonMap   - varint
   field 3 (tag 0x1A) repeated RideAnalogKeyPress (length-delimited)
        field 1 (tag 0x08) enum Location: 0 LEFT, 1 RIGHT, 2 UP, 3 DOWN
        field 2 (tag 0x10) int32 AnalogValue (plain int32 varint; negatives are 10-byte varints)
   (field 2 unused)                                              prop_public/lib/protocol/zwift.pb.dart:419-545
```

**Inverted bits**: a button is pressed when its bit is **cleared** (`PlayButtonStatus.ON = 0`, OFF = 1; decoder checks `buttonMap & mask == 0`, `zwift_ride.dart:247-265`; encoder `buttonMap = ~mask & 0xFFFFFFFF`, `ride_controller_press.dart:465`). Idle/all-released = `0xFFFFFFFF` -> `08 FF FF FF FF 0F` (`controller_keep_alive.dart:8`).

### Button bitmap (use `lib/.../zwift_ride.dart:326-350`, the app's own enum that works with real hardware)

| Button | Mask | Press maps from InGameAction (`ride_controller_press.dart:436-455`) |
|---|---|---|
| LEFT_BTN | 0x00001 | steerLeft (and `navigateLeft` for Rouvy, `rouvy_mdns_emulator.dart:110-111`) |
| UP_BTN | 0x00002 | openActionBar (`up` for Rouvy) |
| RIGHT_BTN | 0x00004 | steerRight (`navigateRight` Rouvy) |
| DOWN_BTN | 0x00008 | uturn (`down` Rouvy) |
| A_BTN | 0x00010 | select |
| B_BTN | 0x00020 | back |
| Y_BTN | 0x00040 | usePowerUp |
| Z_BTN | 0x00080 | rideOnBomb |
| SHFT_UP_L_BTN | 0x00100 | **shiftDown** (left-hand shifter = gear down) |
| SHFT_DN_L_BTN | 0x00200 | leftShiftButton2 |
| POWERUP_L_BTN | 0x00400 | leftDropButton |
| ONOFF_L_BTN | 0x00800 | (no action mapped by emulator) |
| SHFT_UP_R_BTN | 0x01000 | **shiftUp** (right-hand shifter = gear up) |
| SHFT_DN_R_BTN | 0x02000 | rightShiftButton2 |
| POWERUP_R_BTN | 0x04000 | rightDropButton |
| ONOFF_R_BTN | 0x08000 | (none) |

WARNING: `prop_public/lib/protocol/zwift.pbenum.dart:32-48` defines a *different* `RideButtonMask` (Z=256, SHFT_UP_L=512, ..., SHFT_UP_R=8192, etc.). The app imports prop with `hide RideButtonMask` and uses the `zwift_ride.dart` enum everywhere (`zwift_emulator.dart:27`, `ride_controller_press.dart:1,5`), and that one is what is exercised against real controllers. Use the table above.

- Mapping note: Click "+" = shift up = SHFT_UP_R; Click "-" = shift down = SHFT_UP_L (`ride_controller_press.dart:438-439`, `constants.dart:296-340`).
- **Steering** in the Ride protocol is the digital D-pad LEFT/RIGHT buttons (no analog steering axis is sent). Menu navigation = UP/DOWN/LEFT/RIGHT + A (select) / B (back) / Y / Z. Analog paddles (field 3) are only used for `brake` (both paddles): pressed = LEFT with value `-100`, RIGHT with `+100`; the receiver uses |value| >= 25 as pressed (`ride_controller_press.dart:24,452,464-472`; `zwift_ride.dart:22,271-285`). Paddle location LEFT=0 and RIGHT=1 (`zwift_ride.dart:274-278`).
- **Press vs release**: key-down -> send frame with the button's bit cleared; key-up -> send a second frame `0xFF..` (all set). No repeats while held by the emulator (only sends on down and on up; the receiver de-duplicates identical consecutive frames, `zwift_device.dart:207-214`). Release frame with paddles sends the paddles back at value 0 ("a frame without them leaves the last paddle value in place", `ride_controller_press.dart:460-462`). Multi-button chords are possible by clearing several bits in one frame (front shift combo = SHFT_UP_R | SHFT_UP_L cleared in one frame, then a neutral frame; Zwift-SRAM expectation UNVERIFIED, `zwift_emulator.dart:380-403`).
- Dart `writeToBuffer()` for default-valued fields: `buttonMap` always emitted (even for 0xFFFFFFFF); empty paddle list emits nothing.

### Concrete examples (computed with the encoding above)

ASYNC UUID bytes: `00 00 00 02 19 CA 46 51 86 E5 FA 29 DC DD 09 D1`.

GATT payloads:
- shift up press (SHFT_UP_R, mask 0x2000): `23 08 FF BF FF FF 0F`
- shift up release / idle / keepalive: `23 08 FF FF FF FF 0F`
- shift down press (SHFT_UP_L, 0x200): `23 08 FF FB FF FF 0F`
- steer left press (LEFT 0x1): `23 08 FE FF FF FF 0F`
- steer right press (0x4): `23 08 FB FF FF FF 0F`; A: `23 08 EF FF FF FF 0F`; UP: `23 08 FD FF FF FF 0F`; DOWN: `23 08 F7 FF FF FF 0F`
- brake pressed (both paddles): `23 08 FF FF FF FF 0F 1A 0D 08 00 10 9C FF FF FF FF FF FF FF FF 01 1A 04 08 01 10 64`

Full DirCon TCP frames (header layout per sec. 2 = UNVERIFIED; body = confirmed layout `UUID + data`; length 0x0017 = 16+7; version 1, msgId 6, seq 0, resp 0):

```
shift up press   : 01 06 00 00 00 17  00 00 00 02 19 CA 46 51 86 E5 FA 29 DC DD 09 D1  23 08 FF DF FF FF 0F
shift up release : 01 06 00 00 00 17  00 00 00 02 19 CA 46 51 86 E5 FA 29 DC DD 09 D1  23 08 FF FF FF FF 0F
steer left press : 01 06 00 00 00 17  00 00 00 02 19 CA 46 51 86 E5 FA 29 DC DD 09 D1  23 08 FE FF FF FF 0F
```

Rouvy "Click" endpoint (separate, not DirCon-framed in the visible code): `ClickEmulator.writeNotification(bytes)` is given the bare `RideKeyPadStatus` bytes WITHOUT the 0x23 opcode on press (`rouvy_mdns_emulator.dart:130-139`) and `kZwiftControllerReleasedState` (also without opcode) on release (`:142,86`) - inconsistent with the Zwift/FTMS path which prefixes 0x23 (`ftms_mdns_emulator.dart:145-155`); probably the stubbed ClickEmulator prefixes it. UNVERIFIED.

## 6. Timing, keepalive, pitfalls

- **Keepalive (LAN)**: re-send the neutral released frame (`23 08 FF FF FF FF 0F`) every **5 s** while a client is connected. "Zwift (and Rouvy) drop a wired LAN controller after ~30s without any traffic (issue #367)" - `controller_keep_alive.dart:1-24` (interval default 5 s, `:18`; `Timer.periodic`), wired at `ftms_mdns_emulator.dart:31-35,62-72,89-94`; Rouvy `rouvy_mdns_emulator.dart:22-26,82-87`. Safe because it is identical to a key-up.
- **Keepalive (BLE)**: after a RideOn reply, repeat the same neutral frame on SYNC TX (`...0004`) every 5 s (`zwift_emulator.dart:194-196,313-326`).
- No documented notification-rate limit; the emulator sends one frame on down and one on up (`ftms_mdns_emulator.dart:144-156`). The Zwift receiver side ignores byte-identical consecutive frames. Real Ride sends frames ~at button change. UNVERIFIED: any minimum spacing between down and up (the app's long-press/click logic upstream decides timing).
- Ignored devices/duplicates: `lastMessageId = 0` field exists in both emulators but is unused (`ftms_mdns_emulator.dart:24`).
- Pitfalls/comments in code:
  - `serial-number` must be digits-only (uint64 parse), `mac-address` required, TXT values ASCII (`constants.dart(prop):64-66`, `connection.dart:1174-1180`, `supported_app.dart:108-121`).
  - Names: do not embed the trainer's own name, and avoid duplicate `BikeControl` instances of the same service type (`proxy_device.dart:576-588`, `zwift_unlock.dart:60-66`).
  - TrainerRoad hard-dials 36866 and ignores the SRV port; other apps must be told the bound port (`zwift_unlock.dart:27-29`).
  - 16-bit UUID format in `ble-service-uuids` is app-specific (bare vs `0x`) (`proxy_device.dart:649-654`).
  - Zwift Ride firmware > 1.2.0 hides its custom service (real hardware only) (`zwift_device.dart:45-72`).
  - The bridge "takes the Zwift name" while a Click V2 unlock is pending (`proxy_device.dart:539-556`).
  - `zwift_emulator.dart:194`: `response == ZwiftConstants.RIDE_ON` compares `Uint8List`s by identity in Dart - only true if the shared logic returns that very object; potential bug / do not port literally.
  - Front-shift combo bit pair is explicitly UNVERIFIED against live Zwift (`zwift_emulator.dart:385-389`).
  - Click V2 / Ride V2 device types and some constants marked "unconfirmed" (`constants.dart:226-229`).

## 7. Open questions (need the closed `prop` package, a packet capture, or a live Zwift test)

1. Exact DirCon header bytes (assumed 6 bytes: ver, msgId, seq, resp, len16 BE) and seq handling for unsolicited notifications - `prop/dircon/dircon_framing.dart` is missing.
2. Discover Services / Discover Characteristics / Read / Enable-Notification request and response layouts, characteristic property bit values.
3. Real Zwift-controller `ble-service-uuids` TXT string and real `mac-address` value; whether Zwift needs additional TXT keys (e.g. `manufacturer-data`).
4. Whether Zwift needs the Ride service only, or FTMS/power too, to list BikeControl as a Controller; and which GATT service/characteristics (TX: notify vs indicate) it expects on the LAN path.
5. What Zwift writes on SYNC RX after the connection (exact RideOn+suffix, and any follow-up commands / status requests / vendor messages) and what must be answered.
6. Exact RideOn reply (bare `RideOn` vs `RideOn`+`01 03`/`01 04`+key).
7. Whether `ZwiftEmulatorDefinition(asRide:)` changes the emitted data (Ride protobuf vs Click `0x37` frames) - `emulatesRide` is true for Zwift.

## Gap-fill from qdomyos-zwift / OBC

Sources (read-only): `$HOME/mnt/qdomyos-zwift/src/devices/dircon/{dirconpacket.h,dirconpacket.cpp,dirconprocessor.cpp,dirconmanager.cpp}` (abbreviated `pkt.h`, `pkt.cpp`, `proc.cpp`, `mgr.cpp`), `src/zwift_play/*`, `src/devices/ftmsbike/ftmsbike.cpp`; `$HOME/mnt/openbikecontrol-protocol/{README,MDNS,PROTOCOL}.md`. qdomyos is a real, widely-used DirCon **server** (it emulates a Wahoo KICKR to Zwift), so this section resolves sec. 2 / sec. 7 items 1-2 with VERIFIED status.

### A. openbikecontrol-protocol (OBC)
- Open protocol for input devices (shifters, buttons, brake levers) controlling trainer apps; BLE (service `d273f680-d548-419d-b9d1-fa0472345229`) and a LAN variant "similar to Direct Connect" (README.md:5-10; MDNS.md:5).
- Implemented by MyWhoosh, TrainingPeaks Virtual, Strappo, icTrainer, Biketerra, Incyclist etc. (README.md:~27). **Zwift is NOT in the implemented list**; "zwift" only appears as an example App-ID string in the App Info message (MDNS.md:194,208).
- OBC LAN uses its OWN mDNS type `_openbikecontrol._tcp.local.` with TXT `version,id,name,service-uuids,manufacturer,model` (MDNS.md:15-43) and raw message bytes over TCP ("same binary format as the BLE protocol", MDNS.md:57-59) - NOT the Wahoo 6-byte DirCon framing. No DirCon framing is documented there. (The bikecontrol repo nevertheless wraps OBC in DirCon frames under `_wahoo-fitness-tnp` for some apps; that is unrelated to Zwift.) Not relevant for Zwift.

### B. DirCon wire format (VERIFIED, qdomyos)
Header, 6 bytes (`pkt.h:7`, parse `pkt.cpp:79-83`, encode `pkt.cpp:214-219`):
```
0 version  (always 1; encode forces 1)
1 msgId
2 seqNum
3 respCode
4-5 length  uint16 BIG-endian (hi byte first) = body bytes after header
```
TCP stream reassembly: wait until `buf.size() >= 6+length` (`pkt.cpp:85-88`; `proc.cpp:233-268` loops over a per-client buffer, can hold several frames).
Msg IDs: 01 discover svcs, 02 discover chars, 03 read, 04 write, 05 enable notif, 06 unsolicited notification, 0xFF is an internal "no reply" sentinel only (`pkt.h:11-17`). Response codes 0-7 as in sec. 2 (`pkt.h:18-25`) - CONFIRMED identical.
**All UUIDs on the wire are 16 raw bytes, big-endian canonical order.** qdomyos only supports Bluetooth-base UUIDs: template `00 00 XX XX 00 00 10 00 80 00 00 80 5F 9B 34 FB` where bytes 2-3 are the 16-bit UUID (`pkt.h:52-53`, `pkt.cpp:231-233`; parse reads only bytes 2-3, `pkt.cpp:99-100`). A custom 128-bit UUID (Zwift 0x0000000x-19CA-4651-...) is NOT supported by qdomyos, so our implementation must carry full 16 bytes (client Zwift sends/expects them verbatim - UNVERIFIED for Zwift; fine for Wahoo-base).
Request vs response: a frame is a request iff respCode==0 and (last_seq<=0 or seq != last_seq) (`pkt.cpp:183-186`); simple server implementations can instead treat every frame received from Zwift with msgId 1-5 as a request.

**Sequence numbers** (`pkt.cpp:206-213`, `proc.cpp:254`): response seq = copy of the request's seqNum (`resp.encode(pkt.SequenceNumber)`); unsolicited notification (0x06) seq = **0** (`pkt.cpp:210-211`; send site `proc.cpp:214` `encode(0)`). Respcode of notifications = 0.

**Payloads** (request body / response body; all after the 6-byte header):

| Msg | Request body | Success response body | Source |
|---|---|---|---|
| 01 discover services | empty (len 0) | N x 16-byte service UUIDs (all services, in order) | pkt.cpp:91-94, 228-234; proc.cpp:106-109 |
| 02 discover chars | 16-byte service UUID | service UUID (16) + N x [char UUID(16) + 1 property byte] (len = 16+17N) | pkt.cpp:107-130, 236-248; proc.cpp:110-123 |
| 03 read | char UUID (16) | char UUID (16) + value bytes | pkt.cpp:131-141, 261-263; proc.cpp:124-143 |
| 04 write | char UUID (16) + data (len > 16) | **char UUID (16) only** (len 16), plus no data. NB: qdomyos's `out.additional_data` is filled by the write processor but encode for a non-request WRITE appends `additional_data` too (`pkt.cpp:259-270`), so a handler-supplied reply is echoed after the UUID; default handlers leave it empty | proc.cpp:144-168; pkt.cpp:259-270 |
| 05 enable notif | char UUID (16) + 1 byte (0 = off, 1 = on) | char UUID (16) only (len 16), no enable byte | pkt.cpp:155-166, 252-258; proc.cpp:169-195 |
| 06 notification (server->client) | n/a | char UUID (16) + data; seq 0 | pkt.cpp:167-176, 259-263; proc.cpp:200-223 |

Property byte (`pkt.h:8-10`): **0x01 = read, 0x02 = write, 0x04 = notify**. There is NO indicate bit in qdomyos (and no combined values are used there - each characteristic has exactly one flag; combos like 0x05/0x06 are the natural bitwise OR and the processor tests with `&`, `proc.cpp:129,149,174`). Indicate: UNVERIFIED (Wahoo bit 0x08 is the common guess); for Zwift TX (0004) advertising notify (0x04) is the safe choice, since DirCon has only a notification message.

**Error handling** (`proc.cpp`): unknown service on 02 -> respCode 3 (`:123`); unknown char on 03/04/05 -> 4 (`:143,168,194`); op not permitted by property flag -> 5 (`:135,160,186`); write handler failure -> message dropped, NO reply (`:156-157`, Identifier=0xFF sentinel skips send at `:253`); malformed/unparseable frame -> reply with respCode 2 and echoed msgId (`:258-265`) after discarding the bad frame length; msgId unknown to the server (e.g. 0x00 or >6) -> `parse` returns PARSE_ERROR (`pkt.cpp:177-178`) -> respCode 2 (not 1). Error responses have length 0 (6-byte frame, `pkt.cpp:220-222`). Frames with nonzero respCode received from the peer are skipped (`pkt.cpp:89-90`). Enable-notify with 16-byte body (no enable byte) is parsed but NOT treated as a request (`pkt.cpp:156-164`) - always send/expect 17 bytes. Notifications are sent only to clients who enabled that UUID (`proc.cpp:212`; qdomyos also sends to all clients when setting `wahoo_rgt_dircon` is false, `:212`, default false `qzsettings.h:1334`), i.e. do not push before 05 arrives or Zwift may ignore.

**mDNS** (`proc.cpp:46-66`, QMdnsEngine): service type `_wahoo-fitness-tnp._tcp.local.`; instance name = machine name e.g. `Wahoo KICKR 0000` ("Wahoo KICKR $uuid_hex$", 4-digit zero-padded `dircon_id`, default 0, `mgr.cpp:290,379-384`, `qzsettings.h:2106`); hostname label = name + "H" (`proc.cpp:49`); SRV port = `dircon_server_base_port` (default **36866**, `qzsettings.h:1070`) + machine index (`mgr.cpp:385`; KICKR=0, HR=1...). TXT keys exactly: `mac-address` (string from first non-loopback NIC with IPv4, format `AA:BB:CC:DD:EE:FF` from `QNetworkInterface::hardwareAddress()`; fallback `00:11:22:33:44`, `mgr.cpp:397-413`), `serial-number` (decimal string of machine index, e.g. `0`, `proc.cpp:55`, `mgr.cpp:385`), `ble-service-uuids` = comma-separated **full 128-bit** UUIDs, uppercase hex, e.g. `00001826-0000-1000-8000-00805F9B34FB,00001818-0000-1000-8000-00805F9B34FB` (template `DP_BASE_UUID "0000u-0000-1000-8000-00805F9B34FB"`, `proc.cpp:56-62`, `dirconprocessor.h:60`). This contradicts the `0x1826` "0x form" note in sec. 1 for Zwift: qdomyos's full-128-bit form is known to work with Zwift as trainer; use the 128-bit form for FC82 (`0000FC82-0000-1000-8000-00805F9B34FB`). Listener binds `QHostAddress::Any` (`proc.cpp:26`). Multiple TCP clients are allowed (clientsMap).
Services served by qdomyos for bike (`mgr.cpp:281-287,305-344`): FTMS 1826 (2ACC read, 2AD6 read, 2AD9 write, E005 write(Wahoo), 2AD2 notify, 2AD3 read), CPS 1818 (2A65 read, 2A5D read, 2A63 notify), CSC 1816 (2A5C read, 2A5D read, 2A5B notify, 2A55 write), HR 180D (2A37 notify). Note: characteristics with BOTH read and notify (e.g. 2A37, 2AD2) are exposed as notify-only (0x04).

### C. Zwift Play/Ride/Click in qdomyos
- **No emulation.** qdomyos is only a BLE *central/client* for real Zwift Play/Click/Ride hardware (`bluetooth.cpp:2851-2878`, `zwiftclickremote.cpp`, `ftmsbike.cpp`); `grep` for FC82/0xfc82 in dircon/ and virtualdevices finds nothing; DirCon never serves a Zwift Play/Ride service. So there is NO verified source for what Zwift writes on FC82/0003, the RideOn reply from a device, or 0x23 frames being produced by a server. Open items 4-7 of sec. 7 remain UNVERIFIED.
- What the client code shows (useful as the mirror image):
  - UUIDs `00000001-19CA-4651-86E5-FA29DCDD09D1` service; ASYNC `...0002`, SYNC_RX `...0003`, SYNC_TX `...0004`, `...0006` (`zapBleUuids.h:16-20`). Same as sec. 3.
  - RideOn bytes: `RIDE_ON = 52 69 64 65 4F 6E`, `REQUEST_START = 00 09`, `RESPONSE_START = 01 03` (`abstractZapDevice.h:28-35`). Central sends `52 69 64 65 4F 6E 02 01` ("RideOn" + 02 01) in `ftmsbike.cpp:146` (Zwift Play handshake as written by a Zwift-like central), then a series of protobuf-ish init writes `41 08 05`, `04 2A 04 10 C0 BB 01`, `00 08 00`, `00 08 88 04`, ... (`ftmsbike.cpp:149-170`) for gear-ratio control on a Zwift Ride/KICKR - these are central->device commands, i.e. what Zwift might also send to us. Device replies not coded there.
  - Click/Play input parsing: frames starting `0x37` (Click: len 5, byte2==0 => plus, byte4==0 => minus) and `0x07` (Play, byte pattern `40 c7|c8 01`) (`abstractZapDevice.h:78-140`). **0x23 is not handled** by qdomyos; Ride (0x23 + protobuf keypad) frames are not parsed on the non-Android path.
  - FC82 + FTMS/power together: not applicable; qdomyos does not expose FC82 over DirCon, so no evidence either way for the "needs FTMS/power too" question. Characteristic properties for 0002/0003/0004 are not specified anywhere in qdomyos (they are client-discovered). Keep bikecontrol's structure: 0003 write, 0002 notify, 0004 notify (read-only `|` indicate on real HW).

## Controller identity (device-info) research

Question: what does Zwift learn about *what kind of device we are* from our replies after connecting, and can a KICKR-advertised
emulator present itself as a Ride / Click v2 / Click v1 / Play instead of a self-shifting KICKR BIKE? Implemented probe-only as
`--identity kickr|ride|clickv2|clickv1|play` (`ControllerIdentity` in `DeviceProfile.cs`, applied in `ZwiftControlProtocol`).

### a. Request / reply structure (decoded with zp.proto)

- `00 08 xx` is opcode `GET = 0x00` with `Get { required uint32 dataObjectId = 1 }` (`Network/Protos/zp.proto:36` Opcode enum, `zp.proto:510`). The reply is opcode
  `GET_RESPONSE = 0x3c` with `GetResponse { required uint32 dataObjectId = 1; optional bytes dataObjectData = 2 }` (`zp.proto:672`). Data object ids (`enum DO`):
  `0x000` PAGE_DEV_INFO, `0x010` PAGE_CLIENT_SERVER_CONFIGURATION, `0x210` PAGE_TRAINER_GEAR_INDEX_CONFIG, `0x303` BATTERY_STATE, `0x400` PAGE_CONTROLLER_INPUT_CONFIG,
  `0x500` SENSOR_RELAY_DATA_PAGE. So `00 08 00` = 0, `00 08 10` = 16, `00 08 80 08` = 1024, `00 08 83 06` = 771, `00 08 90 04` = 528, `00 08 80 0a` = 1280.
- `dataObjectData` for id 0 is `DevInfo { required DevInfoPage devInfo = 1 }` (`zp.proto:384`; bikecontrol parses it as `DevInfoPage.fromBuffer(response.dataObjectData)`,
  `bikecontrol/lib/bluetooth/devices/zwift/zwift_ride.dart:183`). `DevInfoPage`: f1 protocolVersion (uint16), f2 systemFwVersion (4 bytes), f3 deviceName (12), f6 serialNumber (15),
  f7 systemHwRevision, f8 repeated `DeviceCapabilities { required uint32 deviceType = 1; required uint32 capabilities = 2 }`, f9 manufacturerId, f10 productId, f11 deviceUid (12).
- `enum DeviceType` (`zp.proto:161`): 0 UNDEFINED, 1 CYCLING_TURBO_TRAINER, 2 USER_INPUT_DEVICE, 3 TREADMILL, 4 SENSOR_RELAY, 5 HEART_RATE_MONITOR, 6 POWER_METER, 7 CADENCE_SENSOR, 8 WIFI.
  **There is no Ride / Click / Play / KICKR BIKE / Hub value.** The type information is the f8 list: which roles the device claims. The capability bit meanings are not documented anywhere.
- Decoding the KICKR reply prefix `08 82 04 12 04 12 00 15 01`: `08 82 04` = f1 protocolVersion = 514 = 0x0202; `12 04 12 00 15 01` = f2 firmware bytes (patch, 0, minor, major = 18,0,21,1 = fw 1.21.18 as in 2A26).
  **f1 is the protocol version, not a device-type code.** Evidence: it equals the two bytes after `RideOn` in our reply on SyncTx (KICKR 514 = 02 02; the Victory capture has 515 = 0x0203 and replies `RideOn 02 03`), and
  bikecontrol's per-device "start response" bytes are the same kind of pair: Click v1 `01 03`, Play `01 04`, Click v2 `02 03` (`bikecontrol/lib/bluetooth/devices/zwift/constants.dart:44-46`;
  `zwift_play.dart:38`, `zwift_clickv2.dart:54`). qdomyos only knows `RESPONSE_START = 01 03` (`qdomyos-zwift/src/zwift_play/abstractZapDevice.h:35`).
- What does differ between the trainers and therefore likely drives Zwift's classification and its follow-up GETs: the f8 capability list.
  KICKR BIKE: `(1, 36) (2, 0)` (trainer + user input) -> Zwift then asks 1024 (controller input config), 771, 528 (gear index). Victory (local capture analysis, not in the repository, section 5):
  `(1, 209) (8, 1) (4, 0)`, f9 = 8, f10 = 6 (matches 2A24 model "06") -> Zwift asks 16 and 1280 (sensor relay page, reply `3c 08 80 0a 12 06 0a 04 05 02 06 07`) and sends `44 20 01` / `44 08 01`
  (SENSOR_RELAY_CONFIG, `zp.proto:44`), answered `3e 08 44 10 00` (StatusResponse{command 0x44, status 0}). A pure controller has no trainer role, so the working hypothesis is: a controller claims only `(2, caps)`.
- Reply to 1024 is `ControllerInputConfigPage { supportedDigitalInputs = 216, supportedAnalogInputs = 3, two analogInputRange }` (`zp.proto:475`); the digital-input bit layout is a Zp enum (NOT the Ride keypad mask), unknown for Ride/Click.

### b. What a real Ride / Click replies

Not available. bikecontrol's BLE emulation is test fakes only (`lib/bluetooth/emulation/emulated_peripherals.dart:273-296` answers only RideOn: `RideOn` + start response + 16 fake key bytes; `zwift_profiles.dart` registers
Ride with `01 04`, Click v2 with `02 03`, Play with `01 04`), and the real handler `SharedLogic.handleWriteRequest` ("Zwift Click handshake / device-info responses") is a stub (`prop_public/lib/utils/shared.dart:5-9`),
as are the DirCon emulator and `ZwiftClickDefinition` (`prop_public/lib/emulators/definitions/zwift_click_definition.dart`). The only hint: the closed LAN/BLE emulators take an `asRide` flag
(`ftms_mdns_emulator.dart:28`, `zwift_emulator.dart:183`; `emulatesZwiftRide => true` for Zwift, `lib/utils/keymap/apps/zwift.dart:10`), i.e. the real implementation switches the *device-info reply* between a Ride and a Click, and
BLE emulation advertises as local name `KICKR BIKE PRO 1337` + service FC82 (`zwift_emulator.dart:296`). qdomyos never parses 0x3c (no DeviceInformation use in `src/zwift_play`); `zwift.proto` in our repo has only a
generic `DeviceInformationContent` guess (f8 "ReplyData" = our capability list). The real controller's BLE manufacturer-data type bytes (Play L/R 0x03/0x02, FW2 0x0E, Ride R/L 0x07/0x08, Click v1 0x09, Click v2 R/L 0x0A/0x0B,
`constants.dart:23-37`) are an advertising-only classification and are not available over DirCon.

### c. RideOn reply variants

Reply = `RideOn` + protocolVersion (big endian): KICKR `02 02`, Victory `02 03` (Zwift's request was `02 04` in the Victory capture, `02 03` in the KICKR capture, so the reply is the device's version, not an echo), Click v1 `01 03`,
Play `01 04`, Click v2 `02 03`. Ride: not found (bikecontrol's Ride classes inherit the Click v1 default `01 03` only for matching the key response, `zwift_device.dart:32`; their `processData` also handles a bare `RIDE_ON` opcode, `zwift_ride.dart:156`).
Real BLE devices append a 16-byte public key; the DirCon captures show none.

### d. Device-information strings

bikecontrol's real BLE emulator exposes 2A29 `BikeControl`, 2A25 `09-B48123283828F1337`, 2A27 `A.0`, 2A26 app version (`zwift_emulator.dart:205-215`); test fakes use 2A29 `FakeWorks` (`emulated_peripherals.dart:22-40`). Neither is evidence of what a real
Zwift controller reports (the real strings are not in any source we have). The Victory reports 2A29 `JetBlack`, 2A24 `06`, 2A27 `2`, 2A26 `4.29`. Our GATT model has no 2A24 characteristic, so only 2A29 (`--manufacturer`) can be varied; identities leave it at `Wahoo Fitness`.

### What each identity sends (implemented)

| Identity | f1 protocolVersion / RideOn reply | f8 capabilities | other |
|---|---|---|---|
| `kickr` | 514 / `02 02` | (1,36) (2,0) | unchanged bytes; additionally answers `00 08 80 0a` (Victory bytes) |
| `ride` | 514 / `02 02` (UNKNOWN real value, KICKR fallback + warning) | (2,0) | |
| `clickv2` | 515 / `02 03` | (2,0) | |
| `clickv1` | 259 / `01 03` | (2,0) | |
| `play` | 260 / `01 04` | (2,0) | |

All identities answer `44 xx yy` with `3e 08 44 10 00`, and keep the KICKR replies for 1024 / 771 / 528, name, serial, hardware revision and firmware bytes. Unknowns (logged as warnings): Ride protocol version, the capability bitmask of
USER_INPUT_DEVICE for a real controller (0 copied from the KICKR), manufacturerId/productId, the 0x400 digital-input mask, whether a controller answers 771/528 at all, real 2A29/2A24 strings.

## mDNS manufacturer-data

BikeControl (accepted by Zwift) advertises `_wahoo-fitness-tnp._tcp` with TXT `manufacturer-data=4A0906B653` (local capture analysis, not in the repository). Findings from the BikeControl open code (read-only reference):

- The value is ASCII hex text (10 characters), not binary: BikeControl writes `Uint8List.fromList(String.codeUnits)` (lib/bluetooth/devices/openbikecontrol/obc_mdns_emulator.dart:230). The open repo only holds a placeholder (`BikeControlMdnsMarkers.manufacturerData = '0000000000'`, prop_public/lib/utils/constants.dart:46); BikeControl itself only uses it as a fingerprint to ignore its own adverts (lib/bluetooth/wifi_trainer_scanner.dart:178). The real value is not in the open code.
- Decoding: `4A 09` = Zwift company id 0x094A little-endian (ZWIFT_MANUFACTURER_ID = 2378, lib/bluetooth/devices/zwift/constants.dart:16), the same prefix as BLE manufacturer data. Remaining bytes `06 B6 53`: the BLE payload after the company id begins with a device-type byte (constants.dart:21-37, parsed by ZwiftDeviceType.fromManufacturerData, constants.dart:202; used at lib/bluetooth/devices/bluetooth_device.dart:488-500).
- Known device-type bytes: Play right 0x02, Play left 0x03, Play FW2 0x0E (RC1_*), Ride right 0x07, Ride left 0x08, Click (BC1) 0x09, Click v2 right 0x0A (unconfirmed), Click v2 left 0x0B. In the BLE emulation the payload is only that single byte (lib/bluetooth/emulation/emulated_peripherals.dart:150-200).
- 0x06 is NOT a type BikeControl knows. Best reading: 0x06 is a type byte of its own (or a Zwift-defined value for a generic/unknown controller class), and `B6 53` are two id bytes (possibly derived from a MAC/serial; unconfirmed). Whether Zwift reads this TXT at all is untested; see Test plan I in docs/zwift-lan-testing.md.
- Probe flags to test it: `--mfg ride|clickv1|clickv2|play|bikecontrol` (4A09 + 08/09/0B/03 + B653; bikecontrol = 4A0906B653), `--mfg-data <hex>`, `--txt-extra "k=v;k2=v2"`, `--add-obc-uuid`.

