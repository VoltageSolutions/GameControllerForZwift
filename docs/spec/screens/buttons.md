---
id: SPEC-SCR-BTN
title: Buttons screens
status: Draft
last-updated: 2026-11-17
---

# Buttons screens

!!! tldr "TL;DR"
    The Buttons tab is where you choose what each input on your controller does. A layout picker, Save and Save as new sit at the top. Two tabs below split buttons from sticks and triggers. A controller illustration and a list say the same thing in two ways. Tapping a row or pressing a button on your controller opens a picker of Zwift functions, grouped by what they do. Saving and switching layouts follow the saved layouts spec by ID.

In the rules, "must", "should" and "may" have the meaning given in [how to read a spec](../index.md). A **layout** is what the saved layouts spec (`CFG`) calls a configuration.

```mermaid
flowchart TD
    B["Buttons"] --> P["Layout picker"]
    B --> T1["Buttons tab"]
    B --> T2["Sticks and triggers tab"]
    T1 --> F["Choose what a button does"]
    T2 --> F
    T2 --> A["Advanced"]
    P --> SA["Save as"]
    P --> SW["Unsaved changes when switching"]
    P --> M["Per-layout More:<br/>Rename, Delete"]
    P --> I["Import a layout"]
```

## The Buttons screen

**What it is for:** see and change what every input does.

**Content, top to bottom:** the layout picker (`ButtonsLayoutPicker`, for example "Layout: Climbing, unsaved changes"), **Save** and **Save as new** (`ButtonsSaveButton`, `ButtonsSaveAsButton`), two tabs (Buttons, and Sticks and triggers), the controller illustration (`ButtonsControllerImage`), and the list of what each input does (`ButtonsRow_{input}`, for example "Right bumper: shift up").

The screen's hint says: "Press a button on your controller to select it."

*Example: you press X on your controller. Its row is selected and the picker opens. X is a free button, so the row says "No action".*

| Rule | ID |
|---|---|
| The screen must have a layout picker, Save and Save as new, a Buttons tab, a Sticks and triggers tab, the controller illustration and a list of what each input does. | SCR-BTN-001 |
| The list must carry the same information as the illustration, in text. | SCR-BTN-002 |
| Each list row must say the input and its function, for example "Right bumper: shift up". A button with no function must say "No action". | SCR-BTN-003 |
| The illustration must show the selected button, mark free keys with a dashed outline and use the lit style for a press (see [Visual system](../ux/visual-system.md)). | SCR-BTN-004 |
| Tapping or clicking a row, or pressing the matching button on your controller, must select it and open the function picker. | SCR-BTN-005 |
| A controller press here must only find or assign a button. It must not navigate, save or go back. | SCR-BTN-006 |
| The screen must show the hint "Press a button on your controller to select it." and no controller hint bar. | SCR-BTN-007 |
| The Save button is the screen's one main action, and it must be an accent button only when it is enabled (that is, when a saved layout has unsaved changes, see `CFG-037`). | SCR-BTN-008 |
| The screen must have a **Reset to default** action for the current layout's mappings. | SCR-BTN-009 |
| The Buttons screen must stay visual: the illustration first, the list as the text version. | SCR-BTN-010 |

??? note "Edge cases"
    - The Buttons header has no More menu of its own. Advanced options have their own place at the bottom of Sticks and triggers.
    - With no controller, the screen shows the same "Wake your controller" state as Ride.

## Layouts

A layout is a named set of mappings for one controller model. The behavior, storage and rules live in the saved layouts spec. This section says where each part appears on screen, and refers to the spec for the rule.

### Layout picker

Default is listed first and is locked. Saved layouts follow, sorted by name. The selected one shows **Unsaved changes** in words when it has edits. The picker also offers **Import a layout** and, per layout, a More menu (Rename, Delete). On a phone it offers **Share**. On desktop it offers **Open folder**.

