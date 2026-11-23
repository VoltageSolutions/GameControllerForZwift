using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace GameControllerForZwift.UnitTests.Theme
{
    /// <summary>
    /// Keeps the XAML color tokens equal to the tables in docs/spec/ux/visual-system.md (UX-V-009, UX-V-010). Change a
    /// color in the spec and in Tokens.Dark.xaml or Tokens.Light.xaml together, or these tests fail.
    /// </summary>
    public partial class TokenSpecTests
    {
        private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        private static readonly string[] TokenNames =
            ["bg", "surface", "surface-2", "text", "muted", "accent", "accent-ink", "on-accent", "focus", "ok", "err", "pad", "pad-2", "pad-key", "art-body", "art-shade", "art-line", "art-detail", "art-glyph"];

        public static TheoryData<string> Modes => new() { "Dark", "Light" };

        [Fact]
        public void UXV009_SpecTableListsEveryToken()
        {
            Dictionary<string, (string Dark, string Light)> spec = SpecTokens();

            Assert.Equal(TokenNames, spec.Keys);
        }

        [Theory]
        [MemberData(nameof(Modes))]
        public void UXV010_XamlColorsMatchTheSpec(string mode)
        {
            Dictionary<string, string> expected = SpecTokens().ToDictionary(t => t.Key, t => mode == "Dark" ? t.Value.Dark : t.Value.Light);

            Dictionary<string, string> actual = XamlColors(mode);

            Assert.Equal(expected.OrderBy(t => t.Key, StringComparer.Ordinal), actual.OrderBy(t => t.Key, StringComparer.Ordinal));
        }

        [Theory]
        [MemberData(nameof(Modes))]
        public void UXV010_EveryTokenHasABrush(string mode)
        {
            XDocument document = XDocument.Load(TokensPath(mode));
            HashSet<string> brushes = document.Root!.Elements()
                .Where(e => e.Name.LocalName == "SolidColorBrush")
                .Select(e => (string)e.Attribute(Xaml + "Key")!)
                .ToHashSet(StringComparer.Ordinal);

            foreach (string token in SpecTokens().Keys)
            {
                Assert.Contains("Brush." + token, brushes);
            }
        }

        [Theory]
        [MemberData(nameof(Modes))]
        public void UXV009_ContrastTableMatchesTheXamlColors(string mode)
        {
            Dictionary<string, string> colors = XamlColors(mode);
            List<(string Front, string Back, double Dark, double Light)> rows = SpecContrastRows();
            Assert.NotEmpty(rows);

            foreach ((string front, string back, double dark, double light) in rows)
            {
                double expected = mode == "Dark" ? dark : light;
                double actual = Math.Round(ContrastRatio(colors[front], colors[back]), 2);
                Assert.True(Math.Abs(expected - actual) < 0.006, $"{front} on {back} ({mode}): spec says {expected}, the XAML colors give {actual}.");
            }
        }

        [Theory]
        [MemberData(nameof(Modes))]
        public void UXV009_TextPairsReachAA(string mode)
        {
            Dictionary<string, string> colors = XamlColors(mode);

            foreach (string text in new[] { "text", "muted", "accent-ink" })
            {
                foreach (string background in new[] { "bg", "surface", "surface-2" })
                {
                    double ratio = ContrastRatio(colors[text], colors[background]);
                    Assert.True(ratio >= 4.5, $"{text} on {background} ({mode}) is {ratio:0.00}:1.");
                }
            }

            Assert.True(ContrastRatio(colors["on-accent"], colors["accent"]) >= 4.5);
        }

        [Fact]
        public void HighContrastMapsEveryToken()
        {
            Assert.Equal(
                SpecTokens().Keys.Order(StringComparer.Ordinal),
                UI.WPF.Theme.ThemeService.HighContrastColors().Keys.Order(StringComparer.Ordinal));
        }

        /// <summary>WCAG 2 contrast ratio of two #RRGGBB colors.</summary>
        internal static double ContrastRatio(string a, string b)
        {
            double la = RelativeLuminance(a);
            double lb = RelativeLuminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        private static double RelativeLuminance(string hex)
        {
            double Channel(int offset)
            {
                double c = int.Parse(hex.AsSpan(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
                return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            }

            return (0.2126 * Channel(1)) + (0.7152 * Channel(3)) + (0.0722 * Channel(5));
        }

        private static Dictionary<string, (string Dark, string Light)> SpecTokens()
        {
            var tokens = new Dictionary<string, (string Dark, string Light)>(StringComparer.Ordinal);
            foreach (Match match in TokenRow().Matches(File.ReadAllText(SpecPath())))
            {
                tokens.Add(match.Groups["name"].Value, (match.Groups["dark"].Value, match.Groups["light"].Value));
            }

            return tokens;
        }

        private static List<(string Front, string Back, double Dark, double Light)> SpecContrastRows()
        {
            var rows = new List<(string, string, double, double)>();
            foreach (Match match in ContrastRow().Matches(File.ReadAllText(SpecPath())))
            {
                string? front = ContrastName(match.Groups["front"].Value);
                string? back = ContrastName(match.Groups["back"].Value);
                Assert.True(front is not null && back is not null, "Unknown contrast row: " + match.Value);
                rows.Add((front!, back!,
                    double.Parse(match.Groups["dark"].Value, CultureInfo.InvariantCulture),
                    double.Parse(match.Groups["light"].Value, CultureInfo.InvariantCulture)));
            }

            return rows;
        }

        // A contrast row names tokens in backticks, or uses the words the table uses for the accent fill and the
        // controller illustration.
        private static string? ContrastName(string cell)
        {
            Match token = Regex.Match(cell, "`([a-z0-9-]+)`");
            if (token.Success)
            {
                return token.Groups[1].Value;
            }

            cell = cell.Trim();
            if (cell.StartsWith("accent fill", StringComparison.Ordinal))
            {
                return "accent";
            }

            return cell switch
            {
                "illustration key" => "pad-key",
                "body" => "pad",
                _ => null,
            };
        }

        private static Dictionary<string, string> XamlColors(string mode)
        {
            XDocument document = XDocument.Load(TokensPath(mode));
            return document.Root!.Elements()
                .Where(e => e.Name.LocalName == "Color")
                .ToDictionary(
                    e => ((string)e.Attribute(Xaml + "Key")!)["Color.".Length..],
                    e => e.Value.Trim().ToUpperInvariant(),
                    StringComparer.Ordinal);
        }

        private static string SpecPath() => Path.Combine(RepoRoot(), "docs", "spec", "ux", "visual-system.md");

        private static string TokensPath(string mode) => Path.Combine(RepoRoot(), "windows", "src", "Presentation",
            "GameControllerForZwift", "Resources", "Theme", $"Tokens.{mode}.xaml");

        private static string RepoRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "docs", "spec", "ux", "visual-system.md")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("docs/spec/ux/visual-system.md not found above " + AppContext.BaseDirectory);
        }

        // | `bg` | Screen background | <span ...>#141312</span> | <span ...>#FAF8F4</span> |
        [GeneratedRegex(@"^\| `(?<name>[a-z0-9-]+)` \|.*?>(?<dark>#[0-9A-F]{6})</span>.*?>(?<light>#[0-9A-F]{6})</span> \|\s*$", RegexOptions.Multiline)]
        private static partial Regex TokenRow();

        // | `text` on `bg` | 16.61 | 16.14 |
        [GeneratedRegex(@"^\| (?<front>[^|]+?) on (?<back>[^|]+?) \| (?<dark>\d+\.\d+) \| (?<light>\d+\.\d+) \|\s*$", RegexOptions.Multiline)]
        private static partial Regex ContrastRow();
    }
}
