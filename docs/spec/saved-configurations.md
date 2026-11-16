---
id: SPEC-CFG
title: Saved controller configurations
status: Accepted
target: Windows v0.4.0
last-updated: 2026-11-16
---

# Saved controller configurations

**TL;DR:** Users can save, reload, rename, delete, import and share controller configurations (button mappings). Each configuration is one JSON file in a per-user folder. Configurations belong to a controller *model* (vendor and product ID), so a second controller of the same model shares them. The built-in Default is always available and read-only.

## Terms

- **Configuration (config):** a named set of input mappings for one controller model. The code calls this a `ControllerProfile`.
- **Controller model:** identified by USB vendor ID (VID) and product ID (PID) as reported by SDL. Not the serial number or the per-device instance ID.
- **Default:** the built-in configuration shipped with the app (`defaultprofile.json`).
- **Config folder:** `%LOCALAPPDATA%\GameControllerForZwift\Profiles\`.
- **App data folder:** `%LOCALAPPDATA%\GameControllerForZwift\` (also holds `settings.json`).

Requirements use MUST, SHOULD and MAY in the usual sense. Each has a stable ID so tests, issues and other specs can point to it.

## Storage

- **CFG-001** Each saved configuration MUST be stored as its own JSON file in the config folder.
- **CFG-002** The app MUST create the config folder on first save if it does not exist.
- **CFG-003** The file MUST contain everything needed to load it; the filename is for humans only. Loading MUST NOT depend on the filename.
- **CFG-004** Filenames SHOULD be `<ControllerName>_<ConfigName>.json`, with characters that are invalid in Windows filenames replaced by `-` and whitespace collapsed to `-`. If the name is taken on disk, append `-2`, `-3`, and so on.
- **CFG-005** Files that fail to parse MUST be skipped with a log warning. They MUST NOT crash the app or be deleted.
- **CFG-006** The app SHOULD watch the config folder and refresh lists when files are added, changed or removed outside the app.

### File format (version 1)

```json
{
  "formatVersion": 1,
  "id": "6f1c2b9e-1d2a-4f2e-9a51-3c0d7c1b2a10",
  "name": "Climbing",
  "controller": {
    "vendorId": "045E",
    "productId": "0B13",
    "name": "Xbox Series X Controller"
  },
  "mappings": [
    { "input": "RightShoulder", "function": "ShiftUp" },
    { "input": "LeftShoulder", "function": "ShiftDown" }
  ],
  "modified": "2026-11-16T21:15:00Z"
}
```

- **CFG-007** `formatVersion` MUST be present. The app MUST refuse (with a clear message) files with a higher version than it understands.
- **CFG-008** `id` is a GUID assigned on creation. It identifies a configuration across renames and refreshes.
- **CFG-009** `input` and `function` use the existing enum names (`ControllerInput`, `ZwiftFunction`). Unknown values load as `None` (matches today's JSON converters).
- **CFG-010** Inputs missing from `mappings` load as `None`.

## Controller identity

- **CFG-011** Configurations MUST be associated with a controller model by VID and PID, not by serial number or device instance.
- **CFG-012** When a controller connects, the app MUST list every configuration whose VID and PID match, regardless of which physical unit saved it.
- **CFG-013** The app SHOULD support an alias list of VID/PID pairs that are the same model (for example a controller that reports a different PID over USB and Bluetooth). Start empty; add entries as real cases appear.
- **CFG-014** When SDL reports no VID or PID (0000), the app MUST fall back to the controller name for matching.

## Selecting a configuration

- **CFG-015** The configuration list for the selected controller MUST show Default first, then saved configurations sorted by name (case-insensitive).
- **CFG-016** If the controller has no saved configurations, Default MUST be selected.
- **CFG-017** Otherwise the last-used configuration for that controller model MUST be selected. Last-used is stored in `settings.json` by configuration `id`.
- **CFG-018** If the last-used configuration no longer exists, Default MUST be selected.
- **CFG-019** After any refresh (save, save as, rename, delete, import, folder change), the list MUST be rebuilt and the previously selected configuration MUST stay selected, matched by `id`. If it was deleted, select Default.

## Editing and saving

- **CFG-020** Default MUST be read-only: it cannot be overwritten, renamed or deleted. Editing mappings while Default is selected is allowed; only **Save as** is enabled.
- **CFG-021** When the current mappings differ from the selected configuration as saved, the UI MUST show an unsaved-changes indicator (for example `Climbing •`).
- **CFG-022** **Save** MUST overwrite the selected configuration's file and update `modified`.
- **CFG-023** **Save as** MUST prompt for a name and create a new configuration with a new `id`, then select it.
- **CFG-024** Switching configuration or controller with unsaved changes MUST prompt: Save, Discard or Cancel.
- **CFG-025** **Rename** MUST change `name`, rename the file per CFG-004, and keep the same `id`.

## Names

- **CFG-026** Names MUST be 1 to 50 characters after trimming leading and trailing whitespace.
- **CFG-027** Names MUST be unique per controller model, compared case-insensitively after trimming. `Default` is reserved.
- **CFG-028** The name field MUST validate as the user types: show an inline message ("A configuration named 'Climbing' already exists for this controller") and disable the confirm button until the name is valid. Duplicates MUST be blocked, never silently overwritten.

## Delete

- **CFG-029** **Delete** MUST ask for confirmation, then delete the file and select Default.

## Import and sharing

- **CFG-030** **Import** MUST open a file picker for `.json` files, validate the file (CFG-005, CFG-007), copy it into the config folder and select it.
- **CFG-031** After a successful import the app MUST show a non-blocking notice: "Saved a copy to your configurations."
- **CFG-032** If the imported name collides (CFG-027), the app MUST ask for a new name, pre-filled with a suggestion such as `Climbing (2)`.
- **CFG-033** If the imported file's controller model does not match the selected controller, the app MUST ask whether to use it for the selected controller. If yes, it rewrites `controller` to the selected model and assigns a new `id`.
- **CFG-034** An **Open folder** button MUST open the config folder in File Explorer, creating it first if needed. Sharing is done by copying files from there.

## Settings

- **CFG-035** Settings MUST offer **Delete all saved configurations**, behind a confirmation dialog that states how many files will be deleted and that this cannot be undone.
- **CFG-036** After deleting all, every controller MUST fall back to Default and last-used entries MUST be cleared.

## UI layout (Controller Setup page)

A configuration bar sits above the mapping grid:

```
[ Climbing •          v ]  [Save] [Save as...] [Delete]  [...]
                                                          Import...
                                                          Rename...
                                                          Open folder
