using System.ComponentModel;
namespace GameControllerForZwift.Core.Tests
{
    public class ZwiftFunctionDescriptionTests
    {
        [Theory]
        [InlineData(ZwiftFunction.None, "No Action Configured")]
        [InlineData(ZwiftFunction.NavigateUp, "Navigate Up")]
        [InlineData(ZwiftFunction.NavigateDown, "Navigate Down")]
        [InlineData(ZwiftFunction.NavigateLeft, "Navigate Left")]
        [InlineData(ZwiftFunction.NavigateRight, "Navigate Right")]
        [InlineData(ZwiftFunction.Select, "Select (A)")]
        [InlineData(ZwiftFunction.GoBack, "Back / Menu (B)")]
        [InlineData(ZwiftFunction.Powerup, "Power-up (Y)")]
        [InlineData(ZwiftFunction.RideOn, "Ride On (Z)")]
        [InlineData(ZwiftFunction.ShiftUp, "Shift up / Increase workout intensity")]
        [InlineData(ZwiftFunction.ShiftDown, "Shift down / Decrease workout intensity")]
        [InlineData(ZwiftFunction.SteerLeft, "Steer left")]
        [InlineData(ZwiftFunction.SteerRight, "Steer right")]
        [InlineData(ZwiftFunction.Brake, "Brake")]
        public void ZwiftFunction_HasCorrectDescription(ZwiftFunction function, string expectedDescription)
        {
            var actualDescription = GetEnumDescription(function);
            Assert.Equal(expectedDescription, actualDescription);
        }

        [Fact]
        public void ZwiftFunction_HasExactlyTheZwiftRideFunctions()
        {
            var expected = new[] { "None", "NavigateUp", "NavigateDown", "NavigateLeft", "NavigateRight", "Select", "GoBack", "Powerup", "RideOn", "ShiftUp", "ShiftDown", "SteerLeft", "SteerRight", "Brake" };
            Assert.Equal(expected.OrderBy(x => x), Enum.GetNames<ZwiftFunction>().OrderBy(x => x));
        }

        private string GetEnumDescription(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field.GetCustomAttributes(typeof(DescriptionAttribute), false)
                                 .Cast<DescriptionAttribute>()
                                 .FirstOrDefault();
            return attribute?.Description ?? string.Empty;
        }
    }
}
