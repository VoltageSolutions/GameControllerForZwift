using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace GameControllerForZwift.UnitTests.Theme
{
    /// <summary>
    /// Keeps the screens on the style system (UX-S-012 to UX-S-014, UX-S-016): a screen file sets no raw size, spacing,
    /// radius, font size, font weight, or color. It uses tokens and components. A value that is not a style decision,
    /// such as 0, Auto, a star size, or a binding, is fine. An exception goes in <c>ScreenStyleAllowlist.txt</c> with a
    /// reason.
    /// </summary>
    public partial class ScreenStyleTests
    {
        private static readonly HashSet<string> SizeAttributes = new(StringComparer.Ordinal)
        {
            "Margin", "Padding", "BorderThickness", "FontSize", "FontWeight", "CornerRadius", "Height", "Width", "MinHeight", "MinWidth", "MaxHeight", "MaxWidth", "LineHeight",
        };

        private static readonly HashSet<string> ColorAttributes = new(StringComparer.Ordinal)
        {
            "Background", "Foreground", "Fill", "Stroke", "BorderBrush", "Color", "OpacityMask",
        };

        private sealed record Violation(string File, int Line, string Attribute, string Value)
        {
            public override string ToString() => $"{File}:{Line} {Attribute}=\"{Value}\"";
        }

        private sealed record AllowedValue(string File, string Attribute, string Value, string Reason);

        [Fact]
        public void UXS012_ScreensSetNoRawValues()
        {
            List<Violation> found = Violations();
            List<AllowedValue> allowed = Allowlist();

            List<Violation> notAllowed = found.Where(v => !allowed.Any(a => Matches(a, v))).ToList();

            Assert.True(notAllowed.Count == 0,
                "UX-S-012: raw values in screens. Use a token or a component, or add an allowlist entry with a reason.\n" + string.Join("\n", notAllowed));
        }

        [Fact]
        public void UXS014_EveryAllowlistEntryHasAReasonAndMatchesAValue()
        {
            List<Violation> found = Violations();

            foreach (AllowedValue entry in Allowlist())
            {
                Assert.False(string.IsNullOrWhiteSpace(entry.Reason), $"UX-S-014: the allowlist entry for {entry.File} {entry.Attribute}={entry.Value} has no reason.");
                Assert.True(found.Any(v => Matches(entry, v)), $"UX-S-014: the allowlist entry {entry.File} {entry.Attribute}={entry.Value} matches no raw value. Remove it.");
            }
        }

        [Fact]
        public void UXS016_TheTestSeesTheScreenFiles()
        {
            List<string> files = ScreenFiles().Select(Path.GetFileName).OfType<string>().ToList();

            Assert.Contains("MainWindow.xaml", files);
            Assert.Contains("RidePage.xaml", files);
            Assert.Contains("ControllerSetupPage.xaml", files);
            Assert.Contains("MessageDialogWindow.xaml", files);
        }

        [Theory]
        [InlineData("Margin", "8,0,0,0", true)]
        [InlineData("Margin", "0,0,0,0", false)]
        [InlineData("Margin", "{DynamicResource Layout.PagePadding}", false)]
        [InlineData("Width", "*", false)]
        [InlineData("Width", "2*", false)]
        [InlineData("Width", "Auto", false)]
        [InlineData("Width", "120", true)]
        [InlineData("FontSize", "16", true)]
        [InlineData("FontWeight", "Bold", true)]
        [InlineData("CornerRadius", "{StaticResource Radius.Card}", false)]
        [InlineData("Fill", "#ED7600", true)]
        [InlineData("Foreground", "White", true)]
        [InlineData("Background", "Transparent", false)]
        [InlineData("Foreground", "{DynamicResource Brush.text}", false)]
        public void UXS012_TheRuleClassifiesValues(string attribute, string value, bool isRaw)
        {
            Assert.Equal(isRaw, IsRaw(attribute, value));
        }

        [Fact]
        public void UXS016_AFailureNamesTheFileLineAttributeAndValue()
        {
            const string xaml = """
                <Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                  <TextBlock FontSize="17" Margin="0" />
                  <Style><Setter Property="Padding" Value="8" /></Style>
                  <Border Background="#FFFFFF" Width="{StaticResource Space.8}" />
                </Grid>
                """;

            List<Violation> found = Scan(XDocument.Parse(xaml, LoadOptions.SetLineInfo), "Views/Sample.xaml");

            Assert.Equal(
                ["Views/Sample.xaml:2 FontSize=\"17\"", "Views/Sample.xaml:3 Padding=\"8\"", "Views/Sample.xaml:4 Background=\"#FFFFFF\""],
                found.Select(v => v.ToString()));
        }

        private static bool Matches(AllowedValue entry, Violation violation) =>
            string.Equals(entry.File, violation.File, StringComparison.OrdinalIgnoreCase)
            && entry.Attribute == violation.Attribute
            && entry.Value == violation.Value;

        // A value is raw when it is a literal number, weight, or color that a token or a component should hold.
        private static bool IsRaw(string attribute, string value)
        {
            value = value.Trim();
            if (value.StartsWith('{'))
            {
                return false;
            }

            if (ColorAttributes.Contains(attribute))
            {
                return !value.Equals("Transparent", StringComparison.Ordinal);
            }

            if (!SizeAttributes.Contains(attribute))
            {
                return false;
            }

            return !(value is "Auto" or "*" or "NaN" || Zero().IsMatch(value) || Star().IsMatch(value));
        }

        private static List<Violation> Violations()
        {
            string root = RepoPaths.App();
            var found = new List<Violation>();
            foreach (string path in ScreenFiles())
            {
                found.AddRange(Scan(XDocument.Load(path, LoadOptions.SetLineInfo), Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/')));
            }

            return found;
        }

        private static List<Violation> Scan(XDocument document, string file)
        {
            var found = new List<Violation>();
            foreach (XElement element in document.Descendants())
            {
                if (element.Name.LocalName == "Setter")
                {
                    string? property = (string?)element.Attribute("Property");
                    string? value = (string?)element.Attribute("Value");
                    if (property is not null && value is not null && IsRaw(property.Split('.')[^1], value))
                    {
                        found.Add(new Violation(file, ((System.Xml.IXmlLineInfo)element).LineNumber, property, value));
                    }

                    continue;
                }

                foreach (XAttribute attribute in element.Attributes())
                {
                    // Namespace declarations, design-time values, and attached properties are not style values.
                    if (attribute.IsNamespaceDeclaration || attribute.Name.Namespace != XNamespace.None || attribute.Name.LocalName.Contains('.'))
                    {
                        continue;
                    }

                    if (IsRaw(attribute.Name.LocalName, attribute.Value))
                    {
                        found.Add(new Violation(file, ((System.Xml.IXmlLineInfo)attribute).LineNumber, attribute.Name.LocalName, attribute.Value));
                    }
                }
            }

            return found;
        }

        private static IEnumerable<string> ScreenFiles()
        {
            string root = RepoPaths.App();
            return Directory.EnumerateFiles(Path.Combine(root, "Views"), "*.xaml")
                .Concat(Directory.EnumerateFiles(Path.Combine(root, "Dialogs"), "*.xaml"))
                .Append(Path.Combine(root, "MainWindow.xaml"))
                .Order(StringComparer.Ordinal);
        }

        private static List<AllowedValue> Allowlist()
        {
            var entries = new List<AllowedValue>();
            foreach (string line in File.ReadAllLines(RepoPaths.TestSource("Theme/ScreenStyleAllowlist.txt")))
            {
                if (line.Length == 0 || line.TrimStart().StartsWith('#'))
                {
                    continue;
                }

                string[] parts = line.Split('|', 4, StringSplitOptions.TrimEntries);
                Assert.True(parts.Length == 4, "UX-S-014: an allowlist line is \"file | attribute | value | reason\": " + line);
                entries.Add(new AllowedValue(parts[0], parts[1], parts[2], parts[3]));
            }

            return entries;
        }

        [GeneratedRegex(@"^[0,.\s]+$")]
        private static partial Regex Zero();

        [GeneratedRegex(@"^\d*\.?\d*\*$")]
        private static partial Regex Star();
    }
}
