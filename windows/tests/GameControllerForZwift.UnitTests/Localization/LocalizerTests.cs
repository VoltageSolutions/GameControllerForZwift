using GameControllerForZwift.UI.WPF.Localization;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace GameControllerForZwift.UnitTests.Localization
{
    [Collection(WpfWindows.Name)]
    public class LocalizerTests
    {
        [Fact]
        public void Get_ReturnsTheEnglishString()
        {
            Assert.Equal("Ride", Localizer.Lookup("NavRide", CultureInfo.InvariantCulture));
            Assert.Equal("Zwift is connected to Voltage Gamepad", Localizer.Lookup("ZwiftConnected", new CultureInfo("en-US")));
        }

        [Fact]
        public void Lookup_FallsBackToEnglishForALanguageWithoutTranslations()
        {
            Assert.Equal("Ride", Localizer.Lookup("NavRide", new CultureInfo("de-DE")));
        }

        [Fact]
        public void Lookup_MarksAMissingKey()
        {
            Assert.Equal(Localizer.MissingPrefix + "NoSuchKey", Localizer.Lookup("NoSuchKey", CultureInfo.InvariantCulture));
            Assert.Throws<ArgumentNullException>(() => Localizer.Lookup(null!, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void UXL003_Format_FillsPlaceholdersInOneString()
        {
            Assert.Equal("Connections, Zwift not connected", Localizer.Format("NavConnectionsAlert", "Connections"));
            Assert.Equal(Localizer.Get("NavRide"), Localizer.Instance["NavRide"]);
        }

        [Fact]
        public void SetCulture_RefreshesEveryBoundString()
        {
            CultureInfo original = Localizer.Instance.Culture;
            var changed = new List<string?>();
            PropertyChangedEventHandler handler = (_, e) => changed.Add(e.PropertyName);
            Localizer.Instance.PropertyChanged += handler;
            try
            {
                Localizer.Instance.SetCulture(new CultureInfo("fr-FR"));

                Assert.Equal("fr-FR", Localizer.Instance.Culture.Name);
                Assert.Contains("Item[]", changed);
                Assert.Contains(nameof(Localizer.Culture), changed);
                Assert.Throws<ArgumentNullException>(() => Localizer.Instance.SetCulture(null!));
            }
            finally
            {
                Localizer.Instance.PropertyChanged -= handler;
                Localizer.Instance.SetCulture(original);
            }
        }

        [Fact]
        public void LocExtension_BindsTheString()
        {
            TestApplication.Run(() =>
            {
                var text = new TextBlock();
                var binding = Assert.IsAssignableFrom<BindingBase>(new LocExtension("NavButtons").ProvideValue(null!));

                text.SetBinding(TextBlock.TextProperty, binding);

                Assert.Equal("Buttons", text.Text);
                Assert.Equal(string.Empty, new LocExtension().Key);
            });
        }
    }
}
