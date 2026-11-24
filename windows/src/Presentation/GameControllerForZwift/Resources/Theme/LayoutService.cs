using System.Windows;

namespace GameControllerForZwift.UI.WPF.Theme
{
    /// <summary>The size classes of the style system (docs/spec/ux/style-system.md).</summary>
    public enum SizeClass
    {
        Phone,
        Tablet,
        Desktop,
    }

    /// <summary>
    /// Applies the layout tokens for a size class to the application. It keeps one layout dictionary in the merged
    /// dictionaries and replaces it when the navigation layout changes, so every screen that reads a <c>Layout.*</c>
    /// key with <c>DynamicResource</c> follows. This is the one place that chooses the size class (UX-S-004).
    /// </summary>
    public sealed class LayoutService
    {
        private const string Marker = "Layout.PagePadding";

        private readonly Application _application;
        private ResourceDictionary? _layout;

        /// <summary>Creates a layout service for the current application.</summary>
        public LayoutService()
            : this(Application.Current)
        {
        }

        /// <summary>Creates a layout service for an application.</summary>
        public LayoutService(Application application)
        {
            ArgumentNullException.ThrowIfNull(application);
            _application = application;
        }

        /// <summary>The size class shown now.</summary>
        public SizeClass Current { get; private set; } = SizeClass.Desktop;

        /// <summary>Maps the navigation layout to a size class: bottom bar is phone, rail is tablet, sidebar is desktop (UX-S-005).</summary>
        public static SizeClass ForLayout(NavLayout layout) => layout switch
        {
            NavLayout.BottomBar => SizeClass.Phone,
            NavLayout.Rail => SizeClass.Tablet,
            _ => SizeClass.Desktop,
        };

        /// <summary>The pack URI of the layout dictionary of a size class.</summary>
        public static Uri SourceFor(SizeClass sizeClass) =>
            new($"pack://application:,,,/GameControllerForZwift;component/Resources/Layout.{sizeClass}.xaml");

        /// <summary>Loads the layout dictionary of a size class.</summary>
        public static ResourceDictionary Create(SizeClass sizeClass) => new() { Source = SourceFor(sizeClass) };

        /// <summary>Shows the layout tokens that match a navigation layout.</summary>
        public void Apply(NavLayout layout) => Apply(ForLayout(layout));

        /// <summary>Shows the layout tokens of a size class. Nothing changes when they are already shown.</summary>
        public void Apply(SizeClass sizeClass)
        {
            if (_layout is not null && sizeClass == Current)
            {
                return;
            }

            var merged = _application.Resources.MergedDictionaries;
            for (int i = merged.Count - 1; i >= 0; i--)
            {
                if (merged[i].Contains(Marker))
                {
                    merged.RemoveAt(i);
                }
            }

            _layout = Create(sizeClass);
            merged.Add(_layout);
            Current = sizeClass;
        }
    }
}
