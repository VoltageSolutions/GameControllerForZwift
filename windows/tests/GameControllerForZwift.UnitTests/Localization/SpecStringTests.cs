using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace GameControllerForZwift.UnitTests.Localization
{
    /// <summary>
    /// Keeps the strings the screen specs quote equal to Resources/Strings.resx (docs/spec/ux/localization.md, UX-L-012
    /// and UX-L-013), and keeps each translation complete. A spec lists its strings in a "Quoted strings" table. Change a
    /// string in the spec and in Strings.resx together, or these tests fail.
    /// </summary>
    [Trait("Category", "Localization")]
    public partial class SpecStringTests
    {
        private static readonly string[] SampleSpec =
        [
            "# Sample",
            "",
            "## Quoted strings",
            "",
            "| Key | English text | Rule |",
            "|---|---|---|",
            "| `AppearanceLight` | Light | SCR-SET-001 |",
            "| `AppearanceDark` | Night | SCR-SET-001 |",
            "| `NoSuchKey` | Anything | SCR-SET-001 |",
            "",
            "## After the table",
            "",
            "| `Ignored` | Text | SCR-X-001 |",
        ];

        public static TheoryData<string> SpecFilesWithStrings()
        {
            var data = new TheoryData<string>();
            foreach (string file in SpecFiles().Where(f => QuotedStrings(File.ReadAllLines(f)).Count > 0))
            {
                data.Add(Path.GetRelativePath(RepositoryRoot(), file).Replace('\\', '/'));
            }

            return data;
        }

        public static TheoryData<string> Cultures()
        {
            var data = new TheoryData<string> { "en-US" };
            foreach (string file in Directory.EnumerateFiles(ResourcesFolder(), "Strings.*.resx"))
            {
                data.Add(Path.GetFileNameWithoutExtension(file)["Strings.".Length..]);
            }

            return data;
        }

        [Fact]
        public void UXL012_TheNewScreenSpecsListTheirQuotedStrings()
        {
            string[] expected = ["settings.md", "credits.md", "support.md", "ride.md"];
            List<string> withStrings = SpecFiles().Where(f => QuotedStrings(File.ReadAllLines(f)).Count > 0).Select(Path.GetFileName).ToList()!;

            Assert.All(expected, name => Assert.Contains(name, withStrings));
        }

        [Theory]
        [MemberData(nameof(SpecFilesWithStrings))]
        public void UXL013_EnglishTextInTheResourcesMatchesTheSpec(string specFile)
        {
            string[] lines = File.ReadAllLines(Path.Combine(RepositoryRoot(), specFile));
            Dictionary<string, string> english = EnglishStrings();

            List<string> problems = Mismatches(specFile, QuotedStrings(lines), english).ToList();

            Assert.True(problems.Count == 0, Environment.NewLine + string.Join(Environment.NewLine, problems));
        }

        [Fact]
        public void UXL013_TheCheckFindsAMismatchAndAMissingKey()
        {
            Dictionary<string, string> english = EnglishStrings();

            List<string> problems = Mismatches("sample.md", QuotedStrings(SampleSpec), english).ToList();

            Assert.Equal(2, problems.Count);
            Assert.Contains("sample.md", problems[0], StringComparison.Ordinal);
            Assert.Contains("SCR-SET-001", problems[0], StringComparison.Ordinal);
            Assert.Contains("AppearanceDark", problems[0], StringComparison.Ordinal);
            Assert.Contains("expected \"Night\"", problems[0], StringComparison.Ordinal);
            Assert.Contains("found \"Dark\"", problems[0], StringComparison.Ordinal);
            Assert.Contains("NoSuchKey", problems[1], StringComparison.Ordinal);
            Assert.Contains("does not exist", problems[1], StringComparison.Ordinal);
        }

        [Fact]
        public void UXL013_TheTableEndsAtTheNextHeading()
        {
            List<QuotedString> rows = QuotedStrings(SampleSpec);

            Assert.Equal(["AppearanceLight", "AppearanceDark", "NoSuchKey"], rows.Select(r => r.Key));
            Assert.Equal("SCR-SET-001", rows[0].Rule);
        }

        [Theory]
        [MemberData(nameof(Cultures))]
        public void UXL013_EveryLanguageHasEveryStringWithTheSamePlaceholders(string culture)
        {
            Dictionary<string, string> english = EnglishStrings();
            Dictionary<string, string> translated = culture == "en-US"
                ? english
                : Strings(Path.Combine(ResourcesFolder(), $"Strings.{culture}.resx"));

            var problems = new List<string>();
            foreach ((string key, string text) in english)
            {
                if (!translated.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
                {
                    problems.Add($"{culture}: key {key} has no text.");
                }
                else if (!Placeholders(text).SequenceEqual(Placeholders(value)))
                {
                    problems.Add($"{culture}: key {key} has placeholders [{string.Join(", ", Placeholders(value))}], English has [{string.Join(", ", Placeholders(text))}].");
                }
            }

            Assert.True(problems.Count == 0, Environment.NewLine + string.Join(Environment.NewLine, problems));
        }

        private static IEnumerable<string> Mismatches(string specFile, List<QuotedString> rows, Dictionary<string, string> english)
        {
            foreach (QuotedString row in rows)
            {
                if (!english.TryGetValue(row.Key, out string? actual))
                {
                    yield return $"{specFile}, {row.Rule}: key {row.Key} does not exist in Strings.resx. Expected \"{row.Text}\".";
                }
                else if (!string.Equals(actual, row.Text, StringComparison.Ordinal))
                {
                    yield return $"{specFile}, {row.Rule}: key {row.Key}: expected \"{row.Text}\", found \"{actual}\".";
                }
            }
        }

        internal sealed record QuotedString(string Key, string Text, string Rule);

        private static List<QuotedString> QuotedStrings(string[] lines)
        {
            var rows = new List<QuotedString>();
            bool inTable = false;
            foreach (string line in lines)
            {
                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    inTable = line.Trim().Equals("## Quoted strings", StringComparison.Ordinal);
                    continue;
                }

                if (!inTable)
                {
                    continue;
                }

                Match match = Row().Match(line);
                if (match.Success)
                {
                    rows.Add(new QuotedString(match.Groups["key"].Value, match.Groups["text"].Value.Trim(), match.Groups["rule"].Value.Trim()));
                }
            }

            return rows;
        }

        private static List<string> Placeholders(string text) =>
            PlaceholderPattern().Matches(text).Select(m => m.Value).Order(StringComparer.Ordinal).ToList();

        private static Dictionary<string, string> EnglishStrings() => Strings(Path.Combine(ResourcesFolder(), "Strings.resx"));

        private static Dictionary<string, string> Strings(string path) =>
            XDocument.Load(path).Root!.Elements("data")
                .ToDictionary(d => (string)d.Attribute("name")!, d => (string?)d.Element("value") ?? string.Empty, StringComparer.Ordinal);

        private static IEnumerable<string> SpecFiles() =>
            Directory.EnumerateFiles(Path.Combine(RepositoryRoot(), "docs", "spec"), "*.md", SearchOption.AllDirectories);

        private static string ResourcesFolder() =>
            Path.Combine(RepositoryRoot(), "windows", "src", "Presentation", "GameControllerForZwift", "Resources");

        internal static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "docs", "spec")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("docs/spec was not found above " + AppContext.BaseDirectory);
        }

        [GeneratedRegex(@"^\|\s*`(?<key>[A-Za-z0-9_]+)`\s*\|(?<text>.*)\|\s*(?<rule>[^|]*?)\s*\|\s*$")]
        private static partial Regex Row();

        [GeneratedRegex(@"\{\d+\}")]
        private static partial Regex PlaceholderPattern();
    }
}
