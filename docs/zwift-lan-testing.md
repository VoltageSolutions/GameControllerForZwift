# Testing the Zwift LAN controller

The app (and the standalone probe) present themselves to Zwift as a Wahoo KICKR BIKE SHIFT over LAN (DIRCON + mDNS).
The probe lets you press individual buttons by hand and see what Zwift does, without needing a game controller.

## 1. Allow the traffic through Windows Firewall

Run once in an elevated PowerShell (Private network profile; TCP 36866 is the DIRCON port, UDP 5353 is mDNS):

```powershell
New-NetFirewallRule -DisplayName "Zwift LAN controller (DIRCON TCP 36866)" -Direction Inbound -Protocol TCP -LocalPort 36866 -Action Allow -Profile Private
New-NetFirewallRule -DisplayName "Zwift LAN controller (mDNS UDP 5353)" -Direction Inbound -Protocol UDP -LocalPort 5353 -Action Allow -Profile Private
```

If the port was busy the server falls back up to +10 (36867..36876); the console and log show the port actually used.
Zwift and this PC must be on the same subnet (Zwift on the same PC is fine; a phone/tablet/Apple TV works if they share the LAN).

## 2. Run the probe

```
dotnet run --project tools/ZwiftLanProbe
dotnet run --project tools/ZwiftLanProbe -- --preset kickr --name "KICKR BIKE SHIFT GC4Z" --port 36866 --verbose
```

Options: `--advert kickr|ftms|zwift` (mDNS TXT `ble-service-uuids`), `--gatt full|nowahoo|minimal` (served GATT tree), `--manufacturer "<s>"` (2A29, default `Wahoo Fitness`), `--no-gearing` (no 0x42 gear / ride-data frames), and the shorthand `--profile kickr|ftms|zwift` (kickr = kickr/full, ftms = ftms/nowahoo without gearing, zwift = zwift/minimal without gearing; see Test plan C and E), `--name` (keep it short, 20 characters or fewer; default `KICKR BIKE SHIFT GC4Z` for kickr advert + full GATT, otherwise `Voltage Emulator`), `--port`, `--masks zwift|makinolo`, `--verbose` (wire trace on the console too).
With no arguments the probe behaves like `--preset bcride` (a BikeControl replica named `Voltage Emulator`; Zwift recognises it as a Zwift Ride). `--preset kickr` restores the old default (emulated KICKR BIKE, full Wahoo GATT, gear reports). Other flags still override the preset. `--masks zwift` (default, = the `ZwiftRideButton` enum, Zwift labels) is the confirmed table; `--masks makinolo` is kept for reference only. `--partial N` (1..100, default 50) sets the partial paddle value and `--hold-ms N` (default 550) the hold timeout. Stop the probe with Ctrl+C (neutral frame, mDNS goodbye, server stop).

## 3. Pair in Zwift and press keys

1. Start the probe (stop the main app first, or both will fight over port 36866 and the mDNS name).
2. In Zwift's pairing screen, open **Controls** and pick the device (default `KICKR BIKE SHIFT GC4Z`). The probe prints `Zwift connected, RideOn handshake complete.`
3. Press keys in the probe window (see Test plan K for the full layout; `?` prints it).
4. `#` then a bit number 0-31 and Enter presses raw bit `1 << n` for 150 ms, to map unknown buttons (LPower / RPower) empirically.
5. Note what Zwift does for each key.

## Steering lab

For the Zwift Ride tutorial steering step (`--preset bcride`) where Left/Right buttons are not accepted. Research: BikeControl sends steering as the plain Left/Right button bits (`ride_controller_press.dart:26-27`), the same as `h`/`j`/`n`/`N`, so steering may need something else. `RideAnalogLocation` is LEFT=0, RIGHT=1, UP=2, DOWN=3 (`zwift.proto:70-75`); BikeControl only uses loc 0/1 (brake paddles, +/-100, `ride_controller_press.dart:37,53`). The KICKR BIKE capture shows loc 0/1 at +/-32767 (`kickr-bike-capture-analysis.md`). Which mechanism Zwift reads for steering is unknown, hence this lab.

