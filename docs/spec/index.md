# Spec

!!! tldr "TL;DR"
    The specs describe, in plain language, how each feature must behave. Every rule has an ID that tests and code point to. Each spec has a status: Draft, Accepted or Implemented.

## What the specs are

A spec is a readable description of how one feature behaves. I write it before or alongside the code, and I change it whenever the behavior changes. It is the source of truth. If the app and a spec disagree, one of them has a bug.

Each rule has a requirement ID such as `CFG-015`. Tests and code refer to these IDs, so you can find the check for any rule, and the rule behind any check.

Every spec has a status in its front matter:

| Status | Meaning |
|---|---|
| Draft | I am still working out what the feature should do. |
| Accepted | The behavior is agreed. The work may be in progress or have known gaps. |
| Implemented | The app does everything the spec says. |

## How to read a spec

Start with the TL;DR. Then read the sections you care about. Each section opens with a short description of what you see and what happens, often with an example. After that comes a table of rules with their IDs. Finer points sit in a collapsed "Edge cases" block that you can open when you need it. In the rules, "must" means required, "should" means expected unless there is a good reason, and "may" means optional.

## Specs

- [Saved controller configurations](saved-configurations.md) (`CFG`): save, reload, rename, delete, import and share your button mappings.
- [Controller detection](controller-detection.md) (`CTRL`): how the controller list follows controllers as they connect and disconnect, and which one is selected.
