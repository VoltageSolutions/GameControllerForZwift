using System.Globalization;
using System.Text.RegularExpressions;

namespace GameControllerForZwift.Core.Configurations
{
    /// <summary>
    /// Rules for configuration names (CFG-026 to CFG-028).
    /// </summary>
    public static partial class ConfigurationNames
    {
        /// <summary>
        /// The longest name allowed, after trimming (CFG-026).
        /// </summary>
        public const int MaxLength = 50;

        /// <summary>
        /// The name of the built-in configuration. It is reserved (CFG-027).
        /// </summary>
        public const string DefaultName = "Default";

        /// <summary>
        /// Trims leading and trailing whitespace.
        /// </summary>
        public static string Normalize(string? name) => (name ?? string.Empty).Trim();

        /// <summary>
        /// Returns true if two names are the same once trimmed, ignoring case (CFG-027).
        /// </summary>
        public static bool AreSame(string? first, string? second) =>
            string.Equals(Normalize(first), Normalize(second), StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks a name the user typed. Returns a message to show inline, or null if the name is valid.
        /// </summary>
        /// <param name="name">The name as typed.</param>
        /// <param name="existingNames">The names already used for this controller model, not counting the configuration being renamed.</param>
        public static string? Validate(string? name, IEnumerable<string> existingNames)
        {
            ArgumentNullException.ThrowIfNull(existingNames);
            var trimmed = Normalize(name);
            if (trimmed.Length == 0)
                return "Enter a name.";
            if (trimmed.Length > MaxLength)
                return string.Create(CultureInfo.InvariantCulture, $"Use {MaxLength} characters or fewer.");
            if (AreSame(trimmed, DefaultName))
                return $"'{DefaultName}' is reserved for the built-in configuration.";

            var existing = existingNames.FirstOrDefault(n => AreSame(n, trimmed));
            return existing is null ? null : $"A configuration named '{Normalize(existing)}' already exists for this controller";
        }

        /// <summary>
        /// Suggests a free name based on <paramref name="name"/>, such as "Climbing (2)" (CFG-032).
        /// </summary>
        public static string SuggestUnique(string? name, IEnumerable<string> existingNames)
        {
            ArgumentNullException.ThrowIfNull(existingNames);
            var existing = existingNames.ToList();
            var baseName = Normalize(name);

            // "Climbing (2)" suggests "Climbing (3)", not "Climbing (2) (2)".
            var match = NumberSuffix().Match(baseName);
            if (match.Success)
                baseName = match.Groups["base"].Value.TrimEnd();
            if (baseName.Length == 0)
                baseName = "Configuration";

            if (Validate(baseName, existing) is null)
                return baseName;

            for (int n = 2; ; n++)
            {
                var suffix = string.Create(CultureInfo.InvariantCulture, $" ({n})");
                var stem = baseName.Length + suffix.Length > MaxLength ? baseName[..(MaxLength - suffix.Length)].TrimEnd() : baseName;
                var candidate = stem + suffix;
                if (Validate(candidate, existing) is null)
                    return candidate;
            }
        }

        [GeneratedRegex(@"^(?<base>.*)\s\(\d+\)$")]
        private static partial Regex NumberSuffix();
    }
}
