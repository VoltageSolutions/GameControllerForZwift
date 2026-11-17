# Map your controller

!!! tldr "TL;DR"
    Pick a Zwift function for each controller input on the **Controller Setup** page. The functions match the buttons and paddles of a Zwift Ride: navigation, select, back, power-up, Ride On, shift up and down, steer left and right, and brake. Sticks and triggers give partial steering and braking. Changes apply immediately. Save them as a named configuration to keep them for next time.

Each button, stick direction and trigger on your controller can do one Zwift function. You choose the function from the drop-down next to each input on the **Controller Setup** page. Changes take effect straight away.

## The functions you can choose

The app presents itself to Zwift as a Zwift Ride controller, so the functions are exactly the buttons and paddles a Zwift Ride has.

| Function in the app | Zwift Ride input it sends | What it does in Zwift |
|---|---|---|
| No Action Configured | Nothing | The input is ignored. |
| Navigate Up | D-pad Up | Moves up in menus. In a ride, opens the action bar. |
| Navigate Down | D-pad Down | Moves down in menus. In a ride, makes a U-turn. |
| Navigate Left | D-pad Left | Moves left in menus. |
| Navigate Right | D-pad Right | Moves right in menus. |
| Select (A) | A | Selects the highlighted item. |
| Back / Menu (B) | B | Goes back, or opens the menu. |
| Power-up (Y) | Y | Uses your power-up. |
| Ride On (Z) | Z | Gives a Ride On. |
| Shift up / Increase workout intensity | Right shifter (RS1) | Shifts to a harder gear. In a workout, raises the intensity. |
| Shift down / Decrease workout intensity | Left shifter (LS1) | Shifts to an easier gear. In a workout, lowers the intensity. |
| Steer left | Left paddle (ZL), tilted outward | Steers left. |
| Steer right | Right paddle (ZR), tilted outward | Steers right. |
| Brake | Both paddles, squeezed inward | Brakes. |

The shift directions follow Zwift's **Sequential** shift style for the Zwift Ride, where the left shifters (LS1, LS2) shift to an easier gear and the right shifters (RS1, RS2) shift to a harder gear. See Zwift's [Shift Style and Gear Range](https://support.zwift.com/en_us/shift-style-and-gear-range-rksYezzIC) article.

!!! tip "Press and hold"
    Some buttons do more when you hold them. This is Zwift's own behavior for the Zwift Ride, and the app passes your hold straight through. For example, holding **Ride On (Z)** sends Ride Ons to several nearby riders, and holding **Power-up (Y)** skips the current workout block.

## Save and load configurations

I keep my mappings as named configurations, for example one for climbing and one for flat rides. The configuration bar sits at the top of the **Controller Setup** page.

- **Save as...** saves the current mappings under a new name and selects it.
- **Save** writes my changes to the selected configuration. A dot after the name, like `Climbing •`, means I have unsaved changes.
- **Delete** removes the selected configuration after I confirm.
- The **...** menu has **Import...**, **Rename...** and **Open folder**.

**Default** is the built-in mapping shown below. I can change mappings while it is selected, but I can't overwrite, rename or delete it, so I use **Save as...** to keep my changes.

Configurations belong to a controller model, so a second controller of the same model sees them too. When I start the app, it selects the configuration I used last for that controller.

To reuse a configuration I made for another controller, I check **Show configurations from other controllers**. The list then also shows those configurations, with the controller name in parentheses, for example `Climbing (Xbox Series X Controller)`. When I pick one, the app asks whether to use it with this controller and saves a copy for it. The original stays with the other controller.

Each configuration is a JSON file in `%LOCALAPPDATA%\GameControllerForZwift\Profiles`. To share one, I click **Open folder** and send the file. To use a file someone sent me, I click **Import...**. To remove every saved configuration at once, I go to **Settings** and click **Delete all**.

## Analog sticks and triggers

Buttons are on or off. Sticks and triggers are **analog**: the further you push, the stronger the effect.

- **Steering** from a stick tilt is partial. A small tilt steers a little, a full tilt steers fully.
- **Braking** from a trigger is partial too. A light squeeze sends a light brake value.
- There is a small dead zone near the center of each stick iin anticipation of stick drift. Letting go of the stick should return it to the center.

!!! note "How Zwift reads the brake"
    The app sends a brake value that grows with your trigger. Zwift decides how strongly to brake from that value, and in testing even a light squeeze could bring the rider to a full stop.

## The default mapping

| Controller input | Function |
|---|---|
| A | Select (A) |
| B | Back / Menu (B) |
| X | Ride On (Z) |
| Y | Power-up (Y) |
| D-pad Up, Down, Left, Right | Navigate Up, Down, Left, Right |
| Left bumper (L1) | Shift down |
| Right bumper (R1) | Shift up |
| Left stick tilt left | Steer left |
| Left stick tilt right | Steer right |
| Left trigger (L2) | Brake |

Everything else starts as **No Action Configured**. Change any of these to suit how you ride.
