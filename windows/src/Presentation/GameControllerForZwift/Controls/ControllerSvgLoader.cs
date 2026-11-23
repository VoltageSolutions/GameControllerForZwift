using GameControllerForZwift.Core.Controller;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;
using System.Xml.Linq;

namespace GameControllerForZwift.UI.WPF.Controls
{
    /// <summary>One controller drawing built from a wireframe SVG, with the parts the diagram animates.</summary>
    internal sealed class ControllerSvgDrawing
    {
        public required Canvas Root { get; init; }
        public required Dictionary<string, FrameworkElement> ById { get; init; }
        public TranslateTransform? LeftStick { get; init; }
        public TranslateTransform? RightStick { get; init; }
        public Path? LeftTriggerFill { get; init; }
        public Path? RightTriggerFill { get; init; }
        public Rect LeftTriggerFillBounds { get; init; }
        public Rect RightTriggerFillBounds { get; init; }
        /// <summary>Stick cap travel in drawing units, from <c>data-travel</c> on the movable group (default 8).</summary>
        public double LeftTravel { get; init; } = ControllerSvgLoader.DefaultTravel;
        public double RightTravel { get; init; } = ControllerSvgLoader.DefaultTravel;
        /// <summary>Theme brush key a shape rests at, by id, when it is not <c>Brush.pad-key</c> (flat-art drawings).</summary>
        public required Dictionary<string, string> RestBrushes { get; init; }
        /// <summary>Glyph strokes by the id of the key they mark (<c>data-for</c>); they turn to the ink color when that key is lit.</summary>
        public required Dictionary<string, Path> GlyphFor { get; init; }
    }

    /// <summary>
    /// Builds WPF elements from the restricted SVG subset in <c>docs/assets/controllers</c> (viewBox, g, path, rect, circle,
    /// ellipse, line, polyline, polygon, text, transform, class, id, stroke-dasharray). Classes map to theme brushes
    /// (<c>DynamicResource</c> references) so the drawing follows the appearance. Ids give lookup by <see cref="ControllerInput"/> name.
    /// </summary>
    internal static partial class ControllerSvgLoader
    {
        public const string LeftStickId = "LeftThumbstick";
        public const string RightStickId = "RightThumbstick";
        public const string LeftTriggerFillId = "LeftTriggerFill";
        public const string RightTriggerFillId = "RightTriggerFill";
        public const double DefaultTravel = 8.0;
        public const string GlyphBrushKey = "Brush.art-glyph";
        public const string GlyphLitBrushKey = "Brush.on-accent";

        /// <summary>Folder and file name (without extension) of the drawing for each kind that has its own.</summary>
        private static readonly Dictionary<ControllerKind, string> Files = new()
        {
            [ControllerKind.Generic] = "generic/generic",
            [ControllerKind.PlayStationDualShock4] = "playstation-dualshock4/playstation-dualshock4",
        };

        /// <summary>Kinds without a drawing of their own show the Generic drawing.</summary>
        /// <summary>Path of the SVG relative to the Controllers asset folder, such as <c>xbox/xbox.svg</c>.</summary>
        public static string RelativePath(ControllerKind kind) =>
            (Files.TryGetValue(kind, out string? file) ? file : Files[ControllerKind.Generic]) + ".svg";

        public static Uri ResourceUri(ControllerKind kind) =>
            new($"pack://application:,,,/{typeof(ControllerSvgLoader).Assembly.GetName().Name};component/Assets/Controllers/{RelativePath(kind)}", UriKind.Absolute);

        /// <summary>Loads the embedded SVG for a kind, or null when the resource is missing or unreadable.</summary>
        public static ControllerSvgDrawing? TryLoad(ControllerKind kind)
        {
            try
            {
                var info = Application.GetResourceStream(ResourceUri(kind));
                if (info is null)
                    return null;

                using Stream stream = info.Stream;
                return Load(XDocument.Load(stream));
            }
            catch (Exception ex) when (ex is IOException or System.Xml.XmlException or UriFormatException or InvalidOperationException)
            {
                return null;
            }
        }