Run at Information level; every 0x23 frame sent is logged as `TX 0x23 keypad <hex> => pressed=0x... analog=[loc2=-100]` so you can confirm what went out. Each key resends its frame every 100 ms for 1.5 s, then sends a neutral frame (same locations at 0). Start the tutorial steering step, then try, one at a time:

| Key | Frame (buttons released unless stated) |
|---|---|
| `@` | Prompt `loc,value[,ms[,buttonsHex]]`, entries separated by `;` (e.g. `2,-100;3,0`, or `2,-100,3000,1` = loc 2 -100 for 3 s with Left button held). Default 1500 ms |
| `1` / `2` | loc 2 (UP) = -100 / +100 |
| `3` / `4` | loc 3 (DOWN) = -100 / +100 |
| `5` / `6` | loc 0 = -32767 and loc 1 = 0 / loc 0 = 0 and loc 1 = +32767 (capture range) |
| `7` / `8` | Left button (0x1) + loc 2 = -100 / Right button (0x4) + loc 2 = +100 |

Report which key (if any) moves the steering indicator, and which direction.

## Test plan B (ride simulation, gears, steering)

Background: in the real-bike capture, shifting on a KICKR BIKE SHIFT is reported as opcode 0x42 gear frames (not as 0x23 button bits), and Zwift only reacts to shift/steer input in a ride, when it believes the bike is pedalling.

