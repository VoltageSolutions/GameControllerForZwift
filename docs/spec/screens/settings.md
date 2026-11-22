---
id: SPEC-SCR-SET
title: Settings screen
last-updated: 2026-11-22
---

# Settings screen

!!! tldr "TL;DR"
    Settings opens from the More menu as a pushed screen. It has four short sections: Appearance, While riding, Mappings, and Logs. It changes how the app looks and behaves, and it manages saved mappings and logs. It shows only choices the app can really apply.

In the rules, "must", "should", and "may" have the meaning given in [how to read a spec](../index.md). A **mapping** is what the saved configurations spec (`CFG`) calls a configuration.

## The Settings screen

**What it is for:** change how the app looks and behaves, and clean up saved mappings and logs.

**Content, top to bottom:** the screen title "Settings" (`SettingsPageTitle`), then four sections, each with a level 2 heading.

| Section | Content |
|---|---|
| Appearance | A choice of **Light**, **Dark**, or **Match Windows** (`SettingsAppearanceGroup`). |
| While riding | **Start with Windows** (`SettingsStartWithWindowsToggle`), **Switch window size** with its shortcut (`SettingsSwitchSizeShortcut`), and **Announce each shift** (`SettingsAnnounceShiftsToggle`). |
| Mappings | **Open the mappings folder** (`SettingsOpenMappingsButton`) and **Delete all saved mappings** (`SettingsDeleteAllButton`). |
| Logs | **Open the log folder** (`SettingsOpenLogsButton`). |

*Example: you press Delete all saved mappings and you have three. The dialog says "Delete all 3 saved mappings? This can't be undone. Default stays." After you confirm, every controller is back on Default.*

| Rule | ID |
|---|---|
| Settings must offer the appearance choices Light, Dark, and Match Windows. The default must be Match Windows. A choice must apply right away. | SCR-SET-001 |
| Settings must offer **Announce each shift**, off by default. See UX-A-026. | SCR-SET-002 |
| Settings must offer **Start with Windows**. | SCR-SET-003 |
| Settings must show the shortcut for **Switch window size**. | SCR-SET-004 |
| Settings must offer **Open the mappings folder** (`CFG-034`). | SCR-SET-005 |
| Settings must offer **Delete all saved mappings** behind a confirmation that states the count and that it can't be undone. See `CFG-035` and `CFG-036`. | SCR-SET-006 |
| If there is nothing to delete, Settings must say so and not ask for confirmation. See `CFG-035`. | SCR-SET-007 |
| Settings must offer **Open the log folder**. | SCR-SET-008 |
| The screen must open as a pushed screen with a back button (UX-N-009, UX-N-016). | SCR-SET-009 |
| The screen must have one level 1 heading, the title, and a level 2 heading for each section (UX-A-021). | SCR-SET-010 |
| Every row must follow the left-to-right reading order at every window size, and the section layout must hold at 200% text size (UX-A-029). | SCR-SET-011 |

??? note "Edge cases"
    - Match Windows follows the Windows app mode and switches when it changes. See the [visual system](../ux/visual-system.md).
    - If the saved mappings folder is empty or missing, **Open the mappings folder** still works, because it creates the folder first (`CFG-034`).
    - If a delete fails, the app says so with the reason and keeps the mappings it could not delete.
    - **Start with Windows** and **Switch window size** apply to Windows only. Other platforms follow [Platforms](../ux/platforms.md).

## Accessibility IDs

| ID | Where |
|---|---|
| `MenuSettingsItem` | The More menu item that opens this screen |
| `SettingsPageTitle` | The screen title |
| `SettingsAppearanceGroup` | The appearance choice |
| `SettingsStartWithWindowsToggle` | **Start with Windows** |
| `SettingsSwitchSizeShortcut` | The **Switch window size** shortcut |
| `SettingsAnnounceShiftsToggle` | **Announce each shift** |
| `SettingsOpenMappingsButton` | **Open the mappings folder** |
| `SettingsDeleteAllButton` | **Delete all saved mappings** |
| `SettingsOpenLogsButton` | **Open the log folder** |

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
| `SettingsRidingSection` | While riding | SCR-SET-002 |
| `SettingsStartWithWindows` | Start with Windows | SCR-SET-003 |
| `SettingsSwitchSize` | Switch window size | SCR-SET-004 |
| `SettingsAnnounceShifts` | Announce each shift | SCR-SET-002 |
| `SettingsMappingsSection` | Mappings | SCR-SET-005 |
| `SettingsOpenMappings` | Open the mappings folder | SCR-SET-005 |
| `SettingsDeleteAll` | Delete all saved mappings | SCR-SET-006 |
| `SettingsDeleteAllConfirmOne` | Delete 1 saved mapping? This can't be undone. Default stays. | SCR-SET-006 |
| `SettingsDeleteAllConfirm` | Delete all {0} saved mappings? This can't be undone. Default stays. | SCR-SET-006 |
| `SettingsDeleteNone` | There are no saved mappings to delete. | SCR-SET-007 |
| `SettingsLogsSection` | Logs | SCR-SET-008 |
| `SettingsOpenLogs` | Open the log folder | SCR-SET-008 |
