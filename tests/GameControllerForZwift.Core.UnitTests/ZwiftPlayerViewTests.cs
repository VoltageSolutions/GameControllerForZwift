using System.ComponentModel;

namespace GameControllerForZwift.Core.Tests
{
    public class ZwiftPlayerViewDescriptionTests
    {
        [Theory]
        [InlineData(ZwiftPlayerView.Default, "Default View (Behind Rider)")]
        [InlineData(ZwiftPlayerView.ThirdPerson, "3rd-Person Perspective")]
        [InlineData(ZwiftPlayerView.FPS, "1st-Person Perspective")]
        [InlineData(ZwiftPlayerView.FrontLeftSide, "From the front left-side")]
        [InlineData(ZwiftPlayerView.RearRightSide, "From the rear right-side")]
        [InlineData(ZwiftPlayerView.FacingRider, "Reverse, facing rider")]
        [InlineData(ZwiftPlayerView.Spectator, "Spectator View")]
        [InlineData(ZwiftPlayerView.Helicopter, "Helicopter")]
        [InlineData(ZwiftPlayerView.BirdsEye, "Birds'-Eye View")]
        [InlineData(ZwiftPlayerView.Drone, "Drone")]
        public void ZwiftPlayerView_HasCorrectDescription(ZwiftPlayerView view, string expectedDescription)
        {
            // Act
            var actualDescription = GetEnumDescription(view);

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
