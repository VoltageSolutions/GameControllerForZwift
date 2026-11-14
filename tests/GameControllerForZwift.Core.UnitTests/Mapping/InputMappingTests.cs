using GameControllerForZwift.Core.Controller;
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

            // Act
            var mapping = new InputMapping(input, function);

            // Assert
            Assert.Equal(input, mapping.Input);
            Assert.Equal(function, mapping.Function);
        }

        [Fact]
        public void DefaultConstructor_SetsPropertiesToDefaults()
        {
            // Act
            var mapping = new InputMapping();

            // Assert
            Assert.Equal(default, mapping.Input);
            Assert.Equal(default, mapping.Function);
        }
    }
}