1. Start the probe. In Zwift's pairing screen select the probe device as **Power Source**, **Resistance** and **Controls**. Wait for `Zwift connected`.
2. Press `w` (100 W / 80 rpm). Zwift should show about 100 W and 80 rpm; start a ride (or free ride).
3. Try the 0x23 shift keys: `]` `[` (right shift up/down) and `.` `,` (left shift up/down). Watch the gear indicator on the HUD. Repeat with `p`/`P` etc. if nothing happens, and use `#` for raw bits.
4. Try `g` and `G` (0x42 gear frames) and watch the gear indicator. Check the log for any `Zwift wrote 0x3F` lines after each press; compare their fields with the capture (f2 = counter, f4.f5 = value).
5. Experiments with other controllers' frames: `c` / `C` (Click v1 plus/minus) and `v` / `V` (Play paddles) while the sim ride is on.
6. Steering: with the sim ride on press `h` and `j` (2 s holds of Left/Right) and also the arrow keys; see whether the avatar steers (steering usually needs Zwift's steering mode / a steering-capable device).
7. `W` to try other power/cadence values (e.g. `250,95`); `w` again stops the sim.

Note which of steps 3 to 6 changed anything and attach the log file.

## Test plan C (separate controller device, trainer elsewhere)

Idea: Zwift accepts shifting from a Click when the resistance trainer is a different device, so present the probe as a controller only and let the real trainer (for example the Jetblack Victory) provide Power and Resistance.

Profiles (`--profile`):

| Profile | GATT services | mDNS `ble-service-uuids` |
|---|---|---|
| `kickr` | As captured: 180A, Wahoo EE01/EE0D/EE0B/EE06, 1818, 1826, 181C, Zwift 00000001-... | `0xFC82,0x1818,0x1826,<Zwift 128-bit>,A026EE0D-...` |
| `ftms` (= `--advert ftms --gatt nowahoo --no-gearing`) | 180A, 1818, 1826, 181C, Zwift 00000001-... (manufacturer `Wahoo Fitness`). No A026xxxx services, no E037, no 0x42 | `0x1826,0xFC82,<Zwift 128-bit>` |
| `zwift` (= `--advert zwift --gatt minimal --no-gearing`) | 180A (manufacturer `Wahoo Fitness`) and Zwift 00000001-... only | `0xFC82,<Zwift 128-bit>` |

RideOn, 0x41, 0x00/0x3c and 0x3f handling is the same for all profiles.

1. Run `dotnet run --project tools/ZwiftLanProbe -- --profile ftms` (name `Voltage Emulator`). In Zwift's pairing screen pick the trainer (Victory) as **Power Source** and **Resistance**, and the probe as **Controls** only.
2. Ride, and try `]` `[` `.` `,` (Ride shift bits), `c` `C` (Click), `v` `V` (Play paddles), then steering with `n` `N` (repeated frames), `h` `j` (single frame) and brake with `k` `K`.
3. Stop the probe and repeat with `--profile zwift`.
4. Record per profile whether pairing as Controls worked and which keys had an effect; attach the logs (`Zwift wrote 0x3F` lines show what Zwift sends back).

`?` in the probe reprints the whole key table, the same text as at startup.

## Test plan E (bisect: what makes Zwift list the device?)

Findings so far: with kickr/full Zwift lists the device and 0x23 menu buttons work; with the ftms and zwift profiles (different TXT and different GATT) it is not listed at all. The advertisement and the GATT are now independent, and the startup header prints the effective advert, TXT, gatt, manufacturer and gearing. In every step: Victory as **Power Source** and **Resistance**, the probe as **Controls**. Stop the probe between runs (and let Zwift re-browse).

- E1 (name check, nothing but the name differs from the working setup):
  `dotnet run --project tools/ZwiftLanProbe -- --advert kickr --gatt full --name "Voltage Emulator"`
- E2 (drop the Wahoo A026xxxx services and E037/E03C behaviour):
  `dotnet run --project tools/ZwiftLanProbe -- --advert kickr --gatt nowahoo`
- E3 (180A + Zwift service only):
  `dotnet run --project tools/ZwiftLanProbe -- --advert kickr --gatt minimal`

For each: is the device listed under Controls? If so ride and try `]` `[` `.` `,` `g` `G` `k` `K` `n` `N` (with `w` for a sim ride if useful). Note how the gear is displayed (for kickr/full `g`/`G` showed `1-1`..`1-5`, front-rear) and whether 0x23 shift bits change it.
Interpretation: the first step that stops being listed shows what Zwift filters on. If E1 fails, Zwift checks the name; if E1 and E2 work and E3 fails, it needs a GATT service we removed (180A manufacturer is kept at `Wahoo Fitness`; try `--manufacturer "GC4Z"` to test that string). Afterwards swap `--advert ftms` or `--advert zwift` with the working `--gatt` to test the TXT alone.

## Test plan H (_zwift-protocol._tcp: what does Zwift do with such a device?)

Zwift's mDNS queries (see the probe log at Debug) ask for both `_zwift-protocol._tcp.local` and `_wahoo-fitness-tnp._tcp.local`. Nothing public implements `_zwift-protocol`, so `--zp-listen` advertises a `_zwift-protocol._tcp` instance (default name `Voltage Emulator`, TCP 36870, TXT `ble-service-uuids=0xFC82,00000001-19CA-4651-86E5-FA29DCDD09D1`, `mac-address`, `serial-number`) and runs a raw TCP listener that logs every received chunk as hex + ASCII (console lines `ZP RX ...`, also `ZP accepted/closed`, and notes if the bytes look like TLS or HTTP). If the first bytes are a valid DirCon header the connection is handed to a normal DirCon session (same GATT as the main server, `ZP TX` lines show our replies); otherwise nothing is sent. Options: `--zp-port N`, `--zp-name "<name>"`, `--zp-txt "k=v;k2=v2"` (adds or replaces TXT entries), `--zp-only` (no `_wahoo-fitness-tnp` advert). Allow the zp port through Windows Firewall as in section 1. Victory as Power Source/Resistance, as in the other plans.

- H(a) both adverts: `ZwiftLanProbe.exe --zp-listen`. On the pairing screen check whether a SECOND device (`Voltage Emulator`) appears next to the normal one, and watch the console for `ZP` lines.
- H(b) zp only: `ZwiftLanProbe.exe --zp-listen --zp-only`. Does Zwift list anything on its own?

Report what Zwift lists (under which category/icon), whether it connects, and paste all `ZP` lines (console or `logs\app-*.log`).

## Logs

- Probe: `tools/ZwiftLanProbe/bin/<Configuration>/net9.0/logs/app-yyyyMMdd-HHmmss.log`
- WPF app: `logs\app-yyyyMMdd-HHmmss.log` next to the exe (`AppContext.BaseDirectory`).

Network categories log at Debug (every DIRCON message RX/TX with msgId, seq, response code, characteristic UUID and value hex; ride/power notifications are sampled every 10th; keypad `0x23`/`0x37`/`0x07` frames, gear `0x42` frames, writes and everything on `...0004` are always logged; mDNS queries answered). Other categories log at Information.

## Test plan F (controller identity: does Zwift accept shifting from us if we do not claim to be a trainer?)

Hypothesis: Zwift classifies us from the 0x3c device-info reply (f8 capability list `(1,36) (2,0)` = trainer + user input) and the RideOn reply, and treats a trainer-claiming "KICKR BIKE" as a self-shifting bike, ignoring controller
shift frames while another trainer provides resistance. `--identity` changes only those replies (the KICKR advertisement / Wahoo icon is kept). Details: protocol notes, "Controller identity (device-info) research".

Setup for every run: Victory as **Power Source**, **Resistance** and **Cadence**; the probe only as **Controls**; the Zwift Click NOT paired. Pedal (the zero-cadence caveat of section 4.4 of the findings still applies). Stop the probe between runs and restart Zwift if it fails to reconnect.

1. `ZwiftLanProbe.exe --advert kickr --gatt nowahoo --identity ride`
2. `ZwiftLanProbe.exe --advert kickr --gatt nowahoo --identity clickv2`
3. `ZwiftLanProbe.exe --advert kickr --gatt nowahoo --identity play`
4. `ZwiftLanProbe.exe --advert kickr --gatt nowahoo --identity clickv1`

(Control: the same command with `--identity kickr` = today's behaviour.) For each run: is the probe listed under Controls, and is the Victory still accepted for Power+Resistance+Cadence? Then try `]` `[` `.` `,` (Ride shift bits), `c` `C` (Click v1 `0x37`),
`v` `V` (Play paddles `0x07`), `n` `N` `h` `j` (steer left/right), `k` `K` (brake paddles) and the arrow keys (menu). Note: does the gear display change (single number vs "front-rear"), does the Victory's resistance change (Zwift writes `3f 08 01 ...` to the trainer on a gear change - see the log
lines `Zwift wrote 0x3F`), does steering work, and which Zwift follow-up requests appear in the log (`No reply for get-request id ...`, the warnings printed at connect naming the values we could not determine). Attach the logs.

## Test plan I (mDNS manufacturer-data: does Zwift classify DirCon devices like BLE ones?)

Hypothesis: BikeControl's DirCon advert carries TXT `manufacturer-data=4A0906B653` (Zwift company id 0x094A little-endian, then a device-type byte and two id bytes) and Zwift classifies the device from it the way it does from BLE advertisement manufacturer data (Play 03/02, Ride 08/07, Click 09, Click v2 0B/0A).
Probe flags: `--mfg ride|clickv1|clickv2|play|bikecontrol` (presets 4A09 + type + B653), `--mfg-data <hex>` (raw), `--txt-extra "k=v;k2=v2"`, `--add-obc-uuid` (appends d273f680-d548-419d-b9d1-fa0472345229 to ble-service-uuids).

1. Close Zwift, then delete the emulator's entries from `Documents\Zwift\KnownDevices.xml` (or the whole file) so earlier classification is not cached. Restart Zwift.
2. Run each variant in turn (stop the probe and clear KnownDevices.xml between runs):
   - `ZwiftLanProbe.exe --advert kickr --gatt nowahoo --mfg bikecontrol` (control: exactly 4A0906B653)
   - `ZwiftLanProbe.exe --advert kickr --gatt nowahoo --mfg ride`
   - `ZwiftLanProbe.exe --advert kickr --gatt nowahoo --mfg clickv1`
   - `ZwiftLanProbe.exe --advert kickr --gatt nowahoo --mfg clickv2`
3. In Zwift pairing: Victory = Power + Resistance + Cadence (the Victory entry is the probe's trainer-style source), probe = Controls, via the Power Source -> Controls flow.
4. Try the keys `]` `[` `c` `C` `v` `V` `n` `N` and record what Zwift does for each.
5. Note, per variant, the icon and name Zwift shows for the probe in the Controls list (generic / Ride / Click / Play), whether it listed the device at all, and whether shifting responded.
6. Optional follow-ups: `--add-obc-uuid` on the best variant, and `--mfg-data` with other type bytes.


## Test plan J (BikeControl replica: does Zwift treat an exact copy of BikeControl's controller presentation like BikeControl?)

Hypothesis: `--preset bcride` reproduces everything BikeControl shows Zwift except the trainer relay (served GATT order and properties, 180A strings `BikeControl`/`1337`/`1.0`/`1.0`, `RideOn 02 00`, `RIDE_ON(0)`, the four `0x3c` replies, unanswered `41`/`44`/id 1280, and only the idle keypad `23 08 ff ff ff ff 0f` every 5 s on 00000002; no `0x03`/`0x42`/E03C, no FTMS/HR/power notifications) and BikeControl's mDNS (`--advert kickr --mfg bikecontrol --add-obc-uuid`, name `Voltage Emulator`). If Zwift lets BikeControl shift but not us, the remaining difference is the relayed trainer data or the instance name.
Preset: `--preset bcride` = `--advert kickr --gatt bcride --identity bcride --mfg bikecontrol --add-obc-uuid --no-gearing` (put `--preset` first; later flags such as `--name`, `--mfg`, `--manufacturer` override it).

1. Close Zwift, remove the emulator's entries from `Documents\Zwift\KnownDevices.xml` (or delete the file), start Zwift.
2. `ZwiftLanProbe.exe --preset bcride`.
3. In Zwift pairing: select the probe as **Power Source first** (Controls fills in automatically), then pick Power / Resistance / Cadence = **Victory**. Controls must stay on the probe.
4. In a ride press `=` `-` (shift), `q w e r` / `a s d f` (steer / brake) and the arrow keys (layout: Test plan K). Watch the gear number (and whether the Victory's resistance follows).
5. Repeat with `ZwiftLanProbe.exe --preset bcride --mfg ride` and then `--preset bcride --mfg clickv2` (clear KnownDevices.xml between runs).
6. Attach the logs. Expected log lines: `RideOn handshake completed`, no `ATX` strings, (with `--verbose`) `BikeControl replica: no reply to ...` for 41/44/1280 requests.

## Test plan K (keyboard: shifting, steering and braking in the Zwift Ride tutorial)

Verified 2026-11-13: `ZwiftLanProbe` with no arguments (= `--preset bcride`) is treated by Zwift as a Zwift Ride; menus, steering, braking and shifting work in its Ride tutorial using the confirmed Zwift mask table (BikeControl table; see the findings note "Button table confirmed"). Pair as in Test plan J, then:

| Key | Action | Value |
|---|---|---|
| `=` | RS1 tap (harder gear), 150 ms | `0x1000` |
| `0` | RS2 tap (harder gear) | `0x2000` |
| `-` | LS1 tap (easier gear) | `0x100` |
| `9` | LS2 tap (easier gear) | `0x200` |
| `[` / `]` | LB / RB tap (left / right side button) | `0x400` / `0x4000` |
| `y` / `Y` | Y tap (power-up) / Y hold 1.5 s (skip workout block) | `0x40` |
| `z` / `Z` | Z tap (Ride On) / Z hold 1.5 s (Ride On to 5 riders) | `0x80` |
| `q` | Steer full left (ZL outward) | ZL (loc 0) = -100 |
| `w` | Steer partial left | loc 0 = -P |
| `e` | Steer partial right | right paddle loc 1 = +P |
| `r` | Steer full right | loc 1 = +100 |
| `a` | Brake left full (paddles inward) | loc 0 = +100 |
| `s` | Brake left partial | loc 0 = +P |
| `d` | Brake right partial | loc 1 = -P |
| `f` | Brake right full | loc 1 = -100 |
| Arrows | Left / Up / Right / Down, tap | `0x1` `0x2` `0x4` `0x8` |
| Enter / Backspace | A (select) / B (back), tap | `0x10` / `0x20` |
| `p` / `P` | Toggle simulated ride 100 W / 80 rpm; `P` prompts `watts,rpm` | |
| `i` `?` `#` `@` | Status, help, raw bit, analog lab | |
| Ctrl+C | Clean shutdown | |

P = `--partial` (default 50). LPower (`0x800`) and RPower (`0x8000`) are unbound; reach them with `#` (bit 11, 15). ZL = analog location 0 (left paddle), ZR = location 1: tilt outward steers (ZL negative = left, ZR positive = right), inward brakes (ZL positive, ZR negative), range -100..100.

Hold semantics: a steer/brake key sets its paddle and starts a hold. Keyboard auto-repeat keeps resending the state every 100 ms; 550 ms (`--hold-ms`) after the last repeat the paddle returns to 0 and a neutral frame (buttons released, both paddles 0) is sent. Left and right paddles are independent, shift taps can happen during a hold (the button bit is merged into the current frame). Every frame sent is logged as `TX 0x23 keypad <hex> => pressed=0x... analog=[loc0=..., loc1=...]`.

Checks: (1) menus with arrows/Enter/Backspace; (2) tutorial steering step with `q`/`r` (then `w`/`e`); (3) braking step with `a`/`f`; (4) shifting with `=` `0` (harder) and `-` `9` (easier); (5) `[` `]` (LB/RB), `y`/`Y`, `z`/`Z`. Attach the log.

## Sim power check (bcride: does Zwift show simulated power/cadence?)

1. `ZwiftLanProbe.exe` (default `bcride`), pick the probe as Power Source + Cadence, press `p` to start the simulated ride (100 W, 80 rpm). The replica notifies FTMS 2AD2 at 4 Hz and Heart Rate 2A37 at 1 Hz. Zwift reads 2ACC and gets BikeControl's `8a 40 00 00 1e 20 00 00` (cadence + power measurement bits set).
2. If Zwift shows no power/cadence, clear `KnownDevices.xml` and run `ZwiftLanProbe.exe --add-power-service`: adds Cycling Power 1818 (2A63 notify, 2A65 feature `0c 01 00 00`) after FTMS, notifies 2A63 during the ride, and adds `0x1818` to the TXT `ble-service-uuids` if missing (already present with the kickr advert). Default stays the exact BikeControl clone.
3. Attach the log; check whether Zwift enables 2A63 notifications.

## Remote build watcher

For driving builds from a remote session without a shell on the Windows machine. From the repo root:

```
powershell -ExecutionPolicy Bypass -File tools\dev\build-watcher.ps1
```

It polls every 2 s for `.build-request` in the repo root. The file may be empty (default: `build`, `test-network`, `test-unit`) or list keywords one per line: `build`, `test`, `test-network`, `test-unit`, `clean`, or a custom line starting with `dotnet `. It renames the request to `.build-running`, runs the steps in order, writes `build.log`, `test-network.log`, `test-unit.log` (or `step-N.log` for custom lines), then `.build-done` (timestamp, exit code and duration per step, and a note if ZwiftLanProbe.exe or GameControllerForZwift.exe was running) and removes `.build-running`. A failed build stops the sequence. It never kills a running probe or app. All three marker files are git-ignored.

