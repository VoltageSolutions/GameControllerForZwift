# On-screen gamepad visualization: research and plan

Scope: dashboard card plus click-to-map visual mapper, with controller art chosen by connected controller type. Research only; no code changed.

## 1. Art sources and licensing

| Source | Format | Content | License | Verdict |
|---|---|---|---|---|
| Kenney Input Prompts (https://kenney.nl/assets/input-prompts) | Page does not state formats; pack is 64px tiles, SVG variants believed present (verify in zip) | Per-button glyphs only: Xbox 360/One/Series, PS1-5, Steam Deck/Controller, Switch/Switch 2, Wii, GameCube, generic | CC0 (page) | Good for small glyphs (mapping labels, "Press A"), not a full controller |
| Xelu Free Controller and Key Prompts (https://thoseawesomeguys.com/prompts/) | Zip; formats not on page (Readme in zip) | Glyphs, not documented as full-controller art | CC0, commercial OK (page) | Same use as Kenney; check zip first |
| OpenGameArt "8th gen console vector gamepad collection" (https://opengameart.org/content/8th-gen-console-vector-gamepad-collection) | SVG + 2048px PNG | One generic "current gen" full gamepad | CC0, attribution not required (author asks politely) | Best ready-made full-controller SVG. Original upload carried Xbox/PS/Switch logos (commenters flagged trademark); author says a logo-free version exists. Verify the SVG has no logos. Group/ID quality unknown, needs re-layering |
| OpenGameArt "generic gamepad template" (https://opengameart.org/content/generic-gamepad-template) | Not verified | Generic pad | Not verified, check page | Candidate only |
| SDL test images (testcontroller / gamepadutils.c, https://skia.googlesource.com/external/github.com/libsdl-org/SDL/+/eba06309077777c6ce0efbe5fb2ebca06fbef096/test/gamepadutils.c) | PNG/BMP raster (gamepadutils.c loads PNG; older tree has gamepad_face_abxy/bayx .bmp) | One generic pad (front/back), face-button variants ABXY, AXBY, BAYX, Sony shapes | SDL repo is zlib; header says "Sam Lantinga" with zlib-like terms (could not read repo LICENSE, GitHub blocked). Confirm asset-specific terms | Raster, SDL3-era. Useful as layout reference for part naming, not as art |
| SDL ControllerImage (https://wiki.icculus.org/ControllerImage/FrontPage/raw) | SVG source compiled to .bin, SDL3 only | Xbox, PlayStation, Switch and others; Xbox 360 fallback | Code zlib; art license not stated on page | Do not ship. Interesting as proof the "SVG per family" model works |
| Wikimedia "File:360 controller.svg" (e.g. https://wikipedia.com/wiki/File:360_controller.svg) | SVG | Xbox 360 pad | Could not fetch the page; check license per file | Per-file check; branded shape (trade dress) |
| game-icons.net | SVG | Single-colour icons | CC BY 3.0 (from memory, verify); attribution required | Icon only, not useful for mapping |
| Steam Input glyphs, Gamepad Viewer skins | - | - | Proprietary or unclear | Avoid |

Trademark and trade dress: logos (Xbox "X" sphere, PlayStation symbols as logos, Nintendo marks) are trademarked; do not use them. Button face symbols (triangle/circle/cross/square, ABXY letters) are widely used by open-source projects and Kenney/Xelu publish them CC0. Common practice: draw an original "Xbox-style / PlayStation-style / Switch-style" silhouette that follows layout (stick placement, face-button shape) but not exact industrial design, omit logos and the guide-button emblem, and label families as "Xbox-style layout" or "compatible with". Add a README line: "Not affiliated with Microsoft, Sony or Nintendo". Option (d) below (original art) is the lowest-risk path. This is practical guidance, not legal advice.

## 2. SDL2 in this binding and current app

Facts from `src/Infrastructure/GameControllerForZwift.Gamepad/SDL2/SDL2GameController.cs` and strings in `Sayers.SDL2.Core.dll` (SDL2-CS based, per ACKNOWLEDGEMENTS.md):
- Exposed: `SDL_GameControllerGetType`, `SDL_GameControllerTypeForIndex`, `SDL_GameControllerHasButton`, `SDL_GameControllerHasAxis`, `SDL_GameControllerGetNumTouchpads`, `...NumTouchpadFingers`, `...GetSensorData`, `GetVendor/Product/ProductVersion`, hints `SDL_HINT_GAMECONTROLLERTYPE` and `SDL_HINT_GAMECONTROLLER_USE_BUTTON_LABELS`.
- Enum `SDL_GameControllerType` in the binding: UNKNOWN, XBOX360, XBOXONE, PS3, PS4, PS5, NINTENDO_SWITCH_PRO, VIRTUAL, AMAZON_LUNA, GOOGLE_STADIA. NOT present: NVIDIA_SHIELD, NINTENDO_SWITCH_JOYCON_LEFT/RIGHT/PAIR. Native SDL 2.30.10 can return them (values 10, 11, 12, 13), so cast the raw int: `(int)SDL_GameControllerGetType(h)`.
- Button enum includes GUIDE, MISC1 (Share/Capture/Mic), PADDLE1-4, TOUCHPAD. Not present: `SDL_GameControllerGetButtonLabel` (that is SDL3), `GetBindForButton`, `GetSerial`, `GetPlayerIndex`, `GetFirmwareVersion` (greps returned 0). No exported label function, so decide face labels ourselves.
- Nintendo A/B swap: app already sets `SDL_HINT_GAMECONTROLLER_USE_BUTTON_LABELS=1` and `SDL_HINT_JOYSTICK_HIDAPI_JOY_CONS=1` (SDL2InputService.cs lines 110-111). With the labels hint on, SDL reports Nintendo buttons by printed label (SDL "A" is the Switch east button), with it off by Xbox position (A is south). The skin must map SDL A/B/X/Y to physical positions accordingly (hint on: Switch art uses SDL names directly; hint off: swap A/B and X/Y). Decide once and test with a real Pro controller.
- Current identification: `SDL2GameController` stores `Name` (SDL_GameControllerName) and a private `_vendorProduct` string "VVVV-PPPP". Type is not read. `IController` exposes Name only.
- `ControllerData` has A, B, X, Y, Menu, View, DPad x4, LeftBumper/RightBumper, both stick clicks, stick X/Y (raw int, +-32767, as double), triggers (raw 0-32767), BatteryPercent, Timestamp. Missing for visualization: Guide, Misc1 (Share), Touchpad, Paddles 1-4, controller type, `HasButton` capability flags, touchpad finger position. Add these as optional fields (default false) plus a `ControllerInfo { Name, VendorProduct, ControllerFamily, Capabilities }` on `IController`.

## 3. WPF rendering approaches

(a) SVG to XAML (Inkscape XAML export, SvgToXaml https://github.com/BerndK/SvgToXaml, or Visual Studio/svg2xaml scripts). Output is DrawingImage or Canvas of Paths; rename ids to Part_* by editing. Pros: native, GPU-friendly, theming through DynamicResource brushes, no runtime dependency, hit-testing works on Paths (need Fill not null, use Transparent). Cons: manual cleanup per skin; Inkscape export is verbose.
(b) SharpVectors (https://github.com/ElinamLLC/SharpVectors, BSD-3): runtime SVG load, lookup by id. Pros: drop SVGs in without conversion. Cons: dependency, id lookup then manual brush swapping, less theming control, possible rendering quirks. Reasonable only for prototyping.
(c) Layered PNG overlays: simple but blurry at DPI, no theming, many assets, weak hit-testing. Not recommended.
(d) Procedural/hand-written XAML (original art). Pros: no licence risk, perfect names, tiny. Cons: needs design time; a clean "generic pad" in about 25 Paths is feasible. Recommended for v1/v2, optionally starting from the CC0 OGA SVG as tracing reference.

Performance: 40 Hz updates is trivial. Use static Paths; update only changed properties (diff against previous state in the view model). Avoid per-frame brush allocation: pre-freeze brushes, toggle via a Visibility/Opacity or a shared "Pressed" state. Sticks: TranslateTransform on a dot (frozen geometry, radius*x). Triggers: a Rectangle clipped to the trigger shape with ScaleTransform (RenderTransformOrigin bottom), or a LinearGradient offset. Everything is retained-mode; no custom OnRender needed.
Theming/DPI: all parts are vectors in a Viewbox; colours from DynamicResource (existing keys like CardStrokeColorDefaultBrush; add GamepadBodyBrush, GamepadPressedBrush = accent). Hit-testing: Part_* Paths with `IsHitTestVisible=True`, `Cursor=Hand`, `Tag="A"` (button id). Click raises a routed event `PartClicked(ControlId)`; hover sets IsMouseOver trigger. Keep sticks' hit area a larger transparent ellipse.

Skin format: one UserControl (or ResourceDictionary + ContentControl template) per family, each containing a Viewbox of Paths whose x:Name starts with Part_. A base class `GamepadSkinBase : UserControl, IGamepadSkin` finds parts by name once in Loaded and caches them. Fall back to the generic skin when a part is missing (skin declares `Capabilities`). Selection: `GamepadSkinFactory.For(ControllerFamily)`.

## 4. Plan

v1 (effort M overall):
- Define `ControlId` enum and `GamepadState` view model (items 4a). Extend ControllerData with Guide/Misc1/Touchpad/Paddles (S) and map in SDL2GameController (S).
- Original generic gamepad XAML skin (M, design heavy; S if adopting OGA CC0 SVG after stripping logos), `GamepadView` control with highlight triggers, stick dots, trigger fills (M).
- Hover and click raising `ControlId` for mapper (S), hook into ControllerSetupPage mapping flow (M, depends on existing mapping UI).
- Reuse ThumbstickGraph: keep for the numeric/diagnostic stick plot (axes, deadzone rings); do not reuse inside the pad. Its logic (clamp, scale, IsOutsideOrigin deadzone colouring) can be copied for the dot colour. Note its `originX = MaxX/2` looks suspect for a -1..1 range (origin would be 0.5); verify before reuse.
v2 (L): Read `GetType` into `ControllerFamily` (S); Xbox-style, PlayStation-style (cross/circle/triangle/square, touchpad, no logo), Switch Pro skins (M each); face-label/position mapping per family (S); automatic selection and manual override in settings (S); tests for family mapping (S).
v3 (L): Joy-Con single and pair via raw ints 11-13 (M, single Joy-Con has rotated layout, uses stick as 4-way, SL/SR as extra buttons), Elite paddles via PADDLE1-4 with `HasButton` gating (S), touchpad finger overlay for PS (S), AMAZON_LUNA/STADIA/Shield mapped to generic or closest family (S).

Sketch:

```csharp
public enum ControllerFamily { Generic, Xbox, PlayStation, SwitchPro, JoyConLeft, JoyConRight, JoyConPair }
public enum ControlId { A,B,X,Y,Start,Back,Guide,Misc1,DPadUp,DPadDown,DPadLeft,DPadRight,
  LeftBumper,RightBumper,LeftStickClick,RightStickClick,LeftTrigger,RightTrigger,
  LeftStick,RightStick,Touchpad,Paddle1,Paddle2,Paddle3,Paddle4 }

public interface IGamepadSkin {
    ControllerFamily Family { get; }
    IReadOnlySet<ControlId> Supported { get; }
    void SetPressed(ControlId id, bool pressed);
    void SetStick(ControlId stick, double x, double y);      // -1..1
    void SetTrigger(ControlId trigger, double value);         // 0..1
    event Action<ControlId> PartClicked;
}
public sealed class GamepadViewModel : ObservableObject {
    public ControllerFamily Family { get; set; }
    public GamepadState State { get; private set; }   // immutable snapshot built from ControllerData
    public ControlId? Highlighted { get; set; }        // mapper selection
}
```
XAML naming: `Part_A`, `Part_DPadUp`, `Part_LeftBumper`, `Part_LT_Fill`, `Part_LT_Hit`, `Part_LeftStick_Dot`, `Part_LeftStick_Hit`, `Part_Touchpad`; each Part_* has `Tag="{x:Static ControlId...}"`. Pressed state via a shared Style with `Fill` from `GamepadPressedBrush`.

## Caveats
- Kenney/Xelu zip contents and OGA SVG structure were not downloaded; verify before adoption.
- SDL repo LICENSE and Wikimedia file license pages could not be fetched.
- Sayers binding enum list taken from DLL strings; confirm by decompiling if exact values matter.
