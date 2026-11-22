using System.Text.RegularExpressions;

namespace GameControllerForZwift.UnitTests.Localization
{
    /// <summary>
    /// Keeps the accessibility IDs the screen specs list equal to the IDs in the app (docs/spec/ux/accessibility.md,
    /// UX-A-018 and UX-A-034). A spec lists them in an "Accessibility IDs" table. An ID that the spec lists but the app
    /// does not have fails here, so a screen reader or a UI test never loses an element.
    /// </summary>
    [Trait("Category", "Accessibility")]
    public partial class SpecAccessibilityTests
    {
        private static readonly string[] SampleSpec =
        [
            "## Accessibility IDs",
            "",
            "| ID | Where |",
            "|---|---|",
            "| `RidePageTitle`, `RideNoSuchElement` | Home |",
            "| `ConnectionsStep1` to `ConnectionsStep3` | Steps |",
            "| `ButtonsRow_{input}` | Rows |",
            "",
            "## Next",
            "| `Ignored` | x |",
        ];

        // The first-run screens of setup.md are not built, so the app has none of their elements.
        private static readonly string[] ScreensNotBuilt = ["setup.md"];

        public static TheoryData<string> ScreenSpecs()
        {
            var data = new TheoryData<string>();
            foreach (string name in ScreenSpecNames())
            {
                data.Add(name);
            }

            return data;
        }

        private static List<string> ScreenSpecNames()
        {
            string screens = Path.Combine(SpecStringTests.RepositoryRoot(), "docs", "spec", "screens");
            return Directory.EnumerateFiles(screens, "*.md").Where(f => !ScreensNotBuilt.Contains(Path.GetFileName(f)) && Ids(File.ReadAllLines(f)).Count > 0).Select(f => Path.GetFileName(f)).ToList();
        }

        [Theory]
        [MemberData(nameof(ScreenSpecs))]
        public void UXA034_EveryIdTheSpecListsExistsInTheApp(string specFile)
        {
            string path = Path.Combine(SpecStringTests.RepositoryRoot(), "docs", "spec", "screens", specFile);
            string source = AppSource();

            List<string> missing = Missing(Ids(File.ReadAllLines(path)), source).ToList();

            Assert.True(missing.Count == 0, $"{specFile} lists IDs the app does not have: {string.Join(", ", missing)}");
        }

        [Fact]
        public void UXA034_TheCheckFindsAMissingIdAndSkipsPatterns()
        {
            List<string> ids = Ids(SampleSpec);

            Assert.Equal(["RidePageTitle", "RideNoSuchElement", "ConnectionsStep1", "ConnectionsStep3"], ids);
            Assert.Equal(["RideNoSuchElement"], Missing(ids, "AutomationId=\"RidePageTitle\" \"ConnectionsStep1\" \"ConnectionsStep3\""));
        }

        [Fact]
        public void UXA034_TheScreenSpecsListIds()
        {
            Assert.Contains("settings.md", ScreenSpecNames());
            Assert.Contains("ride.md", ScreenSpecNames());
        }

        private static IEnumerable<string> Missing(List<string> ids, string source) =>
            ids.Where(id => !Regex.IsMatch(source, @"(?<![A-Za-z0-9_])" + Regex.Escape(id) + @"(?![A-Za-z0-9_])")).Distinct();

        private static List<string> Ids(string[] lines)
        {
            var ids = new List<string>();
            bool inTable = false;
            foreach (string line in lines)
            {
                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    inTable = line.Trim().Equals("## Accessibility IDs", StringComparison.Ordinal);
                    continue;
                }

                if (!inTable || !line.StartsWith('|'))
                {
                    continue;
                }

                string firstCell = line.Split('|')[1];
                foreach (Match match in Token().Matches(firstCell))
                {
                    ids.Add(match.Groups["id"].Value);
                }
            }

            return ids;
        }

        private static string AppSource()
        {
            string root = Path.Combine(SpecStringTests.RepositoryRoot(), "windows", "src", "Presentation", "GameControllerForZwift");
            return string.Join('\n', Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                .Where(f => (f.EndsWith(".xaml", StringComparison.Ordinal) || f.EndsWith(".cs", StringComparison.Ordinal))
                    && !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                    && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                .Select(File.ReadAllText));
        }

        // An ID in backticks. A name with a placeholder, such as ButtonsRow_{input}, is a pattern and is skipped.
        [GeneratedRegex(@"`(?<id>[A-Za-z][A-Za-z0-9]*)`")]
        private static partial Regex Token();
    }
}
