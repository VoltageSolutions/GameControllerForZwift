---
id: SPEC-UX-PRINCIPLES
title: Design principles
status: Draft
last-updated: 2026-11-17
---

# Design principles

!!! tldr "TL;DR"
    One fixed UI. It is about the rider and their controller. It never asks who you are, reads at arm's length, says state in words, keeps things simple by default, and never invents numbers. Everything else in the UX specs follows from these rules.

In the rules, "must", "should" and "may" have the meaning given in [how to read a spec](../index.md).

## One UI for everyone

Everyone gets the same screens. No themes per audience, no layouts you arrange yourself. This app should be clear enough to avoid clutter or extensive personalization options.

*Example: a gamer and a cyclist open the app with the same Xbox controller. They see the same screens. The only difference is the mapping they saved.*

| Rule | ID |
|---|---|
| The app must have one fixed UI. It must not offer per-audience themes or user-arranged layouts or HUDs. | UX-P-001 |
| The app must never ask who you are or what kind of rider you are. The connected controller sets the button glyphs and the suggested mappings. | UX-P-002 |
| The brand idea is "use what you already own". Every screen should support the line "Shift and steer in Zwift with the controller you already own. No account, no new hardware." | UX-P-003 |

## About the rider and their controller

The app shows real things it counts: rides together, shifts, riding since. It shows your controller, drawn as a friendly object, not a diagram.

| Rule | ID |
|---|---|
| Screens should show the rider's own things (their controller, rides together, shifts, riding since). | UX-P-004 |
| The app must never show a number it does not actually know. It must not show invented stats, counters or claims. | UX-P-005 |
| The app must not make claims it cannot verify, such as "devices saved". | UX-P-006 |

## Readable and glanceable

You ride sweaty, at arm's length, with Zwift full screen. The riding screen has one big readout.

| Rule | ID |
|---|---|
| Each riding screen must have one big readout that can be read at arm's length. | UX-P-007 |
| State must always be said in words, such as "Awake and connected", "Unsaved changes" or "Zwift can see your controller". Color or an icon alone must never carry state. | UX-P-008 |
| Where the controller navigates, the focus ring must be the most visible element on screen. | UX-P-009 |

## Controller input

| Rule | ID |
|---|---|
| A controller press must have exactly one meaning on each screen. See [Controller input](controller-input.md). | UX-P-010 |
| The app must never require press-and-hold, so riders with limited hand mobility can use every part of it. | UX-P-011 |

## Simple by default, depth on request

| Rule | ID |
|---|---|
| Everyday screens (Ride and Buttons) should stay visual and short. Reference screens such as Help may be list-heavy. | UX-P-012 |
| Advanced options (dead zones, curves) must live behind an explicit "Advanced" section, never on the main path. | UX-P-013 |
| Screens must have no chrome sprawl. An available update shows as a badge in Settings, never as a banner on every screen. | UX-P-014 |

## Performance and cost

| Rule | ID |
|---|---|
| Performance is a feature. The app should use little battery, offer a dark mode that suits OLED screens, and keep working on older devices. | UX-P-015 |
| The app must be free to use with no account and no upsells. It must work offline. | UX-P-016 |

## Donations, once and honest

| Rule | ID |
|---|---|
| The app must ask for a donation at most once, after a good ride, framed as keeping the app free. | UX-P-017 |
| A donation ask must never block anything and must never repeat. | UX-P-018 |

??? note "Edge cases"
    - "Support this project" in the More menu is always available. It is a place you choose to visit, not an ask.
    - The ask is skipped if you chose "Maybe later" once. See [Ride screens](../screens/ride.md).

## Implementation status

Not implemented yet. The current Windows app has a side menu and a drop-down for choosing the controller. Its About page carries a donate link. It has no donation ask, no honest stats, and no per-screen controller rules.
