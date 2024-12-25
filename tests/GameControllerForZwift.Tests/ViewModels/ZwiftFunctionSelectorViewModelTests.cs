using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.ViewModels;
using Xunit;

namespace GameControllerForZwift.UI.WPF.Tests.ViewModels
{
    public class ZwiftFunctionSelectorViewModelTests
    {
        [Fact]
        public void OnSelectedZwiftFunctionChanged_ShouldEnablePlayerView_WhenAdjustCameraAngleSelected()
        {
            // Arrange
            var viewModel = new ZwiftFunctionSelectorViewModel();
            var controllerData = new ControllerData
            {
                A = true, // You can set the state of buttons here as needed
                B = false,
                LeftBumper = false,
                RightBumper = false,
                // Set other necessary properties here
            };
            viewModel.ControllerData = controllerData;

            // Act
            viewModel.SelectedZwiftFunction = ZwiftFunction.AdjustCameraAngle;

            // Assert
            Assert.True(viewModel.ShowPlayerView);
            Assert.False(viewModel.ShowRiderAction);
        }

        [Fact]
        public void OnSelectedZwiftFunctionChanged_ShouldEnableRiderAction_WhenRiderActionSelected()
        {
            // Arrange
            var viewModel = new ZwiftFunctionSelectorViewModel();
            var controllerData = new ControllerData
            {
                A = true, // Set appropriate button values
                B = false,
                LeftBumper = false,
                RightBumper = false,
            };
            viewModel.ControllerData = controllerData;

            // Act
            viewModel.SelectedZwiftFunction = ZwiftFunction.RiderAction;

            // Assert
            Assert.False(viewModel.ShowPlayerView);
            Assert.True(viewModel.ShowRiderAction);
        }

        [Fact]
        public void IsPressed_ShouldReturnTrue_WhenButtonIsPressed()
        {
            // Arrange
            var viewModel = new ZwiftFunctionSelectorViewModel();
            var controllerData = new ControllerData
            {
                A = true,  // Simulating that 'A' is pressed
                B = false, // Simulating that 'B' is not pressed
                LeftBumper = false,
                RightBumper = false,
            };
            viewModel.ControllerData = controllerData;
            viewModel.SelectedInput = ControllerInput.A;

            // Act
            bool result = viewModel.IsPressed;

            // Assert
            Assert.True(result); // 'A' should be pressed
        }

        [Fact]
        public void IsPressed_ShouldReturnFalse_WhenButtonIsNotPressed()
        {
            // Arrange
            var viewModel = new ZwiftFunctionSelectorViewModel();
            var controllerData = new ControllerData
            {
                A = false,  // Simulating that 'A' is not pressed
                B = false,
                LeftBumper = false,
                RightBumper = false,
            };
            viewModel.ControllerData = controllerData;
            viewModel.SelectedInput = ControllerInput.A;

            // Act
            bool result = viewModel.IsPressed;

            // Assert
            Assert.False(result); // 'A' should not be pressed
        }

        [Fact]
        public void OnSelectedZwiftFunctionChanged_ShouldHideSecondarySelections_WhenOtherFunctionSelected()
        {
            // Arrange
            var viewModel = new ZwiftFunctionSelectorViewModel();
            var controllerData = new ControllerData
            {
                A = true, // Set appropriate state here
                B = false,
                LeftBumper = false,
                RightBumper = false,
            };
            viewModel.ControllerData = controllerData;

            // Act
            viewModel.SelectedZwiftFunction = ZwiftFunction.Select; // Replace with a valid ZwiftFunction enum value

            // Assert
            Assert.False(viewModel.ShowPlayerView);
            Assert.False(viewModel.ShowRiderAction);
        }
    }
}
