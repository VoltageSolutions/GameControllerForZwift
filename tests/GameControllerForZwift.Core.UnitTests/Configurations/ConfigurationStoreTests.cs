using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.Core.FileSystem;
using Microsoft.Extensions.Logging;

namespace GameControllerForZwift.Core.Tests.Configurations
{
    public sealed class ConfigurationStoreTests : IDisposable
    {
        private static readonly ControllerModel Xbox = new(0x045E, 0x0B13, "Xbox Series X Controller");

        private readonly TempFolder _temp = new();
        private readonly string _folder;
        private readonly CapturingLogger _logger = new();
        private readonly ConfigurationStore _store;

        public ConfigurationStoreTests()
        {
            // The folder does not exist yet, like on first run (CFG-002).
            _folder = _temp.Combine("Profiles");
            _store = new ConfigurationStore(new FileService(), _folder, _logger, TimeProvider.System);
        }

        public void Dispose() => _temp.Dispose();

        private static SavedConfiguration NewConfiguration(string name, ControllerModel? model = null, ZwiftFunction functionForA = ZwiftFunction.Select) =>
            new(Guid.NewGuid(), name, model ?? Xbox,
                ConfigurationMappings.Normalize([new(ControllerInput.A, functionForA)]), DateTimeOffset.MinValue);

        [Fact]
        public void CFG001_CFG002_Save_CreatesFolderAndOneFilePerConfiguration()
        {
            Assert.False(Directory.Exists(_folder));

            _store.Save(NewConfiguration("Climbing"));
            _store.Save(NewConfiguration("Sprint"));

            Assert.True(Directory.Exists(_folder));
            Assert.Equal(2, Directory.GetFiles(_folder, "*.json").Length);
            Assert.True(File.Exists(Path.Join(_folder, "Xbox-Series-X-Controller_Climbing.json")));
        }

        [Fact]
        public void LoadAll_RoundTripsSavedConfigurations()
        {
            var original = NewConfiguration("Climbing", functionForA: ZwiftFunction.RideOn);
            var saved = _store.Save(original);

            var loaded = Assert.Single(_store.LoadAll());

            Assert.Equal(original.Id, loaded.Id);
            Assert.Equal("Climbing", loaded.Name);
            Assert.Equal(Xbox, loaded.Controller);
            Assert.Equal(ZwiftFunction.RideOn, loaded.Mappings[ControllerInput.A]);
            Assert.Equal(saved.FilePath, loaded.FilePath);
            Assert.Equal(saved.Modified, loaded.Modified);
        }

        [Fact]
        public void CFG004_Save_AddsSuffixWhenFileNameIsTaken()
        {
            var first = _store.Save(NewConfiguration("Climbing"));
            var second = _store.Save(NewConfiguration("Climbing"));
            var third = _store.Save(NewConfiguration("Climbing"));

            Assert.EndsWith("Xbox-Series-X-Controller_Climbing.json", first.FilePath, StringComparison.Ordinal);
            Assert.EndsWith("Xbox-Series-X-Controller_Climbing-2.json", second.FilePath, StringComparison.Ordinal);
            Assert.EndsWith("Xbox-Series-X-Controller_Climbing-3.json", third.FilePath, StringComparison.Ordinal);
        }

        [Fact]
        public void CFG022_Save_OverwritesTheSameFileAndUpdatesModified()
        {
            var time = new ManualTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
            var store = new ConfigurationStore(new FileService(), _folder, _logger, time);
            var saved = store.Save(NewConfiguration("Climbing"));

            time.Now = time.Now.AddHours(1);
            var updated = store.Save(saved with { Mappings = ConfigurationMappings.Normalize([new(ControllerInput.B, ZwiftFunction.Brake)]) });

            Assert.Equal(saved.FilePath, updated.FilePath);
            Assert.Equal(time.Now, updated.Modified);
            var loaded = Assert.Single(store.LoadAll());
            Assert.Equal(ZwiftFunction.Brake, loaded.Mappings[ControllerInput.B]);
            Assert.Equal(ZwiftFunction.None, loaded.Mappings[ControllerInput.A]);
            Assert.Equal(time.Now, loaded.Modified);
        }

        [Fact]
        public void CFG003_LoadAll_DoesNotDependOnFileName()
        {
            var saved = _store.Save(NewConfiguration("Climbing"));
            File.Move(saved.FilePath!, Path.Join(_folder, "something else.json"));

            var loaded = Assert.Single(_store.LoadAll());
            Assert.Equal("Climbing", loaded.Name);
            Assert.Equal(saved.Id, loaded.Id);
        }

        [Fact]
        public void CFG005_LoadAll_SkipsBrokenFilesWithWarningAndKeepsThem()
        {
            _store.Save(NewConfiguration("Climbing"));
            var broken = Path.Join(_folder, "broken.json");
            File.WriteAllText(broken, "{ this is not json");

            var loaded = _store.LoadAll();

            Assert.Single(loaded);
            Assert.True(File.Exists(broken));
            Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("broken.json", StringComparison.Ordinal));
        }

