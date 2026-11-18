using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace GameControllerForZwift.UnitTests.Localization
{
    /// <summary>
    /// Keeps Resources/Strings.resx and the code that uses it in step (docs/spec/ux/localization.md, UX-L-010 and
    /// UX-L-011).
    /// </summary>
    public partial class StringResourceTests
    {
        private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        // Screens and shell parts that must be localized. A file stays on this list once it is localized.
        private static readonly string[] LocalizedFiles =
        [
            "MainWindow.xaml",
            "Resources/Shell.xaml",
            "Views/RidePage.xaml",
            "Views/ConnectionsPage.xaml",
        ];

        // Attributes that hold text people see or hear.
        private static readonly string[] TextAttributes = ["Text", "Content", "Title", "Header", "ToolTip", "AutomationProperties.Name", "AutomationProperties.HelpText"];

        private static readonly string[] ExpectedLiterals = ["TextBlock Text=\"Hello\"", "Button AutomationProperties.Name=\"Go\"", "Setter ToolTip=\"Tip\""];

        public static TheoryData<string> LocalizedFileData()
        {
            var data = new TheoryData<string>();
            foreach (string file in LocalizedFiles)
            {
                data.Add(file);
            }

            return data;
        }

        [Fact]
        public void UXL002_EveryStringHasEnglishTextAndATranslatorComment()
        {
            List<XElement> strings = ResxStrings().ToList();
            Assert.NotEmpty(strings);

            foreach (XElement data in strings)
            {
                string key = (string)data.Attribute("name")!;
                Assert.False(string.IsNullOrWhiteSpace((string?)data.Element("value")), key + " has no English text.");
                Assert.False(string.IsNullOrWhiteSpace((string?)data.Element("comment")), key + " has no translator comment.");
            }
        }

        [Fact]
        public void UXL010_EveryKeyUsedInCodeOrXamlExists()
        {
            HashSet<string> keys = ResxKeys();
            var missing = new List<string>();

            foreach (string file in SourceFiles("*.xaml"))
            {
                foreach (Match match in XamlKey().Matches(File.ReadAllText(file)))
                {
                    if (!keys.Contains(match.Groups["key"].Value))
                    {
                        missing.Add(Path.GetFileName(file) + ": " + match.Groups["key"].Value);
                    }
                }
            }

            foreach (string file in SourceFiles("*.cs"))
            {
                foreach (Match match in CodeKey().Matches(File.ReadAllText(file)))
                {
                    if (!keys.Contains(match.Groups["key"].Value))
                    {
                        missing.Add(Path.GetFileName(file) + ": " + match.Groups["key"].Value);
                    }
                }
            }

            Assert.Empty(missing);
        }

        [Fact]
        public void UXL010_EveryKeyIsUsed()
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (string file in SourceFiles("*.xaml"))
            {
                foreach (Match match in XamlKey().Matches(File.ReadAllText(file)))
                {
                    used.Add(match.Groups["key"].Value);
                }
            }

            // Code may pick a key with a condition, so any string literal that names a key counts as a use.
            foreach (string file in SourceFiles("*.cs"))
            {
                foreach (Match match in StringLiteral().Matches(File.ReadAllText(file)))
                {
                    used.Add(match.Groups["text"].Value);
                }
            }

            Assert.Empty(ResxKeys().Where(k => !used.Contains(k)).Order(StringComparer.Ordinal));
        }

        [Theory]
        [MemberData(nameof(LocalizedFileData))]
        public void UXL011_LocalizedXamlHasNoLiteralText(string relativePath)
        {
            string path = Path.Combine(ProjectRoot(), relativePath);
            Assert.Contains("xmlns:loc=", File.ReadAllText(path), StringComparison.Ordinal);

            List<string> literals = LiteralText(XDocument.Load(path)).ToList();

            Assert.Empty(literals);
        }

        [Fact]
        public void UXL011_TheLiteralTextCheckFindsLiterals()
        {
            XDocument document = XDocument.Parse("""
                <Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                    <TextBlock Text="Hello" />
                    <TextBlock Text="{loc:Loc Hello}" />
                    <TextBlock Text="&#xE921;" />
                    <Button AutomationProperties.Name="Go" Content="" />
                    <Style><Setter Property="ToolTip" Value="Tip" /><Setter Property="Width" Value="10" /></Style>
                </Grid>
                """);

            Assert.Equal(ExpectedLiterals, LiteralText(document));
        }

        private static IEnumerable<string> LiteralText(XDocument document)
        {
            foreach (XElement element in document.Descendants())
            {
                foreach (XAttribute attribute in element.Attributes())
                {
                    if (TextAttributes.Contains(attribute.Name.LocalName) && IsLiteral(attribute.Value))
                    {
                        yield return $"{element.Name.LocalName} {attribute.Name.LocalName}=\"{attribute.Value}\"";
                    }
                }

                if (element.Name.LocalName == "Setter"
                    && TextAttributes.Contains((string?)element.Attribute("Property"))
                    && IsLiteral((string?)element.Attribute("Value")))
                {
                    yield return $"Setter {(string)element.Attribute("Property")!}=\"{(string)element.Attribute("Value")!}\"";
                }
            }
        }

        // Icon glyphs from the symbol font (the Unicode private use area) are not words, so they need no string.
        private static bool IsLiteral(string? value) =>
            !string.IsNullOrWhiteSpace(value)
            && !value.TrimStart().StartsWith('{')
            && !value.All(c => c is >= '\uE000' and <= '\uF8FF');

        private static IEnumerable<XElement> ResxStrings() =>
            XDocument.Load(Path.Combine(ProjectRoot(), "Resources", "Strings.resx")).Root!.Elements("data");

        private static HashSet<string> ResxKeys() =>
            ResxStrings().Select(d => (string)d.Attribute("name")!).ToHashSet(StringComparer.Ordinal);

        private static IEnumerable<string> SourceFiles(string pattern) =>
            Directory.EnumerateFiles(ProjectRoot(), pattern, SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                    && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal));

        private static string ProjectRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "windows", "src", "Presentation", "GameControllerForZwift")))
            {
                directory = directory.Parent;
            }

            return directory is null
                ? throw new DirectoryNotFoundException("The app project was not found above " + AppContext.BaseDirectory)
                : Path.Combine(directory.FullName, "windows", "src", "Presentation", "GameControllerForZwift");
        }

        [GeneratedRegex(@"\{loc:Loc\s+(?:Key=)?(?<key>[A-Za-z0-9_]+)\s*\}")]
        private static partial Regex XamlKey();

        [GeneratedRegex(@"Localizer\.(?:Get|Format)\(\s*""(?<key>[^""]+)""")]
        private static partial Regex CodeKey();

        [GeneratedRegex(@"""(?<text>[A-Za-z0-9_]+)""")]
        private static partial Regex StringLiteral();
    }
}