| Rule | ID |
|---|---|
| The picker must list Default first, then saved layouts by name, and select the last-used one. See `CFG-015` to `CFG-019`. | SCR-BTN-011 |
| Default must show as locked. It cannot be overwritten, renamed or deleted. See `CFG-020`. | SCR-BTN-012 |
| The selected layout must show "Unsaved changes" in words when its mappings differ from what is saved. See `CFG-021`. | SCR-BTN-013 |
| Buttons that do not apply must be disabled, not hidden. See `CFG-037`. | SCR-BTN-014 |
| The picker must offer **Import a layout**. See `CFG-030`. | SCR-BTN-015 |
| On desktop the picker must offer **Open folder**. See `CFG-034`. | SCR-BTN-016 |
| On phone the picker must offer **Share** in its place. Sharing a layout hands the layout's file to the system share sheet. | SCR-BTN-017 |
| Each saved layout must have a More menu with Rename and Delete. Default's More menu must have neither enabled. See `CFG-020`, `CFG-025` and `CFG-029`. | SCR-BTN-018 |

### Save and Save as

| Rule | ID |
|---|---|
| **Save** must overwrite the selected layout. See `CFG-022`. | SCR-BTN-019 |
| **Save as new** must ask for a name and select the new layout. See `CFG-023`. | SCR-BTN-020 |
| The name field must check the name as you type and disable Save until the name is valid. See `CFG-026` to `CFG-028`. | SCR-BTN-021 |
| When a name is already used, the message should read "You already have a layout called Climbing. Try another name." | SCR-BTN-022 |

*Example: you type "climbing" and already have "Climbing". The field says "You already have a layout called Climbing. Try another name." and Save stays disabled.*

### Unsaved changes when switching

| Rule | ID |
|---|---|
| Switching layout or controller with unsaved changes must ask what to do. See `CFG-024`. | SCR-BTN-023 |
| The dialog's choices should read **Save and switch**, **Switch without saving** and **Cancel**. | SCR-BTN-024 |
| While the dialog is open, the controller must be able to answer it, with a focus ring and an A and B hint. See `CFG-038` and UX-C-005. | SCR-BTN-025 |

### Rename, delete and import

| Rule | ID |
|---|---|
| **Rename layout** must open a dialog with the current name filled in and focused, a 50-character limit, and Cancel and Rename. See `CFG-025` and `CFG-026`. | SCR-BTN-026 |
| **Delete** must say the file is deleted and that it can't be undone, and must say the controller then switches to Default. See `CFG-029`. | SCR-BTN-027 |
| Importing a layout made for a different controller must ask whether to use it with this controller. See `CFG-033`. | SCR-BTN-028 |
| Importing a layout whose name is used must ask for a new name, filled in with a suggestion such as "Climbing (2)". See `CFG-032`. | SCR-BTN-029 |
| After a successful import the app must show "Saved a copy to your layouts." with an Undo. See `CFG-031`. | SCR-BTN-030 |
| Settings must offer **Delete all saved layouts**, stating the count and that it can't be undone. See `CFG-035` and [Settings and help](settings-help.md). | SCR-BTN-031 |

??? note "Edge cases"
    - The saved layouts spec describes the notice and the dialog buttons in its own words ("configuration", "Don't save"). The words in this page are the UI words and are used in the app.
    - The saved layouts spec describes a bar of buttons above the mapping grid (`CFG` "UI layout"). The screens here replace that arrangement with the picker, Save and the per-layout More menu. The rules `CFG-037` and `CFG-038` still apply.
    - The Import file picker is the standard system file picker. It works with a mouse and keyboard, not with a controller.

## Choose what a button does

**What it is for:** pick a Zwift function for the selected input.

It opens as a sheet on a phone, and as a side panel on a landscape screen. Functions are grouped by what they do and use Zwift Ride names. Hold hints and a brake caution sit next to the ones that need them.

