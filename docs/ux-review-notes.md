# UX review notes: Game Controller for Zwift (WPF) vs BikeControl (Flutter)

Status: untracked working note for the UI redesign. Read-only research, no code changed.
Staged screenshots (research sandbox): /mnt/user-data/uploads/GameControllerForZwift/docs/assets/images/homescreen_{dark,light}mode.png and /mnt/user-data/uploads/bikecontrol/screenshots/en/{devices,customization,device,companion,trainer}-desktop-2560x1600.png

## 1. Current WPF app (src/Presentation/GameControllerForZwift)

Target state after the in-progress simplification. Being removed: keyboard output, the "Send to" card (Zwift emulator / Keyboard checkboxes), the camera-view and rider-action sub-dropdowns on InputMapper, and the long legacy function list. Remaining function list: Navigate Up/Down/Left/Right, Select (A), Back/Menu (B), Power-up (Y), Ride On (Z), Shift up/down, Steer left/right, Brake (plus "No action").

### Shell and navigation
- MainWindow.xaml: custom title bar (44px row; back arrow button, 20px logo, caption "Game Controller for Zwift", min/max/close drawn by hand with Segoe Fluent glyphs; WindowChrome). Default size 1600x800, min 400x450.
- Left pane: a TreeView (not NavigationView), fed from Navigation/PagesInfoData.json. Items: "Controller Setup" (glyph E7FC) and "About" (E946). "Settings" is a full-width button pinned bottom-left (glyph E713, fixed 250px wide).
- Content: a Frame hosting Pages, inside a Border with 8px top-left radius, 24/16 padding, LayerFill brush. Back-history navigation via NavigationService.
- No status bar, no header chips, no persistent connection indicator.

### Screens
1. Views/ControllerSetupPage.xaml (the only real screen; ~220 lines). ScrollViewer > title "Controller Setup" (28pt semibold) + description line > WrapPanel of cards:
   - Controller card: ComboBox of detected controllers + "Refresh List" button (37px high).
   - "Send to" card (REMOVED in new scope): checkboxes Zwift Controller Emulator / Keyboard.
   - Commented out (never shipped): Reset card (To Default / Clear), Profile card (combo + Load/Save).
   - Mapping cards, all in one wrapping flow, each a list of InputMapper rows (label 80px + function ComboBox 37px high; row highlights with AccentFillColorSelectedTextBackground while the physical input is pressed = the only "live input" display):
     - Buttons: A, B, X, Y, Menu, View.
     - D-Pad: Up, Left, Down, Right.
     - Left Stick: ThumbstickGraph (100x100 concentric circles + dot, live) + L3 click + Up/Left/Down/Right tilt rows.
     - Right Stick: same with R3.
     - Shoulder Buttons: L1, R1, L2, R2 rows + two GaugeControl bars for analog L2/R2 live value.
   - That is about 26 dropdowns on one page, plus 2 conditional sub-dropdowns per row today (removed in the new scope).
2. Views/SettingsPage.xaml: one setting only, "App theme" (Light / Dark / Use system) in an Expander-style card with glyph E790. A large About/Expander block is commented out and still contains leftover Microsoft WPF-Gallery text (clone URL, Microsoft Services Agreement).
3. Views/AboutPage.xaml: title + logo; two large buttons (GitHub icon geometry, Ko-fi SVG via DotNetProjects.SVGImage); FlowDocumentReader rendering ACKNOWLEDGEMENTS.md through Markdig.Wpf.
4. Views/ControllerMapping/ZwiftFunction.xaml: orphan UserControl (2 combos), looks unused/legacy.

### Controls (Controls/)
Card (title + subtitle + content, 6px radius, 5px margin), InputMapper (name + combo(s) + pressed highlight), ThumbstickGraph (code-drawn Canvas, hard-coded Brushes.Black strokes, which are invisible-ish in dark mode), GaugeControl (code-drawn bar). No controller image, no pictogram anywhere.

### Theming
- Native WPF Fluent theme (PresentationFramework.Fluent, .NET 9), not WPF-UI/ModernWpf. Colours come entirely from Fluent system tokens: CardBackgroundFillColorDefaultBrush, LayerFillColorDefaultBrush, CardStrokeColorDefaultBrush, AccentFillColorSelectedTextBackgroundBrush, AccentTextFillColorTertiaryBrush. Accent = the user's Windows accent colour (screenshot shows a light-blue dot; dark window about #1F2430-ish navy-grey gradient, cards about #2B2F3A).
- Dark/Light/System switch in Settings (default "Use system").
- Typography.xaml: Segoe UI Variable (Fluent default). Sizes Caption 12, Body 14, Subtitle 20, Title 28, TitleLarge 40; every style is SemiBold unless Body/Caption (so most labels are semibold: heavy feel).
- Icons: Segoe Fluent Icons glyphs only (SymbolThemeFontFamily) for nav + title bar; a GitHub path geometry; Ko-fi svg.
- Resources/Colors.xaml is effectively empty (one style stub); Images.xaml holds only the GitHub geometry. App has no custom brand palette.
- Assets: Assets/logo.png, logo.ico, logo_transparent.png, kofi_logo.svg; docs/assets/images/logo.png, logo_square.png, thumbnail_preview.png (video thumbnail), homescreen_darkmode.png, homescreen_lightmode.png. No controller images or SVGs exist.

