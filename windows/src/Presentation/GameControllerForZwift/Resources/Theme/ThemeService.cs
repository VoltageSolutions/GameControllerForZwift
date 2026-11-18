using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace GameControllerForZwift.UI.WPF.Theme
{
    /// <summary>The appearance choices in Settings (SCR-SET-001).</summary>
    public enum Appearance
    {
        Light,
        Dark,
        MatchWindows,
    }

    /// <summary>The token set the app shows.</summary>
    public enum TokenSet
    {
        Light,
        Dark,
        HighContrast,
    }

    /// <summary>
    /// Applies the color tokens (docs/spec/ux/visual-system.md) to the application. It keeps one token dictionary as the
    /// last merged dictionary, so the tokens win over the Fluent theme, and replaces it when the appearance, the Windows
    /// app mode, or a Windows contrast theme changes.
    /// </summary>
    public sealed class ThemeService : IDisposable
    {
        private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        private readonly Application _application;
        private readonly Func<bool> _systemUsesLightTheme;
        private readonly Func<bool> _highContrast;
        private readonly Action<ThemeMode> _setThemeMode;
        private ResourceDictionary? _tokens;
        private bool _listening;

        public ThemeService()
            : this(Application.Current, ReadSystemUsesLightTheme, () => SystemParameters.HighContrast, null)
        {
        }

        /// <param name="application">The application whose resources get the tokens.</param>
        /// <param name="systemUsesLightTheme">Reads whether Windows apps use light mode.</param>
        /// <param name="highContrast">Reads whether a Windows contrast theme is on.</param>
        /// <param name="setThemeMode">Sets the Fluent theme mode. By default it sets the application's theme mode.</param>
        public ThemeService(Application application, Func<bool> systemUsesLightTheme, Func<bool> highContrast, Action<ThemeMode>? setThemeMode)
        {
            ArgumentNullException.ThrowIfNull(application);
            ArgumentNullException.ThrowIfNull(systemUsesLightTheme);
            ArgumentNullException.ThrowIfNull(highContrast);
            _application = application;
            _systemUsesLightTheme = systemUsesLightTheme;
            _highContrast = highContrast;
            _setThemeMode = setThemeMode ?? (mode => application.ThemeMode = mode);
        }

        /// <summary>The appearance chosen in Settings. Match Windows by default (SCR-SET-001).</summary>
        public Appearance Appearance { get; private set; } = Appearance.MatchWindows;

        /// <summary>The token set shown now.</summary>
        public TokenSet CurrentTokens { get; private set; } = TokenSet.Dark;

        /// <summary>Raised after the token set changed.</summary>
        public event EventHandler? TokensChanged;

        /// <summary>
        /// Picks the token set. A Windows contrast theme always wins, because the app must then use system colors.
        /// Match Windows follows the Windows app mode.
        /// </summary>
        public static TokenSet Resolve(Appearance appearance, bool systemUsesLightTheme, bool highContrast)
        {
            if (highContrast)
            {
                return TokenSet.HighContrast;
            }

            return appearance switch
            {
                Appearance.Light => TokenSet.Light,
                Appearance.Dark => TokenSet.Dark,
                _ => systemUsesLightTheme ? TokenSet.Light : TokenSet.Dark,
            };
        }

        /// <summary>The Fluent theme mode that matches an appearance.</summary>
        public static ThemeMode ToThemeMode(Appearance appearance) => appearance switch
        {
            Appearance.Light => ThemeMode.Light,
            Appearance.Dark => ThemeMode.Dark,
            _ => ThemeMode.System,
        };

        /// <summary>
        /// Builds the token dictionary for a token set. The light and dark sets load from XAML. The contrast set maps
        /// every token to the Windows system color with the same role.
        /// </summary>
        public static ResourceDictionary CreateTokens(TokenSet tokens) => tokens switch
        {
            TokenSet.Light => new ResourceDictionary { Source = new Uri("pack://application:,,,/GameControllerForZwift;component/Resources/Theme/Tokens.Light.xaml") },
            TokenSet.Dark => new ResourceDictionary { Source = new Uri("pack://application:,,,/GameControllerForZwift;component/Resources/Theme/Tokens.Dark.xaml") },
            _ => CreateHighContrastTokens(),
        };

        /// <summary>The system color that stands in for each token while a Windows contrast theme is on.</summary>
        public static IReadOnlyDictionary<string, Color> HighContrastColors() => new Dictionary<string, Color>
        {
            ["bg"] = SystemColors.WindowColor,
            ["surface"] = SystemColors.WindowColor,
            ["surface-2"] = SystemColors.ControlColor,
            ["text"] = SystemColors.WindowTextColor,
            ["muted"] = SystemColors.GrayTextColor,
            ["accent"] = SystemColors.HighlightColor,
            ["accent-ink"] = SystemColors.HotTrackColor,
            ["on-accent"] = SystemColors.HighlightTextColor,
            ["focus"] = SystemColors.HighlightColor,
            ["ok"] = SystemColors.WindowTextColor,
            ["err"] = SystemColors.WindowTextColor,
            ["pad"] = SystemColors.ControlColor,
            ["pad-2"] = SystemColors.ControlColor,
            ["pad-key"] = SystemColors.WindowColor,
        };

        private static ResourceDictionary CreateHighContrastTokens()
        {
            var dictionary = new ResourceDictionary();
            foreach ((string token, Color color) in HighContrastColors())
            {
                dictionary["Color." + token] = color;
                dictionary["Brush." + token] = new SolidColorBrush(color);
            }

            return dictionary;
        }

        /// <summary>
        /// Sets the appearance and shows the matching tokens. The first call also starts following Windows changes.
        /// </summary>
        public void Apply(Appearance appearance)
        {
            Appearance = appearance;
            _setThemeMode(ToThemeMode(appearance));
            Refresh();

            if (!_listening)
            {
                SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
                _listening = true;
            }
        }

        /// <summary>Shows the token set for the current appearance and Windows state.</summary>
        public void Refresh()
        {
            TokenSet next = Resolve(Appearance, _systemUsesLightTheme(), _highContrast());
            bool changed = _tokens is null || next != CurrentTokens;
            if (changed)
            {
                _tokens = CreateTokens(next);
            }

            // Setting the theme mode can add the Fluent dictionary again, so the tokens always move back to the end.
            Collection<ResourceDictionary> merged = _application.Resources.MergedDictionaries;
            RemoveTokenDictionaries(merged);
            merged.Add(_tokens!);
            CurrentTokens = next;

            if (changed)
            {
                TokensChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // App.xaml merges the dark tokens so the designer and the first frame have colors. They are replaced here.
        private static void RemoveTokenDictionaries(Collection<ResourceDictionary> merged)
        {
            for (int i = merged.Count - 1; i >= 0; i--)
            {
                if (merged[i].Contains("Color.bg"))
                {
                    merged.RemoveAt(i);
                }
            }
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color or UserPreferenceCategory.Accessibility)
            {
                _application.Dispatcher.BeginInvoke(new Action(Refresh));
            }
        }

        private static bool ReadSystemUsesLightTheme()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            return key?.GetValue("AppsUseLightTheme") is not int value || value != 0;
        }

        public void Dispose()
        {
            if (_listening)
            {
                SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
                _listening = false;
            }
        }
    }
}
