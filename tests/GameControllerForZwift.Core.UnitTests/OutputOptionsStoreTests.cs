using GameControllerForZwift.Core;

namespace GameControllerForZwift.Core.Tests
{
    public class OutputOptionsStoreTests
    {
        [Fact]
        public void Defaults_EmulatorOnly()
        {
            var o = new OutputOptions();
            Assert.True(o.SendToZwiftEmulator);
            Assert.False(o.SendToKeyboard);
        }

        [Fact]
        public void SaveThenLoad_RoundTrips_AndMissingFileKeepsDefaults()
        {
            var dir = Path.Combine(Path.GetTempPath(), "gcfz-" + Guid.NewGuid().ToString("N"));
            var store = new OutputOptionsStore(Path.Combine(dir, "settings.json"));
            var fresh = new OutputOptions();
            store.Load(fresh);
            Assert.True(fresh.SendToZwiftEmulator);
            Assert.False(fresh.SendToKeyboard);

            store.Save(new OutputOptions { SendToZwiftEmulator = false, SendToKeyboard = true });
            var loaded = new OutputOptions();
            store.Load(loaded);
            Assert.False(loaded.SendToZwiftEmulator);
            Assert.True(loaded.SendToKeyboard);
            Directory.Delete(dir, true);
        }

        [Fact]
        public void Changed_RaisedOnlyOnActualChange()
        {
            var o = new OutputOptions();
            int n = 0;
            o.Changed += (_, _) => n++;
            o.SendToKeyboard = false;
            o.SendToKeyboard = true;
            Assert.Equal(1, n);
        }
    }
}
