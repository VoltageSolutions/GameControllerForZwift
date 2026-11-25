using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.Core.FileSystem;
using System.Text.Json.Nodes;

namespace GameControllerForZwift.Core.Tests.Configurations
{
    public sealed class AppSettingsStoreTests : IDisposable
    {
        private readonly TempFolder _temp = new();
        private readonly string _path;

        public AppSettingsStoreTests()
        {
            _path = _temp.Combine("app", "settings.json");
        }

        public void Dispose() => _temp.Dispose();

        [Fact]
        public void CFG017_LastUsed_PersistsAcrossInstances()
        {
            var id = Guid.NewGuid();
            new AppSettingsStore(new FileService(), _path).SetLastUsed("045E:0B13", id);

            var reloaded = new AppSettingsStore(new FileService(), _path);

            Assert.Equal(id, reloaded.GetLastUsed("045E:0B13"));
            Assert.Null(reloaded.GetLastUsed("NAME:OTHER"));
        }

        [Fact]
        public void CFG017_LastUsed_IsMissingWithoutSettingsFile()
        {
            Assert.Null(new AppSettingsStore(new FileService(), _path).GetLastUsed("045E:0B13"));
            Assert.False(File.Exists(_path));
        }

        [Fact]
        public void CFG039_ShowOtherControllers_IsOffByDefaultAndPersistsAcrossInstances()
        {
            var store = new AppSettingsStore(new FileService(), _path);
            Assert.False(store.GetShowOtherControllersConfigurations());

            store.SetLastUsed("045E:0B13", Guid.NewGuid());
            store.SetShowOtherControllersConfigurations(true);
            var reloaded = new AppSettingsStore(new FileService(), _path);

            Assert.True(reloaded.GetShowOtherControllersConfigurations());
            Assert.NotNull(reloaded.GetLastUsed("045E:0B13"));
            Assert.Contains("\"showConfigurationsFromOtherControllers\": true", File.ReadAllText(_path), StringComparison.Ordinal);

            reloaded.SetShowOtherControllersConfigurations(false);
            Assert.False(new AppSettingsStore(new FileService(), _path).GetShowOtherControllersConfigurations());
        }

        [Fact]
        public void SCRSET002_Appearance_IsMissingUntilSavedAndPersists()
        {
            var store = new AppSettingsStore(new FileService(), _path);
            Assert.Null(store.GetAppearance());

            store.SetAppearance("Dark");

            Assert.Equal("Dark", new AppSettingsStore(new FileService(), _path).GetAppearance());
        }

        [Fact]
        public void SCRSET012_RideTimeout_DefaultsToTenAndPersists()
        {
            var store = new AppSettingsStore(new FileService(), _path);
            Assert.Equal(10, store.GetRideTimeoutMinutes());

            store.SetRideTimeoutMinutes(25);

            Assert.Equal(25, new AppSettingsStore(new FileService(), _path).GetRideTimeoutMinutes());
        }

        [Theory]
        [InlineData(4)]
        [InlineData(0)]
        [InlineData(-3)]
        public void SCRSET012_RideTimeout_NeverGoesBelowFive(int minutes)
        {
            var store = new AppSettingsStore(new FileService(), _path);

            store.SetRideTimeoutMinutes(minutes);

            Assert.Equal(5, store.GetRideTimeoutMinutes());
        }

        [Fact]
        public void SCRSET012_RideTimeout_AFileValueBelowFiveIsRaised()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, """{ "rideTimeoutMinutes": 1 }""");

            Assert.Equal(5, new AppSettingsStore(new FileService(), _path).GetRideTimeoutMinutes());
        }

        [Fact]
        public void SCRRIDE003_RideStats_StartEmptyAndAddEachRide()
        {
            var store = new AppSettingsStore(new FileService(), _path);
            Assert.Equal(GameControllerForZwift.Core.Rides.RideStats.None, store.GetRideStats());

            var first = new DateTimeOffset(2026, 9, 3, 7, 0, 0, TimeSpan.Zero);
            store.RecordRide(40, first);
            store.RecordRide(-5, first.AddDays(10));

            var reloaded = new AppSettingsStore(new FileService(), _path).GetRideStats();
            Assert.Equal(2, reloaded.Rides);
            Assert.Equal(40, reloaded.Shifts);
            Assert.Equal(first, reloaded.Since);
        }

        [Fact]
        public void SCRRIDE003_RideStats_AnUnreadableValueCountsAsZero()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, """{ "rideStats": { "rides": "many", "shifts": -4, "since": "never" } }""");

            Assert.Equal(GameControllerForZwift.Core.Rides.RideStats.None, new AppSettingsStore(new FileService(), _path).GetRideStats());
        }

        [Fact]
        public void SCRRIDE031_DonationAsk_IsNotShownUntilRecordedAndThenStaysShown()
        {
            var store = new AppSettingsStore(new FileService(), _path);
            Assert.False(store.GetDonationAskShown());

            store.SetDonationAskShown();

            Assert.True(new AppSettingsStore(new FileService(), _path).GetDonationAskShown());
        }

        [Fact]
        public void CFG039_ShowOtherControllers_NotABoolean_IsOff()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, """{ "showConfigurationsFromOtherControllers": "yes" }""");

            Assert.False(new AppSettingsStore(new FileService(), _path).GetShowOtherControllersConfigurations());
        }

        [Fact]
        public void SetLastUsed_NullRemovesEntry()
        {
            var store = new AppSettingsStore(new FileService(), _path);
            store.SetLastUsed("045E:0B13", Guid.NewGuid());

            store.SetLastUsed("045E:0B13", null);

            Assert.Null(store.GetLastUsed("045E:0B13"));
        }

        [Fact]
        public void CFG036_ClearLastUsed_RemovesEveryEntryAndKeepsOtherSettings()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, """{ "theme": "Dark" }""");
            var store = new AppSettingsStore(new FileService(), _path);
            store.SetLastUsed("A", Guid.NewGuid());
            store.SetLastUsed("B", Guid.NewGuid());

            store.ClearLastUsed();

            Assert.Null(store.GetLastUsed("A"));
            Assert.Null(store.GetLastUsed("B"));
            var root = JsonNode.Parse(File.ReadAllText(_path))!.AsObject();
            Assert.Equal("Dark", root["theme"]!.GetValue<string>());
        }

        [Fact]
        public void BrokenSettingsFile_IsTreatedAsEmpty()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, "{ broken");
            var store = new AppSettingsStore(new FileService(), _path);

            Assert.Null(store.GetLastUsed("A"));
            var id = Guid.NewGuid();
            store.SetLastUsed("A", id);
            Assert.Equal(id, store.GetLastUsed("A"));
        }
    }
}
