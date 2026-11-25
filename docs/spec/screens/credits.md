---
id: SPEC-SCR-CRD
title: About and Credits screens
last-updated: 2026-11-22
---

# About and Credits screens

!!! tldr "TL;DR"
    About opens from the More menu as a pushed screen. It has no visible title: the VOLTAGE wordmark sits at the top and is the level 1 heading, named "About". It says what the app is, which version you have, and who made it and what it builds on. It links to the project on GitHub, says the app is not affiliated with Zwift, and has two rows, Credits and License, that each open their own pushed screen with a back button and a visible title. The version and the Credits link live only here. It has no donation button. That lives on [Support](support.md).

In the rules, "must", "should", and "may" have the meaning given in [how to read a spec](../index.md).

## The About screen

**What it is for:** say who made the app, which version it is, and what it uses.

**Content, top to bottom:** the VOLTAGE wordmark (`CreditsPageTitle`), which is the screen's heading and has no title text beside it, the app name, the version (`CreditsVersion`, for example "Version 0.5.0"), a short description, the not-affiliated statement, **Project on GitHub** (`CreditsGitHubLink`), **Credits** (`CreditsListLink`), and **License** (`CreditsLicenseLink`). **Credits** opens the Credits list screen, titled "Credits" (`CreditsListPageTitle`). **License** opens the License screen, titled "License" (`CreditsLicensePageTitle`).

*Example: "Game Controller for Zwift, Version 0.5.0. Not made by or affiliated with Zwift. Zwift and Zwift Ride are trademarks of Zwift, Inc."*

| Rule | ID |
|---|---|
| About must show the app name and the app version. The version must be the real version of the running app. | SCR-CRD-001 |
| About must link to the project on GitHub (`CreditsGitHubLink`). | SCR-CRD-002 |
| About must have a **Credits** row that opens a pushed screen, with a back button, that lists the projects and people the app builds on. | SCR-CRD-003 |
| About must have a **License** row, labeled with the license name, GNU General Public License v3. It must open a pushed screen, with a back button, that shows the license text. | SCR-CRD-004 |
| The Credits list screen must show the text of the project's acknowledgements shipped with the app. It must group entries under level 2 headings, and a link in it must open in the default browser. | SCR-CRD-011 |
| The License screen must show the full license text shipped with the app. | SCR-CRD-012 |
| Back on the Credits list and License screens must return to About (UX-N-016). | SCR-CRD-013 |
| About must say the app is not affiliated with Zwift, and that Zwift and Zwift Ride are trademarks of Zwift, Inc. | SCR-CRD-005 |
| About must say the app is free and open source, with no ads and no account. | SCR-CRD-006 |
| About must not show a donation button. Support lives on its own screen. | SCR-CRD-007 |
| The screen must open as a pushed screen with a back button (UX-N-009, UX-N-016). | SCR-CRD-008 |
| About must show no title text. The VOLTAGE wordmark must sit at the top, follow the rules of SCR-RIDE-034, be the one level 1 heading with the accessible name "About", and be followed by level 2 headings for its parts (UX-A-021). The Credits list and License screens must show their titles as level 1 headings. | SCR-CRD-009 |
| Each link must be a text link with a 48 px tall hit area (UX-A-011) and a visible 3 px focus ring (UX-A-023). | SCR-CRD-010 |

??? note "Edge cases"
    - Links open in the default browser. If the browser cannot open, nothing else on the screen changes.

## Accessibility IDs

| ID | Where |
|---|---|
| `MenuCreditsItem` | The More menu item that opens About |
| `CreditsPageTitle` | The wordmark, the level 1 heading of About |
| `CreditsVersion` | The version line |
| `CreditsGitHubLink` | **Project on GitHub** |
| `CreditsListLink` | **Credits** |
| `CreditsLicenseLink` | **License** |
| `CreditsListPageTitle` | The title of the Credits list screen |
| `CreditsLicensePageTitle` | The title of the License screen |

## Controller input on this screen

On About, a controller press goes to Zwift as mapped. See [Controller input](../ux/controller-input.md).

## Quoted strings

| Key | English text | Rule |
|---|---|---|
| `MenuAbout` | About | SCR-CRD-009 |
| `AboutTitle` | About | SCR-CRD-009 |
| `AppName` | Game Controller for Zwift | SCR-CRD-001 |
| `CreditsVersionFormat` | Version {0} | SCR-CRD-001 |
| `CreditsDescription` | Use the controller you already own to shift and steer in Zwift. Free and open source, with no ads and no account. | SCR-CRD-006 |
| `CreditsNotAffiliated` | Not made by or affiliated with Zwift. Zwift and Zwift Ride are trademarks of Zwift, Inc. | SCR-CRD-005 |
| `CreditsGitHub` | Project on GitHub | SCR-CRD-002 |
| `CreditsGitHubDescription` | Source code, issues and docs | SCR-CRD-002 |
| `CreditsList` | Credits | SCR-CRD-003 |
| `CreditsListDescription` | The projects and people this builds on | SCR-CRD-003 |
| `CreditsLicenseName` | License | SCR-CRD-004 |
| `CreditsLicenseValue` | GNU General Public License v3 | SCR-CRD-004 |