        public static ControllerSvgDrawing Load(XDocument document)
        {
            XElement svg = document.Root ?? throw new InvalidDataException("The SVG has no root element.");
            double[] box = Numbers(svg.Attribute("viewBox")?.Value);
            if (box.Length != 4)
                throw new InvalidDataException("The SVG needs a viewBox.");

            var styles = StyleInfo.Parse(svg.Descendants().FirstOrDefault(e => e.Name.LocalName == "style")?.Value ?? "");
            var context = new Context(styles);

            var content = new Canvas { RenderTransform = new TranslateTransform(-box[0], -box[1]) };
            foreach (XElement child in svg.Elements())
                AddElement(content, child, context, inheritedClasses: [], inIconGroup: false);

            var root = new Canvas { Width = box[2], Height = box[3], SnapsToDevicePixels = false };
            root.Children.Add(content);

            return new ControllerSvgDrawing
            {
                Root = root,
                ById = context.ById,
                LeftStick = context.LeftStick,
                RightStick = context.RightStick,
                LeftTriggerFill = context.LeftFill,
                RightTriggerFill = context.RightFill,
                LeftTriggerFillBounds = context.LeftFill?.Data?.Bounds ?? Rect.Empty,
                RightTriggerFillBounds = context.RightFill?.Data?.Bounds ?? Rect.Empty,
                LeftTravel = context.LeftTravel,
                RightTravel = context.RightTravel,
                RestBrushes = context.RestBrushes,
                GlyphFor = context.GlyphFor,
            };
        }

        private sealed class Context(StyleInfo styles)
        {
            public StyleInfo Styles { get; } = styles;
            public Dictionary<string, FrameworkElement> ById { get; } = [];
            public TranslateTransform? LeftStick { get; set; }
            public TranslateTransform? RightStick { get; set; }
            public Path? LeftFill { get; set; }
            public Path? RightFill { get; set; }
            public double LeftTravel { get; set; } = DefaultTravel;
            public double RightTravel { get; set; } = DefaultTravel;
            public Dictionary<string, string> RestBrushes { get; } = [];
            public Dictionary<string, Path> GlyphFor { get; } = [];
        }

        private static void AddElement(Canvas parent, XElement source, Context context, HashSet<string> inheritedClasses, bool inIconGroup)
        {
            string tag = source.Name.LocalName;
            string? id = (string?)source.Attribute("id");
            var classes = new HashSet<string>(
                ((string?)source.Attribute("class"))?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [],
                StringComparer.Ordinal);

            FrameworkElement? element;
            bool isFill = id is LeftTriggerFillId or RightTriggerFillId;

            if (tag == "g")
            {
                var group = new Canvas();
                // A group of track-class icons (the small marks inside the center controls) draws as glyphs, not as wells.
                bool icons = inIconGroup || classes.Contains("track");
                var next = new HashSet<string>(inheritedClasses.Concat(classes), StringComparer.Ordinal);
                foreach (XElement child in source.Elements())
                    AddElement(group, child, context, next, icons);
                element = group;
            }
            else if (tag == "text")
            {
                element = BuildText(source);
            }
            else
            {
                Geometry? geometry = BuildGeometry(source, tag);
                if (geometry is null)
                    return; // title, desc, style, defs and anything else outside the subset
                var path = new Path { Data = geometry };
                var all = new HashSet<string>(inheritedClasses.Concat(classes), StringComparer.Ordinal);
                if (context.Styles.IsFlatArt)
                    ApplyArtStyle(path, all, context.Styles, (string?)source.Attribute("fill") == "none");
                else
                    ApplyStyle(path, all, classes.Count == 0 ? inIconGroup : inIconGroup && !classes.Contains("key"), isFill, source, tag, context.Styles);
                element = path;
                if (context.Styles.IsFlatArt)
                {
                    if (!string.IsNullOrEmpty(id) && classes.Contains("key")) context.RestBrushes[id] = "Brush.art-shade";
                    if (!string.IsNullOrEmpty(id) && classes.Contains("cap")) context.RestBrushes[id] = "Brush.art-shade";
                    if ((string?)source.Attribute("data-for") is { Length: > 0 } target && classes.Contains("glyph"))
                        context.GlyphFor[target] = path;
                }
                if (id == LeftTriggerFillId) context.LeftFill = path;
                if (id == RightTriggerFillId) context.RightFill = path;
            }

            TransformGroup transform = BuildTransform((string?)source.Attribute("transform"), dropTriggerScale: isFill);
            if (id is LeftStickId or RightStickId)
            {
                var move = new TranslateTransform();
                transform.Children.Add(move);
                double travel = double.TryParse((string?)source.Attribute("data-travel"), NumberStyles.Float, CultureInfo.InvariantCulture, out double dt) && dt > 0 ? dt : DefaultTravel;
                if (id == LeftStickId) { context.LeftStick = move; context.LeftTravel = travel; }
                else { context.RightStick = move; context.RightTravel = travel; }
            }
            if (transform.Children.Count > 0)
                element.RenderTransform = transform;

            if (!string.IsNullOrEmpty(id))
                context.ById[id] = element;

            parent.Children.Add(element);
        }

