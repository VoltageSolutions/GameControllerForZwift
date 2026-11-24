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

**Content:** common problems, **Open log folder**, **Report a problem** (opens GitHub, with what to attach), **Send feedback** (the Issues page of the project on GitHub), and a link to "Zwift can't find Voltage Gamepad".

| Rule | ID |
|---|---|
| Help must list the common problems, each with a short plain fix. | SCR-HELP-001 |
| Help must link to "Zwift can't find Voltage Gamepad" on [Connections](connections.md). | SCR-HELP-002 |
| Help must offer **Open log folder**. | SCR-HELP-003 |
| **Report a problem** must open GitHub and say what to attach: the app version, the controller, and the log file. | SCR-HELP-004 |
| The screen should tell you to check the log before sharing it publicly, because it lists local network addresses. | SCR-HELP-005 |
| **Send feedback** must open the Issues page of the project on GitHub (`https://github.com/VoltageSolutions/GameControllerForZwift/issues`). | SCR-HELP-006 |

## Controller input on this screen

On Help and feedback, a controller press goes to Zwift as mapped. See [Controller input](../ux/controller-input.md).

## Accessibility IDs

| ID | Where |
|---|---|
| `MenuHelpItem` | The More menu item that opens this screen |
| `HelpPageTitle` | The screen title |
| `HelpFindLink` | The row that opens the steps for "Zwift can't find Voltage Gamepad" on Connections |
| `HelpOpenLogsButton` | **Open log folder** |
| `HelpReportButton` | **Report a problem** |
| `HelpFeedbackButton` | **Send feedback** |

## Quoted strings

| Key | English text | Rule |
|---|---|---|
| `HelpTitle` | Help and feedback | SCR-HELP-001 |
| `HelpIntro` | Most problems have a quick fix. Start with the list, and reach me on GitHub if yours is not there. | SCR-HELP-001 |
| `HelpProblemsSection` | Common problems | SCR-HELP-001 |
| `HelpProblemFindTitle` | Zwift can't find Voltage Gamepad | SCR-HELP-002 |
| `HelpProblemFindFix` | Follow the three steps on Connections. | SCR-HELP-002 |
| `HelpProblemSeeTitle` | Zwift can't see your controller | SCR-HELP-001 |
| `HelpProblemSeeFix` | In Zwift, pick Voltage Gamepad under Controls again. Your controller stays connected to this app. | SCR-HELP-001 |
| `HelpProblemButtonTitle` | A button does nothing | SCR-HELP-001 |
| `HelpProblemButtonFix` | Open Buttons and look at the row for that button. A row that says No action does nothing in Zwift. | SCR-HELP-001 |
| `HelpProblemMissingTitle` | My controller isn't showing up | SCR-HELP-001 |
| `HelpProblemMissingFix` | Press a button to wake it. For Bluetooth, pair it in Windows first, or plug it in with a USB cable. | SCR-HELP-001 |
| `HelpContactSection` | Get in touch | SCR-HELP-003 |
| `HelpOpenLogs` | Open log folder | SCR-HELP-003 |
| `HelpOpenLogsDescription` | Check the log before you share it publicly. It lists local network addresses. | SCR-HELP-005 |
| `HelpReport` | Report a problem | SCR-HELP-004 |
| `HelpReportDescription` | Opens GitHub. Attach the app version, your controller, and the log file. | SCR-HELP-004 |
| `HelpFeedback` | Send feedback | SCR-HELP-006 |
| `HelpFeedbackDescription` | Opens the Issues page of the project on GitHub. | SCR-HELP-006 |
