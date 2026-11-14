# Troubleshooting

!!! tldr "TL;DR"
    If Zwift cannot see Voltage Gamepad, check that the app is running, that both devices share the same local network, and that Windows Firewall allows the app on private networks. For anything else, share the log file from the `logs` folder next to `GameControllerForZwift.exe`.

Most problems come down to Zwift not being able to see the app on the network. Work through the checks below in order.

## Zwift does not list Voltage Gamepad

1. **Is the app running?** Zwift only sees Voltage Gamepad while Game Controller for Zwift is open.
2. **Same network?** If Zwift runs on another device, that device and your Windows PC must be on the same local network. Guest Wi-Fi networks and some mesh or "client isolation" settings keep devices apart. A VPN on either device can do the same.
3. **Firewall allowed?** Windows Firewall must allow the app on **private networks**. If you clicked Cancel on the first-run prompt, open **Windows Security**, then **Firewall & network protection**, then **Allow an app through firewall**, and tick **Private** for Game Controller for Zwift.
4. **Private network profile?** Windows treats a network marked as **Public** more strictly. In Windows settings, open your Wi-Fi or Ethernet connection and set the network profile to **Private** if this is your home network.
5. **Reopen the pairing screen.** Close Zwift's pairing screen and open it again. Zwift searches for devices again when it opens.
6. **Only one copy.** Run only one copy of the app at a time.

!!! tip "Pair in the right order"
    Choose Voltage Gamepad as the **Power Source** first so that Zwift fills it in under **Controls**. Then switch Power Source back to your trainer. See [Pair with Zwift](pair-with-zwift.md).

## Zwift lost the connection

- Check that the app is still open and your PC has not gone to sleep.
- Open Zwift's pairing screen. If Voltage Gamepad shows as disconnected, select it again under **Controls**.
- If you restarted the app while Zwift was running and Zwift does not reconnect, restart Zwift as well.

When the app closes normally, it tells the network that Voltage Gamepad is leaving, so Zwift drops it straight away instead of waiting.

## A button does nothing in Zwift

- Check the mapping on the **Controller Setup** page. The row for an input lights up while you press it. If it does not light up, the app is not seeing that input. Try **Refresh List** and pick your controller again.
- Check that the input is not set to **No Action Configured**.
- Remember that mappings go back to the defaults each time the app starts.

## Find and share the log file

The app writes a log file every time it starts. It is in the `logs` folder next to `GameControllerForZwift.exe`:

```text
<folder where you unpacked the app>\logs\app-yyyyMMdd-HHmmss.log
```

The file name holds the date and time the app started, for example `app-20260314-183005.log`. The newest file is the one you want.

To share it:

1. Close the app so the log is complete.
2. Open an issue on the [Issues page](https://github.com/VoltageSolutions/GameControllerForZwift/issues) and describe what you did and what happened.
3. Attach the log file to the issue.

!!! note "What is in the log"
    The log records what the app sends to and receives from Zwift, plus the local network addresses it advertises on. Have a quick look before you share it publicly, and remove anything you would rather keep private.

The app also writes important messages to the Windows Event Log (under **Windows Logs**, then **Application**).
