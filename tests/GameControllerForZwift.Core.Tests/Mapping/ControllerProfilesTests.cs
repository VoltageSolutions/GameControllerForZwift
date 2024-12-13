using GameControllerForZwift.Core.Mapping;

namespace GameControllerForZwift.Core.Tests.Mapping
{
    public class ControllerProfilesTests
    {
        [Fact]
        public void Constructor_InitializesEmptyProfilesList()
        {
            // Act
            var controllerProfiles = new ControllerProfiles();

            // Assert
            Assert.NotNull(controllerProfiles.Profiles);
            Assert.Empty(controllerProfiles.Profiles);
        }

        [Fact]
        public void AddProfile_AddsProfileToList()
        {
            // Arrange
            var controllerProfiles = new ControllerProfiles();
            var profile = new ControllerProfile { Name = "Test Profile" };

            // Act
            controllerProfiles.Profiles.Add(profile);

            // Assert
            Assert.Single(controllerProfiles.Profiles);
            Assert.Contains(profile, controllerProfiles.Profiles);
        }
    }
}
