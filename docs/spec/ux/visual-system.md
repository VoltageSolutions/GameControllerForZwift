---
id: SPEC-UX-VISUAL
title: Visual system
last-updated: 2026-11-19
---

# Visual system

!!! tldr "TL;DR"
    Warm neutrals, no outlines, one confident orange (`#ED7600`) action per screen, Figtree type, rounded shapes, filled friendly illustrations. The orange fill always carries a dark label (`#1F1B16`), never white. In light mode the orange text color is a darker `#A55200`.

In the rules, "must", "should", and "may" have the meaning given in [how to read a spec](../index.md).

## Color tokens

Tokens are named roles. Screens use tokens, never raw colors.

On Windows the tokens live in `Resources/Theme/Tokens.Dark.xaml` and `Tokens.Light.xaml` as `Color.<token>` and `Brush.<token>`. A unit test compares them, and the contrast table below, with this page.

| Token | Use | Dark | Light |
|---|---|---|---|
| `bg` | Screen background | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#141312;color:#F5F2EC;padding:2px 8px;border-radius:8px">#141312</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#FAF8F4;color:#1F1B16;padding:2px 8px;border-radius:8px">#FAF8F4</span> |
| `surface` | Cards and rows | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#1F1D1B;color:#F5F2EC;padding:2px 8px;border-radius:8px">#1F1D1B</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#FFFFFF;color:#1F1B16;padding:2px 8px;border-radius:8px">#FFFFFF</span> |
| `surface-2` | Secondary buttons, chips, keycaps | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#2A2826;color:#F5F2EC;padding:2px 8px;border-radius:8px">#2A2826</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#F1EDE6;color:#1F1B16;padding:2px 8px;border-radius:8px">#F1EDE6</span> |
| `text` | Body text | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#F5F2EC;color:#1F1B16;padding:2px 8px;border-radius:8px">#F5F2EC</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#1F1B16;color:#F5F2EC;padding:2px 8px;border-radius:8px">#1F1B16</span> |
| `muted` | Secondary text | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#ABA59C;color:#141312;padding:2px 8px;border-radius:8px">#ABA59C</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#655E55;color:#FFFFFF;padding:2px 8px;border-radius:8px">#655E55</span> |
| `accent` | Fill for the one main action | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#ED7600;color:#1F1B16;padding:2px 8px;border-radius:8px">#ED7600</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#ED7600;color:#1F1B16;padding:2px 8px;border-radius:8px">#ED7600</span> |
| `accent-ink` | Accent as text, icons, status dots | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#141312;color:#ED7600;padding:2px 8px;border-radius:8px">#ED7600</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#FAF8F4;color:#A55200;padding:2px 8px;border-radius:8px">#A55200</span> |
| `on-accent` | Label on the accent fill | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#ED7600;color:#1F1B16;padding:2px 8px;border-radius:8px">#1F1B16</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#ED7600;color:#1F1B16;padding:2px 8px;border-radius:8px">#1F1B16</span> |
| `focus` | Focus ring | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#7CC4FF;color:#141312;padding:2px 8px;border-radius:8px">#7CC4FF</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#1F5FCC;color:#FFFFFF;padding:2px 8px;border-radius:8px">#1F5FCC</span> |
| `ok` | Good state (with words) | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#5FD39A;color:#141312;padding:2px 8px;border-radius:8px">#5FD39A</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#1E7A4C;color:#FFFFFF;padding:2px 8px;border-radius:8px">#1E7A4C</span> |
| `err` | Problem state (with words) | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#FF7A70;color:#141312;padding:2px 8px;border-radius:8px">#FF7A70</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#B3261E;color:#FFFFFF;padding:2px 8px;border-radius:8px">#B3261E</span> |
| `pad` | Controller illustration body | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#3A3633;color:#F5F2EC;padding:2px 8px;border-radius:8px">#3A3633</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#E6E0D5;color:#1F1B16;padding:2px 8px;border-radius:8px">#E6E0D5</span> |
| `pad-2` | Illustration shading | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#2A2725;color:#F5F2EC;padding:2px 8px;border-radius:8px">#2A2725</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#D5CDBF;color:#1F1B16;padding:2px 8px;border-radius:8px">#D5CDBF</span> |
| `pad-key` | Illustration keys | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#4A4542;color:#F5F2EC;padding:2px 8px;border-radius:8px">#4A4542</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#F6F2EB;color:#1F1B16;padding:2px 8px;border-radius:8px">#F6F2EB</span> |
| `shadow` | Elevation | none | `0 1px 2px rgba(60,40,20,.06), 0 8px 24px rgba(60,40,20,.08)` |

