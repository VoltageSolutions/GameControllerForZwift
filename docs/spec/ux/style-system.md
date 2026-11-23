---
id: SPEC-UX-STY
title: Style system
last-updated: 2026-11-22
---

# Style system

!!! tldr "TL;DR"
    Every app has a style system in its own language, in four layers: tokens, size classes, components, and screens. A screen only composes components and tokens, and it never sets a raw size, spacing, radius, font size, font weight, or color. A change to the style system reaches every screen with no page edits, and two tests per platform catch drift: spec to tokens, and screens to style system.

In the rules, "must", "should", and "may" have the meaning given in [how to read a spec](../index.md).

## Why

The [visual system](visual-system.md) says how the app looks. This page says how each app keeps to it. Without it, a change to the spec means a change to every page, and the pages drift apart: different margins, different title sizes, clipped shadows. With it, I change a value in one place and every screen follows.

## Layers

Each layer uses only the layers above it in this list.

| Layer | What it holds |
|---|---|
| Tokens | Color, type, spacing, radius, and elevation, as named values. |
| Size classes | Phone, tablet, and desktop. Tokens that vary by size class resolve through the class. |
| Components | Page, Stack, Section, Card, and the rest of the component list below. Each one is built from tokens. |
| Screens | Compositions of components. A screen holds no style decisions. |

| Rule | ID |
|---|---|
| Every app must implement the four layers in its own native form. | UX-S-001 |
| A screen must use only components and tokens. | UX-S-002 |
| A change to a token or a component must reach every screen that uses it, with no edit to any screen. | UX-S-003 |

## Size classes

| Rule | ID |
|---|---|
| The app must choose the size class once, from the window width, in one place. | UX-S-004 |
| On Windows the size class must follow the navigation layout (UX-N-010): the bottom bar is phone, the rail is tablet, and the sidebar is desktop. | UX-S-005 |
| A token that varies by size class must resolve through the class. A screen must never check the width itself. | UX-S-006 |
| Where the visual system gives one value for all three classes, all three sets of tokens must hold that value. | UX-S-007 |

*Example: the screen title is 34 on phone and 48 on tablet and desktop. The Ride screen asks for the title style and gets 34 or 48. It never reads the window width.*

## Components

Each component maps to a rule in the [visual system](visual-system.md) and to a name on each platform.

| Component | What it is | Rules |
|---|---|---|
| Page | The scroll area of a screen. It spans the window width and keeps the page padding inside the scroll area, so a shadow never clips. The screen title takes the page title style, and on a pushed screen the shell puts the back button inside the page margin. | UX-V-051, UX-V-052, UX-N-016 |
| Stack | A vertical or horizontal list with a gap from a spacing token. | UX-V-015, UX-V-031 |
| Section | A group label over its rows. | UX-V-047 |
| Card | A surface for a group of content. | UX-V-018, UX-V-022, UX-V-023 |
| ActionRow | A row that does something. | UX-V-045, UX-V-046 |
| Chip | A pill for a keycap or a short label. | UX-V-016 |
| StatusChip | A chip with a status dot and words. | UX-V-056, UX-V-057 |
| StatusDot | An 8 px dot in `ok` or `err`. | UX-V-055, UX-V-056 |
| Stat | A number over a label. | UX-V-013 |
| Segmented | A choice among a few labeled options. | UX-V-048 |
| Buttons | Primary, standard, secondary, and icon buttons. | UX-V-026 to UX-V-033 |
| Fields and choices | Dropdowns, text boxes, checkboxes, switches, and radio buttons. | UX-V-039 to UX-V-043 |
| AdaptiveColumns | One column on a narrow window and two on a wide one. | UX-V-049 |
| Icon | One icon on a 24 px grid with a 2 px stroke. | UX-V-053, UX-V-054 |

| Rule | ID |
|---|---|
| Every component must be built from tokens and must take its size-class values through the size class. | UX-S-008 |
| Every platform must keep a table of its component names, as the Windows table below does. | UX-S-009 |
| A component with behavior must be a control. A component that is only looks may be a style. | UX-S-010 |
| Each component must appear in every state in the style gallery of a Debug build. | UX-S-011 |

## No raw values in screens

