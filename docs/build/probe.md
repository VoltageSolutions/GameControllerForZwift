# ZwiftLanProbe

!!! tldr "TL;DR"
    `ZwiftLanProbe` is a console app that runs the app's network side on its own: the Direct Connect server and the mDNS advertisement, presenting the same Zwift Ride as the app. You press keys on your keyboard and see exactly what Zwift does with each Zwift Ride button and paddle value. It is the quickest way to test protocol changes without a game controller.

## Run it

Close the main app first. Both use port 36866 and the same mDNS name.

```powershell
dotnet run --project tools/ZwiftLanProbe
```

With options:

```powershell
dotnet run --project tools/ZwiftLanProbe -- --name "Probe Test" --verbose --log-file logs/probe.log
```

Then pair it in Zwift the same way as the app (see [Pair with Zwift](../use/pair-with-zwift.md)). When Zwift connects, the probe prints:

```text
>>> Zwift connected, RideOn handshake complete.
```

## Options

| Option | Default | Meaning |
|---|---|---|
| `--name "<name>"` | `Voltage Gamepad` | The name shown in Zwift. Keep it short (20 characters or fewer). |
| `--port N` | `36866` | First TCP port to try (1 to 65535). If it is busy, the next 10 ports are tried. |
| `--partial N` | `50` | Paddle value (1 to 100) used by the "partial" steer and brake keys. |
| `--hold-ms N` | `550` | How long (at least 150 ms) a steer or brake key stays held after the last key repeat. |
| `--log-file <path>` | none | Also write every log line, at all levels, to this file. |
| `--verbose` | off | Show Debug messages on the console, including every Direct Connect message. |

Any other argument prints the usage line and exits.

## Keys

| Key | Action | Sent |
|---|---|---|
| `=` | RS1 tap (harder gear) | `0x1000` |
| `0` | RS2 tap (harder gear) | `0x2000` |
| `-` | LS1 tap (easier gear) | `0x100` |
| `9` | LS2 tap (easier gear) | `0x200` |
| `[` | LB tap (left side button) | `0x400` |
| `]` | RB tap (right side button) | `0x4000` |
| `q` | Steer full left | ZL (location 0) = -100 |
| `w` | Steer partial left | ZL = -partial |
| `e` | Steer partial right | ZR (location 1) = +partial |
| `r` | Steer full right | ZR = +100 |
| `a` | Brake, left paddle full | ZL = +100 |
| `s` | Brake, left paddle partial | ZL = +partial |
| `d` | Brake, right paddle partial | ZR = -partial |
| `f` | Brake, right paddle full | ZR = -100 |
| Arrow keys | Left, Up, Right, Down | `0x1`, `0x2`, `0x4`, `0x8` |
| Enter | A (select) | `0x10` |
| Backspace | B (back) | `0x20` |
| `y` / `Y` | Y tap / Y held for 1.5 s (skip workout block) | `0x40` |
| `z` / `Z` | Z tap / Z held for 1.5 s (Ride On to nearby riders) | `0x80` |
| `i` | Show status (connected, port, name, settings) | |
| `?` | Show the key table | |
| `#` | Press a raw bit: type 0 to 31, then Enter. Reaches LPower (bit 11) and RPower (bit 15). | `1 << n` |
| `@` | Analog lab: type `loc,value[,ms[,buttonsHex]]`, with several entries separated by `;` | Custom frame |
| Ctrl+C | Clean shutdown: neutral frame, mDNS goodbye, server stop | |

Taps last 150 ms. Steer and brake keys work like a held button: keyboard auto-repeat keeps the paddle set, and it returns to 0 once no repeat has arrived for `--hold-ms`. The left and right paddles are independent, and you can tap a shift key while a paddle is held.

Every frame the probe sends is logged with a decoded summary, for example:

```text
TX 0x23 keypad 2308FFDFFFFF0F1A04080010001A0408011000 => pressed=0x00001000 analog=[loc0=0, loc1=0]
```

!!! tip "Start fresh in Zwift"
    If a test run leaves Zwift confused, close Zwift and remove the probe's entries from `Documents\Zwift\KnownDevices.xml`. Zwift then forgets what it learned about the device.
