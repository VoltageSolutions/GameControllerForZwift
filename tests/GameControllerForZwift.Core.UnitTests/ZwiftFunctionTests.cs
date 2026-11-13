using System.ComponentModel;
namespace GameControllerForZwift.Core.Tests
{
    public class ZwiftFunctionDescriptionTests
    {
        [Theory]
        [InlineData(ZwiftFunction.None, "No Action Configured")]
        [InlineData(ZwiftFunction.ShowMenu, "Up / Show the in-ride menu")]
        [InlineData(ZwiftFunction.NavigateLeft, "Left")]
        [InlineData(ZwiftFunction.NavigateRight, "Right")]
        [InlineData(ZwiftFunction.Uturn, "Down / Make a U-Turn")]
        [InlineData(ZwiftFunction.Powerup, "Use a Power-up")]
        [InlineData(ZwiftFunction.Select, "Select")]
        [InlineData(ZwiftFunction.GoBack, "Go Back")]
        [InlineData(ZwiftFunction.SkipWorkoutBlock, "Skip a workout block")]
        [InlineData(ZwiftFunction.ShiftUp, "Shift up / Increase workout intensity")]
        [InlineData(ZwiftFunction.ShiftDown, "Shift down / Decrease workout intensity")]
        [InlineData(ZwiftFunction.AdjustCameraAngle, "Adjust the camera angle")]
        [InlineData(ZwiftFunction.RiderAction, "Rider (Function Key) action")]
        [InlineData(ZwiftFunction.ShowPairedDevices, "Show Paired Devices screen")]
        [InlineData(ZwiftFunction.ShowGarage, "Show the Garage")]
        [InlineData(ZwiftFunction.ShowTrainingMenu, "Show the Training Menu")]
        [InlineData(ZwiftFunction.ToggleGraphs, "Hide the watt and HR graphs")]
        [InlineData(ZwiftFunction.SendGroupText, "Send a group text")]
        [InlineData(ZwiftFunction.HideHUD, "Hide the Heads-Up Display")]
        [InlineData(ZwiftFunction.PromoCode, "Enter a Promo Code")]
        [InlineData(ZwiftFunction.SteerLeft, "Steer left - Zwift LAN controller only")]
        [InlineData(ZwiftFunction.SteerRight, "Steer right - Zwift LAN controller only")]
        [InlineData(ZwiftFunction.NavigateUp, "Navigate up")]
        [InlineData(ZwiftFunction.NavigateDown, "Navigate down")]
        [InlineData(ZwiftFunction.Brake, "Brake")]
        public void ZwiftFunction_HasCorrectDescription(ZwiftFunction function, string expectedDescription)
        {
            // Act
            var actualDescription = GetEnumDescription(function);

            // Assert
            Assert.Equal(expectedDescription, actualDescription);
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