---
id: SPEC-UX-ACCESS
title: Accessibility
last-updated: 2026-11-17
---

# Accessibility

!!! tldr "TL;DR"
    Target WCAG 2.2 AA on every platform. Every color pair is checked. Every tap or click target is at least 48 by 48 px. Every interactive or meaningful element has a stable ID (the same string on Windows, Android and iOS), a spoken name, a role, and, where it changes, a live status.

In the rules, "must", "should", and "may" have the meaning given in [how to read a spec](../index.md).

## Color contrast

Text needs 4.5:1. Icons, focus rings, status dots, and graphics people need to see need 3:1. Color is never the only signal: every status also has words. The full pair table is in the [visual system](visual-system.md#contrast).

| Rule | ID |
|---|---|
| Text pairs must reach 4.5:1 and meaningful non-text pairs 3:1, in both modes. | UX-A-001 |
| Every status must have words as well as color. | UX-A-002 |
| Accent text, icons, and dots must use `accent-ink` (`#ED7600` in dark mode, `#A55200` in light mode). | UX-A-003 |
| Text on the accent fill must be `#1F1B16` (5.86:1). The app must not use white on the accent (2.92:1). | UX-A-004 |
| The app must follow the system dark or light setting. | UX-A-005 |
| When a Windows contrast theme is on, the app must use system colors. | UX-A-006 |

Fixes made to pairs that failed:

| Problem | Before | Fix |
|---|---|---|
| Connections tab dot, light mode | Amber placeholder at 1.57:1 | `accent-ink` `#A55200`, 5.20:1 on `bg` |
| Lit button on the controller drawing, light mode | Accent on the body at 1.39:1 | Dark outline around the lit key |
| Keys against the body, both modes | 1.2:1 | Thin muted outline on every key, dashed outline on free keys |

| Rule | ID |
|---|---|
| The lit key must have a dark outline. Every key must have a thin muted outline, and free keys a dashed one. | UX-A-007 |
| The Buttons list must carry the same information as the drawing in text, so the drawing is never the only way to know a mapping. | UX-A-008 |

??? note "Edge cases"
    In light mode the accent fill is 2.75:1 against the background. A primary button is identified by its label and shape, never by fill alone (UX-V-005).

## Tap and click targets

| Rule | ID |
|---|---|
| Anything you can tap, click or focus must have a hit area of at least 48 by 48 px. | UX-A-009 |
| The visual may be smaller than the hit area. The More button is a 40 px circle inside a 48 px target. | UX-A-010 |
| Text links must have a 48 px tall hit area. | UX-A-011 |
| Neighboring targets must be at least 8 px apart. | UX-A-012 |

??? note "Edge cases"
    WCAG 2.2 AA asks for 24 px. Material asks for 48 dp and Apple for 44 pt. The app goes beyond all of them.

## IDs, names, and roles

### ID naming scheme

An ID is `{Area}{Element}{Role}` in PascalCase. IDs never change with language or state. Names are translated and may include state.

| | Windows (WPF or WinUI) | Android (Compose) | iOS (SwiftUI) |
|---|---|---|---|
| **ID** (tests, tools) | `AutomationProperties.AutomationId` | `Modifier.testTag` with `testTagsAsResourceId = true` | `.accessibilityIdentifier` |
| **Name** (read out) | `AutomationProperties.Name` | `contentDescription` or semantics | `.accessibilityLabel` |
| **Hint** | `AutomationProperties.HelpText` | `onClickLabel`, `stateDescription` | `.accessibilityHint` |
| **Heading** | `AutomationProperties.HeadingLevel` | `semantics { heading() }` | `.accessibilityAddTraits(.isHeader)` |
| **Live status** | `AutomationProperties.LiveSetting="Polite"` and LiveRegionChanged | `liveRegion = LiveRegionMode.Polite` | Announcement notification |
| **Hide decoration** | `AutomationProperties.AccessibilityView="Raw"` | `clearAndSetSemantics {}` | `.accessibilityHidden(true)` |

| Part | Values |
|---|---|
| Areas | `Setup`, `Nav`, `Menu`, `Ride`, `Buttons`, `Mapping`, `Connections`, `Settings`, `Help`, `Credits`, `Support`, `Dialog` |
| Roles | `Button`, `Tab`, `Item`, `Link`, `Toggle`, `Field`, `Status`, `Title`, `Row`, `Card`, `Image` |

Repeated items add a key after an underscore: `ButtonsRow_RB`, `FunctionItem_ShiftUp`, `MappingItem_{id}`.

| Rule | ID |
|---|---|
| Every interactive or meaningful element must have an ID, a name, and a role. | UX-A-013 |
| IDs must follow `{Area}{Element}{Role}` in PascalCase, with the same string on every platform. | UX-A-014 |
| IDs must not change with language or state. | UX-A-015 |
| A mapping's ID key must be the mapping's GUID (see `CFG-008`), never its name. | UX-A-016 |
| The app must hide decorative elements from assistive technology. | UX-A-017 |

### Core IDs

| ID | Role | Name read out |
|---|---|---|
| `NavRideTab`, `NavButtonsTab`, `NavConnectionsTab` | Tab (1 to 3 of 3) | "Ride", "Buttons", "Connections" (plus ", Zwift not connected" when the dot shows) |
| `MenuButton` | Button, expands | "More: settings, help, credits, support" or "Close menu" |
| `MenuSettingsItem`, `MenuHelpItem`, `MenuCreditsItem`, `MenuSupportItem` | Menu item | "Settings", "Help and feedback", "Credits", "Support this project" |
| `SetupPressAnyButtonStatus` | Live status | "Got it. That's your left bumper" |
| `SetupContinueButton`, `SetupPairBluetoothButton` | Button | "Continue", "Pair a new Bluetooth controller" |
| `SetupZwiftStatus` | Live status | "Waiting for Zwift" or "Zwift is connected" |
| `RidePageTitle` | Heading 1 | "Ready when you are" |
| `RideControllerCard` | Group | "Your Xbox controller, awake and connected" |
| `RideStats` | Group | "12 rides together, 1,086 shifts, riding since September" |
| `RideZwiftStatus` | Live status | "Zwift can see your controller" or "Zwift isn't connected yet" |
| `RideStartButton` | Button | "Start riding" |
| `RideLastPressStatus` | Live status | "Shift up, right bumper" |
| `RideWindowSizeGroup` | Radio group | "Window size: Full, Float, Mini" |
| `RideStopButton` | Button | "Stop riding" |
| `ButtonsMappingPicker` | Button, opens | "Mapping: Climbing, unsaved changes" |
| `ButtonsSaveButton`, `ButtonsSaveAsButton` | Button | "Save", "Save as new" |
| `ButtonsRow_{input}` | Button | "Right bumper: shift up" |
| `ButtonsControllerImage` | Image | "Your controller. Right bumper selected. X and Menu are free" |
| `FunctionItem_{function}` | Radio | "Shift up, harder gear, selected" |
| `ConnectionsZwiftCard`, `ConnectionsShowMeHowButton` | Group, Button | Per screen |
| `DialogPrimaryButton`, `DialogSecondaryButton`, `DialogCancelButton` | Button | Per dialog |

| Rule | ID |
|---|---|
| Screens must use the core IDs above where the element exists. | UX-A-018 |
| The example numbers in the name column (12 rides, 1,086 shifts) are examples. The app must show only real values (UX-P-005). | UX-A-019 |

??? note "Edge cases"
    The existing UI tests locate elements by `SettingsButton`, `PageTitle`, `OpenGitHubButton`, `OpenkofiButton`, and the tree item "Controller Setup". They move to the new IDs (for example `MenuSettingsItem`, `CreditsGitHubLink`, `SupportKofiButton`) as screens are rebuilt.

## Reading and focus order

| Rule | ID |
|---|---|
| Focus order must be the More button first, then the screen's content top to bottom, then the tabs last. | UX-A-020 |
| Each screen must have one level 1 heading (the screen title). Card titles must be level 2. | UX-A-021 |
| Dialogs and menus must trap focus. Esc must close them, and focus must return to what opened them. | UX-A-022 |
| Every focusable element must show the 3 px focus ring. Arrow keys must move in the menu and tab bar. Enter must activate. | UX-A-023 |
| The Buttons screen must tell you its input rule: "Press a button on your controller to select it." | UX-A-024 |

## Live updates without noise

| Rule | ID |
|---|---|
| The app must announce changes, not repeats: Zwift connected or lost, controller asleep or back, and the first press in setup. | UX-A-025 |
| While riding, shift announcements must be optional: Settings has "Announce each shift", off by default. | UX-A-026 |
| The app must always announce connection problems. | UX-A-027 |
| The app must never move focus to announce something. | UX-A-028 |

*Example: on the setup screen your first press is announced ("Got it. That's your left bumper"). Your next ten presses are not.*

## Text size and motion

| Rule | ID |
|---|---|
| Layouts must work at 200% text size (Windows text size, Android font scale, iOS Dynamic Type). Rows grow taller and nothing truncates. | UX-A-029 |
| The riding readout must wrap to two lines before it shrinks. | UX-A-030 |
| With reduce-motion on, the menu and sheets must appear without sliding, and the "waiting" pulse must be static. | UX-A-031 |

## How to check

| Platform | Tools |
|---|---|
| Windows | Accessibility Insights for Windows (FastPass) and Narrator on every new screen. The UI tests locate elements by ID. |
| Android | Accessibility Scanner and TalkBack. |
| iOS | Accessibility Inspector and VoiceOver. |
| Design | A contrast check against the token table and an annotated reading order before a screen is built. |

| Rule | ID |
|---|---|
| Every new screen must pass a contrast check and have a written reading order before I build it. | UX-A-032 |
| I must check every new Windows screen with Accessibility Insights and Narrator. | UX-A-033 |