| Rule | ID |
|---|---|
| A screen file must not set a raw size, spacing, radius, font size, font weight, or color. | UX-S-012 |
| A screen may use a value that is not a style decision, such as `0`, `Auto`, a star size in a grid, a binding, or a token reference. | UX-S-013 |
| An exception must live in an allowlist, and every entry must carry a reason. | UX-S-014 |

## Drift tests

| Rule | ID |
|---|---|
| Every platform must have a test that compares the values in the spec tables with the platform's tokens, and a failure must name the spec rule, the token, and both values. | UX-S-015 |
| Every platform must have a test that parses the screen files and fails on a raw value, and a failure must name the file, the line, the attribute, and the value. | UX-S-016 |

## Windows

The tokens and components live in the WPF app under `windows/src/Presentation/GameControllerForZwift/`.

| Layer | Item | File | Key or type |
|---|---|---|---|
| Tokens | Color | `Resources/Theme/Tokens.Dark.xaml`, `Tokens.Light.xaml` | `Color.<token>`, `Brush.<token>` |
| Tokens | Spacing scale | `Resources/Space.xaml` | `Space.4` to `Space.48` |
| Tokens | Radius | `Resources/Space.xaml` | `Radius.Row`, `Radius.Card`, `Radius.Panel`, `Radius.Pill` |
| Tokens | Elevation | `Resources/Theme/Tokens.Light.xaml` | `Effect.shadow` |
| Tokens | Type roles | `Resources/Typography.xaml` | `Text.*` |
| Size classes | Phone, tablet, desktop values | `Resources/Layout.Phone.xaml`, `Layout.Tablet.xaml`, `Layout.Desktop.xaml` | `Layout.*` |
| Size classes | The switch | `Resources/Theme/LayoutService.cs` | `LayoutService` |
| Components | Page | `Controls/ScreenPage.cs` | `ScreenPage` |
| Components | Stack | `Controls/Stack.cs` | `Stack` |
| Components | Section | `Controls/Section.cs` | `Section` |
| Components | Card | `Controls/Card.cs`, `Resources/Surfaces.xaml` | `Card`, `Surface.Card` |
| Components | ActionRow | `Controls/ActionRow.cs`, `Resources/Controls.xaml` | `ActionRow`, `ActionRow.Danger` |
| Components | Chip | `Resources/Surfaces.xaml` | `Surface.Chip`, `Chip.Key` |
| Components | StatusChip | `Controls/StatusChip.cs`, `Resources/Components.xaml` | `StatusChip` |
| Components | StatusDot | `Controls/StatusDot.cs`, `Resources/Components.xaml` | `StatusDot` |
| Components | Stat | `Controls/Stat.cs`, `Resources/Components.xaml` | `Stat` |
| Components | Segmented | `Resources/Controls.xaml` | `Segmented` |
| Components | Buttons | `Resources/Controls.xaml` | `Button.Primary`, `Button.Standard`, `Button.Secondary`, `Button.Icon` |
| Components | Fields and choices | `Resources/Controls.xaml` | Implicit styles, `Switch` |
| Components | AdaptiveColumns | `Controls/AdaptiveColumns.cs` | `AdaptiveColumns` |
| Components | Icon | `Resources/Icons.xaml`, `Resources/Controls.xaml` | `Icon.*`, `IconStroke` |
| Screens | Shell and pages | `MainWindow.xaml`, `Views/`, `Dialogs/` | none |

On Windows a screen refers to a token or a component by key and never writes a number.

### Tests on Windows

| Test | What it checks | Rules |
|---|---|---|
| `TokenSpecTests` | The color tables in the visual system equal the color tokens. | UX-S-015 |
| `LayoutSpecTests` | The layout and type tables in the visual system equal `Layout.Phone.xaml`, `Layout.Tablet.xaml`, and `Layout.Desktop.xaml`. | UX-S-007, UX-S-015 |
| `ScreenStyleTests` | No screen file holds a raw value. The allowlist is `Theme/ScreenStyleAllowlist.txt` in the test project, and every entry has a reason. | UX-S-012 to UX-S-014, UX-S-016 |
| `LayoutServiceTests` | Each navigation layout loads the matching layout dictionary. | UX-S-004 to UX-S-006 |
