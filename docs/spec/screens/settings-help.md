---
id: SPEC-SCR-SET
title: Settings, help, credits, and support
last-updated: 2026-11-17
---

# Settings, help, credits, and support

!!! tldr "TL;DR"
    Four screens open from the More button as pushed screens with a back button: Settings, Help and feedback, Credits, and Support this project. They are reference screens, so lists are fine, but they stay short and plain. An available update shows as a badge in Settings, never as a banner.

In the rules, "must", "should", and "may" have the meaning given in [how to read a spec](../index.md).

## Settings

**What it is for:** change how the app looks and behaves, and manage saved mappings and logs.

**Sections:** Appearance, While riding, Mappings, Help, About.

| Section | Content |
|---|---|
| Appearance | **Light**, **Dark**, or **Match Windows**. |
| While riding | **Announce each shift** (off by default). On Windows also a shortcut to switch window size. |
| Startup (Windows) | **Start with Windows**. |
| Mappings | **Open folder** and **Delete all saved mappings**. |
| Help | Links to Help and feedback, with **Open log folder**. |
| About | Version, and a link to Credits. |

*Example: you press Delete all saved mappings. The dialog says "Delete all 3 saved mappings? This can't be undone. Default stays." After you confirm, every controller is back on Default.*

| Rule | ID |
|---|---|
| Settings must offer the appearance choices Light, Dark, and Match Windows. The default must be Match Windows. | SCR-SET-001 |
| Settings must offer **Announce each shift**, off by default. See UX-A-026. | SCR-SET-002 |
| On Windows, Settings must offer **Start with Windows** and a shortcut for switching the window size. | SCR-SET-003 |
| Settings must offer **Open folder** for saved mappings (`CFG-034`). | SCR-SET-004 |
| Settings must offer **Delete all saved mappings** behind a confirmation that states the count and that it can't be undone. See `CFG-035` and `CFG-036`. | SCR-SET-005 |
| If there is nothing to delete, Settings must say so and not ask for confirmation. See `CFG-035`. | SCR-SET-006 |
| Settings must offer **Open log folder**. | SCR-SET-007 |
| When an update is available, Settings must show a badge. The app must not show a banner on other screens. | SCR-SET-008 |
| The screen must open as a pushed screen with a back button. | SCR-SET-009 |

## Help and feedback

**What it is for:** solve common problems and reach the maintainer.

**Content:** common problems, **Open log folder**, **Report a problem** (opens GitHub, with what to attach), **Send feedback** (GitHub), the list of supported controllers, and a link to "Zwift can't find Voltage Gamepad".

| Rule | ID |
|---|---|
| Help must list the common problems, each with a short plain fix. | SCR-SET-010 |
| Help must link to "Zwift can't find Voltage Gamepad" on [Connections](connections.md). | SCR-SET-011 |
| Help must offer **Open log folder**. | SCR-SET-012 |
| **Report a problem** must open GitHub and say what to attach: the app version, the controller, and the log file. | SCR-SET-013 |
| The screen should tell you to check the log before sharing it publicly, because it lists local network addresses. | SCR-SET-014 |
| **Send feedback** must open GitHub. | SCR-SET-015 |
| Help should offer the list of supported controllers once that list exists. | SCR-SET-016 |

## Credits

**What it is for:** say who made the app and what it uses.

**Content:** the version, a link to GitHub, credits and dependencies, the GPLv3 license, and the statement that the app is not affiliated with Zwift.

| Rule | ID |
|---|---|
| Credits must show the app version. | SCR-SET-017 |
| Credits must link to the project on GitHub (`CreditsGitHubLink`). | SCR-SET-018 |
| Credits must list credits and dependencies. | SCR-SET-019 |
| Credits must show the license (GPLv3). | SCR-SET-020 |
| Credits must say the app is not affiliated with Zwift. | SCR-SET-021 |

## Support this project

**What it is for:** one calm place to support the app, chosen by you, never pushed.

**Content:** **Chip in on Ko-fi** (`SupportKofiButton`), **Star the project on GitHub**, **Tell a friend**, and **Contribute**.

| Rule | ID |
|---|---|
| The screen must offer Ko-fi, a star on GitHub, telling a friend, and contributing. | SCR-SET-022 |
| The pitch must be short, honest, and framed as supporting the project. It must not make claims the app cannot verify. | SCR-SET-023 |
| Only one action may use the accent fill (Chip in on Ko-fi). | SCR-SET-024 |
| The screen must follow the platform's donation rules (see [Platforms](../ux/platforms.md)). | SCR-SET-025 |

??? note "Edge cases"
    The Support pitch tends to grow long. Keep it to a few lines, because lists are fine on reference screens but long copy is not.

## Controller input on these screens

On these screens, a controller press goes to Zwift as mapped. Dialogs work with touch, mouse, and keyboard. See [Controller input](../ux/controller-input.md).

## Accessibility IDs

| ID | Where |
|---|---|
| `MenuSettingsItem`, `MenuHelpItem`, `MenuCreditsItem`, `MenuSupportItem` | The More menu |
| `CreditsGitHubLink` | Credits |
| `SupportKofiButton` | Support this project |
