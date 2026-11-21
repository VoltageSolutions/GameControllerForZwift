---
id: SPEC-UX-LOC
title: Localization
last-updated: 2026-11-18
---

# Localization

!!! tldr "TL;DR"
    Every word the app shows or reads out comes from a string resource, never from code or markup. English (US) is the source language. Strings are whole sentences with placeholders, IDs never translate, and layouts grow to fit longer words.

In the rules, "must", "should", and "may" have the meaning given in [how to read a spec](../index.md).

## Strings

A string is one label, sentence, or name, stored under a key with a note for translators. Screens look strings up by key. Translating the app means translating the string files, not the screens.

*Example: the Connections tab reads "Connections" in English. Its screen reader name with the dot is the string `NavConnectionsAlert`, "{0}, Zwift not connected", where `{0}` is the tab label. A translator translates the whole sentence and can move `{0}`.*

| Rule | ID |
|---|---|
| Every piece of text the app shows or reads out must come from a string resource. That includes labels, messages, screen reader names, tooltips, and window titles. | UX-L-001 |
| English (US) must be the source language. Every string must have a comment that tells a translator where the string appears. | UX-L-002 |
| A string must be a whole label or sentence. The app must not build a sentence from fragments. It fills placeholders such as `{0}` inside one string. | UX-L-003 |
| A message with a count must have one string for one item and one for other counts. | UX-L-004 |
| The app must show numbers, dates, and times in the formats of the person's region. | UX-L-005 |
| IDs must never be translated (UX-A-015). Names read out to screen readers are translated. | UX-L-006 |
| Layouts must leave room for longer text. Labels grow and wrap as the [visual system](visual-system.md#buttons) says, and the app must never clip text. | UX-L-007 |
| Brand and product names must stay as they are in every language: VOLTAGE, Voltage Gamepad, Zwift, Zwift Ride, Ko-fi, and GitHub. | UX-L-008 |
| Screens only developers see, such as the style gallery in Debug builds, may use English only. | UX-L-009 |

## On Windows

The strings live in `windows/src/Presentation/GameControllerForZwift/Resources/Strings.resx`. XAML reads a string with `{loc:Loc Key}` and code reads one with `Localizer.Get("Key")` or `Localizer.Format("Key", ...)`. A XAML file that declares the `loc` namespace is a localized file.

| Rule | ID |
|---|---|
| A unit test must fail when code or XAML uses a key that is missing, when a key is never used, or when a key has no translator comment. | UX-L-010 |
| A unit test must fail when a localized XAML file sets text, a title, a tooltip, or a screen reader name to a literal value. | UX-L-011 |

??? note "Edge cases"
    - The app follows the Windows display language. A language with no translation shows English.
    - The style gallery is not a localized file, so its English text is allowed (UX-L-009).