### Live status that exists / does not
- Exists: per-input pressed highlight; live thumbstick position; live L2/R2 gauges; controller list + refresh; mapping auto-saved to profile on change.
- Does NOT exist in the UI: Zwift connection/pairing state (emulator "Voltage ..." advertised/connected), LAN/network status or IP/port, controller connected/disconnected indicator (only the combo selection), battery level, which controller type is attached, errors/toasts, output activity (what was sent to Zwift), onboarding/instructions, profile save/load/reset (commented out), "test mode".

### Pain points seen in XAML
- No first-run guidance: user must know to pair "Voltage ..." inside Zwift.
- One dense wall of about 26 near-identical dropdowns; low scannability; labels like "Right Bumper (R1)" wrap awkwardly (see screenshot); cards of uneven height in a WrapPanel and ragged layout.
- Abstract names (Up/Left/Down/Right under three different groups) instead of a controller picture.
- Function names were verbose ("Up / Show the in-ride menu", "Descease the FTP Bias" typo in screenshot); the new reduced list fixes most of this.
- Hard-coded black strokes in thumbstick graph in dark mode; TreeView used as nav (indent chrome, 250px settings button); hand-rolled title bar; Settings page nearly empty; leftover template text.
- Cards: title only, no status chips, subtle depth.

## 2. Screenshots of the current app
- homescreen_darkmode.png (1485x1137): dark Fluent window, left TreeView with "Controller Setup" (selected, accent bar) and "About", Settings pinned bottom. Title 28pt + sentence; "Controller" card with combo "Xbox One S Controller" + Refresh List. Then rows of cards: Buttons (A Select, B Go Back, X Use a Power-up, Y Hide the HUD, Menu, View), D-Pad, Left Stick (concentric target + blue dot), Right Stick, Shoulder Buttons (L1/R1 mapped, L2/R2 empty gauges). Rest of the screen is empty at right. Reads as a form/settings dump.
- homescreen_lightmode.png: same layout in light.
(Look at the dark one first.)

## 3. BikeControl (Flutter, shadcn_flutter + Geist)

