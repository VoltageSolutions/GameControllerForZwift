# Gamepad visualization

!!! tldr "TL;DR"
    The goal is an on-screen controller that lights up as you press buttons and lets you click a part to map it, with art that matches the connected controller family. The safest art is original vector drawings in XAML that follow each family's layout but use no logos. SDL2 can already tell the controller type, so the right skin can be picked automatically. The plan grows in three steps: one generic skin, then Xbox, PlayStation and Switch Pro skins, then Joy-Cons, paddles and touchpads.

## 1. Art sources and licensing

| Source | Format | Content | License | Verdict |
|---|---|---|---|---|
| [Kenney Input Prompts](https://kenney.nl/assets/input-prompts) | 64 px tiles; SVG variants likely (check the zip) | Button glyphs only: Xbox, PlayStation, Steam Deck, Switch, Wii, GameCube, generic | CC0 | Good for small glyphs ("Press A"), not a full controller. |
| [Xelu Free Controller and Key Prompts](https://thoseawesomeguys.com/prompts/) | Zip (formats listed in its readme) | Glyphs | CC0, commercial use allowed | Same use as Kenney. Check the zip first. |
| [OpenGameArt "8th gen console vector gamepad collection"](https://opengameart.org/content/8th-gen-console-vector-gamepad-collection) | SVG and 2048 px PNG | One generic modern gamepad | CC0, attribution not required | The best ready-made full controller SVG. The original upload had console logos; the author mentions a logo-free version. Check the SVG for logos and expect to re-layer it. |
| [OpenGameArt "generic gamepad template"](https://opengameart.org/content/generic-gamepad-template) | Not checked | Generic pad | Check the page | Candidate only. |
| SDL test images (`test/gamepadutils.c` in the SDL repository) | PNG and BMP raster | One generic pad, front and back, with face button variants | zlib style (confirm the asset terms) | Raster and from the SDL3 era. Useful as a reference for part names, not as art. |
| [SDL ControllerImage](https://wiki.icculus.org/ControllerImage/FrontPage/raw) | SVG compiled to a binary format, SDL3 only | Xbox, PlayStation, Switch and more | Code is zlib; art license not stated | Do not ship. Interesting proof that "one SVG per family" works. |
| Wikimedia controller SVGs | SVG | Individual pads, for example the Xbox 360 controller | Varies per file | Check each file. Branded shapes raise trade dress concerns. |
| game-icons.net | SVG | Single-colour icons | CC BY 3.0 (verify), attribution required | Icons only, not useful for mapping. |
| Steam Input glyphs, Gamepad Viewer skins | | | Proprietary or unclear | Avoid. |

### Trademarks and trade dress

Logos such as the Xbox sphere, PlayStation logos and Nintendo marks are trademarks. Do not use them. Button face symbols (triangle, circle, cross, square and the letters A, B, X, Y) are widely used by open-source projects, and Kenney and Xelu publish them as CC0.

The common approach is to draw an original "Xbox-style", "PlayStation-style" or "Switch-style" outline that follows the layout (where the sticks are, the shape of the face buttons) without copying the exact industrial design, and to leave out logos and the guide button emblem. Label families as "Xbox-style layout". Add a line such as "Not affiliated with Microsoft, Sony or Nintendo". Original art (option d below) carries the lowest risk. This is practical guidance, not legal advice.

## 2. What SDL2 offers

From `src/Infrastructure/GameControllerForZwift.Gamepad/SDL2/SDL2GameController.cs` and the SDL2 binding the app uses:

- **Available:** `SDL_GameControllerGetType`, `SDL_GameControllerTypeForIndex`, `SDL_GameControllerHasButton`, `SDL_GameControllerHasAxis`, touchpad and sensor functions, vendor, product and product version, and the hints `SDL_HINT_GAMECONTROLLERTYPE` and `SDL_HINT_GAMECONTROLLER_USE_BUTTON_LABELS`.
- **Controller types in the binding:** Unknown, Xbox 360, Xbox One, PS3, PS4, PS5, Switch Pro, Virtual, Amazon Luna, Google Stadia. Not in the binding: NVIDIA Shield and the Joy-Con types (left, right, pair). The native SDL 2.30.10 library can return them as the raw values 10 to 13, so cast the raw integer: `(int)SDL_GameControllerGetType(handle)`.
- **Buttons:** the enum includes Guide, Misc1 (Share, Capture or Mic), Paddle 1 to 4 and Touchpad. There is no button label function (that is SDL3).
- **Nintendo A/B swap:** the app sets `SDL_HINT_GAMECONTROLLER_USE_BUTTON_LABELS=1` and `SDL_HINT_JOYSTICK_HIDAPI_JOY_CONS=1` in `SDL2InputService`. With the labels hint on, SDL reports Nintendo buttons by their printed label (SDL "A" is the Switch's east button). With it off, buttons follow Xbox positions (A is south). The skin must place SDL A, B, X and Y accordingly. Decide once and test with a real Switch Pro controller.
- **Today:** `SDL2GameController` stores the controller name and a private vendor and product string, but not the type. `IController` exposes only `Name`.
- **ControllerData** has A, B, X, Y, Menu, View, the D-pad, both bumpers, both stick clicks, stick X and Y, both triggers and the battery percentage. For visualization it lacks Guide, Misc1, Touchpad, Paddles 1 to 4, the controller type and capability flags. These can be added as optional fields, along with a `ControllerInfo { Name, VendorProduct, ControllerFamily, Capabilities }` on `IController`.

## 3. Rendering in WPF

| Option | How | Pros | Cons |
|---|---|---|---|
| a. SVG converted to XAML | Inkscape XAML export or [SvgToXaml](https://github.com/BerndK/SvgToXaml), then rename parts to `Part_*` | Native, fast, themeable with `DynamicResource` brushes, no runtime dependency, hit testing works on paths (use a transparent fill) | Manual clean-up per skin; exports are verbose |
| b. SharpVectors | Load SVG at run time with [SharpVectors](https://github.com/ElinamLLC/SharpVectors) (BSD-3) and look up parts by id | Drop SVGs in without conversion | Extra dependency, manual brush swapping, less theming control. Fine for prototypes. |
| c. Layered PNGs | Overlay images per pressed state | Simple | Blurry at high DPI, no theming, many files, weak hit testing. Not recommended. |
| d. Hand-written XAML (original art) | Draw the pad as about 25 paths | No licence risk, perfect part names, tiny | Needs design time. Recommended, optionally traced from the CC0 OpenGameArt SVG. |

**Performance.** Updates at 40 Hz are easy for WPF. Keep the paths static and only change what differs from the last state. Avoid creating brushes per frame: freeze them and toggle visibility, opacity or a shared "pressed" state. Move stick dots with a `TranslateTransform`. Fill triggers with a clipped rectangle and a `ScaleTransform`, or a gradient offset.

**Theming and DPI.** All parts are vectors inside a `Viewbox`. Colours come from `DynamicResource` keys, reusing Fluent keys where possible and adding `GamepadBodyBrush` and `GamepadPressedBrush` (the accent colour).

**Hit testing.** Each `Part_*` path has `IsHitTestVisible="True"`, a hand cursor and a `Tag` with its control id. A click raises a routed `PartClicked(ControlId)` event, and hover uses an `IsMouseOver` trigger. Give the sticks a larger transparent hit area.

**Skin format.** One `UserControl` per family, each a `Viewbox` of paths named `Part_*`. A base class `GamepadSkinBase : UserControl, IGamepadSkin` finds the parts once on load and caches them. If a part is missing, fall back to the generic skin. A `GamepadSkinFactory.For(ControllerFamily)` picks the skin.

## 4. Plan

### Version 1 (medium effort)

- Define a `ControlId` enum and a `GamepadState` view model. Extend `ControllerData` with Guide, Misc1, Touchpad and Paddles and fill them in `SDL2GameController` (small).
- An original generic gamepad skin in XAML (medium, mostly design; small if adapting the CC0 OpenGameArt SVG with logos removed) and a `GamepadView` control with highlights, stick dots and trigger fills (medium).
- Hover and click that raise a `ControlId` for the mapper (small), wired into the Controller Setup mapping flow (medium).
- Keep `ThumbstickGraph` for the diagnostic stick plot with dead-zone rings, but do not reuse it inside the pad. Its clamping and dead-zone colouring can be copied for the stick dot. Its origin calculation looks suspect for a -1 to 1 range; check it before reuse.

### Version 2 (large)

- Read the controller type into a `ControllerFamily` (small).
- Xbox-style, PlayStation-style (cross, circle, triangle, square and a touchpad, no logo) and Switch Pro skins (medium each).
- Face label and position mapping per family (small), automatic skin selection with a manual override in Settings (small), and tests for the family mapping (small).

### Version 3 (large)

- Single Joy-Cons and pairs via the raw type values 11 to 13 (medium: a single Joy-Con is held sideways, uses its stick as a 4-way pad and has SL and SR as extra buttons).
- Elite-style paddles via Paddle 1 to 4, shown only when `HasButton` says they exist (small).
- A touchpad finger overlay for PlayStation controllers (small).
- Map Amazon Luna, Google Stadia and NVIDIA Shield controllers to the generic or closest family (small).

### Sketch

```csharp
public enum ControllerFamily { Generic, Xbox, PlayStation, SwitchPro, JoyConLeft, JoyConRight, JoyConPair }

public enum ControlId { A, B, X, Y, Start, Back, Guide, Misc1, DPadUp, DPadDown, DPadLeft, DPadRight,
    LeftBumper, RightBumper, LeftStickClick, RightStickClick, LeftTrigger, RightTrigger,
    LeftStick, RightStick, Touchpad, Paddle1, Paddle2, Paddle3, Paddle4 }

public interface IGamepadSkin
{
    ControllerFamily Family { get; }
    IReadOnlySet<ControlId> Supported { get; }
    void SetPressed(ControlId id, bool pressed);
    void SetStick(ControlId stick, double x, double y);   // -1..1
    void SetTrigger(ControlId trigger, double value);      // 0..1
    event Action<ControlId> PartClicked;
}

public sealed class GamepadViewModel : ObservableObject
{
    public ControllerFamily Family { get; set; }
    public GamepadState State { get; private set; }   // immutable snapshot built from ControllerData
    public ControlId? Highlighted { get; set; }        // the part selected in the mapper
}
```

XAML part names: `Part_A`, `Part_DPadUp`, `Part_LeftBumper`, `Part_LT_Fill`, `Part_LT_Hit`, `Part_LeftStick_Dot`, `Part_LeftStick_Hit`, `Part_Touchpad`. Each `Part_*` has a `Tag` set to its `ControlId`. The pressed state comes from a shared style whose fill uses `GamepadPressedBrush`.

## Open checks

- Download the Kenney and Xelu zips and the OpenGameArt SVG and check their contents and structure before adopting them.
- Confirm the licence terms of the SDL test images and of any Wikimedia file.
- The binding's enum list was read from the library; confirm exact values if they matter.
