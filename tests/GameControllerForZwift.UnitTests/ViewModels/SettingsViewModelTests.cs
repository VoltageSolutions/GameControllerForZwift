using GameControllerForZwift.UI.WPF.ViewModels;
using static GameControllerForZwift.UI.WPF.Tests.ViewModels.ConfigurationTestContext;

namespace GameControllerForZwift.UI.WPF.Tests.ViewModels
{
    public sealed class SettingsViewModelTests : IDisposable
    {
        private readonly ConfigurationTestContext _context = new();

        public void Dispose() => _context.Dispose();

        private SettingsViewModel CreateViewModel() => new(_context.Store, _context.Settings, _context.Dialogs);

        [Fact]
        public void CFG035_DeleteAll_ConfirmationStatesCountAndCanBeCanceled()
        {
            _context.Seed("Climbing", Xbox);
            _context.Seed("Flat", DualSense);
            File.WriteAllText(Path.Join(_context.ConfigurationsFolder, "broken.json"), "{ nope");
            _context.Dialogs.ConfirmAnswers.Enqueue(false);

            CreateViewModel().DeleteAllConfigurationsCommand.Execute(null);

            var message = Assert.Single(_context.Dialogs.ConfirmMessages);
            Assert.Contains("3", message, StringComparison.Ordinal);
            Assert.Contains("cannot be undone", message, StringComparison.Ordinal);
            Assert.Equal(3, _context.ConfigurationFiles().Length);
        }

        [Fact]
        public void CFG035_CFG036_DeleteAll_RemovesFilesClearsLastUsedAndFallsBackToDefault()
        {
            var climbing = _context.Seed("Climbing", Xbox);
            var flat = _context.Seed("Flat", DualSense);
            _context.Settings.SetLastUsed(_context.KeyFor(Xbox), climbing.Id);
            _context.Settings.SetLastUsed(_context.KeyFor(DualSense), flat.Id);
            var setup = _context.CreateViewModel(Xbox);
            Assert.Equal(climbing.Id, setup.SelectedConfiguration!.Id);
            _context.Dialogs.ConfirmAnswers.Enqueue(true);

            CreateViewModel().DeleteAllConfigurationsCommand.Execute(null);

            Assert.Empty(_context.ConfigurationFiles());
            Assert.Null(_context.Settings.GetLastUsed(_context.KeyFor(Xbox)));
            Assert.Null(_context.Settings.GetLastUsed(_context.KeyFor(DualSense)));
            Assert.Equal(["Default"], setup.Names());
            Assert.True(setup.SelectedConfiguration!.IsDefault);
        }

        [Fact]
        public void CFG035_DeleteAll_WithNothingSaved_DoesNotAskToDelete()
        {
            CreateViewModel().DeleteAllConfigurationsCommand.Execute(null);

            Assert.Empty(_context.Dialogs.Confirmations);
            Assert.Single(_context.Dialogs.Notices);
        }
    }
}