### Structure
- Left sidebar (sunken surface, brand mark + wordmark top), nav items Ride / Devices / Activity / Settings, selected item = filled brand-blue pill; bottom: Current Plan (PRO) tile with contour-line pattern + Help & Support. Compact variant on phones. App shell: lib/pages/shell/app_shell.dart, navigation.dart.
- Top bar: page title left; two status chips right ("Controller 81%" and "Smart Trainer -> Trainer app") = always-visible connection summary + battery; overflow menu.
- Pages: Ride (home: lib/pages/home/home_page.dart, widgets/home/*): "Ready to ride" banner (green check + sentence; turns into a fix-list of outstanding setup steps), Virtual shifting hero card (blue-teal gradient header with topo pattern, SIM/ERG segmented control, huge gear numeral "12 of 24", -/+ buttons, drivetrain graphic), and "YOUR BUTTONS" panel (controller drawing with button badges, below it a list of button -> action rows, "Edit buttons >"). Devices: grouped sections CONTROLLERS / SMART TRAINER / TRAINER APP; each is a card row with icon tile, name, sub meta (FW 1.2.0 - Signal good), a status dot+words ("Connected", "Receiving commands") and chevron; "Connect Controllers" add-row with hint "Wake it with a button press first."; trainer card has live metrics strip (250 W, 90 rpm, 12/24 gear). Controller settings: device header (green dot, battery, FW pill, signal), "Button Mapping" with target-app dropdown, cloud/settings icon buttons, controller graphic (lib/widgets/controller/controller_canvas.dart + contour painter + SVG) with coloured A/B/Y/Z/d-pad buttons and small blue badges showing assigned action; tapping a button opens detail (widgets/keymap/button_detail.dart) and a list below.
- Onboarding (pages/onboarding): left step rail + content; steps app -> where -> controller (phases permission/scanning/empty/list) -> virtual shifting -> connection -> done; Back/Continue; help icon; network precheck.
- Other: bottom sheets (BkBottomSheet), toasts, skeleton loaders, help center, ride overlay window, paywall, network troubleshooting page.

### Palette (lib/widgets/ui/app_theme.dart, colors.dart)
- Brand: #0E74B7 (blue), gradient end #0E9297 (teal). Dark-mode primary #4DA9E8 (on-primary #04121C). Light accent text #0C6AA8. PRO orange #F97316 (badge only).
- Light: page #F2F2F7 (Apple grouped), card #FFFFFF, text #1C1C1E, muted text #636366, muted/secondary fill #E9E9EE, border #DCDCE0, input #D1D1D6, destructive #DC2626.
- Dark: page #101318, card #1B1F25, popover/secondary/muted fill #262B33, border #2A3038, input #383E47, text #FFFFFF, muted text #A3A3A3. Sunken sidebar = page lerped 35% toward black.
- Status (light / dark): success #147A3B / #22C55E; warning #B45309 / #FBBF24; info #1D4ED8 / #93C5FD; danger #B91C1C / #FC8C8C; washes = ~15% alpha of the colour on dark (e.g. #2622C55E), pastel fills on light (#F0FDF4, #FFFBEB, #EFF6FF, #FEF2F2).
- Controller button colours (SVG art, from screenshots): A green, B pink, Y blue, Z orange, d-pad/black.

### Typography and shape
- Geist Sans (shadcn_flutter), CJK fallbacks; Barlow Condensed (600/700/800) for display: gear numerals, section labels in caps ("CONTROLLERS", "YOUR BUTTONS"), metric values. Scale: caption 11, xSmall 12, small 14, base 16, large 18, xLarge 20, 2xLarge 24, 3xLarge 30. Tabular figures on live numbers.
- Cards 16px radius, no outline (separation by tone), soft two-layer shadow on light (0x0A0F2D46 1px; 0x290F2D46 6px blur 16 spread -10); controls radius multiplier 0.7. Status = dot + words, not tinted pills ("● Connected"). Icons: Lucide line icons. Motion: bk_motion, reduced-motion aware, skeletons. Touch targets and contrast explicitly tuned (4.5:1 checks in comments).

### What makes it feel modern
1. Always-visible status chips with battery. 2. One "Ready to ride" state banner that converts to a to-do list. 3. A controller drawing is the mapping UI (tap the button, assign an action), with colour-coded buttons and badges. 4. Large condensed numerals for live data. 5. Tonal cards, generous padding, a single accent plus semantic status colours, light and dark. 6. Guided onboarding with a step rail. 7. Short plain-language helper text on every empty state.

## 4. Design brief

### Users
Zwift riders with an existing Xbox/PlayStation/Switch/8BitDo pad who want shifting/steering without buying Zwift Play; mostly non-technical, on a Windows PC running Zwift on the same machine (or LAN); setup is occasional, then the app should sit in the background. Secondary: tinkerers who remap.

### Key tasks
1. First run: understand what the app does, plug in a controller, make Zwift pair "Voltage ..." as controller (Zwift Ride) and confirm success.
2. Pick/refresh controller (and handle swap/disconnect).
3. Map buttons/sticks/triggers to the Zwift Ride functions (Navigate U/D/L/R, Select, Back/Menu, Power-up, Ride On, Shift up/down, Steer left/right, Brake), with sane defaults and reset.
4. Verify live input ("press a button and see it light up").
5. At a glance: is Zwift connected, is the controller connected, battery.

### Information architecture options
- A. Single dashboard + mapping drawer. One home screen: status header (Controller chip + battery, Zwift chip), controller picker, a compact list/grid of Zwift functions with their assigned buttons; clicking a row opens a right-hand drawer/sheet to change the binding ("press the button you want", or pick from list). Settings/About in a footer/overflow. Fewest screens; best for the small function set.
- B. Stepper onboarding + dashboard. First launch: 4 steps (Welcome -> Choose controller -> Pair with Zwift -> Test/Done), then the same dashboard as A; "Setup guide" re-openable from a banner when something is not ready (BikeControl "Ready to ride" pattern).
- C. Controller-centric visual mapper. Centre = controller illustration (Xbox-style generic outline, optionally per-type) with live press highlighting and action badges; click a control to assign; side panel lists functions and a learn-by-pressing mode. Status chips top-right. Best fidelity but needs art assets (SVG outline, buttons, sticks, triggers) and handling of non-Xbox layouts.
- Recommended: B + C inside A's frame (onboarding once, dashboard whose main panel is the visual mapper, with a function-list fallback view).

### Navigation suggestion
Either no sidebar (single dashboard + settings sheet) or a slim icon sidebar: Home/Map, Settings, About/Support. Replace the TreeView and the hand-made title bar with a standard WPF/Fluent title bar or a modern custom one.

### States to mock
Zwift link: not found/not paired (instructions, "Voltage ..." name, retry), waiting/advertising, connected (green), connection lost.
Controller: none detected (empty state + Refresh/plug-in hint), detected but not selected, connected with battery (wired = no battery; low battery warning), disconnected mid-session, multiple controllers (picker).
Mapping: default mapping loaded, edit one binding (listening for button press), conflict (button already used - swap?), unassigned function warning (e.g. no Shift mapped), reset to defaults confirm.
Live input: button press highlight, stick deflection with dead-zone, trigger gauge, "test mode" banner, action sent toast ("Shift up sent to Zwift").
Chrome: light and dark, first-run welcome, settings (theme, start with Windows, minimise to tray), about/donate, narrow window (min 400px) and wide (1600px).
Open data gaps for design: battery and Zwift connection state are not yet exposed by the app's view-models; confirm with engineering which signals will exist.
