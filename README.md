# GameControllerForZwift

The goal of this project is to enable using standard Game Controllers to interact with Zwift, much like the capabilities of the [Zwift Play Controllers](https://us.zwift.com/products/zwift-play?variant=43737779896576) or the [Wahoo Kickr Bike Shift](https://www.wahoofitness.com/devices/indoor-cycling/smart-bikes/kickr-bike-shift-buy).

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
