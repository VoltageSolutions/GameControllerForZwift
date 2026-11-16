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
                // Losing the last-used entry is not worth interrupting the user; the Default is selected instead.
                LogWriteFailed(FilePath, ex.Message);
            }
        }

        [LoggerMessage(Level = LogLevel.Warning, Message = "Could not read settings file {Path}: {Reason}")]
        private partial void LogReadFailed(string path, string reason);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Could not write settings file {Path}: {Reason}")]
        private partial void LogWriteFailed(string path, string reason);
    }
}
