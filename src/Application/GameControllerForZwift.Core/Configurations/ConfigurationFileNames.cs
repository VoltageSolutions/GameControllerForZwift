using System.Text;

namespace GameControllerForZwift.Core.Configurations
{
    /// <summary>
    /// Builds file names for configurations (CFG-004). The file name is for humans only; loading never depends on it (CFG-003).
    /// </summary>
    public static class ConfigurationFileNames
    {
        /// <summary>
        /// The longest controller part of a file name, so file names stay well inside Windows path limits.
        /// </summary>
        public const int MaxControllerPartLength = 60;

        // Characters Windows does not allow in file names, listed explicitly so the result is the same on every OS.
        private static readonly char[] InvalidChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

        /// <summary>
        /// Returns "&lt;ControllerName&gt;_&lt;ConfigName&gt;" without the extension. Invalid characters and runs of
        /// whitespace become a single '-'.
        /// </summary>
        public static string GetBaseName(string? controllerName, string? configurationName)
        {
            var controller = Sanitize(controllerName, "Controller");
            if (controller.Length > MaxControllerPartLength)
                controller = controller[..MaxControllerPartLength].TrimEnd('-', '.');
            var configuration = Sanitize(configurationName, "Configuration");
            return controller + "_" + configuration;
        }

        /// <summary>
        /// Replaces characters that are invalid in Windows file names with '-' and collapses whitespace to '-'.
        /// </summary>
        public static string Sanitize(string? text, string fallback)
        {
            var builder = new StringBuilder();
            bool lastWasDash = false;
            foreach (var c in (text ?? string.Empty).Trim())
            {
                if (char.IsWhiteSpace(c) || char.IsControl(c) || Array.IndexOf(InvalidChars, c) >= 0)
                {
                    if (!lastWasDash)
                        builder.Append('-');
                    lastWasDash = true;
                }
                else
                {
                    builder.Append(c);
                    lastWasDash = false;
                }
            }

            // Windows drops trailing dots and spaces, which would make two names collide unexpectedly.
            var result = builder.ToString().TrimEnd('.');
            return result.Length == 0 || result.All(ch => ch == '-') ? fallback : result;
        }

        /// <summary>
        /// Returns a file name that is not taken: "base.json", then "base-2.json", "base-3.json", and so on (CFG-004).
        /// </summary>
        /// <param name="baseName">The name without the extension.</param>
        /// <param name="isTaken">Returns true if a file name (with extension) is already in use.</param>
        public static string MakeUnique(string baseName, Func<string, bool> isTaken)
        {
            ArgumentNullException.ThrowIfNull(isTaken);
            var candidate = baseName + ".json";
            for (int n = 2; isTaken(candidate); n++)
                candidate = $"{baseName}-{n.ToString(System.Globalization.CultureInfo.InvariantCulture)}.json";
            return candidate;
        }
    }
}