        [Fact]
        public void CFG007_LoadAll_SkipsNewerFormatVersion()
        {
            var saved = _store.Save(NewConfiguration("Climbing"));
            var json = File.ReadAllText(saved.FilePath!).Replace("\"formatVersion\": 1", "\"formatVersion\": 99", StringComparison.Ordinal);
            File.WriteAllText(saved.FilePath!, json);

            Assert.Empty(_store.LoadAll());
            Assert.True(File.Exists(saved.FilePath));
            Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("newer version", StringComparison.Ordinal));
        }

        [Fact]
        public void LoadAll_ReturnsEmptyWhenFolderDoesNotExist()
        {
            Assert.Empty(_store.LoadAll());
            Assert.Equal(0, _store.CountFiles());
        }

        [Fact]
        public void LoadAll_ListsOneConfigurationPerId()
        {
            var saved = _store.Save(NewConfiguration("Climbing"));
            File.Copy(saved.FilePath!, Path.Join(_folder, "copy.json"));

            Assert.Single(_store.LoadAll());
        }

        [Fact]
        public void CFG025_Rename_ChangesNameAndFileAndKeepsId()
        {
            var saved = _store.Save(NewConfiguration("Climbing"));

            var renamed = _store.Rename(saved, "  Hills  ");

            Assert.Equal(saved.Id, renamed.Id);
            Assert.Equal("Hills", renamed.Name);
            Assert.EndsWith("Xbox-Series-X-Controller_Hills.json", renamed.FilePath, StringComparison.Ordinal);
            Assert.False(File.Exists(saved.FilePath));
            var loaded = Assert.Single(_store.LoadAll());
            Assert.Equal("Hills", loaded.Name);
            Assert.Equal(saved.Id, loaded.Id);
        }

        [Fact]
        public void CFG025_Rename_OnlyCaseChangeKeepsFile()
        {
            var saved = _store.Save(NewConfiguration("Climbing"));

            var renamed = _store.Rename(saved, "climbing");

            Assert.Equal(saved.FilePath, renamed.FilePath);
            Assert.Equal("climbing", Assert.Single(_store.LoadAll()).Name);
        }

        [Fact]
        public void CFG025_Rename_AvoidsOtherFiles()
        {
            _store.Save(NewConfiguration("Hills"));
            var saved = _store.Save(NewConfiguration("Climbing"));

            var renamed = _store.Rename(saved, "Hills");

            Assert.EndsWith("Xbox-Series-X-Controller_Hills-2.json", renamed.FilePath, StringComparison.Ordinal);
            Assert.Equal(2, _store.LoadAll().Count);
        }

        [Fact]
        public void CFG029_Delete_RemovesTheFile()
        {
            var saved = _store.Save(NewConfiguration("Climbing"));

            _store.Delete(saved);

            Assert.False(File.Exists(saved.FilePath));
            Assert.Empty(_store.LoadAll());
        }

        [Fact]
        public void CFG035_DeleteAll_DeletesEveryJsonFileIncludingBrokenOnes()
        {
            _store.Save(NewConfiguration("Climbing"));
            _store.Save(NewConfiguration("Sprint"));
            File.WriteAllText(Path.Join(_folder, "broken.json"), "nope");
            File.WriteAllText(Path.Join(_folder, "notes.txt"), "keep me");

            Assert.Equal(3, _store.CountFiles());
            Assert.Equal(3, _store.DeleteAll());

            Assert.Equal(0, _store.CountFiles());
            Assert.True(File.Exists(Path.Join(_folder, "notes.txt")));
        }

        [Fact]
        public void Changed_IsRaisedForStoreChanges()
        {
            int changes = 0;
            _store.Changed += (_, _) => changes++;

            var saved = _store.Save(NewConfiguration("Climbing"));
            saved = _store.Rename(saved, "Hills");
            _store.Delete(saved);
            _store.DeleteAll();
            _store.NotifyExternalChange();

            Assert.Equal(5, changes);
        }

        [Fact]
        public void CFG034_EnsureFolder_CreatesTheFolder()
        {
            Assert.Equal(_folder, _store.EnsureFolder());
            Assert.True(Directory.Exists(_folder));
        }

        [Fact]
        public void ReadFile_ThrowsFormatExceptionForBrokenFile()
        {
            var path = _temp.Combine("import.json");
            File.WriteAllText(path, "{ broken");

            Assert.Throws<ConfigurationFormatException>(() => _store.ReadFile(path));
        }

        internal sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
        {
            public DateTimeOffset Now { get; set; } = now;

            public override DateTimeOffset GetUtcNow() => Now;
        }

        internal sealed class CapturingLogger : ILogger<ConfigurationStore>
        {
            public List<(LogLevel Level, string Message)> Entries { get; } = [];

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                Entries.Add((logLevel, formatter(state, exception)));
            }
        }
    }
}
