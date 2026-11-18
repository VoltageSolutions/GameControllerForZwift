---
id: SPEC-UX
title: UX overview
status: Draft
last-updated: 2026-11-17
---

# UX overview

!!! tldr "TL;DR"
    The brand idea is **use what you already own**. The app feels warm and human and is about the rider's own controller: no outlines, warm neutrals, one confident orange action per screen, friendly type and plain words. You cannot change the layout.

## The idea

Voltage Gamepad lets you shift and steer in Zwift or other apps with a controller you already own. The pitch is one line, and every screen supports it:

> Shift and steer in Zwift with the controller you already own. No account, no new hardware.

It is not a developer tool. It is a small, calm app that sits beside a game and proves it works.

## Audiences, in priority order

Audiences decide what the words say and which features exist. They never decide the layout or the theme.

| # | Audience | What they need |
|---|---|---|
| 1 | **Cyclists with indoor trainers.** The main audience. | Reliability, calm polish, a glanceable riding screen, and a setup that proves it works. They compare the app with polished paid hardware. |
| 2 | **Gamers** | Correct button glyphs for their controller (Xbox, PlayStation, Nintendo), detected automatically. Controller navigation where it is safe. Depth on request, such as dead zones and curves behind "Advanced". No neon, no all caps. |
| 3 | **Right-to-repair tech advocates** | Values they can see: free, no account, no upsells, works offline, runs on old phones and tablets, small download. No claims the app cannot verify. No green-leaf clichés. |
| 4 | **Riders with limited hand mobility** | Large targets, every input remappable, and an app that never requires press-and-hold. Adaptive and big-button controllers can make shifting possible. |

## How the UX specs are organized

| Page | Answers |
|---|---|
| [Principles](principles.md) | Why the app is designed the way it is. |
| [Visual system](visual-system.md) | What it looks like. |
| [Voice and copy](voice.md) | What it says. |
| [Navigation](navigation.md) | How you move around. |
| [Controller input](controller-input.md) | What a controller press does. |
| [Accessibility](accessibility.md) | How everyone can use it. |
| [Platforms](platforms.md) | What the shell changes on each platform. |
| [Screens](../screens/setup.md) | What each screen contains, one page per area. |

Rules on these pages are written as "must", "should" and "may" (see [how to read a spec](../index.md)).

## How screens are designed and reviewed

Every new screen and every new platform goes through the same steps.

1. **Describe the situation first.** Who uses this, where, and with what in their hands? Setup happens at a desk with time to spare. Riding happens sweaty, at arm's length, with Zwift full screen and a controller close by.
2. **Name the audiences and rank them.** Use the table above.
3. **Study real apps, both ways.** For each one, write what to borrow and what to avoid.
4. **Turn findings into tokens.** Each token traces back to a finding. For example, the large "glance" type size comes from numbers that are too small in other riding apps.
5. **Sketch the real screens, then style them.** Content and flow come first.
6. **Review against the checklist below.** Check one light and one dark example before changing every screen.
7. **Write it down.** Update the spec whenever a decision changes.

Lessons from other apps that shaped this one:

| App | Borrow | Avoid |
|---|---|---|
| Zwift | A way to hide everything. | Years of piled-up HUD clutter. |
| TrainerRoad | A slim window that sits beside other apps, and one central visual everyone understands. | Numbers that are too small to read. |
| Sufferfest | Personality in the words. | Nothing specific. |
| MyWhoosh | Text cues next to graphics. | Missing text cues. |
| Peloton Lanebreak | Showing input as space. | Making riders take a hand off the bars too much. |
| BikeControl | One-line explanations under choices. A "Can't find your controller?" help path. | Payment-first home screens, banners on every screen, equal-weight card stacks, feature sprawl. |
| Strava and Airbnb | Showing your own stuff, committing to one color, separating with space instead of lines. | Nothing specific. |

### Review checklist

| Test | The question |
|---|---|
| Swap test | Could another app's logo go on this screen without anyone noticing? If yes, it is generic. |
| Reason test | Can you state why for the color, type, layout and each word? |
| Glance test | Can you read the riding screen from the saddle? |
| One-accent test | Is there one obvious main action, and nothing else competing in the accent color? |
| Words test | Would a rider say these words out loud? |
| Input test | On this screen, does a controller press mean exactly one thing? |
| Honesty test | Is every number something the app actually knows? |
| Both modes | Is it checked in light and dark? |
| Audience test | Does anything put off a gamer, a conservationist or someone with limited hand mobility? |

### Anti-patterns

| Looks generic, techy or machine-made | Do instead |
|---|---|
| Every card and row outlined with 1 px borders | Surface color, spacing and rounded corners. Soft shadows in light mode. |
| Blue-black dark mode | Warm near-black. |
| Condensed all-caps headings and buttons | Sentence case, bold friendly type. |
| Default typefaces such as Inter, DM Sans or Space Grotesk | A chosen face with character (Figtree). |
| A timid accent sprinkled everywhere, or neon in big areas | One confident use per screen (the main action) plus live moments. |
| Wireframe line illustrations | Filled, soft, friendly illustrations. |
| An interface with no content | The rider's own things: their controller, rides together, shifts. |
| Technical nouns such as Config, Mapped, Linked | Plain words such as Climbing layout, Free button, Zwift can see your controller. |
| A setup checklist copied from a competitor | A first screen that does something: "Press any button" lights up the controller. |
| Purple or violet gradients | Flat color, no gradient washes. |
| A neighbor's category color (Strava orange, Wahoo or Garmin blue, Xbox green, PlayStation blue, Nintendo red, conservation green) | A hue no neighbor owns. The app's own orange is its brand color. |
| Controller hints on screens where the controller is being configured | One meaning for controller input per screen. |
| Explaining a default after the fact | If you cannot say why, it is a default. Change it or justify it first. |
