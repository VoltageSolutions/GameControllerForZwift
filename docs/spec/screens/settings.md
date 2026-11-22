---
id: SPEC-SCR-SET
title: Settings screen
last-updated: 2026-11-22
---

# Settings screen

!!! tldr "TL;DR"
    Settings opens from the More menu as a pushed screen with a back button. It has four short sections: Appearance, Mappings, Help, and About. It changes how the app looks, manages saved mappings, and points to the log folder, Help, and Credits. Your choices are kept between runs.

In the rules, "must", "should", and "may" have the meaning given in [how to read a spec](../index.md). A **mapping** is what the saved configurations spec (`CFG`) calls a configuration.

## The Settings screen

**What it is for:** change how the app looks and manage saved mappings and logs.

**Content, top to bottom:** the screen title "Settings" (`SettingsPageTitle`), then four sections, each with a level 2 heading.

| Section | Content |
|---|---|
| Appearance | A choice of **Light**, **Dark**, or **Match Windows** (`SettingsAppearanceGroup`). |
| Mappings | **Open the mappings folder** (`SettingsOpenMappingsButton`) and **Delete all saved mappings** (`SettingsDeleteAllButton`). |
| Help | **Open the log folder** (`SettingsOpenLogsButton`) and a link to Help and feedback (`SettingsHelpLink`). |
| About | The version (`SettingsVersion`) and a link to Credits (`SettingsCreditsLink`). |

*Example: you press Delete all saved mappings and you have three. The dialog says "Delete all 3 saved mappings? This can't be undone. Default stays." After you confirm, every controller is back on Default, and the button is disabled.*

| Rule | ID |
|---|---|
| Settings must offer the appearance choices Light, Dark, and Match Windows. The default must be Match Windows. A choice must apply right away. | SCR-SET-001 |
| The app must keep your Settings choices between runs. It must store them in the same place as saved mappings. | SCR-SET-002 |
| Settings must offer **Open the mappings folder** (`CFG-034`). | SCR-SET-003 |
| Settings must offer **Delete all saved mappings** behind a confirmation that states the count and that it can't be undone. See `CFG-035` and `CFG-036`. | SCR-SET-004 |
| When there are no saved mappings, **Delete all saved mappings** must be disabled, and its description must say there is nothing to delete. | SCR-SET-005 |
| Settings must offer **Open the log folder**. | SCR-SET-006 |
| Settings must link to [Help and feedback](help.md). | SCR-SET-007 |
| Settings must show the app version and link to [Credits](credits.md). | SCR-SET-008 |
| The screen must open as a pushed screen with a back button (UX-N-009, UX-N-016). | SCR-SET-009 |
| The screen must have one level 1 heading, the title, and a level 2 heading for each section (UX-A-021). | SCR-SET-010 |
| Every row must follow the left-to-right reading order at every window size, and the layout must hold at 200% text size (UX-A-029). | SCR-SET-011 |

??? note "Edge cases"
    - Match Windows follows the Windows app mode and switches when it changes. See the [visual system](../ux/visual-system.md).
    - If the saved mappings folder is empty or missing, **Open the mappings folder** still works, because it creates the folder first (`CFG-034`).
    - If a delete fails, the app says so with the reason and keeps the mappings it could not delete.
    - The link to Credits opens a pushed screen. Back returns to Settings (UX-N-016).

## Accessibility IDs

| ID | Where |
|---|---|
| `MenuSettingsItem` | The More menu item that opens this screen |
| `SettingsPageTitle` | The screen title |
| `SettingsAppearanceGroup` | The appearance choice |
| `SettingsOpenMappingsButton` | **Open the mappings folder** |
| `SettingsDeleteAllButton` | **Delete all saved mappings** |
| `SettingsOpenLogsButton` | **Open the log folder** |
| `SettingsHelpLink` | The link to Help and feedback |
| `SettingsVersion` | The version line |
| `SettingsCreditsLink` | The link to Credits |

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
| `SettingsMappingsSection` | Mappings | SCR-SET-003 |
| `SettingsOpenMappings` | Open the mappings folder | SCR-SET-003 |
| `SettingsDeleteAll` | Delete all saved mappings | SCR-SET-004 |
| `SettingsDeleteAllConfirmOne` | Delete 1 saved mapping? This can't be undone. Default stays. | SCR-SET-004 |
| `SettingsDeleteAllConfirm` | Delete all {0} saved mappings? This can't be undone. Default stays. | SCR-SET-004 |
| `SettingsDeleteNone` | There are no saved mappings to delete. | SCR-SET-005 |
| `SettingsHelpSection` | Help | SCR-SET-006 |
| `SettingsOpenLogs` | Open the log folder | SCR-SET-006 |
| `SettingsHelpLinkText` | Help and feedback | SCR-SET-007 |
| `SettingsAboutSection` | About | SCR-SET-008 |
| `SettingsVersionFormat` | Version {0} | SCR-SET-008 |
| `SettingsCreditsLinkText` | Credits | SCR-SET-008 |
