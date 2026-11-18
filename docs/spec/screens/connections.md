---
id: SPEC-SCR-CONN
title: Connections screens
last-updated: 2026-11-17
---

# Connections screens

!!! tldr "TL;DR"
    The Connections tab has two jobs: your controller and Zwift. For the controller you can use a different one or pair a new one. For Zwift you see its status, the three pairing steps with live progress where the app can tell, and a help screen, "Zwift can't find Voltage Gamepad", with live checks and the checks the app cannot make. The tab shows a dot whenever Zwift is not connected.

In the rules, "must", "should", and "may" have the meaning given in [how to read a spec](../index.md).

## The Connections screen

**What it is for:** see and fix both connections in one place.

**Content, top to bottom:** your controller (its name and state in words, with **Use a different controller** and **Pair a new Bluetooth controller**), then the Zwift card (`ConnectionsZwiftCard`) with its status, the three steps, and a **Show me how** button (`ConnectionsShowMeHowButton`), then a link to the help screen.

| State | What you see |
|---|---|
| Controller here | "Your Xbox controller is here" or "Awake and connected". |
| No controller | "Wake your controller" with "Looking for your controller...". Both ways out stay visible. |
| Zwift connected | "Zwift is connected to Voltage Gamepad" with a reminder to set Power Source back to your trainer. |
| Zwift not connected | "Zwift isn't connected yet", the three steps, and the tab dot. |

| Rule | ID |
|---|---|
| The screen must show your controller's name and its state in words. | SCR-CONN-001 |
| The screen must offer **Use a different controller** and **Pair a new Bluetooth controller**. Both must follow [Setup screens](setup.md). On Windows, **Pair a new Bluetooth controller** opens the Windows Bluetooth settings. **Use a different controller** is a dropdown of the connected controllers, and it stays visible but disabled until two or more are connected. | SCR-CONN-002 |
| With no controller, the screen must show the same "Wake your controller" state as Ride. | SCR-CONN-003 |
| The Zwift card must say "Zwift is connected to Voltage Gamepad" or "Zwift isn't connected yet", based on `IsZwiftConnected`. | SCR-CONN-004 |
| The card must show a live-updating status, and the app must announce it as a live region. | SCR-CONN-005 |
| When Zwift is not connected, the Connections tab must show its dot, and the card's main action must be **Pair with Zwift** (or **Show me how** if the steps are collapsed). | SCR-CONN-006 |
| When Zwift is connected, the screen must not use the accent fill for any action. | SCR-CONN-007 |
| The screen must link to the list of supported controllers once that list exists. | SCR-CONN-008 |

## The three Zwift steps

The steps are the same as on [Setup](setup.md#pair-with-zwift): Power Source set to Voltage Gamepad, then Controls set to Voltage Gamepad, then Power Source back to your trainer. Zwift needs this every session.

The app sees one thing: Zwift opened a connection and finished the handshake (`IsZwiftConnected`). When Zwift connects, it subscribes to everything at once: the controller characteristics, power, and heart rate. It does the same whether you picked Voltage Gamepad as Power Source or as Controls. So the app cannot tell which steps you took, and it never marks a step as done.

| Rule | ID |
|---|---|
| The steps must be instructions only. The app must not mark a step done, because it cannot see which steps the rider took (UX-W-006). | SCR-CONN-009 |
| The step list must name **Voltage Gamepad** as the device to pick. | SCR-CONN-010 |
| When Zwift is connected, the list should collapse to a one-line status and offer to show the steps again. | SCR-CONN-011 |
| While Zwift is connected, the card must show the reminder "Set Power Source back to your trainer in Zwift." under the status, whether or not the steps are open. It must not show while Zwift is not connected. | SCR-CONN-018 |

The steps are plain numbered text. When Zwift is not connected, the steps are always open and the button is hidden. When Zwift is connected, the steps start closed, and the **Show me how** button opens them and becomes **Hide steps**, which closes them again. The steps close again the next time Zwift connects.

While Zwift is connected, a one-line reminder sits under the status, whether the steps are open or closed (SCR-CONN-018).

## Zwift can't find Voltage Gamepad

**What it is for:** when Zwift does not list the app, find out why. The screen runs the checks it can and lists the ones it cannot, each with a fix.

**Content:** a short line, a list of checks each with a status in words and a fix, and a link to Help and feedback.

| Check | Can the app run it? | Fix |
|---|---|---|
| The app is running | Yes | Start the app and keep it running while you ride. |
| Network profile is Private (Windows) | Yes | In Windows settings, open your Wi-Fi or Ethernet connection and set the network profile to Private. |
| Firewall allows the app on private networks (Windows) | Yes | In Windows Security, open Firewall and network protection, then Allow an app through firewall, and tick Private for Game Controller for Zwift. |
| VPN is off (phone) | Where the platform allows | Turn off the VPN, or use split tunneling for your local network. |
| Zwift and the app are on the same network | No | Put both devices on the same local network. |
| Guest Wi-Fi or client isolation is not in the way | No | Use the main network. Guest Wi-Fi and some routers block devices from seeing each other. |

| Rule | ID |
|---|---|
| The screen must run the checks the app can run and show each result in words. Color alone must not carry the result. | SCR-CONN-012 |
| The screen must also list the checks the app cannot make, each with a fix, and must say that it cannot check them. | SCR-CONN-013 |
| Every check must have a fix written in plain words. | SCR-CONN-014 |
| Windows checks must cover the network profile and the firewall rule. Phone checks must cover VPNs. | SCR-CONN-015 |
| The screen must not claim a check passed unless the app actually ran it. | SCR-CONN-016 |
| You must be able to reach the screen from Connections and from Help and feedback. | SCR-CONN-017 |

??? note "Edge cases"
    - The checks that the app cannot make still appear, so a rider with guest Wi-Fi finds the fix.
    - If every check the app can run passes and Zwift still cannot find the app, the screen points to Help and feedback to report a problem with the log.

## Controller input on these screens

On Connections, a controller press goes to Zwift as mapped. This is not a screen where buttons are assigned. See [Controller input](../ux/controller-input.md).

## Accessibility IDs

| ID | Where |
|---|---|
| `NavConnectionsTab` | The tab. Its name adds ", Zwift not connected" when the dot shows. |
| `ConnectionsControllerCard` | The controller card |
| `ConnectionsControllerName` | The controller's name and state line |
| `ConnectionsControllerPicker` | The **Use a different controller** dropdown |
| `ConnectionsPairBluetoothButton` | **Pair a new Bluetooth controller** |
| `ConnectionsZwiftCard` | The Zwift card |
| `ConnectionsZwiftStatus` | The Zwift status line |
| `ConnectionsTrainerReminder` | The reminder to set Power Source back to your trainer |
| `ConnectionsStep1` to `ConnectionsStep3` | The step texts |
| `ConnectionsShowMeHowButton` | The steps button |
