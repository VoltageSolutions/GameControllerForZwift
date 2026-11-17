using GameControllerForZwift.Core.FileSystem;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameControllerForZwift.Core.Configurations
{
    /// <summary>
    /// Stores configurations as JSON files in one folder. All file access goes through <see cref="IFileService"/>.
    /// </summary>
    public sealed partial class ConfigurationStore : IConfigurationStore
    {
        private const string Extension = ".json";

        private readonly IFileService _fileService;
        private readonly ILogger<ConfigurationStore> _logger;
        private readonly TimeProvider _timeProvider;

        public ConfigurationStore(IFileService fileService, string folderPath)
            : this(fileService, folderPath, NullLogger<ConfigurationStore>.Instance, TimeProvider.System)
        {
        }

        public ConfigurationStore(IFileService fileService, string folderPath, ILogger<ConfigurationStore> logger, TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(fileService);
            ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(timeProvider);
            _fileService = fileService;
            FolderPath = folderPath;
            _logger = logger;
            _timeProvider = timeProvider;
        }

        public string FolderPath { get; }

        public event EventHandler? Changed;

        public IReadOnlyList<SavedConfiguration> LoadAll()
        {
            var result = new List<SavedConfiguration>();
            var ids = new HashSet<Guid>();
            foreach (var path in ListFiles().Order(StringComparer.OrdinalIgnoreCase))
            {
                SavedConfiguration configuration;
                try
                {
                    configuration = ReadFile(path);
                }
                catch (ConfigurationFormatException ex)
                {
                    LogSkippedFile(path, ex.Message);
                    continue;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    LogSkippedFile(path, ex.Message);
                    continue;
                }

                // A copy made in Explorer keeps the id; listing it twice would make selection by id ambiguous.
                if (!ids.Add(configuration.Id))
                {
                    LogDuplicateId(path, configuration.Id);
                    continue;
                }
                result.Add(configuration);
            }
            return result;
        }

        public SavedConfiguration ReadFile(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            var json = _fileService.ReadFileContent(filePath);
            return ConfigurationSerializer.Deserialize(json, filePath);
        }

        public SavedConfiguration Save(SavedConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            EnsureFolder();

            var path = configuration.FilePath;
            if (string.IsNullOrEmpty(path))
            {
                var baseName = ConfigurationFileNames.GetBaseName(configuration.Controller.Name, configuration.Name);
                path = Path.Join(FolderPath, ConfigurationFileNames.MakeUnique(baseName, IsTaken));
            }

            var saved = configuration with
            {
                Name = ConfigurationNames.Normalize(configuration.Name),
                Modified = TruncateToSeconds(_timeProvider.GetUtcNow()),
                FilePath = path,
            };
            _fileService.WriteFileContent(path, ConfigurationSerializer.Serialize(saved));
            OnChanged();
            return saved;
        }

        public SavedConfiguration Rename(SavedConfiguration configuration, string newName)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            var name = ConfigurationNames.Normalize(newName);
            if (name.Length == 0)
                throw new ArgumentException("The name cannot be empty.", nameof(newName));

            EnsureFolder();
            var oldPath = configuration.FilePath;
            var baseName = ConfigurationFileNames.GetBaseName(configuration.Controller.Name, name);
            var oldFileName = string.IsNullOrEmpty(oldPath) ? null : Path.GetFileName(oldPath);

            // Keep the current file if its name already fits (for example a rename that only changes case).
            string newPath = oldFileName is not null && string.Equals(oldFileName, baseName + Extension, StringComparison.OrdinalIgnoreCase)
                ? oldPath!
                : Path.Join(FolderPath, ConfigurationFileNames.MakeUnique(baseName,
                    candidate => !string.Equals(candidate, oldFileName, StringComparison.OrdinalIgnoreCase) && IsTaken(candidate)));

            var renamed = configuration with { Name = name, Modified = TruncateToSeconds(_timeProvider.GetUtcNow()), FilePath = newPath };

            // Write the new file before removing the old one, so a failure never loses the configuration.
            _fileService.WriteFileContent(newPath, ConfigurationSerializer.Serialize(renamed));
            if (!string.IsNullOrEmpty(oldPath) && !string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase) && _fileService.FileExists(oldPath))
                _fileService.DeleteFile(oldPath);

            OnChanged();
            return renamed;
        }

        public void Delete(SavedConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            if (!string.IsNullOrEmpty(configuration.FilePath) && _fileService.FileExists(configuration.FilePath))
                _fileService.DeleteFile(configuration.FilePath);
            OnChanged();
        }

        public int CountFiles() => ListFiles().Count;

        public int DeleteAll()
        {
            int deleted = 0;
            foreach (var path in ListFiles())
            {
                try
                {
                    _fileService.DeleteFile(path);
                    deleted++;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    LogDeleteFailed(path, ex.Message);
                }
            }
            OnChanged();
            return deleted;
        }

        public string EnsureFolder()
        {
            if (!_fileService.DirectoryExists(FolderPath))
                _fileService.CreateDirectory(FolderPath);
            return FolderPath;
        }

        public void NotifyExternalChange() => OnChanged();

        private List<string> ListFiles() =>
            _fileService.GetFiles(FolderPath, "*" + Extension)
                .Where(p => string.Equals(Path.GetExtension(p), Extension, StringComparison.OrdinalIgnoreCase))
                .ToList();

        private bool IsTaken(string fileName) => _fileService.FileExists(Path.Join(FolderPath, fileName));

        private static DateTimeOffset TruncateToSeconds(DateTimeOffset value) =>
            new(value.Ticks - (value.Ticks % TimeSpan.TicksPerSecond), value.Offset);

        private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Skipped configuration file {Path}: {Reason}")]
        private partial void LogSkippedFile(string path, string reason);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Skipped configuration file {Path}: another file already uses id {Id}.")]
        private partial void LogDuplicateId(string path, Guid id);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Could not delete configuration file {Path}: {Reason}")]
        private partial void LogDeleteFailed(string path, string reason);
    }
}
