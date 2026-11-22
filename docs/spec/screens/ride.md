---
id: SPEC-SCR-RIDE
title: Ride screens
last-updated: 2026-11-22
---

# Ride screens

!!! tldr "TL;DR"
    Ride is the home tab. It shows your controller, honest stats, your mapping, and whether Zwift can see you. There is no Start riding button: a ride starts by itself when a controller and Zwift are both connected, in any order. A brief loss of either does not end it. It ends when the app closes, or when the ride timeout (10 minutes by default, set in Settings) passes with no button press or with either connection missing. While riding you get one big readout of your last press. Problems are said in words: "Your controller is fine", "Wake your controller".

In the rules, "must", "should", and "may" have the meaning given in [how to read a spec](../index.md).

```mermaid
flowchart TD
    W["Home: waiting<br/>(controller, Zwift, or both missing)"] -->|"Both connected,<br/>in any order"| R["Riding view"]
    W -->|"Go to Connections"| C["Connections"]
    R -->|"Zwift lost"| L["Zwift lost the connection"]
    R -->|"Controller asleep"| S["Controller asleep"]
    L -->|"Zwift back"| R
    S -->|"Controller back"| R
    R -->|"Ride timeout:<br/>no press, or no connection"| F["Ride finished"]
    L -->|"Ride timeout"| F
    S -->|"Ride timeout"| F
    F --> W
```

## Home

