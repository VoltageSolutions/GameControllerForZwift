using GameControllerForZwift.Core.Mapping;

namespace GameControllerForZwift.Core.Tests.Mapping
{
    public class InputMappingTests
    {
        [Fact]
        public void Constructor_InitializesProperties()
        {
            // Arrange
            var input = ControllerInput.A;
            var function = ZwiftFunction.Select;
            var playerView = ZwiftPlayerView.Drone;
            var riderAction = ZwiftRiderAction.Nice;

            // Act
            var mapping = new InputMapping(input, function, playerView, riderAction);

            // Assert
            Assert.Equal(input, mapping.Input);
            Assert.Equal(function, mapping.Function);
            Assert.Equal(playerView, mapping.PlayerView);
            Assert.Equal(riderAction, mapping.RiderAction);
        }

        [Fact]
        public void DefaultConstructor_SetsPropertiesToDefaults()
        {
            // Act
            var mapping = new InputMapping();

            // Assert
            Assert.Equal(default, mapping.Input);
            Assert.Equal(default, mapping.Function);
            Assert.Null(mapping.PlayerView);
            Assert.Null(mapping.RiderAction);
        }
    }
}