```

- **CFG-037** Buttons that do not apply MUST be disabled, not hidden (Save and Delete on Default; Save when nothing changed).
- **CFG-038** All dialogs MUST be usable with a game controller and keyboard as well as a mouse.

## Implementation status (v0.4.0)

Every requirement is implemented except one gap in CFG-038:

- The app's own dialogs (confirm, unsaved changes, name, notices) work with a mouse, the keyboard (Tab, Enter, Escape) and the selected game controller (D-pad moves focus, A presses, B cancels).
- **Gap:** the **Import...** file picker is the standard Windows Open dialog. It works with a mouse and keyboard but not with a game controller.
- **Gap:** typing a name with a game controller needs the Windows touch keyboard; the name dialog itself does not offer one.

The status stays Accepted until these are resolved or the requirement is relaxed.

## Out of scope for v0.4.0

- Syncing configurations between machines (users copy files).
- Per-configuration deadzones or curves (current deadzones stay global).
- Sharing configurations online.
- Moving `logs\` into the app data folder (a separate change).

## Acceptance tests (summary)

1. First run, no saved configs: Default selected; Save and Delete disabled.
2. Save as "Climbing": file appears in the config folder; list shows Default, Climbing; Climbing selected.
3. Restart the app: Climbing is selected again (last-used).
4. Connect a second controller of the same model: Climbing is listed.
5. Save as "climbing " (case and space differ): blocked with the inline message.
6. Import a file from another machine: notice shown; copy exists in the config folder; it is selected.
7. Edit a mapping, then switch configs: Save/Discard/Cancel prompt appears; Cancel keeps the edit.
8. Delete all in Settings: confirmation shows the count; afterwards only Default remains.
9. Drop a broken JSON file in the folder: it is skipped and logged; the app keeps working.
