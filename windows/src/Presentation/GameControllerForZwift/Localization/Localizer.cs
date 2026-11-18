using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace GameControllerForZwift.UI.WPF.Localization
{
    /// <summary>
    /// Looks up UI text in Resources/Strings.resx for the current UI culture (docs/spec/ux/localization.md). XAML binds
    /// to the indexer through <see cref="LocExtension"/>, so changing the culture updates every bound string.
    /// </summary>
    public sealed class Localizer : INotifyPropertyChanged
    {
        /// <summary>Shown in place of a string whose key does not exist, so a missing string is easy to spot.</summary>
        public const string MissingPrefix = "!";

        private static readonly ResourceManager Strings =
            new("GameControllerForZwift.Resources.Strings", typeof(Localizer).Assembly);

        private Localizer()
        {
        }

        /// <summary>The one instance the app and its XAML use.</summary>
        public static Localizer Instance { get; } = new();

        /// <summary>The culture strings are looked up in. It starts as the Windows display language.</summary>
        public CultureInfo Culture { get; private set; } = CultureInfo.CurrentUICulture;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>The string for <paramref name="key"/> in <see cref="Culture"/>.</summary>
        public string this[string key] => Lookup(key, Culture);

        /// <summary>The string for <paramref name="key"/> in the current culture.</summary>
        public static string Get(string key) => Instance[key];

        /// <summary>
        /// The string for <paramref name="key"/> with its placeholders filled. Numbers and dates use the culture's
        /// formats (UX-L-005).
        /// </summary>
        public static string Format(string key, params object?[] args) =>
            string.Format(Instance.Culture, Instance[key], args);

        /// <summary>The string for <paramref name="key"/> in <paramref name="culture"/>, falling back to English.</summary>
        public static string Lookup(string key, CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(key);
            return Strings.GetString(key, culture) ?? MissingPrefix + key;
        }

        /// <summary>Switches the culture and refreshes every string bound through <see cref="LocExtension"/>.</summary>
        public void SetCulture(CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(culture);
            Culture = culture;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Culture)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }
    }
}
