using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.Core.Mapping;

namespace GameControllerForZwift.Core.Tests.Mapping
{
    public class ControllerProfileTests
    {
        [Fact]
        public void Constructor_InitializesNameAndDefaultMappings()
        {
            // Arrange & Act
            var profile = new ControllerProfile();

            // Assert
            Assert.Equal(string.Empty, profile.Name);
            Assert.NotNull(profile.Mappings);
            Assert.NotEmpty(profile.Mappings);

            Assert.All<ControllerInput>(Enum.GetValues<ControllerInput>(), input =>
                Assert.Contains(profile.Mappings, m => m.Input == input)
            );
        }


        [Fact]
        public void UpdateMapping_UpdatesExistingMapping()
        {
            // Arrange
            var profile = new ControllerProfile();
            var input = ControllerInput.A;
            var newFunction = ZwiftFunction.Select;

            // Act
            profile.UpdateMapping(input, newFunction);

            // Assert
            var updatedMapping = profile.Mappings.FirstOrDefault(m => m.Input == input);
            Assert.NotNull(updatedMapping);
            Assert.Equal(newFunction, updatedMapping.Function);
        }

        [Fact]
        public void UpdateMapping_ThrowsException_WhenInputIsInvalid()
        {
            // Arrange
            var profile = new ControllerProfile();
            var invalidInput = (ControllerInput)999;

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                profile.UpdateMapping(invalidInput, ZwiftFunction.Select)
            );
        }
    }
}
