---
id: SPEC-UX-VISUAL
title: Visual system
last-updated: 2026-11-22
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
| `accent-ink` | Accent as text and icons | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#141312;color:#ED7600;padding:2px 8px;border-radius:8px">#ED7600</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#FAF8F4;color:#A55200;padding:2px 8px;border-radius:8px">#A55200</span> |
| `on-accent` | Label on the accent fill | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#ED7600;color:#1F1B16;padding:2px 8px;border-radius:8px">#1F1B16</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#ED7600;color:#1F1B16;padding:2px 8px;border-radius:8px">#1F1B16</span> |
| `focus` | Focus ring | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#7CC4FF;color:#141312;padding:2px 8px;border-radius:8px">#7CC4FF</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#1F5FCC;color:#FFFFFF;padding:2px 8px;border-radius:8px">#1F5FCC</span> |
| `ok` | Good state (with words) | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#5FD39A;color:#141312;padding:2px 8px;border-radius:8px">#5FD39A</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#1E7A4C;color:#FFFFFF;padding:2px 8px;border-radius:8px">#1E7A4C</span> |
| `err` | Problem state (with words) | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#FF7A70;color:#141312;padding:2px 8px;border-radius:8px">#FF7A70</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#B3261E;color:#FFFFFF;padding:2px 8px;border-radius:8px">#B3261E</span> |
| `pad` | Controller illustration body | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#3A3633;color:#F5F2EC;padding:2px 8px;border-radius:8px">#3A3633</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#E6E0D5;color:#1F1B16;padding:2px 8px;border-radius:8px">#E6E0D5</span> |
| `pad-2` | Illustration shading | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#2A2725;color:#F5F2EC;padding:2px 8px;border-radius:8px">#2A2725</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#D5CDBF;color:#1F1B16;padding:2px 8px;border-radius:8px">#D5CDBF</span> |
| `pad-key` | Illustration keys | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#4A4542;color:#F5F2EC;padding:2px 8px;border-radius:8px">#4A4542</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#F6F2EB;color:#1F1B16;padding:2px 8px;border-radius:8px">#F6F2EB</span> |
| `art-body` | Controller art: body and touchpad fill | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#34302D;color:#F5F2EC;padding:2px 8px;border-radius:8px">#34302D</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#EFEAE0;color:#1F1B16;padding:2px 8px;border-radius:8px">#EFEAE0</span> |
| `art-shade` | Controller art: shade for keys, wells, sticks, caps | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#3E3A36;color:#F5F2EC;padding:2px 8px;border-radius:8px">#3E3A36</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#E5DECF;color:#1F1B16;padding:2px 8px;border-radius:8px">#E5DECF</span> |
| `art-line` | Controller art: outline strokes | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#A89F94;color:#1F1B16;padding:2px 8px;border-radius:8px">#A89F94</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#6F675C;color:#F5F2EC;padding:2px 8px;border-radius:8px">#6F675C</span> |
| `art-detail` | Controller art: detail strokes (well and cap rings) | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#8F877D;color:#1F1B16;padding:2px 8px;border-radius:8px">#8F877D</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#82796B;color:#F5F2EC;padding:2px 8px;border-radius:8px">#82796B</span> |
| `art-glyph` | Controller art: glyph on a key | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#E4DDD2;color:#1F1B16;padding:2px 8px;border-radius:8px">#E4DDD2</span> | <span style="font-family:var(--md-code-font-family, ui-monospace, Consolas, monospace);background:#4F483F;color:#F5F2EC;padding:2px 8px;border-radius:8px">#4F483F</span> |
| `shadow` | Elevation | none | `0 1px 2px rgba(60,40,20,.06), 0 8px 24px rgba(60,40,20,.08)` |

The accent is the VOLTAGE brand orange.

### Accent rules

