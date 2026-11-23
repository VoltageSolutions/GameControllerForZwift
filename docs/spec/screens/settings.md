---
id: SPEC-SCR-SET
title: Settings screen
last-updated: 2026-11-22
---

# Settings screen

!!! tldr "TL;DR"
    Settings opens from the More menu as a pushed screen with a back button. It has three short sections: Appearance, Rides, and Mappings. It changes how the app looks, sets when a ride ends, and manages saved mappings. It has no Help, log, version, or Credits content: Help opens only from the More menu, the log folder lives only on [Help and feedback](help.md), and the version and the Credits link live only on [About](credits.md). Your choices are kept between runs.

In the rules, "must", "should", and "may" have the meaning given in [how to read a spec](../index.md). A **mapping** is what the saved configurations spec (`CFG`) calls a configuration.

## The Settings screen

**What it is for:** change how the app looks, set when a ride ends, and manage saved mappings.

**Content, top to bottom:** the screen title "Settings" (`SettingsPageTitle`), then three sections, each with a level 2 heading.

| Section | Content |
|---|---|
| Appearance | A choice of **Light**, **Dark**, or **Match Windows** (`SettingsAppearanceGroup`). |
| Rides | **End a ride after** a number of minutes (`SettingsRideTimeoutInput`). |
| Mappings | **Open the mappings folder** (`SettingsOpenMappingsButton`) and **Delete all saved mappings** (`SettingsDeleteAllButton`). |

*Example: you press Delete all saved mappings and you have three. The dialog says "Delete all 3 saved mappings? This can't be undone. Default stays." After you confirm, every controller is back on Default, and the button is disabled.*

| Rule | ID |
|---|---|
| Settings must offer the appearance choices Light, Dark, and Match Windows. The default must be Match Windows. A choice must apply right away. | SCR-SET-001 |
| The app must keep your Settings choices between runs. It must store them in the same place as saved mappings. | SCR-SET-002 |
| Settings must offer **Open the mappings folder** (`CFG-034`). | SCR-SET-003 |
| Settings must offer **Delete all saved mappings** behind a confirmation that states the count and that it can't be undone. See `CFG-035` and `CFG-036`. | SCR-SET-004 |
| When there are no saved mappings, **Delete all saved mappings** must be disabled, and its description must say there is nothing to delete. | SCR-SET-005 |
| The screen must open as a pushed screen with a back button (UX-N-009, UX-N-016). | SCR-SET-009 |
| The screen must have one level 1 heading, the title, and a level 2 heading for each section (UX-A-021). | SCR-SET-010 |
| Every row must follow the left-to-right reading order at every window size, and the layout must hold at 200% text size (UX-A-029). | SCR-SET-011 |
| Settings must offer **End a ride after**, the ride timeout in whole minutes. It must default to 10, and it must not accept a value below 5. See SCR-RIDE-037. | SCR-SET-012 |
| When a value below 5 is entered, the app must say the minimum in words and keep the last valid value. | SCR-SET-013 |

??? note "Edge cases"
    - Match Windows follows the Windows app mode and switches when it changes. See the [visual system](../ux/visual-system.md).
    - If the saved mappings folder is empty or missing, **Open the mappings folder** still works, because it creates the folder first (`CFG-034`).
    - If a delete fails, the app says so with the reason and keeps the mappings it could not delete.

## Accessibility IDs

| ID | Where |
|---|---|
| `MenuSettingsItem` | The More menu item that opens this screen |
| `SettingsPageTitle` | The screen title |
| `SettingsAppearanceGroup` | The appearance choice |
| `SettingsRideTimeoutInput` | **End a ride after** |
| `SettingsOpenMappingsButton` | **Open the mappings folder** |
| `SettingsDeleteAllButton` | **Delete all saved mappings** |

## Controller input on this screen

On Settings, a controller press goes to Zwift as mapped. Dialogs work with touch, mouse, and keyboard. See [Controller input](../ux/controller-input.md).

## Quoted strings

| Key | English text | Rule |
|---|---|---|
| `SettingsTitle` | Settings | SCR-SET-010 |
| `SettingsAppearanceSection` | Appearance | SCR-SET-001 |
| `AppearanceLight` | Light | SCR-SET-001 |
| `AppearanceDark` | Dark | SCR-SET-001 |
| `AppearanceMatchWindows` | Match Windows | SCR-SET-001 |
| `SettingsRidesSection` | Rides | SCR-SET-012 |
| `SettingsRideTimeout` | End a ride after | SCR-SET-012 |
| `SettingsRideTimeoutDescription` | Minutes without a button press or a connection before a ride ends. The minimum is 5. | SCR-SET-012 |
| `SettingsRideTimeoutMinimum` | The ride timeout must be 5 minutes or more. | SCR-SET-013 |
| `SettingsMappingsSection` | Mappings | SCR-SET-003 |
| `SettingsOpenMappings` | Open the mappings folder | SCR-SET-003 |
| `SettingsDeleteAll` | Delete all saved mappings | SCR-SET-004 |
| `SettingsDeleteAllConfirmOne` | Delete 1 saved mapping? This can't be undone. Default stays. | SCR-SET-004 |
| `SettingsDeleteAllConfirm` | Delete all {0} saved mappings? This can't be undone. Default stays. | SCR-SET-004 |
| `SettingsDeleteNone` | There are no saved mappings to delete. | SCR-SET-005 |
