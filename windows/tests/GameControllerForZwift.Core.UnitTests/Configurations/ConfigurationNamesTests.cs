using GameControllerForZwift.Core.Configurations;

namespace GameControllerForZwift.Core.Tests.Configurations
{
    public class ConfigurationNamesTests
    {
        private static readonly string[] Existing = ["Climbing", "Flat Out"];

        [Theory]
        [InlineData("Sprint")]
        [InlineData("  Sprint  ")]
        [InlineData("A")]
        public void CFG026_Validate_AcceptsValidNames(string name)
        {
            Assert.Null(ConfigurationNames.Validate(name, Existing));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void CFG026_Validate_RejectsEmptyNames(string? name)
        {
            Assert.NotNull(ConfigurationNames.Validate(name, Existing));
        }

        [Fact]
        public void CFG026_Validate_AllowsFiftyCharactersAfterTrimming()
        {
            var fifty = new string('x', 50);
            Assert.Null(ConfigurationNames.Validate("  " + fifty + "  ", Existing));
            Assert.NotNull(ConfigurationNames.Validate(fifty + "y", Existing));
        }

        [Theory]
        [InlineData("climbing")]
        [InlineData("CLIMBING")]
        [InlineData("climbing ")]
        [InlineData("  Climbing")]
        [InlineData("flat out")]
        public void CFG027_Validate_RejectsDuplicatesIgnoringCaseAndWhitespace(string name)
        {
            var message = ConfigurationNames.Validate(name, Existing);
            Assert.NotNull(message);
            Assert.Contains("already exists for this controller", message, StringComparison.Ordinal);
        }

        [Fact]
        public void CFG028_Validate_DuplicateMessageNamesTheExistingConfiguration()
        {
            Assert.Equal("A configuration named 'Climbing' already exists for this controller",
                ConfigurationNames.Validate("climbing ", Existing));
        }

        [Theory]
        [InlineData("Default")]
        [InlineData("default")]
        [InlineData(" DEFAULT ")]
        public void CFG027_Validate_ReservesDefault(string name)
        {
            var message = ConfigurationNames.Validate(name, []);
            Assert.NotNull(message);
            Assert.Contains("reserved", message, StringComparison.Ordinal);
        }

        [Fact]
        public void CFG032_SuggestUnique_AddsNumberSuffix()
        {
            Assert.Equal("Climbing (2)", ConfigurationNames.SuggestUnique("Climbing", Existing));
            Assert.Equal("Climbing (3)", ConfigurationNames.SuggestUnique("Climbing", ["Climbing", "Climbing (2)"]));
            Assert.Equal("Climbing (3)", ConfigurationNames.SuggestUnique("Climbing (2)", ["Climbing", "Climbing (2)"]));
        }

        [Fact]
        public void CFG032_SuggestUnique_ReturnsNameWhenFree()
        {
            Assert.Equal("Sprint", ConfigurationNames.SuggestUnique(" Sprint ", Existing));
        }

        [Fact]
        public void CFG032_SuggestUnique_KeepsSuggestionWithinMaxLength()
        {
            var longName = new string('x', 50);
            var suggestion = ConfigurationNames.SuggestUnique(longName, [longName]);
            Assert.True(suggestion.Length <= ConfigurationNames.MaxLength);
            Assert.Null(ConfigurationNames.Validate(suggestion, [longName]));
        }

        [Fact]
        public void CFG027_SuggestUnique_AvoidsDefault()
        {
            Assert.Equal("Default (2)", ConfigurationNames.SuggestUnique("Default", []));
        }
    }
}
