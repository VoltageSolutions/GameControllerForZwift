# Testing the Zwift LAN controller

The app (and the standalone probe) present themselves to Zwift as a Zwift Ride controller over LAN (Wahoo Direct Connect, "DIRCON": Bluetooth LE GATT tunnelled over TCP, plus mDNS). There is a single presentation (`ZwiftRideProfile`); the earlier KICKR / FTMS / Click / Play variants were research and are gone (see "Historical test plans").
The probe lets you press individual buttons by hand and see what Zwift does, without needing a game controller.

## 1. Allow the traffic through Windows Firewall

Run once in an elevated PowerShell (Private network profile; TCP 36866 is the Direct Connect port, UDP 5353 is mDNS):

```powershell
New-NetFirewallRule -DisplayName "Zwift LAN controller (DIRCON TCP 36866)" -Direction Inbound -Protocol TCP -LocalPort 36866 -Action Allow -Profile Private
New-NetFirewallRule -DisplayName "Zwift LAN controller (mDNS UDP 5353)" -Direction Inbound -Protocol UDP -LocalPort 5353 -Action Allow -Profile Private
```

If the port was busy the server falls back up to +10 (36867..36876); the console and log show the port actually used.
Zwift and this PC must be on the same subnet (Zwift on the same PC is fine; a phone/tablet/Apple TV works if they share the LAN).

## 2. Run the probe

```
dotnet run --project tools/ZwiftLanProbe
dotnet run --project tools/ZwiftLanProbe -- --name "Voltage Emulator" --port 36866 --verbose --log-file logs/probe.log
```

Options: `--name` (instance name; keep it short, 20 characters or fewer; default `Voltage Gamepad`), `--port` (first port to try, default 36866), `--partial N` (1..100, default 50, the partial paddle value), `--hold-ms N` (default 550, the hold timeout of the steer and brake keys), `--log-file <path>` (also write every log line to this file; without it the probe logs to the console only), `--verbose` (Debug level on the console: every Direct Connect message with msgId, seq, response code, characteristic UUID and value hex). Stop the probe with Ctrl+C (neutral frame, mDNS goodbye, server stop).

## 3. Pair in Zwift and press keys

1. Start the probe (stop the main app first, or both will fight over port 36866 and the mDNS name).
2. Pairing flow that works: in Zwift's pairing screen select the probe as **Power Source first** (Controls fills in automatically), then pick Power / Resistance / Cadence = your trainer. Controls must stay on the probe. The probe prints `Zwift connected, RideOn handshake complete.`
3. Press keys in the probe window (see "Keyboard test plan"; `?` prints the table).
4. `#` then a bit number 0-31 and Enter presses raw bit `1 << n` for 150 ms, to map unknown buttons (LPower / RPower) empirically.
5. Note what Zwift does for each key. If a run misbehaves, close Zwift and remove the probe's entries from `Documents\Zwift\KnownDevices.xml` so earlier classification is not cached.

## Historical test plans (code removed)

These plans drove probe options and keys that no longer exist (`--preset`, `--profile`, `--advert`, `--gatt`, `--identity`, `--mfg`, `--zp-*`, `--add-power-service`, `--masks`, simulated ride, 0x42 gear, Click and Play frames). They are summarised here for the results only; the commands are in git history before the Ride-only refactor. Detailed results are in [zwift-lan-findings.md](zwift-lan-findings.md).

