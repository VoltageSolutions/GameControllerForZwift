# Game Controller for Zwift

<img width="128" align="right" src="./assets/images/logo.png">

The goal of this project is to enable using standard Game Controllers to interact with Zwift, much like the capabilities of the Zwift Play Controllers or the Wahoo Kickr Bike Shift.




## Project Organization

This project's organization loosely follows a Clean Architecture Layout.

### Domain

`GameControllerForZwift.Core` defines base entities and interfaces for the other projects to use.

### Application

`GameControllerForZwift.Logic` is the main functionality of the application. This has turned out to be simple enough that I will combine the Domain and Application projects in the future.

### Infrastructure

`GameControllerForZwift.Gamepad` leverages SDL2 to capture game controller input.

`GameControllerForZwift.Keyboard` translate Zwift functions to key-presses that Zwift understands. The goal in the future is to augment or replace this implementation with a Bluetooth or WiFi compatible option instead.

### Presentation

`GameControllerForZwift.UI.WPF` contains ViewModels and any custom controls.

`GameControllerForZwift.WPF` is a WPF executable application.



