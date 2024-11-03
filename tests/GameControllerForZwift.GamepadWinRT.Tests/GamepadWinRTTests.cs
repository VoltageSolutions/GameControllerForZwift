using GameControllerForZwift.Core;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Gaming.Input;

namespace GameControllerForZwift.GamepadWinRT.Tests
{ 
    public class GamepadWinRTTests
    {
        [Fact]
        public void GetControllers_ReturnsCorrectNumberOfControllers()
        {
            // Arrange
            var mockGamepad1 = Substitute.For<Gamepad>();
            var mockGamepad2 = Substitute.For<Gamepad>();
            Gamepad.Gamepads.Returns(new List<Gamepad> { mockGamepad1, mockGamepad2 });

            // Act
            var controllers = GamepadWinRT.GetControllers.ToList();

            // Assert
            Assert.Equal(2, controllers.Count);
        }

        [Fact]
        public void GetControllers_ReturnsInstancesOfIController()
        {
            // Arrange
            var mockGamepad1 = Substitute.For<Gamepad>();
            Gamepad.Gamepads.Returns(new List<Gamepad> { mockGamepad1 });

            // Act
            var controllers = GamepadWinRT.GetControllers.ToList();

            // Assert
            Assert.All(controllers, item => Assert.IsAssignableFrom<IController>(item));
        }
    }
}
