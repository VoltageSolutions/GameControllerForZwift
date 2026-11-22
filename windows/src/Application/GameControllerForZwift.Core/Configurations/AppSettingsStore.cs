using GameControllerForZwift.Core.FileSystem;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GameControllerForZwift.Core.Configurations
{
    /// <summary>
    /// Reads and writes settings.json. Settings this class does not know about are kept as they are.
    /// </summary>
    public sealed partial class AppSettingsStore : IAppSettingsStore
    {
        private const string LastUsedProperty = "lastUsedConfigurations";
        private const string ShowOtherControllersProperty = "showConfigurationsFromOtherControllers";
        private const string AppearanceProperty = "appearance";
        private const string RideTimeoutProperty = "rideTimeoutMinutes";
        private const string RideStatsProperty = "rideStats";
        private const string DonationAskShownProperty = "donationAskShown";
        private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

        private readonly IFileService _fileService;
        private readonly ILogger<AppSettingsStore> _logger;
        private readonly Lock _sync = new();

        public AppSettingsStore(IFileService fileService, string filePath)
            : this(fileService, filePath, NullLogger<AppSettingsStore>.Instance)
        {
        }

        public AppSettingsStore(IFileService fileService, string filePath, ILogger<AppSettingsStore> logger)
        {
            ArgumentNullException.ThrowIfNull(fileService);
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            ArgumentNullException.ThrowIfNull(logger);
            _fileService = fileService;
            FilePath = filePath;
            _logger = logger;
        }

        /// <summary>
        /// Gets the path of settings.json.
        /// </summary>
        public string FilePath { get; }

        public Guid? GetLastUsed(string modelKey)
        {
            ArgumentNullException.ThrowIfNull(modelKey);
            lock (_sync)
            {
                var lastUsed = Load()[LastUsedProperty] as JsonObject;
                var value = lastUsed?[modelKey];
                return value is JsonValue v && v.TryGetValue<string>(out var text) && Guid.TryParse(text, out var id) ? id : null;
            }
        }

        public void SetLastUsed(string modelKey, Guid? configurationId)
        {
            ArgumentNullException.ThrowIfNull(modelKey);
            lock (_sync)
            {
                var root = Load();
                if (root[LastUsedProperty] is not JsonObject lastUsed)
                {
                    lastUsed = new JsonObject();
                    root[LastUsedProperty] = lastUsed;
                }

                if (configurationId is Guid id && id != Guid.Empty)
                    lastUsed[modelKey] = id.ToString("D");
                else if (!lastUsed.Remove(modelKey))
                    return;

                Write(root);
            }
        }

        public void ClearLastUsed()
        {
            lock (_sync)
            {
                var root = Load();
                if (root.Remove(LastUsedProperty))
                    Write(root);
            }
        }

        public bool GetShowOtherControllersConfigurations()
        {
            lock (_sync)
            {
                return Load()[ShowOtherControllersProperty] is JsonValue v && v.TryGetValue<bool>(out var show) && show;
            }
        }

        public void SetShowOtherControllersConfigurations(bool show)
        {
            lock (_sync)
            {
                var root = Load();
                root[ShowOtherControllersProperty] = show;
                Write(root);
            }
        }

        public string? GetAppearance()
        {
            lock (_sync)
            {
                return Load()[AppearanceProperty] is JsonValue v && v.TryGetValue<string>(out var text) && !string.IsNullOrWhiteSpace(text) ? text : null;
            }
        }

        public void SetAppearance(string appearance)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(appearance);
            lock (_sync)
            {
                var root = Load();
                root[AppearanceProperty] = appearance;
                Write(root);
            }
        }

        public int GetRideTimeoutMinutes()
        {
            lock (_sync)
            {
                return Load()[RideTimeoutProperty] is JsonValue v && v.TryGetValue<int>(out var minutes)
                    ? Math.Max(minutes, IAppSettingsStore.MinimumRideTimeoutMinutes)
                    : IAppSettingsStore.DefaultRideTimeoutMinutes;
            }
        }

        public void SetRideTimeoutMinutes(int minutes)
        {
            lock (_sync)
            {
                var root = Load();
                root[RideTimeoutProperty] = Math.Max(minutes, IAppSettingsStore.MinimumRideTimeoutMinutes);
                Write(root);
            }
        }

        public Rides.RideStats GetRideStats()
        {
            lock (_sync)
            {
                if (Load()[RideStatsProperty] is not JsonObject stats)
                {
                    return Rides.RideStats.None;
                }

                int rides = stats["rides"] is JsonValue r && r.TryGetValue<int>(out var rv) ? Math.Max(rv, 0) : 0;
                int shifts = stats["shifts"] is JsonValue s && s.TryGetValue<int>(out var sv) ? Math.Max(sv, 0) : 0;
                DateTimeOffset? since = stats["since"] is JsonValue d && d.TryGetValue<string>(out var text)
                    && DateTimeOffset.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed)
                    ? parsed : null;
                return new Rides.RideStats(rides, shifts, since);
            }
        }

        public void RecordRide(int shifts, DateTimeOffset startedAt)
        {
            lock (_sync)
            {
                Rides.RideStats current = GetRideStats();
                var root = Load();
                root[RideStatsProperty] = new JsonObject
                {
                    ["rides"] = current.Rides + 1,
                    ["shifts"] = current.Shifts + Math.Max(shifts, 0),
                    ["since"] = (current.Since ?? startedAt).ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                };
                Write(root);
            }
        }

        public bool GetDonationAskShown()
        {
            lock (_sync)
            {
                return Load()[DonationAskShownProperty] is JsonValue v && v.TryGetValue<bool>(out var shown) && shown;
            }
        }

        public void SetDonationAskShown()
        {
            lock (_sync)
            {
                var root = Load();
                root[DonationAskShownProperty] = true;
                Write(root);
            }
        }

        private JsonObject Load()
        {
            try
            {
                if (!_fileService.FileExists(FilePath))
                    return new JsonObject();
                return JsonNode.Parse(_fileService.ReadFileContent(FilePath)) as JsonObject ?? new JsonObject();
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                LogReadFailed(FilePath, ex.Message);
                return new JsonObject();
            }
        }

        private void Write(JsonObject root)
        {
            try
            {
                _fileService.WriteFileContent(FilePath, root.ToJsonString(WriteOptions));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Losing a setting is not worth interrupting the user; the app uses the default value instead.
                LogWriteFailed(FilePath, ex.Message);
            }
        }

        [LoggerMessage(Level = LogLevel.Warning, Message = "Could not read settings file {Path}: {Reason}")]
        private partial void LogReadFailed(string path, string reason);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Could not write settings file {Path}: {Reason}")]
        private partial void LogWriteFailed(string path, string reason);
    }
}
