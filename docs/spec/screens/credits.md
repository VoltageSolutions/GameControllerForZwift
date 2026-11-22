---
id: SPEC-SCR-CRD
title: Credits screen
last-updated: 2026-11-22
---

# Credits screen

!!! tldr "TL;DR"
    Credits opens from the More menu as a pushed screen. It says what the app is, which version you have, and who made it and what it builds on. It links to the project on GitHub, lists the credits, shows the GPLv3 license, and says the app is not affiliated with Zwift. It has no donation button. That lives on [Support](support.md).

In the rules, "must", "should", and "may" have the meaning given in [how to read a spec](../index.md).

## The Credits screen

**What it is for:** say who made the app, which version it is, and what it uses.

**Content, top to bottom:** the screen title "Credits" (`CreditsPageTitle`), the app name with its logo, the version (`CreditsVersion`, for example "Version 0.5.0"), a short description, the not-affiliated statement, **Project on GitHub** (`CreditsGitHubLink`), **Credits** (`CreditsListLink`), and **License** (`CreditsLicense`).

*Example: "Game Controller for Zwift, Version 0.5.0. Not made by or affiliated with Zwift. Zwift and Zwift Ride are trademarks of Zwift, Inc."*

| Rule | ID |
|---|---|
| Credits must show the app name and the app version. The version must be the real version of the running app. | SCR-CRD-001 |
| Credits must link to the project on GitHub (`CreditsGitHubLink`). | SCR-CRD-002 |
| Credits must list the credits and dependencies, from the project's acknowledgements. | SCR-CRD-003 |
| Credits must show the license, GNU General Public License v3, and must link to its text. | SCR-CRD-004 |
| Credits must say the app is not affiliated with Zwift, and that Zwift and Zwift Ride are trademarks of Zwift, Inc. | SCR-CRD-005 |
| Credits must say the app is free and open source, with no ads and no account. | SCR-CRD-006 |
| Credits must not show a donation button. Support lives on its own screen. | SCR-CRD-007 |
| The screen must open as a pushed screen with a back button (UX-N-009, UX-N-016). | SCR-CRD-008 |
| The screen must have one level 1 heading, the title, and level 2 headings for its parts (UX-A-021). | SCR-CRD-009 |
| Each link must be a text link with a 48 px tall hit area (UX-A-011) and a visible 3 px focus ring (UX-A-023). | SCR-CRD-010 |

??? note "Edge cases"
    - Links open in the default browser. If the browser cannot open, nothing else on the screen changes.

## Accessibility IDs

| ID | Where |
|---|---|
| `MenuCreditsItem` | The More menu item that opens this screen |
| `CreditsPageTitle` | The screen title |
| `CreditsVersion` | The version line |
| `CreditsGitHubLink` | **Project on GitHub** |
| `CreditsListLink` | **Credits** |
| `CreditsLicense` | **License** |

## Controller input on this screen

On Credits, a controller press goes to Zwift as mapped. See [Controller input](../ux/controller-input.md).

## Quoted strings

| Key | English text | Rule |
|---|---|---|
| `CreditsTitle` | Credits | SCR-CRD-009 |
| `CreditsVersionFormat` | Version {0} | SCR-CRD-001 |
| `CreditsDescription` | Use the controller you already own to shift and steer in Zwift. Free and open source, with no ads and no account. | SCR-CRD-006 |
| `CreditsNotAffiliated` | Not made by or affiliated with Zwift. Zwift and Zwift Ride are trademarks of Zwift, Inc. | SCR-CRD-005 |
| `CreditsGitHub` | Project on GitHub | SCR-CRD-002 |
| `CreditsGitHubDescription` | Source code, issues and docs | SCR-CRD-002 |
| `CreditsList` | Credits | SCR-CRD-003 |
| `CreditsListDescription` | The projects and people this builds on | SCR-CRD-003 |
| `CreditsLicenseName` | License | SCR-CRD-004 |
| `CreditsLicenseValue` | GNU General Public License v3 | SCR-CRD-004 |
