using GameControllerForZwift.Core.Configurations;

namespace GameControllerForZwift.Core.Tests.Configurations
{
    public class ConfigurationFileNamesTests
    {
        [Fact]
        public void CFG004_GetBaseName_JoinsControllerAndConfigurationWithUnderscore()
        {
            Assert.Equal("Xbox-Series-X-Controller_Climbing", ConfigurationFileNames.GetBaseName("Xbox Series X Controller", "Climbing"));
        }

        [Theory]
        [InlineData("A/B", "A-B")]
        [InlineData("A\\B", "A-B")]
        [InlineData("What?*", "What-")]
        [InlineData("a:b|c\"d<e>f", "a-b-c-d-e-f")]
        [InlineData("Hill   climb", "Hill-climb")]
        [InlineData("Tab\tname", "Tab-name")]
        [InlineData("a / b", "a-b")]
        [InlineData("Ends with dot.", "Ends-with-dot")]
        public void CFG004_Sanitize_ReplacesInvalidCharactersAndCollapsesWhitespace(string input, string expected)
        {
            Assert.Equal(expected, ConfigurationFileNames.Sanitize(input, "x"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("???")]
        [InlineData(null)]
        public void CFG004_Sanitize_UsesFallbackWhenNothingIsLeft(string? input)
        {
            Assert.Equal("fallback", ConfigurationFileNames.Sanitize(input, "fallback"));
        }

        [Fact]
        public void CFG004_MakeUnique_AppendsCounterWhenTaken()
        {
            var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Pad_Climbing.json", "Pad_Climbing-2.json" };
            Assert.Equal("Pad_Climbing-3.json", ConfigurationFileNames.MakeUnique("Pad_Climbing", taken.Contains));
            Assert.Equal("Pad_Sprint.json", ConfigurationFileNames.MakeUnique("Pad_Sprint", taken.Contains));
        }

        [Fact]
        public void CFG004_MakeUnique_FirstCollisionGetsSuffixTwo()
        {
            Assert.Equal("Pad_Climbing-2.json", ConfigurationFileNames.MakeUnique("Pad_Climbing", name => name == "Pad_Climbing.json"));
        }

        [Fact]
        public void CFG004_GetBaseName_LimitsControllerPart()
        {
            var baseName = ConfigurationFileNames.GetBaseName(new string('c', 200), "Climbing");
            Assert.Equal(ConfigurationFileNames.MaxControllerPartLength + "_Climbing".Length, baseName.Length);
        }
    }
}
