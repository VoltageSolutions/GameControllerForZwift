# Spec

!!! tldr "TL;DR"
    The specs describe, in plain language, how the app must look and behave. Every rule has an ID that tests, issues and code point to. Each spec has a status: Draft, Accepted or Implemented.

## What the specs are

A spec is a readable description of how one part of the app behaves. I write it before or alongside the code, and I change it whenever the behavior changes. It is the source of truth. If the app and a spec disagree, one of them has a bug.

Each rule has a requirement ID such as `CFG-015` or `UX-V-012`. Tests and code refer to these IDs, so you can find the check for any rule, and the rule behind any check.

## Words that have a fixed meaning

| Word | Meaning |
|---|---|
| **must** | The rule is required. |
| **should** | The rule is expected unless there is a good reason to do otherwise. |
| **may** | The rule is optional. |

## Statuses

Every spec has a status in its front matter.

| Status | Meaning |
|---|---|
| Draft | I am still working out what this part should do. |
| Accepted | The behavior is agreed. The work may be in progress or have known gaps. |
| Implemented | The app does everything the spec says. |

## How to read a spec

Start with the TL;DR. Then read the sections you care about. Each section opens with a short description of what you see and what happens, often with an example. After that comes a table of rules with their IDs. Finer points sit in a collapsed "Edge cases" block that you can open when you need it.

## The specs

### Features

| Spec | ID prefix | What it covers |
|---|---|---|
| [Saved configurations](features/saved-configurations.md) | `CFG` | Save, reload, rename, delete, import and share mappings. |
| [Controller detection](features/controller-detection.md) | `CTRL` | How the controller list follows controllers as they connect and disconnect, and which one is selected. |

### UX

| Spec | ID prefix | What it covers |
|---|---|---|
| [UX overview](ux/index.md) | none | Brand idea, audiences, how screens are designed and reviewed. |
| [Principles](ux/principles.md) | `UX-P` | The design principles the whole app follows. |
| [Visual system](ux/visual-system.md) | `UX-V` | Color, type, spacing, buttons, illustration, motion. |
| [Voice and copy](ux/voice.md) | `UX-W` | How the app talks and what it calls things. |
| [Navigation](ux/navigation.md) | `UX-N` | Tabs, the More button and where they go on each screen size. |
| [Controller input](ux/controller-input.md) | `UX-C` | What a controller press means on each kind of screen. |
| [Accessibility](ux/accessibility.md) | `UX-A` | Contrast, targets, IDs, reading order, live updates. |
| [Platforms](ux/platforms.md) | `UX-PL` | What changes per platform, and what never does. |

### Screens

| Spec | ID prefix | What it covers |
|---|---|---|
| [Setup screens](screens/setup.md) | `SCR-SETUP` | Connect a controller, pair with Zwift, try a shift. |
| [Ride screens](screens/ride.md) | `SCR-RIDE` | Home, riding view, connection lost, ride finished. |
| [Buttons screens](screens/buttons.md) | `SCR-BTN` | Mappings, the function picker, sticks and triggers. |
| [Connections screens](screens/connections.md) | `SCR-CONN` | Controller and Zwift status, "Zwift can't find Voltage Gamepad". |
| [Settings and help screens](screens/settings-help.md) | `SCR-SET` | Settings, Help and feedback, Credits, Support this project. |

The saved configurations spec calls a saved set of button assignments a "configuration". The app calls it a **mapping**. The screen specs use "mapping" and refer to the `CFG` rules by ID.
