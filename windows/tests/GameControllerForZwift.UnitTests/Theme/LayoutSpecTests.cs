using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace GameControllerForZwift.UnitTests.Theme
{
    /// <summary>
    /// Keeps the layout, type, spacing, and radius tokens equal to the tables in docs/spec/ux/visual-system.md
    /// (UX-S-007, UX-S-015). Change a value in the spec and in the Layout.*.xaml or Space.xaml files together, or these
    /// tests fail. A failure names the spec rule, the token, and both values.
    /// </summary>
    public partial class LayoutSpecTests
    {
        private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        private static readonly string[] Classes = ["Phone", "Tablet", "Desktop"];

        public static TheoryData<string> SizeClasses => new() { "Phone", "Tablet", "Desktop" };

        // The spec item, the token, and the part of the token that holds the value.
        private static readonly (string Item, string Key, string Part, string Rule)[] LayoutItems =
        [
            ("Page side margin", "Layout.PagePadding", "Left", "UX-V-051"),
            ("Page side margin", "Layout.PagePadding", "Right", "UX-V-051"),
            ("Page top padding", "Layout.PagePadding", "Top", "UX-V-051"),
            ("Page bottom padding", "Layout.PagePadding", "Bottom", "UX-V-051"),
            ("Gap between sections", "Layout.SectionGap", "", "UX-V-051"),
            ("Gap from a group label to its rows", "Layout.LabelGap", "", "UX-V-051"),
            ("Gap between list rows", "Layout.RowGap", "", "UX-V-051"),
            ("Gap between cards", "Layout.CardGap", "", "UX-V-051"),
            ("Card padding", "Layout.CardPadding", "Left", "UX-V-051"),
            ("Card padding", "Layout.CardPadding", "Top", "UX-V-051"),
            ("Gap between buttons in a group", "Layout.ButtonGap", "", "UX-V-031"),
            ("Status chip height", "Layout.ChipHeight", "", "UX-V-056"),
            ("Status chip padding, left and right", "Layout.ChipPadding", "Left", "UX-V-056"),
        ];

        private static readonly (string Role, string Key)[] TypeRoles =
        [
            ("Display", "Layout.DisplaySize"),
            ("Readout (big number)", "Layout.ReadoutSize"),
            ("Screen title", "Layout.PageTitleSize"),
            ("Title", "Layout.TitleSize"),
            ("Body", "Layout.BodySize"),
            ("Caption", "Layout.CaptionSize"),
        ];

        [Theory]
        [MemberData(nameof(SizeClasses))]
        public void UXV051_LayoutTableMatchesTheLayoutTokens(string sizeClass)
        {
            Dictionary<string, string[]> table = Table("## Layout and spacing", 4);
            Dictionary<string, string> tokens = Tokens($"Layout.{sizeClass}.xaml");
            int column = Array.IndexOf(Classes, sizeClass);

            foreach ((string item, string key, string part, string rule) in LayoutItems)
            {
                Assert.True(table.ContainsKey(item), $"{rule}: the spec table has no row \"{item}\".");
                double expected = Parse(table[item][column]);
                double actual = Part(tokens, key, part);
                Assert.True(expected == actual, $"{rule} {key}{(part.Length > 0 ? "." + part : string.Empty)} ({sizeClass}): spec \"{item}\" says {expected}, Layout.{sizeClass}.xaml has {actual}.");
            }
        }

        [Theory]
        [MemberData(nameof(SizeClasses))]
        public void UXV051_RowPaddingMatchesTheSpec(string sizeClass)
        {
            // "12, 20" is the vertical and then the horizontal padding.
            string cell = Table("## Layout and spacing", 4)["Row padding (vertical, horizontal)"][Array.IndexOf(Classes, sizeClass)];
            double[] spec = cell.Split(',').Select(Parse).ToArray();
            Dictionary<string, string> tokens = Tokens($"Layout.{sizeClass}.xaml");

            Assert.True(spec[0] == Part(tokens, "Layout.RowPadding", "Top") && spec[0] == Part(tokens, "Layout.RowPadding", "Bottom"),
                $"UX-V-051 Layout.RowPadding vertical ({sizeClass}): spec says {spec[0]}, Layout.{sizeClass}.xaml has {Part(tokens, "Layout.RowPadding", "Top")}.");
            Assert.True(spec[1] == Part(tokens, "Layout.RowPadding", "Left") && spec[1] == Part(tokens, "Layout.RowPadding", "Right"),
                $"UX-V-051 Layout.RowPadding horizontal ({sizeClass}): spec says {spec[1]}, Layout.{sizeClass}.xaml has {Part(tokens, "Layout.RowPadding", "Left")}.");
        }

        [Theory]
        [MemberData(nameof(SizeClasses))]
        public void UXV050_TypeTableMatchesTheTypeTokens(string sizeClass)
        {
            Dictionary<string, string[]> table = Table("## Type", 3);
            Dictionary<string, string> tokens = Tokens($"Layout.{sizeClass}.xaml");

            // Desktop uses the tablet column.
            int column = sizeClass == "Phone" ? 0 : 1;
            foreach ((string role, string key) in TypeRoles)
            {
                Assert.True(table.ContainsKey(role), $"UX-V-050: the type table has no row \"{role}\".");
                string[] range = table[role][column].Split(" to ", StringSplitOptions.TrimEntries);
                double low = Parse(range[0]);
                double high = Parse(range[^1]);
                double actual = Part(tokens, key, string.Empty);
                Assert.True(actual >= low && actual <= high, $"UX-V-050 {key} ({sizeClass}): spec \"{role}\" says {table[role][column]}, Layout.{sizeClass}.xaml has {actual}.");
            }
        }

        [Fact]
        public void UXS007_TheThreeLayoutFilesHoldTheSameKeys()
        {
            string[] phone = Tokens("Layout.Phone.xaml").Keys.Order(StringComparer.Ordinal).ToArray();

            Assert.Equal(phone, Tokens("Layout.Tablet.xaml").Keys.Order(StringComparer.Ordinal));
            Assert.Equal(phone, Tokens("Layout.Desktop.xaml").Keys.Order(StringComparer.Ordinal));
        }

        [Fact]
        public void UXV015_SpacingScaleMatchesTheSpaceTokens()
        {
            string row = Regex.Match(SpecText(), @"^\| Spacing scale \| (?<values>[^|]+) \| UX-V-015 \|", RegexOptions.Multiline).Groups["values"].Value;
            double[] spec = row.Split(',').Select(Parse).ToArray();
            Assert.NotEmpty(spec);

            Dictionary<string, string> space = Tokens("Space.xaml");
            double[] actual = space.Keys.Where(k => k.StartsWith("Space.", StringComparison.Ordinal)).Select(k => Parse(space[k])).Order().ToArray();

            Assert.True(spec.SequenceEqual(actual), $"UX-V-015 Space.*: spec says {string.Join(", ", spec)}, Space.xaml has {string.Join(", ", actual)}.");
        }

        [Theory]
        [InlineData("Radius.Row", "Rows", "UX-V-017")]
        [InlineData("Radius.Card", "Cards", "UX-V-018")]
        [InlineData("Radius.Panel", "Panels", "UX-V-019")]
        public void UXV017_RadiiFallInTheSpecRange(string key, string item, string rule)
        {
            string cell = Regex.Match(SpecText(), $@"^\| {item} \| Radius (?<value>[^|]+?) \| {rule} \|", RegexOptions.Multiline).Groups["value"].Value;
            Assert.False(string.IsNullOrEmpty(cell), $"{rule}: the spec has no \"{item}\" radius row.");

            double[] range = Regex.Matches(cell, @"\d+").Select(m => Parse(m.Value)).ToArray();
            double actual = Parse(Tokens("Space.xaml")[key]);

            Assert.True(actual >= range[0] && actual <= range[^1], $"{rule} {key}: spec says \"{cell}\", Space.xaml has {actual}.");
        }

        // The rows of the table that follows a heading, keyed by the first cell.
        private static Dictionary<string, string[]> Table(string heading, int columns)
        {
            string text = SpecText();
            int start = text.IndexOf(heading + "\n", StringComparison.Ordinal);
            Assert.True(start >= 0, "The spec has no section " + heading);
            string section = text[start..];
            int next = section.IndexOf("\n## ", heading.Length, StringComparison.Ordinal);
            if (next > 0)
            {
                section = section[..next];
            }

            var rows = new Dictionary<string, string[]>(StringComparer.Ordinal);
            foreach (Match match in TableRow().Matches(section))
            {
                string[] cells = match.Groups["cells"].Value.Split('|', StringSplitOptions.TrimEntries);
                if (cells.Length == columns && !cells[0].StartsWith("---", StringComparison.Ordinal) && cells[0] is not ("Item" or "Role"))
                {
                    rows.TryAdd(cells[0], cells[1..]);
                }
            }

            return rows;
        }

        // The value of each x:Key in a resource dictionary, as text.
        private static Dictionary<string, string> Tokens(string file)
        {
            XDocument document = XDocument.Load(RepoPaths.Resource(file));
            return document.Root!.Elements()
                .Where(e => e.Attribute(Xaml + "Key") is not null)
                .ToDictionary(e => (string)e.Attribute(Xaml + "Key")!, e => e.Value.Trim(), StringComparer.Ordinal);
        }

        // A number, or one side of a Thickness ("20" for all sides, or "left,top,right,bottom").
        private static double Part(Dictionary<string, string> tokens, string key, string part)
        {
            Assert.True(tokens.TryGetValue(key, out string? value), $"The tokens have no key {key}.");
            string[] sides = value.Split(',', StringSplitOptions.TrimEntries);
            if (part.Length == 0 || sides.Length == 1)
            {
                return Parse(sides[0]);
            }

            int index = part switch { "Left" => 0, "Top" => 1, "Right" => 2, _ => 3 };
            return Parse(sides[index]);
        }

        private static string SpecText() => File.ReadAllText(RepoPaths.VisualSystemSpec()).ReplaceLineEndings("\n");

        private static double Parse(string text) => double.Parse(text.Trim(), CultureInfo.InvariantCulture);

        [GeneratedRegex(@"^\|(?<cells>.+)\|\s*$", RegexOptions.Multiline)]
        private static partial Regex TableRow();
    }
}
