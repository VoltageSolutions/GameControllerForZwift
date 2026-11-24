using NSubstitute;
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
            Assert.Contains("can't be undone", message, StringComparison.Ordinal);
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
        public void SCRSET005_DeleteAll_WithNothingSaved_IsDisabledAndSaysWhy()
        {
            SettingsViewModel viewModel = CreateViewModel();

            Assert.False(viewModel.HasSavedMappings);
            Assert.False(viewModel.DeleteAllConfigurationsCommand.CanExecute(null));
            Assert.Equal("There are no saved mappings to delete.", viewModel.DeleteDescription);
            viewModel.DeleteAllConfigurationsCommand.Execute(null);
            Assert.Empty(_context.Dialogs.Confirmations);
        }

        [Fact]
        public void SCRSET005_DeleteAll_IsEnabledWhileAMappingIsSavedAndDisabledAfterDeleting()
        {
            _context.Seed("Climbing", Xbox);
            SettingsViewModel viewModel = CreateViewModel();
            Assert.True(viewModel.DeleteAllConfigurationsCommand.CanExecute(null));
            Assert.Equal(string.Empty, viewModel.DeleteDescription);
            _context.Dialogs.ConfirmAnswers.Enqueue(true);

            viewModel.DeleteAllConfigurationsCommand.Execute(null);

            Assert.False(viewModel.HasSavedMappings);
            Assert.False(viewModel.DeleteAllConfigurationsCommand.CanExecute(null));
        }

        [Fact]
        public void SCRSET004_DeleteAll_SingularConfirmationSaysOneMapping()
        {
            _context.Seed("Climbing", Xbox);
            _context.Dialogs.ConfirmAnswers.Enqueue(false);

            CreateViewModel().DeleteAllConfigurationsCommand.Execute(null);

            Assert.Equal("Delete 1 saved mapping? This can't be undone. Default stays.", Assert.Single(_context.Dialogs.ConfirmMessages));
        }

        [Fact]
        public void SCRSET003_OpenMappingsFolder_OpensTheFolderTheStoreCreates()
        {
            CreateViewModel().OpenMappingsFolderCommand.Execute(null);

            Assert.Equal([_context.Store.EnsureFolder()], _context.Dialogs.OpenedFolders);
        }

        [Fact]
        public void SCRSET002_AppearanceIsSaved()
        {
            SettingsViewModel viewModel = CreateViewModel();

            viewModel.Appearance = GameControllerForZwift.UI.WPF.Theme.Appearance.Dark;

            Assert.Equal("Dark", _context.Settings.GetAppearance());
        }

        [Fact]
        public void SCRSET012_RideTimeoutDefaultsToTenAndAValidValueIsSaved()
        {
            SettingsViewModel viewModel = CreateViewModel();
            Assert.Equal("10", viewModel.RideTimeoutText);

            viewModel.RideTimeoutText = "25";

            Assert.Equal(25, viewModel.RideTimeoutMinutes);
            Assert.Equal(25, _context.Settings.GetRideTimeoutMinutes());
            Assert.False(viewModel.HasRideTimeoutMessage);
        }

        [Theory]
        [InlineData("4")]
        [InlineData("0")]
        [InlineData("-1")]
        [InlineData("abc")]
        [InlineData("")]
        public void SCRSET013_ARideTimeoutBelowFiveIsRefusedWithAMessage(string text)
        {
            SettingsViewModel viewModel = CreateViewModel();
            viewModel.RideTimeoutText = "12";

            viewModel.RideTimeoutText = text;

            Assert.Equal("12", viewModel.RideTimeoutText);
            Assert.Equal(12, _context.Settings.GetRideTimeoutMinutes());
            Assert.Equal("The ride timeout must be 5 minutes or more.", viewModel.RideTimeoutMessage);
            Assert.True(viewModel.HasRideTimeoutMessage);
        }
    }
}
