using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.ViewModels;
using Xunit;

namespace GameControllerForZwift.UI.WPF.Tests.ViewModels
{
    public class ZwiftFunctionSelectorViewModelTests
    {
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
        public void SelectedZwiftFunction_Change_RaisesMappingChanged_AndRoundTripsThroughInputMapping()
        {
            var viewModel = new ZwiftFunctionSelectorViewModel { SelectedInput = ControllerInput.X };
            int raised = 0;
            viewModel.MappingChanged += (_, _) => raised++;

            viewModel.SelectedZwiftFunction = ZwiftFunction.RideOn;

            Assert.Equal(1, raised);
            var mapping = viewModel.GetInputMapping();
            Assert.Equal(ControllerInput.X, mapping.Input);
            Assert.Equal(ZwiftFunction.RideOn, mapping.Function);
        }

        [Fact]
        public void ZwiftFunctions_ListsAllZwiftRideFunctions()
        {
            var viewModel = new ZwiftFunctionSelectorViewModel();
            Assert.Equal(Enum.GetValues<ZwiftFunction>(), viewModel.ZwiftFunctions);
        }
    }
}
