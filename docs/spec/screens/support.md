---
id: SPEC-SCR-SUP
title: Support this project screen
last-updated: 2026-11-22
---

# Support this project screen

!!! tldr "TL;DR"
    Support this project opens from the More menu as a pushed screen. It is one calm place to help the app, chosen by you and never pushed. It has one main action, **Chip in on Ko-fi**, and three quieter ways to help: star the project on GitHub, tell a friend, and help with the code or docs.

In the rules, "must", "should", and "may" have the meaning given in [how to read a spec](../index.md).

## The Support screen

**What it is for:** let a rider who wants to help find out how, without pressure.

**Content, top to bottom:** the screen title "Support this project" (`SupportPageTitle`), a short pitch, **Chip in on Ko-fi** (`SupportKofiButton`), then a section "Other ways to help" with three rows: **Star it on GitHub** (`SupportStarButton`), **Tell a friend** (`SupportShareButton`), and **Help with the code or docs** (`SupportContributeButton`).

| Rule | ID |
|---|---|
| The screen must offer Ko-fi, a star on GitHub, telling a friend, and contributing. | SCR-SUP-001 |
| **Chip in on Ko-fi** must open the project's Ko-fi page in the default browser. | SCR-SUP-002 |
| **Star it on GitHub** must open the project's GitHub page. | SCR-SUP-003 |
| **Help with the code or docs** must open the contributing guide on GitHub. | SCR-SUP-004 |
| **Tell a friend** must lead to the download page for the app. | SCR-SUP-005 |
| Only **Chip in on Ko-fi** may use the accent fill. | SCR-SUP-006 |
| The pitch must be short and honest, and it must frame the ask as supporting the project. It must not make claims the app cannot verify (UX-W-006). | SCR-SUP-007 |
| The screen must follow the platform's donation rules (see [Platforms](../ux/platforms.md)). | SCR-SUP-008 |
| The screen must open as a pushed screen with a back button (UX-N-009, UX-N-016). | SCR-SUP-009 |
| The screen must have one level 1 heading, the title, and a level 2 heading for "Other ways to help" (UX-A-021). | SCR-SUP-010 |

??? note "Edge cases"
    - Links open in the default browser. If the browser cannot open, nothing else on the screen changes.
    - The one-time ask after a ride, on [Ride](ride.md#ride-finished), links here.

## Accessibility IDs

| ID | Where |
|---|---|
| `MenuSupportItem` | The More menu item that opens this screen |
| `SupportPageTitle` | The screen title |
| `SupportKofiButton` | **Chip in on Ko-fi** |
| `SupportStarButton` | **Star it on GitHub** |
| `SupportShareButton` | **Tell a friend** |
| `SupportContributeButton` | **Help with the code or docs** |

## Controller input on this screen

On Support, a controller press goes to Zwift as mapped. See [Controller input](../ux/controller-input.md).

## Quoted strings

| Key | English text | Rule |
|---|---|---|
| `SupportTitle` | Support this project | SCR-SUP-010 |
| `SupportPitch` | This app is free and open source, with no ads and no account. If it's earned a place in your rides, you can chip in. | SCR-SUP-007 |
| `SupportKofi` | Chip in on Ko-fi | SCR-SUP-002 |
| `SupportOtherWays` | Other ways to help | SCR-SUP-010 |
| `SupportStar` | Star it on GitHub | SCR-SUP-003 |
| `SupportStarDescription` | Helps other riders find it | SCR-SUP-003 |
| `SupportShare` | Tell a friend | SCR-SUP-005 |
| `SupportShareDescription` | Share the download page | SCR-SUP-005 |
| `SupportContribute` | Help with the code or docs | SCR-SUP-004 |
| `SupportContributeDescription` | Contributing guide on GitHub | SCR-SUP-004 |
