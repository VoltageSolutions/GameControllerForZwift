---
id: SPEC-UX-VOICE
title: Voice and copy
last-updated: 2026-11-22
---

# Voice and copy

!!! tldr "TL;DR"
    Plain, warm, second person. Name things after what they mean to the rider, not after how the app works inside. Sentence case everywhere. The app is "Voltage Gamepad". A saved set of button assignments is a "mapping".

In the rules, "must", "should", and "may" have the meaning given in [how to read a spec](../index.md).

## The voice

The app talks like a friendly person next to you. It says what is happening and what to do. It does not use jargon.

*Example: not "Controller connected" but "Your Xbox controller is here". Not "Zwift linked" but "Zwift is connected to Voltage Gamepad".*

| Rule | ID |
|---|---|
| Copy must be plain, warm, and in the second person ("you", "your"). | UX-W-001 |
| Copy must use sentence case. It must not use all caps. | UX-W-002 |
| Copy must name things after what they mean to the rider, not after internal terms. | UX-W-003 |
| Every sentence should be short. A rider should be able to say it out loud. | UX-W-004 |
| Copy must not use marketing claims. | UX-W-005 |
| Copy must not make claims the app cannot verify. | UX-W-006 |
| Copy and docs must use the serial (Oxford) comma in lists of three or more, as in "Settings, Help and feedback, About, and Support this project". | UX-W-017 |
| Copy and docs must use active voice with a clear actor. Write "The app must always announce connection problems." Do not write "Connection problems must always be announced." | UX-W-018 |

## Avoid and use

| Avoid | Use |
|---|---|
| Config: Climbing | Climbing mapping |
| Not mapped, No Action Configured | No action |
| Zwift linked | Zwift is connected to Voltage Gamepad |
| Last press | You just pressed |
| Controller connected | Your Xbox controller is here, or Awake and connected |
| Unsaved | Unsaved changes |
| Profile, Configuration, Layout | Mapping |
| Mapped, Unmapped | Free button |

| Rule | ID |
|---|---|
| Copy should use the "Use" column wording for these concepts. | UX-W-007 |

## Names

| Thing | Name | ID |
|---|---|---|
| The app as Zwift sees it, and in copy | **Voltage Gamepad** | UX-W-008 |
| A saved set of button assignments | **Mapping** (the `CFG` spec calls it "configuration") | UX-W-009 |
| The built-in mapping | **Default** | UX-W-010 |
| A button with no mapping | **Free button** (and the function "No action") | UX-W-011 |
| Zwift's pairing screen | **Paired Devices** | UX-W-012 |

| Rule | ID |
|---|---|
| The UI must say "mapping", never "configuration", "config", "layout", or "profile". | UX-W-013 |
| Copy that tells you what to pick in Zwift must name **Voltage Gamepad**. | UX-W-014 |
| Copy must use the Zwift Ride function names (Shift up, Shift down, Steer left, Steer right, Brake, Navigate up, Navigate down, Select, Back, Power-up, Ride On, No action). | UX-W-015 |

## Suggested copy for key states

| State | Copy |
|---|---|
| Home, ready | "Ready when you are" |
| Setup, first screen | "Press any button on your controller." |
| Choose controller | "Press any button on the one you want." |
| Controller found | "Your Xbox controller is here." |
| No controller | "Wake your controller" |
| Looking for it | "Looking for your controller..." |
| Zwift connected | "Zwift is connected to Voltage Gamepad" |
| Zwift not connected | "Zwift isn't connected yet" |
| Waiting for Zwift | "Waiting for Zwift..." |
| Shift test | "Did your gear change?" with "Yes, it shifted" and "Not yet" |
| Zwift cannot see the controller | "Zwift can't see your controller" |
| Unsaved mapping | "Unsaved changes" |
| Saved mapping copy | "Saved a copy to your mappings." |
| Name already used | "You already have a mapping called Climbing. Try another name." |
| Donation ask | "Chip in on Ko-fi" and "Maybe later" |

| Rule | ID |
|---|---|
| Screens must use the suggested copy above unless a screen spec gives its own wording. | UX-W-016 |

??? note "Edge cases"
    - Translated labels can be longer. The button rules in the [visual system](visual-system.md) make long labels move to their own line rather than break.
    - Where a problem is shown, the copy says what is fine too, such as "Your controller is still connected" when Zwift cannot see it.
    - An item name that contains "and" stays one item, so "Help and feedback" and "Sticks and triggers" get no comma inside them. A list of two items gets no comma either: "Ride and Buttons".
