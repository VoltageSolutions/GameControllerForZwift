---
id: SPEC-SCR-HELP
title: Help and feedback screen
last-updated: 2026-11-22
---

# Help and feedback screen

!!! tldr "TL;DR"
    Help and feedback opens from the More menu as a pushed screen with a back button. It lists common problems with plain fixes, and it is where you open the log folder and reach the maintainer on GitHub.

In the rules, "must", "should", and "may" have the meaning given in [how to read a spec](../index.md).

## Help and feedback

**What it is for:** solve common problems and reach the maintainer.

**Content:** common problems, **Open log folder**, **Report a problem** (opens GitHub, with what to attach), **Send feedback** (GitHub), the list of supported controllers, and a link to "Zwift can't find Voltage Gamepad".

| Rule | ID |
|---|---|
| Help must list the common problems, each with a short plain fix. | SCR-HELP-001 |
| Help must link to "Zwift can't find Voltage Gamepad" on [Connections](connections.md). | SCR-HELP-002 |
| Help must offer **Open log folder**. | SCR-HELP-003 |
| **Report a problem** must open GitHub and say what to attach: the app version, the controller, and the log file. | SCR-HELP-004 |
| The screen should tell you to check the log before sharing it publicly, because it lists local network addresses. | SCR-HELP-005 |
| **Send feedback** must open GitHub. | SCR-HELP-006 |
| Help should offer the list of supported controllers once that list exists. | SCR-HELP-007 |

## Controller input on this screen

On Help and feedback, a controller press goes to Zwift as mapped. See [Controller input](../ux/controller-input.md).

## Accessibility IDs

| ID | Where |
|---|---|
| `MenuHelpItem` | The More menu item that opens this screen |
