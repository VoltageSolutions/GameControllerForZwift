# Map your controller

!!! tldr "TL;DR"
    Pick a Zwift function for each controller input on the **Buttons** tab. The functions match the buttons and paddles of a Zwift Ride: navigation, select, back, power-up, Ride On, shift up and down, steer left and right, and brake. Sticks and triggers give partial steering and braking. Changes apply immediately. Save them as a named mapping to keep them for next time.

Each button, stick direction and trigger on your controller can do one Zwift function. The **Buttons** tab lists every input in a row, with what it does. I click a row and a panel opens with the functions grouped by what they do. Picking one applies it straight away. The row for the input I changed shows **Changed** until I save.

The **Buttons** tab has two tabs of its own. **Buttons** lists the buttons, the D-pad, and the bumpers. **Sticks and triggers** lists each stick with its live position, and each trigger with a bar that fills as I pull it.

## The functions you can choose

The app presents itself to Zwift as a Zwift Ride controller, so the functions are exactly the buttons and paddles a Zwift Ride has.

| Function in the app | Zwift Ride input it sends | What it does in Zwift |
|---|---|---|
| No action | Nothing | The input is ignored. |
| Navigate up | D-pad Up | Moves up in menus. In a ride, opens the action bar. |
| Navigate down | D-pad Down | Moves down in menus. In a ride, makes a U-turn. |
| Navigate left | D-pad Left | Moves left in menus. |
| Navigate right | D-pad Right | Moves right in menus. |
| Select | A | Selects the highlighted item. |
| Back | B | Goes back, or opens the menu. |
| Power-up | Y | Uses your power-up. |
| Ride On | Z | Gives a Ride On. |
| Shift up | Right shifter (RS1) | Shifts to a harder gear. In a workout, raises the intensity. |
| Shift down | Left shifter (LS1) | Shifts to an easier gear. In a workout, lowers the intensity. |
| Steer left | Left paddle (ZL), tilted outward | Steers left. |
| Steer right | Right paddle (ZR), tilted outward | Steers right. |
| Brake | Both paddles, squeezed inward | Brakes. |

The shift directions follow Zwift's **Sequential** shift style for the Zwift Ride, where the left shifters (LS1, LS2) shift to an easier gear and the right shifters (RS1, RS2) shift to a harder gear. See Zwift's [Shift Style and Gear Range](https://support.zwift.com/en_us/shift-style-and-gear-range-rksYezzIC) article.

!!! tip "Press and hold"
    Some buttons do more when you hold them. This is Zwift's own behavior for the Zwift Ride, and the app passes your hold straight through. For example, holding **Ride On** sends Ride Ons to several nearby riders, and holding **Power-up** skips the current workout block.

## Save and load mappings

I keep my mappings as named sets, for example one for climbing and one for flat rides. The mapping controls sit at the top of the **Buttons** tab.

- The **mapping** list picks the mapping. **Unsaved changes** shows next to the selected one when I have edits I haven't saved.
- **Save** writes my changes to the selected mapping. It turns orange only when there is something to save.
- **Save as new** saves the current buttons under a new name and selects it.
- **Delete** removes the selected mapping after I confirm.
- The **...** menu has **Import a mapping...**, **Rename...**, **Open folder**, and **Reset to default**.

**Reset to default** sets every input to what the Default mapping does. It counts as an edit, so I can still save it or switch away.

**Default** is the built-in mapping shown below. I can change buttons while it is selected, but I can't overwrite, rename or delete it, so I use **Save as new** to keep my changes.

Mappings belong to a controller model, so a second controller of the same model sees them too. When I start the app, it selects the mapping I used last for that controller.

To reuse a mapping I made for another controller, I check **Show mappings from other controllers**. The list then also shows those mappings, with the controller name in parentheses, for example `Climbing (Xbox Series X Controller)`. When I pick one, the app asks whether to use it with this controller and saves a copy for it. The original stays with the other controller.

Each mapping is a JSON file in `%LOCALAPPDATA%\GameControllerForZwift\Profiles`. To share one, I click **Open folder** in the **...** menu and send the file. To use a file someone sent me, I click **Import a mapping...**. To remove every saved mapping at once, I open **Settings** from the **...** menu and click **Delete all saved mappings**. I can also click **Open the mappings folder** there. The delete button is disabled when nothing is saved.

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
| A | Select |
| B | Back |
| X | Ride On |
| Y | Power-up |
| D-pad up, down, left, right | Navigate up, down, left, right |
| Left bumper (LB) | Shift down |
| Right bumper (RB) | Shift up |
| Left stick tilt left | Steer left |
| Left stick tilt right | Steer right |
| Left trigger (LT) | Brake |

Everything else starts as **No action**. Change any of these to suit how you ride.