| Rule | ID |
|---|---|
| The accent fill (`#ED7600`) must fill only the one main action on a screen (Pair with Zwift, Continue, Save) and mark live moments (the lit button, "You just pressed"). | UX-V-001 |
| The label on an accent fill must be `on-accent` (`#1F1B16`) in both modes. The app must never use white text on the accent (2.92:1). | UX-V-002 |
| Accent used as text or an icon must use `accent-ink`: `#ED7600` in dark mode, `#A55200` in light mode. | UX-V-003 |
| The Connections tab dot must use `err` (`#B3261E` in light mode, `#FF7A70` in dark mode), because it means something needs the person. | UX-V-004 |
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
| `art-line` on `art-body` | 5.01 | 4.65 |
| `art-line` on `art-shade` | 4.32 | 4.16 |
| `art-detail` on `art-body` | 3.69 | 3.58 |
| `art-detail` on `art-shade` | 3.18 | 3.20 |
| `art-glyph` on `art-shade` | 8.36 | 6.73 |
| `art-body` on `bg` (graphic) | 1.42 | 1.13 |
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
| Spacing scale | 4, 8, 12, 16, 20, 24, 32, 40, 48 | UX-V-015 |
| Pills (buttons, chips, keycaps) | Radius is half the height | UX-V-016 |
| Rows | Radius 16 to 20 | UX-V-017 |
| Cards | Radius 24 to 28 on phone | UX-V-018 |
| Panels | Radius 32 on tablet | UX-V-019 |
| Minimum target | 48 px | UX-V-020 |
| Focus ring | 3 px, 2 px offset | UX-V-021 |

## Layout and spacing

Every screen uses the same margins and gaps, so a person who has seen one screen has seen the spacing of all of them. Values are in px.

| Item | Phone | Tablet | Desktop |
|---|---|---|---|
| Page side margin | 20 | 48 | 40 |
| Page top padding | 24 | 32 | 24 |
| Page bottom padding | 24 | 48 | 40 |
| Gap between sections | 24 | 32 | 32 |
| Gap from a group label to its rows | 8 | 8 | 8 |
| Gap between list rows | 8 | 12 | 12 |
| Gap between cards | 16 | 24 | 24 |
| Card padding | 20 | 32 | 32 |
| Row padding (vertical, horizontal) | 12, 20 | 12, 20 | 12, 20 |

A status chip is 40 high with 16 of padding left and right. It holds an 8 px dot, then 8 px of space, then the words.

| Rule | ID |
|---|---|
| Every screen must take the values in this section from shared styles. A screen must not set its own margins, gaps, or padding. | UX-V-051 |
| A scrolling area must never clip a shadow. The scroll area must span the window width, and its content must keep the page side margin as padding inside the scroll area, so a card's shadow has room on every side. | UX-V-052 |

On Windows the card, row, and chip styles live in `Resources/Surfaces.xaml`, and the page margins and gaps live in `Resources/Layout.xaml`. A screen refers to them by key and never writes a number.

## Icons

| Rule | ID |
|---|---|
| The app must use one icon set. Each icon must be drawn on a 24 px grid with a 2 px stroke and round caps and joins, in the current text color. | UX-V-053 |
| Icons must be 22 px in the navigation, 18 to 20 px in buttons and rows (UX-V-026), and 24 px for the back button. | UX-V-054 |
| Icons must mark navigation and actions only. An icon must never show a status: no check marks, plugs, or warning signs for a state. | UX-V-055 |

## Status

Status is a dot plus words.

| Rule | ID |
|---|---|
| A working state must use an `ok` dot with words. Anything that needs the person must use an `err` dot with words, including the Connections tab dot (UX-V-004). | UX-V-056 |
| Status text must stay in `text`. Only the dot carries the color. | UX-V-057 |
| The accent must mark only the one main action and live moments (UX-V-001). It must not mark a status. | UX-V-058 |

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

*Example: a German label for "Pair with Zwift" is longer. The button grows, and if the row is full, the button moves to its own line instead of breaking the label.*

## Fields and choices

Dropdowns, text boxes, checkboxes, switches, and radio buttons share one look, so a screen reads as one set of controls.

| Rule | ID |
|---|---|
| Dropdowns and text boxes must be 48 px high with the `surface-2` fill and radius 14. Their text must be centered vertically. | UX-V-039 |
| A dropdown's list must open below it on a `surface` panel with radius 16. The selected item must be bold. | UX-V-040 |
| A checked checkbox, switch, or radio button must use the `text` color, not the accent (UX-V-006). | UX-V-041 |
| A choice and its label must form one 48 px target (UX-V-020). Clicking the label toggles the choice. | UX-V-042 |
| An unchecked checkbox, an off switch, and an empty radio button must have a 2 px `muted` outline, so the control stands out from the background at 3:1 or more. This is one of the outlines accessibility needs (see the edge cases under Surfaces). | UX-V-043 |

## Rows and two-column screens

Settings, Help and feedback, About, and Support share one layout, so a person who has seen one has seen them all.

