using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.Core.FileSystem;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameControllerForZwift.Core.Tests.Configurations
{
    /// <summary>
    /// CFG-006: changes made to the config folder outside the app are noticed.
    /// </summary>
    public sealed class ConfigurationFolderWatcherTests : IDisposable
    {
        private static readonly TimeSpan Short = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

        private readonly TempFolder _temp = new();
        private readonly ConfigurationStore _store;

        public ConfigurationFolderWatcherTests()
        {
            _store = new ConfigurationStore(new FileService(), _temp.Combine("Profiles"));
        }

        public void Dispose() => _temp.Dispose();

        private ConfigurationFolderWatcher CreateWatcher() =>
            new(_store, null, Short, NullLogger<ConfigurationFolderWatcher>.Instance);

        [Fact]
        public void CFG006_StartWithoutFolder_DoesNotWatchUntilFolderExists()
        {
            using var watcher = CreateWatcher();

            watcher.Start();

            Assert.False(watcher.IsWatching);
        }

        [Fact]
        public void CFG006_StartWithFolder_Watches()
        {
            Directory.CreateDirectory(_store.FolderPath);
            using var watcher = CreateWatcher();

            watcher.Start();

            Assert.True(watcher.IsWatching);
        }

        [Fact]
        public void CFG006_StartTwice_StillWatchesOnce()
        {
            Directory.CreateDirectory(_store.FolderPath);
            using var watcher = CreateWatcher();

            watcher.Start();
            watcher.Start();

            Assert.True(watcher.IsWatching);
        }

        [Fact]
        public void CFG006_ExternalFileChange_RaisesChangedAfterDebounce()
        {
            Directory.CreateDirectory(_store.FolderPath);
            using var raised = new ManualResetEventSlim();
            _store.Changed += (_, _) => raised.Set();
            using var watcher = CreateWatcher();
            watcher.Start();

            File.WriteAllText(Path.Join(_store.FolderPath, "outside.json"), "{}");

            Assert.True(raised.Wait(Wait));
        }

        [Fact]
        public void CFG006_ChangeEventPostsToTheGivenContext()
        {
            Directory.CreateDirectory(_store.FolderPath);
            var context = new RecordingContext();
            using var raised = new ManualResetEventSlim();
            _store.Changed += (_, _) => raised.Set();
            using var watcher = new ConfigurationFolderWatcher(_store, context, Short, NullLogger<ConfigurationFolderWatcher>.Instance);
            watcher.Start();

            File.WriteAllText(Path.Join(_store.FolderPath, "outside.json"), "{}");

            Assert.True(raised.Wait(Wait));
            Assert.True(context.PostCount > 0);
        }

        [Fact]
        public void CFG006_FolderCreatedLater_WatchingStartsOnStoreChange()
        {
            using var watcher = CreateWatcher();
            watcher.Start();
            Assert.False(watcher.IsWatching);

            _store.Save(new SavedConfiguration(Guid.NewGuid(), "Climbing", new ControllerModel(1, 2, "Pad"), ConfigurationMappings.Normalize([]), DateTimeOffset.UtcNow));

            Assert.True(watcher.IsWatching);
        }

        [Fact]
        public void CFG006_Dispose_StopsWatchingAndIsRepeatable()
        {
            Directory.CreateDirectory(_store.FolderPath);
            var watcher = CreateWatcher();
            watcher.Start();

            watcher.Dispose();
            watcher.Dispose();

            Assert.False(watcher.IsWatching);
        }

        [Fact]
        public void CFG006_StartAfterDispose_DoesNotWatch()
        {
            Directory.CreateDirectory(_store.FolderPath);
            var watcher = CreateWatcher();
            watcher.Dispose();

            watcher.Start();

            Assert.False(watcher.IsWatching);
        }

        [Fact]
        public void Constructor_NullStore_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ConfigurationFolderWatcher(null!, null));
        }

        private sealed class RecordingContext : SynchronizationContext
        {
            private int _posts;

            public int PostCount => Volatile.Read(ref _posts);

            public override void Post(SendOrPostCallback d, object? state)
            {
                Interlocked.Increment(ref _posts);
                d(state);
            }
        }
    }
}