| Group | Functions |
|---|---|
| **Riding** | Shift up (harder gear), Shift down (easier gear), Steer left, Steer right, Brake |
| **Zwift buttons** | Ride On, Power-up |
| **Menus** | Navigate up, Navigate down, Navigate left, Navigate right, Select, Back |
| **This app** | Functions that are the app's own, such as switching window size |
| **Nothing** | No action |

| Hint | Where |
|---|---|
| "In a ride, opens the action bar" | Navigate up |
| "In a ride, makes a U-turn" | Navigate down |
| "Hold to send Ride Ons to nearby riders (Zwift's behavior)" | Ride On |
| "Hold to skip a workout block (Zwift's behavior)" | Power-up |
| "Even a light squeeze can stop you" | Brake |

| Rule | ID |
|---|---|
| The picker must show functions in the groups Riding, Zwift buttons, Menus, This app and Nothing, in that order. | SCR-BTN-032 |
| Functions must use the real Zwift Ride names. The picker must not list invented functions such as "U-turn" or "Camera view". | SCR-BTN-033 |
| Each function must be a radio choice (`FunctionItem_{function}`) with one short line saying what it does, such as "Shift up, harder gear". | SCR-BTN-034 |
| Navigate up, Navigate down, Ride On and Power-up must show their hint. | SCR-BTN-035 |
| **This app** must list only functions the app really implements. "Switch window size" belongs here and must not be shown as a Zwift function. | SCR-BTN-036 |
| The sheet or panel must trap focus, and Esc or B must close it. See [Accessibility](../ux/accessibility.md). | SCR-BTN-037 |
| Choosing a function must apply right away to the current layout and show "Unsaved changes". | SCR-BTN-038 |

??? note "Edge cases"
    - Shift up in a workout raises the intensity, and shift down lowers it.
    - Offering Zwift's hold actions as separate mappable functions is only possible if Zwift's protocol allows it. Until then the hint explains the hold.

## Sticks and triggers

**What it is for:** show analog inputs live and let you map each direction.

Sticks and triggers are analog. A small tilt steers a little. A light squeeze brakes a little. The tab shows each stick's live position with its dead zone, one row per direction, and live trigger fill.

| Rule | ID |
|---|---|
| Each stick must show its live position and the dead zone. | SCR-BTN-039 |
| Each stick must have a row per direction (left, right, up, down) and each stick click must have its own row. | SCR-BTN-040 |
| Each trigger must show a live fill. | SCR-BTN-041 |
| The screen must show a brake caution: "Even a light squeeze can stop you." It must show where the brake function is chosen or assigned. | SCR-BTN-042 |
| **Advanced** (dead zones and curves) must be a section at the bottom of this tab. | SCR-BTN-043 |

??? note "Edge cases"
    Per-layout dead zones or curves are out of scope in the saved layouts spec. Dead zones are global.

## Controller input on these screens

A controller press finds or assigns a button. Dialogs are answered with A and B. See [Controller input](../ux/controller-input.md).

## Accessibility IDs

| ID | Where |
|---|---|
| `ButtonsLayoutPicker`, `ButtonsSaveButton`, `ButtonsSaveAsButton` | Top of the screen |
| `ButtonsRow_{input}` | List rows |
| `ButtonsControllerImage` | Illustration |
| `FunctionItem_{function}` | Function picker |
| `LayoutItem_{id}` | A saved layout in the picker (the layout's GUID) |
| `DialogPrimaryButton`, `DialogSecondaryButton`, `DialogCancelButton` | Dialogs |

## Implementation status

Not implemented yet for the screens described here. The current Windows app has a single Controller Setup page with cards for Buttons, D-pad, Left stick, Right stick and Shoulder buttons. Each input has a drop-down of function names. Rows light up while pressed, and the sticks and triggers show live graphs. Saved layouts (`CFG`) are specified separately. The current function names are still the older labels, for example "Select (A)" and "No Action Configured". There is no function picker, no Advanced section and no illustration.
