# GameControllerForZwift

The goal of this project is to enable using standard Game Controllers to interact with Zwift, much like the capabilities of the [Zwift Play Controllers](https://us.zwift.com/products/zwift-play?variant=43737779896576) or the [Wahoo Kickr Bike Shift](https://www.wahoofitness.com/devices/indoor-cycling/smart-bikes/kickr-bike-shift-buy).

## Known Bugs

- Navigation can blow up and crash the app when going backwards if Settings has been selected.
- When first picking a navigation item on the tree, it will not show up as selected. Future selections will highlight the item properly.

## Project Organization

The project follows a Clean Architecture Layout.

### Domain

`GameControllerForZwift.Core` defines base entities and interfaces for the other projects to use.

### Application

`GameControllerForZwift.App` is the main functionality of the application.

### Infrastructure

`GameControllerForZwift.GamepadWinRT` leverages Windows Runtime APIs to capture XInput controllers.
`GameControllerForZwift.ZwiftNetwork` presents this project as a network-based controller for Zwift to talk to.

### Presentation

`GameControllerForZwift.UI.WPF` contains ViewModels and any custom controls.

`GameControllerForZwift.WPF` is a WPF executable application.

## Project Goals

### UI

Follow WPF Gallary layout.

Screen or wizard for selecting a controller and mapping its inputs
way to save and recall profiles/configurations
settings to configure app
	run at startup
	check for updates?
log
debug - visualize controller button presses like
https://ds4-windows.com/wp-content/uploads/2021/09/GamepadTesterDoubleInput.gif


make controllersetup main screen
add an about screen
	include acknowledgements, link to github
in settings, block the darkmode option in W10

make controller list auto-populate

In w10 clicking in navigation pane highlights the whole window