![The VOLTAGE wordmark in light mode](../../assets/images/voltage_wordmark_light.svg#only-light)
![The VOLTAGE wordmark in dark mode](../../assets/images/voltage_wordmark_dark.svg#only-dark)

**What it is for:** the screen you see every day. It tells you that you are ready, or what is missing. The controller and Zwift can connect in any order. Until both are connected, Home says which is missing and suggests going to Connections.

**Content, top to bottom:** the VOLTAGE wordmark (`RideWordmark`), the screen title "Ready when you are" (`RidePageTitle`), your current mapping as a row (`RideChangeMappingButton`), the Zwift status (`RideZwiftStatus`), and your controller card (for example "Your Xbox controller", with "Awake and connected" under it, `RideControllerCard`) with your stats (`RideStats`) as three numbers: rides together, shifts, and riding since. There is no Start riding button.

*Example: "12 rides together, 1,086 shifts, riding since September."*

| Rule | ID |
|---|---|
| The title must be "Ready when you are". | SCR-RIDE-001 |
| The controller card must name your controller and say its state in words, such as "Awake and connected". | SCR-RIDE-002 |
| The stats must show only real values the app counts: rides together, shifts, and riding since. The app must count them and keep them between runs. Until there is something real to show, the app must hide the stats or say nothing. | SCR-RIDE-003 |
| The screen must show the selected mapping and let you change it by going to Buttons. | SCR-RIDE-004 |
| The Zwift status must say "Zwift is connected to Voltage Gamepad" when `IsZwiftConnected` is true. Zwift can see Voltage Gamepad whether or not a controller is connected. | SCR-RIDE-005 |
| Home must not have a **Start riding** button or any other control that starts a ride. The app must work without one. | SCR-RIDE-006 |
| The status text must be a live region and announce only changes. | SCR-RIDE-007 |
| When the controller, Zwift, or both are not connected, Home must say which is missing and offer a way to [Connections](connections.md): **Use a different controller** and **Pair a new Bluetooth controller** with no controller, and **Pair with Zwift** when Zwift is not connected. | SCR-RIDE-040 |
| Home must show the VOLTAGE wordmark at the top of the content, above the title (`RideWordmark`). In light mode, the letters VOLT must use the accent color and AGE must be near-black. In dark mode, all letters must be white. It must have the accessible name "VOLTAGE". | SCR-RIDE-034 |
| Home must have one level 1 heading, the title, and no other level 1 heading (UX-A-021). | SCR-RIDE-035 |

## Home, no controller

**What it is for:** every rider will open the app with the controller turned off, asleep, or unplugged. The screen says so and shows the way out.

**Copy:** title "Wake your controller", status "Looking for your controller...".

**Content:** a faded controller, the status, and two ways out: **Use a different controller**, and **Pair a new Bluetooth controller** (opens Windows settings). The Connections tab shows its dot.

| Rule | ID |
|---|---|
| When no controller is found after first run, Ride must show "Wake your controller" with "Looking for your controller..." and a faded controller. | SCR-RIDE-008 |
| It must offer two ways out: use a different controller, and pair a new Bluetooth controller. | SCR-RIDE-009 |
| The screen must return to Home by itself when the controller wakes, and announce that it is back. | SCR-RIDE-011 |

## Home, Zwift not connected

**What it is for:** the state at the start of every Zwift session. Zwift needs the three pairing steps again, so this is the most repeated task in the app.

**Copy:** "Zwift isn't connected yet", with a short version of the three steps. The main action is **Pair with Zwift**.

| Rule | ID |
|---|---|
| When Zwift is not connected, Home must say "Zwift isn't connected yet" instead of "Zwift is connected to Voltage Gamepad". | SCR-RIDE-012 |
| Home must show a short version of the three steps and make **Pair with Zwift** the main action. | SCR-RIDE-013 |
| **Pair with Zwift** must open the live pairing steps on [Connections](connections.md). | SCR-RIDE-014 |
| The Connections tab must show its dot while Zwift is not connected (UX-N-002). | SCR-RIDE-015 |

## Riding view

**What it is for:** a glance from the saddle. One big readout, nothing else competing.

A ride starts when the app knows both that a controller is connected and that Zwift is connected. It continues until you close the app, or until the ride timeout passes with no button press or with the controller or Zwift disconnected. A short loss of either connection does not end the ride. There is nothing to press to start or stop it.

**Content:** the big "You just pressed" readout (`RideLastPressStatus`, for example "Shift up, right bumper"), shifts this ride, and a quick reference of your main buttons.

| Rule | ID |
|---|---|
| A ride must start when a controller and Zwift are both connected, with no action from you. | SCR-RIDE-036 |
| A ride must end when the app closes, or when for the ride timeout in Settings (SCR-SET-012) no controller button has been pressed, or the controller has been disconnected, or Zwift has been disconnected. The default timeout is 10 minutes. | SCR-RIDE-037 |
| A ride must not end when the controller or Zwift disconnects for less than the ride timeout. The ride must return to the riding readout when the connection returns. | SCR-RIDE-041 |
| The riding view must have one big readout (the last press) readable at arm's length. | SCR-RIDE-016 |
| The riding view must show shifts this ride and a quick reference of the main buttons, which are the buttons the current mapping sets to Shift up and Shift down. | SCR-RIDE-017 |
| The riding view must have no **Stop riding** button. | SCR-RIDE-038 |
| The riding view must show the same problems (Zwift lost, controller asleep) as the other Ride states. | SCR-RIDE-020 |
| The riding view must follow platform limits in [Platforms](../ux/platforms.md). | SCR-RIDE-021 |

??? note "Edge cases"
    - At 200% text size the readout wraps to two lines before it shrinks.
    - You can resize the window by hand. The riding view must stay readable at every size.

## Zwift lost the connection

**What it is for:** tell you the controller is fine and Zwift is the one that needs a nudge.

**Copy:** "Your controller is fine." The ride continues until the ride timeout. Then: pick **Voltage Gamepad** under **Controls** in Zwift again.

| Rule | ID |
|---|---|
| When `IsZwiftConnected` becomes false while riding, the app must say "Your controller is fine" and explain what to pick in Zwift. | SCR-RIDE-023 |
| The app must announce the message right away. | SCR-RIDE-024 |
| The screen must not move focus when it appears. | SCR-RIDE-025 |

## Controller asleep while riding

**What it is for:** the controller went to sleep or disconnected mid-ride.

| Rule | ID |
|---|---|
| When the controller goes to sleep or disconnects while riding, the riding view must say the controller disconnected, and ask the person to wake it to reconnect and keep riding. It must not say the controller fell asleep, because not every controller sleeps. | SCR-RIDE-026 |
| The app must announce the message. | SCR-RIDE-027 |
| When the controller is back, the app must say so and return to the riding readout. | SCR-RIDE-028 |

## Ride finished

**What it is for:** show a short honest summary and, once, ask for support.

**Content:** time, shifts, and rides together. The one-time donation ask: **Chip in on Ko-fi** or **Maybe later**.

**Copy:** a short line about supporting the project.

| Rule | ID |
|---|---|
| The summary must show only real values: ride time, shifts, and rides together. | SCR-RIDE-029 |
| The donation ask must appear at most once, on a ride finished screen, with "Chip in on Ko-fi" and "Maybe later". | SCR-RIDE-030 |
| The ask must never block anything. "Maybe later" must dismiss it, and the app must not ask again. The app must keep that choice between runs. | SCR-RIDE-031 |
| The app must frame the ask as supporting the project. It must not claim impact the app cannot verify. | SCR-RIDE-032 |
| **Chip in on Ko-fi** must open the Ko-fi page, as it does on [Support](support.md). | SCR-RIDE-039 |
| Ride finished must have a **Done** button that returns to Home. | SCR-RIDE-042 |
| After a ride ends while the controller and Zwift are still connected, the next button press must start a new ride. | SCR-RIDE-043 |
| On a window at least 900 px wide, Home must show two columns: the title, the mapping row, and the Zwift status on the left, and the controller card on the right. Below 900 px it must show one column in the same order. The mapping and Zwift status must be rows as in [Visual system](../ux/visual-system.md#rows-and-two-column-screens). | SCR-RIDE-044 |
| The riding view must start with two status chips, one for the controller and one for Zwift, each with a dot in `ok` when it works and `err` when it does not, and the words say the same ("Controller ready", "Zwift sees it", "Zwift lost"). Under them it must say "Riding with" and the mapping name. | SCR-RIDE-045 |
| The big readout must show the pressed input as an accent pill with its short label, such as "RB", before the function's name, such as "Shift up". A press of an input with no function must not change the readout. | SCR-RIDE-046 |

## Controller input on these screens

On Home and the riding view, a controller press goes to Zwift as mapped. See [Controller input](../ux/controller-input.md).

| Rule | ID |
|---|---|
| Controller input on Ride must follow UX-C-004. | SCR-RIDE-033 |

## Accessibility IDs

| ID | Where |
|---|---|
| `RideWordmark`, `RidePageTitle`, `RideControllerCard`, `RideStats`, `RideChangeMappingButton`, `RideZwiftStatus`, `RidePairWithZwiftButton`, `RideUseDifferentControllerButton`, `RidePairBluetoothButton` | Home |
| `RideLastPressStatus`, `RideProblem`, `RideRecovery` | Riding view |
| `RideFinishedTitle`, `RideSummary`, `RideDonationAsk`, `RideChipInButton`, `RideMaybeLaterButton`, `RideDoneButton` | Ride finished |
| `NavConnectionsTab` | Name adds ", Zwift not connected" when the dot shows |

## Quoted strings

| Key | English text | Rule |
|---|---|---|
| `RideTitle` | Ready when you are | SCR-RIDE-001 |
| `RideControllerHere` | Your {0} is here | SCR-RIDE-002 |
| `RideControllerName` | Your {0} | SCR-RIDE-002 |
| `RideControllerAwake` | Awake and connected | SCR-RIDE-002 |
| `ZwiftConnected` | Zwift is connected to Voltage Gamepad | SCR-RIDE-005 |
| `ZwiftNotConnected` | Zwift isn't connected yet | SCR-RIDE-012 |
| `RideNoControllerTitle` | Wake your controller | SCR-RIDE-008 |
| `RideNoControllerStatus` | Looking for your controller... | SCR-RIDE-008 |
| `ConnectionsPickerLabel` | Use a different controller | SCR-RIDE-009 |
| `ConnectionsPairBluetooth` | Pair a new Bluetooth controller | SCR-RIDE-009 |
| `RidePairWithZwift` | Pair with Zwift | SCR-RIDE-013 |
| `RideMappingLabel` | Mapping | SCR-RIDE-004 |
| `RideChangeMapping` | Change | SCR-RIDE-004 |
| `RideWordmarkName` | VOLTAGE | SCR-RIDE-034 |
| `RideStatsRidesOne` | 1 ride together | SCR-RIDE-003 |
| `RideStatsRidesOther` | {0} rides together | SCR-RIDE-003 |
| `RideStatsShiftsOne` | 1 shift | SCR-RIDE-003 |
| `RideStatsShiftsOther` | {0} shifts | SCR-RIDE-003 |
| `RideStatsSince` | Riding since {0} | SCR-RIDE-003 |
| `RideStatRidesOne` | ride together | SCR-RIDE-003 |
| `RideStatRidesOther` | rides together | SCR-RIDE-003 |
| `RideStatShiftsOne` | shift | SCR-RIDE-003 |
| `RideStatShiftsOther` | shifts | SCR-RIDE-003 |
| `RideStatSince` | riding since | SCR-RIDE-003 |
| `RideChipControllerReady` | Controller ready | SCR-RIDE-045 |
| `RideChipControllerGone` | Controller disconnected | SCR-RIDE-045 |
| `RideChipZwiftSees` | Zwift sees it | SCR-RIDE-045 |
| `RideChipZwiftLost` | Zwift lost | SCR-RIDE-045 |
| `RideRidingWith` | Riding with | SCR-RIDE-045 |
| `RideJustPressed` | You just pressed | SCR-RIDE-016 |
| `RidePressFormat` | {0}, {1} | SCR-RIDE-016 |
| `RideNoPress` | Nothing yet | SCR-RIDE-016 |
| `RideShiftsThisRide` | Shifts this ride: {0} | SCR-RIDE-017 |
| `RideQuickReference` | Your main buttons | SCR-RIDE-017 |
| `RideQuickReferenceFormat` | {0}: {1} | SCR-RIDE-017 |
| `RideZwiftLostTitle` | Your controller is fine | SCR-RIDE-023 |
| `RideZwiftLostBody` | Pick Voltage Gamepad under Controls in Zwift again. | SCR-RIDE-023 |
| `RideControllerAsleepTitle` | Your controller disconnected. | SCR-RIDE-026 |
| `RideControllerAsleepBody` | Wake it up to reconnect and continue riding. | SCR-RIDE-026 |
| `RideControllerBack` | Your controller is back | SCR-RIDE-028 |
| `RideFinishedTitle` | Ride finished | SCR-RIDE-029 |
| `RideFinishedTime` | Ride time: {0} | SCR-RIDE-029 |
| `RideFinishedShifts` | Shifts: {0} | SCR-RIDE-029 |
| `RideFinishedRides` | Rides together: {0} | SCR-RIDE-029 |
| `RideDurationHoursMinutes` | {0} h {1} min | SCR-RIDE-029 |
| `RideDurationMinutes` | {0} min | SCR-RIDE-029 |
| `RideAskPitch` | This app is free. If it's earned a place in your rides, you can support the project. | SCR-RIDE-032 |
| `SupportKofi` | Chip in on Ko-fi | SCR-RIDE-030 |
| `RideAskLater` | Maybe later | SCR-RIDE-030 |
| `RideFinishedDone` | Done | SCR-RIDE-042 |
