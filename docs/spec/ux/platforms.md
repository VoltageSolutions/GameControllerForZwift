---
id: SPEC-UX-PLATFORMS
title: Platforms
last-updated: 2026-11-22
---

# Platforms

!!! tldr "TL;DR"
    Screens, content, tokens, and voice are the same on every platform. Only the shell adapts: pairing, background input, and a few platform states. Windows is the main platform.

In the rules, "must", "should", and "may" have the meaning given in [how to read a spec](../index.md).

## What stays the same and what adapts

| Always the same | Adapts to the platform |
|---|---|
| Screens and their content | How you pair a Bluetooth controller |
| Color tokens, type, spacing | Background input |
| Voice and copy | Platform states, extras, and donation rules |
| The three tabs and the More menu | Nothing else |

| Rule | ID |
|---|---|
| Screens, content, tokens, and voice must be the same on every platform. | UX-PL-001 |
| Only the shell may adapt to a platform: pairing, background input, platform states, extras, and donations. | UX-PL-002 |
| Order of buttons in dialogs follows each platform (see UX-V-032). | UX-PL-003 |

## The platform shell

| | Android | Windows | iOS | Linux |
|---|---|---|---|---|
| **Role** | Bridge on phone or tablet. Can sit beside Zwift on a tablet. | Main platform, beside Zwift on the same PC. | Second device only (input goes to the app on screen). | Bridge or maker setup. Zwift is not native. |
| **Pairing** | Companion-device dialog. Fallback is Bluetooth settings. | Hand off to Windows Bluetooth settings. | Instructions plus "Open Settings" (apps cannot pair). | Instructions. No universal settings link. |
| **Background input** | Needs testing. | SDL background events on. | Not possible. Keep the screen open. | Works. May need udev rules or Flatpak device access. |
| **Platform states to design** | None yet. | Double input if Zwift reads the gamepad too. | "Keep this screen open". | "Found your controller, but can't read it". "Steam is using your controller". |
| **Extras** | None yet. | Start with Windows. Keyboard shortcuts (Enter, Ctrl+Shift+V). | System button glyphs (for example PlayStation R1 and ✕). | Logs and a supported-controller list for tinkerers. |
| **Donations** | Store rules apply. | A direct link is fine. | Check the current App Store rules on tips. | A direct link is fine. |

| Rule | ID |
|---|---|
| Pairing on each platform must follow the Pairing row. Where an app cannot pair a controller itself, it must give clear instructions and open the system settings if it can. | UX-PL-004 |
| On iOS the app must tell you to keep the screen open, because input goes to the app on screen. | UX-PL-006 |
| On Linux the app should show "Found your controller, but can't read it" and "Steam is using your controller" when those states occur. | UX-PL-007 |
| On Windows the app should handle double input when Zwift also reads the gamepad. | UX-PL-008 |
| Donation links must follow the Donations row. | UX-PL-009 |

??? note "Edge cases"
    - Windows is the only platform that ships today. The other columns describe the target shell, so the same screens can be built later.
    - Android background input has not been tested.
    - The "Pair a new Bluetooth controller" screen shows the same pairing-mode steps everywhere. Only the last step (the hand-off) changes.