| Rule | ID |
|---|---|
| A row that does something must be one button, at least 64 px high, radius 20, on `surface` with the soft shadow. It must show a title (17, weight 700), an optional line under it (14, `muted`), and at its right edge a chevron when it opens another screen or an arrow out of a box when it opens something outside the app. The whole row must be the target. | UX-V-045 |
| A row that deletes something must show its title in `err`. A disabled row must use `surface-2` and `muted`, with no shadow. | UX-V-046 |
| A label over a group of rows must be 17, weight 700, `muted`, with 4 px of indent. | UX-V-047 |
| A choice among a few labeled options, such as Appearance, must be a segmented track: pills on `surface-2`, with the chosen pill raised on `surface`. | UX-V-048 |
| On a window at least 900 px wide, these screens must show two columns: the title and the explanation on the left, and a 520 px column of rows on the right. Below 900 px they must show one column, in the same order. | UX-V-049 |
| A screen title on tablet and desktop must be 48, weight 800. | UX-V-050 |

On Windows the row is the `ActionRow` control, the segmented track is the `Segmented` style, and the two columns are the `AdaptiveColumns` panel.

On Windows the buttons, fields, and surfaces live in `Resources/Controls.xaml` and `Resources/Surfaces.xaml`. In Debug builds, the style gallery in the More menu shows each one in every state.

## Illustration

How a drawing is made, with its classes, line weights, and shoulder shapes, is in the [controller graphics style](controller-graphics-style.md).

| Rule | ID |
|---|---|
| The controller illustration must be a wireframe outline with a ground shadow. It must use theme tokens only (`Brush.pad` and the other `pad` tokens), so it works in light and dark mode. | UX-V-034 |
| Keys on the illustration must have a thin muted outline, free keys a dashed outline, and the lit key a dark outline plus the accent fill. | UX-V-035 |
| The illustration must never be the only way to know a mapping. The Buttons list says the same in text. | UX-V-036 |
| The app must draw an illustration that matches the controller's kind (CTRL-011). Xbox, PlayStation DualShock 4, PlayStation DualSense, Switch Pro and the Joy-Con pair each have their own drawing. Every other kind, the Generic kind included, uses the Xbox drawing. | UX-V-059 |
| Every kind must behave the same: the selected input is lit, pressed inputs are lit, stick caps move with the axis, and free keys are dashed (UX-V-035). | UX-V-060 |
| Art must be original or licensed CC BY 4.0, with credit in ACKNOWLEDGEMENTS. The app must never use art copied from another site, such as controllertest.io. | UX-V-061 |
| In every drawing that includes bumpers and triggers, they must be drawn attached to the body at its top edge. A bumper must light when pressed. | UX-V-062 |
| In every drawing that includes analog triggers, they must fill in proportion to the pull, from 0 to 100 percent. The trigger's outline must not light while it is pulled. | UX-V-063 |
| In every drawing that includes digital triggers, they must light when pressed and must not fill. | UX-V-064 |
| Face button glyphs must follow the drawing's labels: A, B, X, and Y for the Xbox drawing, and Cross, Circle, Square, and Triangle for the PlayStation drawing. Logical inputs keep their `ControllerInput` names. | UX-V-065 |
| A drawing must show only the inputs it has. Logical inputs map to the shapes by id (UX-V-068). | UX-V-066 |
| Vector assets must live in the repo as SVG files under `docs/assets/controllers/<kind>/`, one folder per kind that has its own drawing. | UX-V-067 |
| Each interactive shape in an SVG must carry an `id` equal to its `ControllerInput` name, for example `A`, `LeftBumper`, `LeftTrigger`, `LeftThumbstick`, `LeftThumbstickClick`, and `DPadUp`. Each analog trigger must also carry `LeftTriggerFill` or `RightTriggerFill`; a digital trigger (ZL and ZR) carries only `LeftTrigger` or `RightTrigger`. WPF, Android, and iOS must build their native components from the same files. | UX-V-068 |
| A kind without its own drawing must use the Xbox drawing. | UX-V-069 |

??? note "Edge cases"
    - A kind has no shape for an input it lacks, so the app lights nothing for it. A Joy-Con pair uses the left Joy-Con's ids for the left half and the right Joy-Con's ids for the right half.
    - A single Joy-Con is drawn sideways, so a stick direction on screen is rotated from the logical direction. The logical input stays the same.

## Motion

| Rule | ID |
|---|---|
| Motion must be 100 to 200 ms and only for state changes. | UX-V-037 |
| The app must respect the system reduce-motion setting. See [Accessibility](accessibility.md#text-size-and-motion). | UX-V-038 |
