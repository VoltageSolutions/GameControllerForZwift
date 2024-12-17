using System.ComponentModel;

namespace GameControllerForZwift.Core.Tests
{
    public class ZwiftRiderActionDescriptionTests
    {
        [Theory]
        [InlineData(ZwiftRiderAction.Elbow, "Stick out elbow")]
        [InlineData(ZwiftRiderAction.WaveHand, "Wave hand")]
        [InlineData(ZwiftRiderAction.RideOn, "Give a 'Ride On!'")]
        [InlineData(ZwiftRiderAction.HammerTime, "Say 'Hammer Time!'")]
        [InlineData(ZwiftRiderAction.Nice, "Say 'Nice!'")]
        [InlineData(ZwiftRiderAction.BringIt, "Say 'Bring It!'")]
        [InlineData(ZwiftRiderAction.ImToast, "Say 'I'm toast!'")]
        [InlineData(ZwiftRiderAction.BikeBell, "Ring the bike bell")]
        [InlineData(ZwiftRiderAction.ScreenShot, "Take a screen shot")]
        public void ZwiftRiderAction_HasCorrectDescription(ZwiftRiderAction action, string expectedDescription)
        {
            // Act
            var actualDescription = GetEnumDescription(action);

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
