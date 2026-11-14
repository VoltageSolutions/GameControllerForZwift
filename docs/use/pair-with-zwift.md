# Pair with Zwift

The app shows up in Zwift as a controller named **Voltage Gamepad**. You pair it once on Zwift's pairing screen, the same way you would pair a Zwift Ride.

## Same computer or a different device?

Both work.

- **Same computer:** run the app and Zwift on the same Windows PC.
- **Different devices:** run the app on a Windows PC and Zwift on another device, such as a Mac, tablet, phone or Apple TV. Both devices must be on the **same local network** (for example the same home Wi-Fi).

## Step by step

1. Start Game Controller for Zwift and pick your controller (see [Install and first run](install.md)).
2. Start Zwift and open the **pairing screen**.
3. Under **Power Source**, choose **Voltage Gamepad** first. Zwift then fills in **Controls** with Voltage Gamepad for you.
4. Now switch **Power Source**, **Resistance** and **Cadence** to your real trainer (and your heart rate monitor, if you use one).
5. Keep **Controls** on **Voltage Gamepad**.
6. Start riding.

!!! tip "Why choose Power Source first?"
    Picking Voltage Gamepad as the Power Source is the quickest way to get Zwift to place it under Controls. Once Controls is filled in, you move your trainer back into the Power Source, Resistance and Cadence slots. Voltage Gamepad does not send power or cadence, so your trainer must provide those.

```mermaid
flowchart TD
    a["Open the pairing screen"] --> b["Power Source: Voltage Gamepad"]
    b --> c["Zwift fills Controls:<br/>Voltage Gamepad"]
    c --> d["Power Source, Resistance, Cadence:<br/>your trainer"]
    d --> e["Controls stays on<br/>Voltage Gamepad"]
    e --> f["Ride On!"]
```

## What connected looks like

- On the pairing screen, **Controls** shows **Voltage Gamepad** as connected.
- In a ride, pressing your shift buttons changes gear, and pressing your navigation buttons moves through Zwift's menus.
- The app's log file records `DIRCON RideOn handshake completed` when Zwift has connected. See [Troubleshooting](troubleshooting.md#find-and-share-the-log-file) for where to find the log.

If Voltage Gamepad does not appear, see [Troubleshooting](troubleshooting.md).

## Next step

Choose what each button does in [Map your controller](map-your-controller.md).