        /// <summary>Flat-art drawings (a style block with an <c>ol</c> class): each class maps to its <c>Brush.art-*</c> tokens, widths from the SVG.</summary>
        private static void ApplyArtStyle(Path path, HashSet<string> classes, StyleInfo styles, bool hasOwnNoFill)
        {
            (string? fill, string? stroke, string cls) = classes switch
            {
                _ when classes.Contains("ol") => (null, "Brush.art-line", "ol"),
                _ when classes.Contains("body-fill") => ("Brush.art-body", null, "body-fill"),
                _ when classes.Contains("key") => ("Brush.art-shade", "Brush.art-line", "key"),
                _ when classes.Contains("well") => ("Brush.art-shade", "Brush.art-detail", "well"),
                _ when classes.Contains("stick") => ("Brush.art-shade", "Brush.art-line", "stick"),
                _ when classes.Contains("cap") => ("Brush.art-shade", "Brush.art-detail", "cap"),
                _ when classes.Contains("glyph") => (null, "Brush.art-glyph", "glyph"),
                _ => (null, "Brush.art-line", ""),
            };

            // A shape that sets fill="none" itself (the touchpad, drawn as a key outline over the body fill) stays unfilled.
            if (fill is not null && !hasOwnNoFill) path.SetResourceReference(Shape.FillProperty, fill);
            if (stroke is not null)
            {
                path.SetResourceReference(Shape.StrokeProperty, stroke);
                path.StrokeThickness = styles.Width(cls) ?? 1;
                path.StrokeLineJoin = PenLineJoin.Round;
            }
            if (cls == "glyph")
                path.StrokeStartLineCap = path.StrokeEndLineCap = PenLineCap.Round;

            if (classes.Contains("free") && path.StrokeThickness > 0)
                path.StrokeDashArray = new DoubleCollection((styles.Dash("free") ?? [4, 3]).Select(d => d / path.StrokeThickness));
        }

