---
id: SPEC-UX-LOC
title: Localization
last-updated: 2026-11-22
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

## Quoted strings in the specs

A screen spec says in words what the screen shows, and it often quotes the text. A quote can drift from the real string. To catch that, each screen spec lists the strings it quotes in one **Quoted strings** table at the end of the page. A unit test reads the table and compares each row with `Strings.resx`.

The table is a section headed `## Quoted strings` with three columns: **Key**, **English text**, and **Rule**. The key is the resx key in backticks. The English text is the exact value of the string, without quotes. The rule is the ID of the rule that needs the string. A placeholder keeps its `{0}` form. A message with a count has one row for one item and one row for other counts (UX-L-004). A key that a spec names but does not yet have a string must still be listed, so the test fails until the string exists.

*Example:*

```markdown
## Quoted strings

| Key | English text | Rule |
|---|---|---|
| `RideTitle` | Ready when you are | SCR-RIDE-001 |
| `SettingsDeleteAllConfirm` | Delete all {0} saved mappings? This can't be undone. Default stays. | SCR-SET-006 |
```

| Rule | ID |
|---|---|
| A screen spec that quotes a string the app shows must list it in a `## Quoted strings` table with its resx key, its exact English text, and the rule that needs it. | UX-L-012 |
| A unit test must fail when a key in a spec Quoted strings table is missing from `Strings.resx`, or when its English value differs from the spec text. The failure must name the spec file, the rule ID, the key, the expected text, and the actual text. | UX-L-013 |

??? note "Edge cases"
    - The table checks the strings listed in it. A quote in running text that is not in the table is not checked, so list every string a rule depends on.
    - Spec text uses straight apostrophes and quotation marks, as `Strings.resx` does.
    - A Quoted strings table lists only the strings of that screen. A string that two screens share may be listed in both specs, as long as the text is the same.

## On Windows

The strings live in `windows/src/Presentation/GameControllerForZwift/Resources/Strings.resx`. XAML reads a string with `{loc:Loc Key}` and code reads one with `Localizer.Get("Key")` or `Localizer.Format("Key", ...)`. A XAML file that declares the `loc` namespace is a localized file.

| Rule | ID |
|---|---|
| A unit test must fail when code or XAML uses a key that is missing, when a key is never used, or when a key has no translator comment. | UX-L-010 |
| A unit test must fail when a localized XAML file sets text, a title, a tooltip, or a screen reader name to a literal value. | UX-L-011 |

??? note "Edge cases"
    - The app follows the Windows display language. A language with no translation shows English.
    - The style gallery is not a localized file, so its English text is allowed (UX-L-009).
