using System.Reflection;

namespace GameControllerForZwift.UI.WPF
{
    /// <summary>The version of the running app (SCR-CRD-001, SCR-SET-008).</summary>
    public static class AppVersion
    {
        /// <summary>The version number, for example 0.6.0, without any build metadata.</summary>
        public static string Current { get; } = Read(typeof(AppVersion).Assembly);

        internal static string Read(Assembly assembly)
        {
            string? informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            string? version = informational ?? assembly.GetName().Version?.ToString(3);
            if (string.IsNullOrWhiteSpace(version))
            {
                return "0.0.0";
            }

            int plus = version.IndexOf('+', StringComparison.Ordinal);
            return plus >= 0 ? version[..plus] : version;
        }
    }
}