| Plan | Question | Outcome |
|---|---|---|
| Steering lab | Which analog frames move the steering indicator? | Led to the Ride paddle frames (locations 0 and 1, +/-100) that the app now sends; the `@` analog lab remains in the probe. |
| B (ride simulation, gears, steering) | Does Zwift react to shift/steer only while a ride is simulated? | Not needed with the Ride presentation. |
| C (separate controller device, trainer elsewhere) | Does a controller-only device work next to a real trainer? | Yes, once the device is presented as a Ride (see J, K). |
| E (bisect: what makes Zwift list the device?) | Which advertisement / GATT parts does Zwift filter on? | Zwift lists devices with the KICKR style advertisement; FTMS-only or FC82-only TXT were not listed. |
| F (controller identity) | Can a KICKR-advertised device present itself as Ride / Click / Play? | The BikeControl Ride replies are what works. |
| H (`_zwift-protocol._tcp`) | Does Zwift do anything with that service? | No implementation found; the experiment code was removed. |
| I (mDNS manufacturer-data) | Does Zwift classify DIRCON devices from it? | Yes: `manufacturer-data=4A0906B653` is part of the shipped advertisement. |
| J (BikeControl replica) | Does an exact copy of BikeControl's presentation work? | Verified 2026-11-13: Zwift treats it as a Zwift Ride. This is the shipped `ZwiftRideProfile`. |
| Sim power check | Does Zwift show simulated power and cadence? | No; removed (the Ride presentation does not relay trainer data). |

## Logs

- Probe: console, plus the file given with `--log-file <path>` (all levels, one timestamped line per entry).
- WPF app: `logs\app-yyyyMMdd-HHmmss.log` next to the exe (`AppContext.BaseDirectory`), written by `FileLoggerProvider` next to the Windows Event Log provider.

Network categories log at Debug in the app's file (every Direct Connect message RX/TX with msgId, seq, response code, characteristic UUID and value hex; keypad `0x23` frames, writes and everything on `...0004` are always logged; mDNS queries answered). Other categories log at Information.

## Keyboard test plan (shifting, steering and braking in the Zwift Ride tutorial)

Verified 2026-11-13: `ZwiftLanProbe` is treated by Zwift as a Zwift Ride; menus, steering, braking and shifting work in its Ride tutorial using the confirmed Zwift mask table (BikeControl table; see the findings note "Button table confirmed"). Pair as in section 3, then:

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
| `i` `?` `#` `@` | Status, help, raw bit, analog lab | |
| Ctrl+C | Clean shutdown | |

P = `--partial` (default 50). LPower (`0x800`) and RPower (`0x8000`) are unbound; reach them with `#` (bit 11, 15). ZL = analog location 0 (left paddle), ZR = location 1: tilt outward steers (ZL negative = left, ZR positive = right), inward brakes (ZL positive, ZR negative), range -100..100.

Hold semantics: a steer/brake key sets its paddle and starts a hold. Keyboard auto-repeat keeps resending the state every 100 ms; 550 ms (`--hold-ms`) after the last repeat the paddle returns to 0 and a neutral frame (buttons released, both paddles 0) is sent. Left and right paddles are independent, shift taps can happen during a hold (the button bit is merged into the current frame). Every frame sent is logged as `TX 0x23 keypad <hex> => pressed=0x... analog=[loc0=..., loc1=...]`.

Checks: (1) menus with arrows/Enter/Backspace; (2) tutorial steering step with `q`/`r` (then `w`/`e`); (3) braking step with `a`/`f`; (4) shifting with `=` `0` (harder) and `-` `9` (easier); (5) `[` `]` (LB/RB), `y`/`Y`, `z`/`Z`. Attach the log.

## Remote build watcher

For driving builds from a remote session without a shell on the Windows machine. From the repo root:

```
powershell -ExecutionPolicy Bypass -File tools\dev\build-watcher.ps1
```

It polls every 2 s for `.build-request` in the repo root. The file may be empty (default: `build`, `test-network`, `test-unit`) or list keywords one per line: `build`, `test`, `test-network`, `test-unit`, `clean`, or a custom line starting with `dotnet `. It renames the request to `.build-running`, runs the steps in order, writes `build.log`, `test-network.log`, `test-unit.log` (or `step-N.log` for custom lines), then `.build-done` (timestamp, exit code and duration per step, and a note if ZwiftLanProbe.exe or GameControllerForZwift.exe was running) and removes `.build-running`. A failed build stops the sequence. It never kills a running probe or app. All three marker files are git-ignored.