        private static void ApplyStyle(Path path, HashSet<string> classes, bool asGlyph, bool isFill, XElement source, string tag, StyleInfo styles)
        {
            const string stroke2 = "Brush.pad-2";

            if (isFill)
            {
                path.SetResourceReference(Shape.FillProperty, "Brush.accent");
                path.Opacity = 0.85;
                return;
            }

            bool isLine = tag is "line" or "polyline";
            double thickness = 2;

            if (classes.Contains("body"))
            {
                thickness = 3;
                if (!isLine) path.SetResourceReference(Shape.FillProperty, "Brush.pad");
                path.SetResourceReference(Shape.StrokeProperty, stroke2);
            }
            else if (classes.Contains("glyph") || asGlyph)
            {
                thickness = 1.5;
                if (styles.GlyphIsFilled && !asGlyph)
                {
                    path.SetResourceReference(Shape.FillProperty, "Brush.muted");
                    thickness = 0;
                }
                else
                {
                    path.SetResourceReference(Shape.StrokeProperty, "Brush.muted");
                    path.StrokeStartLineCap = path.StrokeEndLineCap = PenLineCap.Round;
                }
            }
            else if (classes.Contains("cap"))
            {
                path.SetResourceReference(Shape.FillProperty, "Brush.pad-key");
                path.SetResourceReference(Shape.StrokeProperty, "Brush.pad");
            }
            else if (classes.Contains("track"))
            {
                if (!isLine) path.SetResourceReference(Shape.FillProperty, stroke2);
                path.SetResourceReference(Shape.StrokeProperty, stroke2);
                if (styles.TrackThickness is double t) thickness = t;
            }
            else
            {
                // key, free, or unclassed: a pressable shape that rests at the key color.
                if (!isLine) path.SetResourceReference(Shape.FillProperty, "Brush.pad-key");
                path.SetResourceReference(Shape.StrokeProperty, stroke2);
            }

            path.StrokeThickness = thickness;
            path.StrokeLineJoin = PenLineJoin.Round;

            double[]? dash = Numbers((string?)source.Attribute("stroke-dasharray")) is { Length: > 0 } own ? own : null;
            if (dash is null && classes.Contains("free")) dash = styles.Dash("free") ?? [4, 3];
            if (dash is null && classes.Contains("track")) dash = styles.Dash("track");
            if (dash is not null && thickness > 0)
                path.StrokeDashArray = new DoubleCollection(dash.Select(d => d / thickness));
        }

        private static TextBlock BuildText(XElement source)
        {
            double x = Num(source, "x"), y = Num(source, "y");
            var text = new TextBlock
            {
                Text = source.Value.Trim(),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
            };
            text.SetResourceReference(TextBlock.ForegroundProperty, "Brush.muted");
            // text-anchor middle, dominant-baseline central.
            text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(text, x - text.DesiredSize.Width / 2);
            Canvas.SetTop(text, y - text.DesiredSize.Height / 2);
            return text;
        }

        private static Geometry? BuildGeometry(XElement e, string tag)
        {
            switch (tag)
            {
                case "path":
                    return Geometry.Parse((string?)e.Attribute("d") ?? "");
                case "rect":
                    {
                        double rx = Num(e, "rx"), ry = e.Attribute("ry") is null ? rx : Num(e, "ry");
                        return new RectangleGeometry(new Rect(Num(e, "x"), Num(e, "y"), Num(e, "width"), Num(e, "height")), rx, ry);
                    }
                case "circle":
                    return new EllipseGeometry(new Point(Num(e, "cx"), Num(e, "cy")), Num(e, "r"), Num(e, "r"));
                case "ellipse":
                    return new EllipseGeometry(new Point(Num(e, "cx"), Num(e, "cy")), Num(e, "rx"), Num(e, "ry"));
                case "line":
                    return new LineGeometry(new Point(Num(e, "x1"), Num(e, "y1")), new Point(Num(e, "x2"), Num(e, "y2")));
                case "polyline":
                case "polygon":
                    {
                        double[] n = Numbers((string?)e.Attribute("points"));
                        if (n.Length < 4) return null;
                        var figure = new PathFigure { StartPoint = new Point(n[0], n[1]), IsClosed = tag == "polygon", IsFilled = tag == "polygon" };
                        for (int i = 2; i + 1 < n.Length; i += 2)
                            figure.Segments.Add(new LineSegment(new Point(n[i], n[i + 1]), true));
                        return new PathGeometry([figure]);
                    }
                default:
                    return null;
            }
        }

