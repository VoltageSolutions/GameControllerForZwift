using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameControllerForZwift.Core.Configurations
{
    /// <summary>
    /// Watches the config folder and tells the store when files change outside the app (CFG-006). Bursts of events are
    /// debounced into one notification, which is posted to the synchronization context the watcher was created with (the
    /// UI thread in the app). The app's own writes also produce events; listeners refresh idempotently, so the extra
    /// notification changes nothing.
    /// </summary>
    public sealed partial class ConfigurationFolderWatcher : IDisposable
    {
        private readonly IConfigurationStore _store;
        private readonly SynchronizationContext? _context;
        private readonly TimeSpan _debounce;
        private readonly ILogger<ConfigurationFolderWatcher> _logger;
        private readonly Lock _sync = new();
        private readonly Timer _timer;
        private FileSystemWatcher? _watcher;
        private bool _disposed;

        public ConfigurationFolderWatcher(IConfigurationStore store, SynchronizationContext? context)
            : this(store, context, TimeSpan.FromMilliseconds(400), NullLogger<ConfigurationFolderWatcher>.Instance)
        {
        }

        public ConfigurationFolderWatcher(IConfigurationStore store, SynchronizationContext? context, TimeSpan debounce, ILogger<ConfigurationFolderWatcher> logger)
        {
            ArgumentNullException.ThrowIfNull(store);
            ArgumentNullException.ThrowIfNull(logger);
            _store = store;
            _context = context;
            _debounce = debounce;
            _logger = logger;
            _timer = new Timer(_ => Raise(), null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Gets a value indicating whether the folder is being watched. The folder is created on first save (CFG-002), so
        /// watching starts as soon as it exists.
        /// </summary>
        public bool IsWatching
        {
            get
            {
                lock (_sync)
                    return _watcher is not null;
            }
        }

        /// <summary>
        /// Starts watching if the folder exists, and retries whenever the store reports a change.
        /// </summary>
        public void Start()
        {
            _store.Changed -= OnStoreChanged;
            _store.Changed += OnStoreChanged;
            TryStartWatching();
        }

        private void OnStoreChanged(object? sender, EventArgs e) => TryStartWatching();

        private void TryStartWatching()
        {
            lock (_sync)
            {
                if (_disposed || _watcher is not null || !Directory.Exists(_store.FolderPath))
                    return;

                try
                {
                    var watcher = new FileSystemWatcher(_store.FolderPath, "*.json")
                    {
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                        IncludeSubdirectories = false,
                    };
                    watcher.Created += OnFileEvent;
                    watcher.Changed += OnFileEvent;
                    watcher.Deleted += OnFileEvent;
                    watcher.Renamed += OnFileEvent;
                    watcher.Error += OnError;
                    watcher.EnableRaisingEvents = true;
                    _watcher = watcher;
                }
                catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
                {
                    LogWatchFailed(_store.FolderPath, ex.Message);
                }
            }
        }

        private void OnFileEvent(object sender, FileSystemEventArgs e)
        {
            lock (_sync)
            {
                if (!_disposed)
                    _timer.Change(_debounce, Timeout.InfiniteTimeSpan);
            }
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            // The buffer overflowed or the folder went away: drop the watcher, refresh, and start again when possible.
            LogWatchFailed(_store.FolderPath, e.GetException().Message);
            lock (_sync)
            {
                StopWatching();
                if (!_disposed)
                    _timer.Change(_debounce, Timeout.InfiniteTimeSpan);
            }
        }

        private void Raise()
        {
            lock (_sync)
            {
                if (_disposed)
                    return;
            }

            if (_context is null)
                _store.NotifyExternalChange();
            else
                _context.Post(_ => _store.NotifyExternalChange(), null);
        }

        private void StopWatching()
        {
            if (_watcher is null)
                return;
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed)
                    return;
                _disposed = true;
                StopWatching();
            }
            _store.Changed -= OnStoreChanged;
            _timer.Dispose();
        }

        [LoggerMessage(Level = LogLevel.Warning, Message = "Could not watch the configurations folder {Path}: {Reason}")]
        private partial void LogWatchFailed(string path, string reason);
    }
}
