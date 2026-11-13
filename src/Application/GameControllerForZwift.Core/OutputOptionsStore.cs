using System.Text.Json;

namespace GameControllerForZwift.Core
{
    /// <summary>
    /// Persists <see cref="OutputOptions"/> as a small JSON user-settings file (missing or invalid file / missing keys = defaults).
    /// Kept separate from the profile JSON, which is read-only and lives in the (possibly non-writable) install directory.
    /// </summary>
    public sealed class OutputOptionsStore
    {
        private sealed record Dto(bool? SendToZwiftEmulator, bool? SendToKeyboard);

        private readonly string _path;
        private readonly object _lock = new();

        public OutputOptionsStore(string path) => _path = path;

        /// <summary>Default location: %APPDATA%\GameControllerForZwift\settings.json.</summary>
        public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameControllerForZwift", "settings.json");

        /// <summary>Applies stored values (if any) to <paramref name="options"/>.</summary>
        public void Load(OutputOptions options)
        {
            try
            {
                if (!File.Exists(_path)) return;
                var dto = JsonSerializer.Deserialize<Dto>(File.ReadAllText(_path));
                if (dto == null) return;
                options.SendToZwiftEmulator = dto.SendToZwiftEmulator ?? true;
                options.SendToKeyboard = dto.SendToKeyboard ?? false;
            }
            catch { /* corrupt file: keep defaults */ }
        }

        /// <summary>Writes the current values; best effort (never throws).</summary>
        public void Save(OutputOptions options)
        {
            try
            {
                lock (_lock)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                    File.WriteAllText(_path, JsonSerializer.Serialize(new Dto(options.SendToZwiftEmulator, options.SendToKeyboard), new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch { /* best effort */ }
        }
    }
}