        /// <summary>
        /// SVG lists transforms outermost first; WPF applies a group's children first to last, so the list is reversed.
        /// A trigger fill ships collapsed with <c>translate(0 c) scale(1 0) translate(0 -c)</c>; the scale and the
        /// vertical pivots are dropped because the diagram reveals the fill with a clip instead.
        /// </summary>
        internal static TransformGroup BuildTransform(string? value, bool dropTriggerScale)
        {
            var steps = new List<Transform>();
            foreach (Match m in TransformRegex().Matches(value ?? ""))
            {
                double[] n = Numbers(m.Groups[2].Value);
                switch (m.Groups[1].Value)
                {
                    case "translate" when n.Length >= 1:
                        {
                            double ty = n.Length > 1 ? n[1] : 0;
                            if (dropTriggerScale && n[0] == 0 && ty != 0) break;
                            steps.Add(new TranslateTransform(n[0], ty));
                            break;
                        }
                    case "scale" when n.Length >= 1:
                        if (!dropTriggerScale)
                            steps.Add(new ScaleTransform(n[0], n.Length > 1 ? n[1] : n[0]));
                        break;
                    case "rotate" when n.Length >= 1:
                        steps.Add(n.Length >= 3 ? new RotateTransform(n[0], n[1], n[2]) : new RotateTransform(n[0]));
                        break;
                    case "matrix" when n.Length == 6:
                        steps.Add(new MatrixTransform(n[0], n[1], n[2], n[3], n[4], n[5]));
                        break;
                }
            }

            steps.Reverse();
            var group = new TransformGroup();
            foreach (Transform step in steps)
                group.Children.Add(step);
            return group;
        }

        private static double Num(XElement e, string name) =>
            double.TryParse((string?)e.Attribute(name), NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0;

        private static double[] Numbers(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? []
                : [.. value.Split([' ', ',', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0)];

        [GeneratedRegex(@"(\w+)\s*\(([^)]*)\)")]
        private static partial Regex TransformRegex();

        [GeneratedRegex(@"([^{}]+)\{([^}]*)\}")]
        private static partial Regex RuleRegex();

        [GeneratedRegex(@"stroke-dasharray\s*:\s*([^;}]+)")]
        private static partial Regex DashRegex();

        [GeneratedRegex(@"stroke-width\s*:\s*([0-9.]+)")]
        private static partial Regex WidthRegex();

        /// <summary>The few facts read from the SVG's preview <c>&lt;style&gt;</c> block; colors in it are ignored (the theme owns them).</summary>
        private sealed class StyleInfo
        {
            private readonly Dictionary<string, string> _declarations = [];

            public bool GlyphIsFilled { get; private set; } = true;
            public double? TrackThickness { get; private set; }

            /// <summary>True for the flat controller art, whose style block declares the <c>ol</c> (body outline) class.</summary>
            public bool IsFlatArt => _declarations.ContainsKey("ol");

            public double? Width(string cls) =>
                _declarations.TryGetValue(cls, out string? d) && WidthRegex().Matches(d) is { Count: > 0 } m
                    ? double.Parse(m[^1].Groups[1].Value, CultureInfo.InvariantCulture)
                    : null;

            public double[]? Dash(string cls) =>
                _declarations.TryGetValue(cls, out string? d) && DashRegex().Matches(d) is { Count: > 0 } m
                    ? Numbers(m[^1].Groups[1].Value)
                    : null;

            public static StyleInfo Parse(string css)
            {
                var info = new StyleInfo();
                foreach (Match rule in RuleRegex().Matches(css))
                {
                    foreach (string selector in rule.Groups[1].Value.Split(','))
                    {
                        string s = selector.Trim();
                        int dot = s.LastIndexOf('.');
                        if (dot < 0 || s.StartsWith('#')) continue;
                        string cls = s[(dot + 1)..];
                        info._declarations[cls] = info._declarations.GetValueOrDefault(cls, "") + ";" + rule.Groups[2].Value;
                    }
                }

                if (info._declarations.TryGetValue("glyph", out string? glyph))
                    info.GlyphIsFilled = !Regex.IsMatch(glyph, @"fill\s*:\s*none");
                if (info._declarations.TryGetValue("track", out string? track) && WidthRegex().Matches(track) is { Count: > 0 } w)
                    info.TrackThickness = double.Parse(w[^1].Groups[1].Value, CultureInfo.InvariantCulture);
                return info;
            }
        }
    }
}