The accent is the VOLTAGE brand orange.

### Accent rules

| Rule | ID |
|---|---|
| The accent fill (`#ED7600`) must fill only the one main action on a screen (Start riding, Continue, Save) and mark live moments (the lit button, "You just pressed"). | UX-V-001 |
| The label on an accent fill must be `on-accent` (`#1F1B16`) in both modes. The app must never use white text on the accent (2.92:1). | UX-V-002 |
| Accent used as text, an icon, or a status dot must use `accent-ink`: `#ED7600` in dark mode, `#A55200` in light mode. | UX-V-003 |
| The Connections tab dot must use `accent-ink` (`#A55200` in light mode, `#ED7600` in dark mode). | UX-V-004 |
| In light mode the accent fill against the background is only 2.75:1. You must therefore be able to identify a primary button by its dark text label and its shape, never by the fill color alone. | UX-V-005 |
| Everything else must stay neutral. Screens must not use the accent for decoration. | UX-V-006 |
| The app's identity must never rely on color alone. The name "Voltage Gamepad", the controller illustration, and the wordmark carry it. | UX-V-007 |
| The app must not adopt a neighbor's category color: Strava orange, Wahoo or Garmin blue, Xbox green, PlayStation blue, Nintendo red, conservation green. | UX-V-008 |

??? note "Edge cases"
    - The app sits beside Zwift, which is also orange-leaning. That is why UX-V-007 asks for identity through name, illustration, and wordmark rather than color.
    - Purple and violet gradients, neon fills, and gradient washes are not used.

## Contrast

Text needs 4.5:1. Icons, focus rings, status dots, and graphics people need to see need 3:1. Values below are computed.

| Pair | Dark | Light |
|---|---|---|
| `text` on `bg` | 16.61 | 16.14 |
| `text` on `surface` | 15.04 | 17.12 |
| `muted` on `bg` | 7.59 | 6.03 |
| `muted` on `surface` | 6.88 | 6.39 |
| `muted` on `surface-2` | 6.01 | 5.48 |
| `accent-ink` on `bg` | 6.36 | 5.20 |
| `accent-ink` on `surface` | 5.75 | 5.52 |
| `accent-ink` on `surface-2` | 5.03 | 4.73 |
| `on-accent` on accent fill | 5.86 | 5.86 |
| `focus` on `bg` | 9.89 | 5.55 |
| `ok` on `bg` | 9.96 | 5.02 |
| `err` on `bg` | 7.31 | 6.16 |
| accent fill on `bg` (graphic) | 6.36 | 2.75 |
| illustration key on body | 1.27 | 1.18 |

The last two rows are below 3:1, so they carry extra cues. See [Accessibility](accessibility.md#color-contrast).

| Rule | ID |
|---|---|
| Every text pair must reach 4.5:1 and every meaningful non-text pair 3:1, in both modes. | UX-V-009 |
| I must check every color pair against this table before I build a screen. | UX-V-010 |

## Type

| Rule | ID |
|---|---|
| The typeface must be Figtree. | UX-V-011 |
| Headings must use weight 800 with slightly negative tracking. Body text must use weight 400 to 600. | UX-V-012 |
| Numbers must use tabular figures so readouts do not jitter. | UX-V-013 |
| Text must be sentence case. The app must not use all caps. | UX-V-014 |
| Text must wrap to fit its container. The app must never cut off or clip a word, at any window width or text size. A row or card grows taller instead. | UX-V-044 |

*Example: on a narrow window the Buttons list drops from two columns to one. A long function name wraps to a second line inside its row, and the row grows taller.*

| Role | Phone | Tablet |
|---|---|---|
| Display | 32 to 34 | 52 to 56 |
| Readout (big number) | 44 to 56 | 64 |
| Title | 22 to 24 | 26 to 30 |
| Body | 16 to 17 | 18 to 20 |
| Caption | 14 | 15 to 16 |

Sizes are in px (dp on Android, pt on iOS). Desktop uses the tablet column.

## Spacing, radius, and size

| Item | Value | ID |
|---|---|---|
| Spacing scale | 4, 8, 12, 16, 20, 24, 32, 48 | UX-V-015 |
| Pills (buttons, chips, keycaps) | Radius is half the height | UX-V-016 |
| Rows | Radius 16 to 20 | UX-V-017 |
| Cards | Radius 24 to 28 on phone | UX-V-018 |
| Panels | Radius 32 on tablet | UX-V-019 |
| Minimum target | 48 px | UX-V-020 |
| Focus ring | 3 px, 2 px offset | UX-V-021 |

## Surfaces

| Rule | ID |
|---|---|
| The app must separate surfaces with color, spacing, and rounded corners. It must not use outlines (1 px borders). | UX-V-022 |
| The app may use soft shadows in light mode only. Dark mode has no shadows. | UX-V-023 |
| Neutrals must be warm: a warm near-black in dark mode and a warm off-white in light mode. The app must not use blue-gray. | UX-V-024 |
| The app must not use gradients. | UX-V-025 |

??? note "Edge cases"
    - The only outlines allowed are the ones the accessibility rules require: a thin muted outline on controller keys, a dashed outline on free keys, a dark outline on the lit key, and the 2 px muted outline on an unchecked choice (UX-V-043).
    - With a Windows contrast theme on, the app uses system colors instead of these tokens.

## Buttons

| Kind | Height | Padding left and right | Label |
|---|---|---|---|
| Row, dialog, and secondary actions | 48 | 24 | 15 to 20, weight 700 |
| Standard | 56 | 28 | 15 to 20, weight 700 |
| The screen's one primary action | 60 to 64 | 28 | 15 to 20, weight 800 |

| Rule | ID |
|---|---|
| An icon in a button must be 18 to 20 px with a 10 px gap to the label. | UX-V-026 |
| Buttons must hug their label by default. The minimum width is 120 px, and 160 px for the primary action. | UX-V-027 |
| A button may be full width only for phone bottom actions and phone dialogs. On larger screens a full-width button must stop at 480 px. | UX-V-028 |
| The app must never wrap or truncate a button label, and a button must never shrink below its label. | UX-V-029 |
| When a row runs out of room, the row must wrap and the button must move to its own line. On phone it goes full width. | UX-V-030 |
| Buttons in a group must be 8 px apart (12 px on tablet and desktop). | UX-V-031 |
| Button order must follow the platform: Windows dialogs put the primary action first, Android and iOS put it last. | UX-V-032 |
| A screen must have at most one primary (accent) button. | UX-V-033 |

*Example: a German label for "Start riding" is longer. The button grows, and if the row is full, the button moves to its own line instead of breaking the label.*

## Fields and choices

Dropdowns, text boxes, checkboxes, switches, and radio buttons share one look, so a screen reads as one set of controls.

| Rule | ID |
|---|---|
| Dropdowns and text boxes must be 48 px high with the `surface-2` fill and radius 14. Their text must be centered vertically. | UX-V-039 |
| A dropdown's list must open below it on a `surface` panel with radius 16. The selected item must be bold. | UX-V-040 |
| A checked checkbox, switch, or radio button must use the `text` color, not the accent (UX-V-006). | UX-V-041 |
| A choice and its label must form one 48 px target (UX-V-020). Clicking the label toggles the choice. | UX-V-042 |
| An unchecked checkbox, an off switch, and an empty radio button must have a 2 px `muted` outline, so the control stands out from the background at 3:1 or more. This is one of the outlines accessibility needs (see the edge cases under Surfaces). | UX-V-043 |

On Windows the buttons, fields, and surfaces live in `Resources/Controls.xaml` and `Resources/Surfaces.xaml`. In Debug builds, the style gallery in the More menu shows each one in every state.

## Illustration

| Rule | ID |
|---|---|
| The app must fill the controller illustration with soft shapes and a ground shadow. It must never draw a wireframe. | UX-V-034 |
| Keys on the illustration must have a thin muted outline, free keys a dashed outline, and the lit key a dark outline plus the accent fill. | UX-V-035 |
| The illustration must never be the only way to know a mapping. The Buttons list says the same in text. | UX-V-036 |

## Motion

| Rule | ID |
|---|---|
| Motion must be 100 to 200 ms and only for state changes. | UX-V-037 |
| The app must respect the system reduce-motion setting. See [Accessibility](accessibility.md#text-size-and-motion). | UX-V-038 |
