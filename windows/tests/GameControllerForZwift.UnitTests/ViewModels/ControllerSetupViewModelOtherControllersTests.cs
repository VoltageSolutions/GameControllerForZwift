using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.Core.FileSystem;
using GameControllerForZwift.UI.WPF.Dialogs;
using GameControllerForZwift.UI.WPF.ViewModels;
using static GameControllerForZwift.UI.WPF.Tests.ViewModels.ConfigurationTestContext;

namespace GameControllerForZwift.UI.WPF.Tests.ViewModels
{
    /// <summary>
    /// Showing and using the configurations of other controller models (CFG-039 to CFG-042).
    /// </summary>
    public sealed class ControllerSetupViewModelOtherControllersTests : IDisposable
    {
        private static readonly ControllerModel SwitchPro = new(0x057E, 0x2009, "Nintendo Switch Pro Controller");

        private readonly ConfigurationTestContext _context = new();

        public void Dispose() => _context.Dispose();

        private static string[] DisplayNames(ControllerSetupViewModel viewModel) =>
            viewModel.Configurations.Select(c => c.DisplayName).ToArray();

        private ControllerSetupViewModel CreateWithOthersShown()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            viewModel.ShowOtherControllersConfigurations = true;
            return viewModel;
        }

        private static void SelectByDisplayName(ControllerSetupViewModel viewModel, string displayName) =>
            viewModel.SelectedConfiguration = viewModel.Configurations.Single(c => c.DisplayName == displayName);

        [Fact]
        public void CFG039_Unchecked_ListsOnlyThisControllersConfigurations()
        {
            _context.Seed("Shimano Style", Xbox);
            _context.Seed("Climbing", DualSense);

            var viewModel = _context.CreateViewModel(Xbox);

            Assert.False(viewModel.ShowOtherControllersConfigurations);
            Assert.Equal(["Default", "Shimano Style"], DisplayNames(viewModel));
        }

        [Fact]
        public void CFG039_Checked_ListsOtherModelsAfterThisControllersByNameWithTheirControllerName()
        {
            _context.Seed("SRAM Style", Xbox);
            _context.Seed("Shimano Style", Xbox);
            _context.Seed("Climbing", DualSense);
            _context.Seed("aero", SwitchPro);
            _context.Seed("Climbing", SwitchPro);

            var viewModel = CreateWithOthersShown();

            Assert.Equal(
            [
                "Default",
                "Shimano Style",
                "SRAM Style",
                "aero (Nintendo Switch Pro Controller)",
                "Climbing (DualSense Wireless Controller)",
                "Climbing (Nintendo Switch Pro Controller)",
            ], DisplayNames(viewModel));
            Assert.All(viewModel.Configurations.Skip(3), c => Assert.True(c.IsFromOtherController));
            Assert.All(viewModel.Configurations.Take(3), c => Assert.False(c.IsFromOtherController));
        }

        [Fact]
        public void CFG039_Toggling_KeepsTheSelectionAndUnsavedEdits()
        {
            _context.Seed("Shimano Style", Xbox);
            _context.Seed("Climbing", DualSense);
            var viewModel = _context.CreateViewModel(Xbox);
            viewModel.Select("Shimano Style");
            viewModel.Selector(ControllerInput.Menu).SelectedZwiftFunction = ZwiftFunction.RideOn;

            viewModel.ShowOtherControllersConfigurations = true;

            Assert.Equal(3, viewModel.Configurations.Count);
            Assert.Equal("Shimano Style", viewModel.SelectedConfiguration?.Name);
            Assert.True(viewModel.IsDirty);

            viewModel.ShowOtherControllersConfigurations = false;

            Assert.Equal(["Default", "Shimano Style •"], DisplayNames(viewModel));
        }

        [Fact]
        public void CFG039_Setting_IsRememberedInSettings()
        {
            var viewModel = _context.CreateViewModel(Xbox);

            viewModel.ShowOtherControllersConfigurations = true;

            Assert.True(new AppSettingsStore(new FileService(), _context.SettingsFile).GetShowOtherControllersConfigurations());
            Assert.True(_context.CreateViewModel(Xbox).ShowOtherControllersConfigurations);

            viewModel.ShowOtherControllersConfigurations = false;

            Assert.False(_context.Settings.GetShowOtherControllersConfigurations());
        }

        [Fact]
        public void CFG039_NoController_ListsOnlyDefault()
        {
            _context.Seed("Climbing", DualSense);
            _context.Settings.SetShowOtherControllersConfigurations(true);

            var viewModel = _context.CreateViewModel();

            Assert.Equal(["Default"], DisplayNames(viewModel));
        }

        [Fact]
        public void CFG040_SelectingOther_Yes_SavesACopyForThisControllerAndSelectsIt()
        {
            var original = _context.Seed("Climbing", DualSense, ZwiftFunction.Brake);
            var viewModel = CreateWithOthersShown();
            _context.Dialogs.ConfirmAnswers.Enqueue(true);

            SelectByDisplayName(viewModel, "Climbing (DualSense Wireless Controller)");

            Assert.Equal("Use Climbing with this controller?", Assert.Single(_context.Dialogs.Confirmations));
            var selected = viewModel.SelectedConfiguration!;
            Assert.False(selected.IsFromOtherController);
            Assert.Equal("Climbing", selected.Name);
            Assert.NotEqual(original.Id, selected.Id);
            Assert.True(_context.Matcher.Matches(selected.Configuration!.Controller, Xbox));
            Assert.Equal(ZwiftFunction.Brake, viewModel.Selector(ControllerInput.X).SelectedZwiftFunction);
            Assert.False(viewModel.IsDirty);
            Assert.Equal(["Default", "Climbing", "Climbing (DualSense Wireless Controller)"], DisplayNames(viewModel));
            Assert.Equal(selected.Id, _context.Settings.GetLastUsed(_context.KeyFor(Xbox)));

            // CFG-041: the original stays with its own controller.
            var files = _context.ConfigurationFiles().Select(_context.Store.ReadFile).ToList();
            Assert.Equal(2, files.Count);
            var stored = Assert.Single(files, c => c.Id == original.Id);
            Assert.True(_context.Matcher.Matches(stored.Controller, DualSense));
        }

        [Fact]
        public void CFG040_SelectingOther_No_RestoresThePreviousSelection()
        {
            _context.Seed("Shimano Style", Xbox);
            _context.Seed("Climbing", DualSense);
            var viewModel = CreateWithOthersShown();
            viewModel.Select("Shimano Style");
            _context.Dialogs.ConfirmAnswers.Enqueue(false);

            SelectByDisplayName(viewModel, "Climbing (DualSense Wireless Controller)");

            Assert.Equal("Shimano Style", viewModel.SelectedConfiguration?.Name);
            Assert.Equal(2, _context.ConfigurationFiles().Length);
            Assert.Null(viewModel.NoticeText);
        }

        [Fact]
        public void CFG042_SelectingOther_NameTaken_AsksForANewName()
        {
            _context.Seed("Climbing", Xbox);
            _context.Seed("Climbing", DualSense);
            var viewModel = CreateWithOthersShown();
            _context.Dialogs.ConfirmAnswers.Enqueue(true);
            _context.Dialogs.NameAnswers.Enqueue("Climbing (2)");

            SelectByDisplayName(viewModel, "Climbing (DualSense Wireless Controller)");

            var prompt = Assert.Single(_context.Dialogs.NamePrompts);
            Assert.Equal("Climbing (2)", prompt.InitialName);
            Assert.NotNull(prompt.Validate("climbing"));
            Assert.Equal("Climbing (2)", viewModel.SelectedConfiguration?.Name);
            Assert.Equal(["Default", "Climbing", "Climbing (2)", "Climbing (DualSense Wireless Controller)"], DisplayNames(viewModel));
        }

        [Fact]
        public void CFG042_SelectingOther_NamePromptCanceled_RestoresTheSelection()
        {
            _context.Seed("Climbing", Xbox);
            _context.Seed("Climbing", DualSense);
            var viewModel = CreateWithOthersShown();
            SelectByDisplayName(viewModel, "Climbing");
            _context.Dialogs.ConfirmAnswers.Enqueue(true);

            SelectByDisplayName(viewModel, "Climbing (DualSense Wireless Controller)");

            Assert.Single(_context.Dialogs.NamePrompts);
            Assert.Equal("Climbing", viewModel.SelectedConfiguration?.DisplayName);
            Assert.Equal(2, _context.ConfigurationFiles().Length);
        }

        [Fact]
        public void CFG024_SelectingOther_WithUnsavedEdits_CancelKeepsEdits()
        {
            _context.Seed("Climbing", DualSense);
            var viewModel = CreateWithOthersShown();
            viewModel.Selector(ControllerInput.Menu).SelectedZwiftFunction = ZwiftFunction.RideOn;
            _context.Dialogs.ConfirmAnswers.Enqueue(true);
            _context.Dialogs.UnsavedChangesAnswers.Enqueue(UnsavedChangesChoice.Cancel);

            SelectByDisplayName(viewModel, "Climbing (DualSense Wireless Controller)");

            Assert.Equal(1, _context.Dialogs.UnsavedChangesPrompts);
            Assert.True(viewModel.SelectedConfiguration!.IsDefault);
            Assert.True(viewModel.IsDirty);
            Assert.Single(_context.ConfigurationFiles());
        }

        [Fact]
        public void CFG024_SelectingOther_WithUnsavedEdits_DiscardUsesTheCopy()
        {
            _context.Seed("Climbing", DualSense);
            var viewModel = CreateWithOthersShown();
            viewModel.Selector(ControllerInput.Menu).SelectedZwiftFunction = ZwiftFunction.RideOn;
            _context.Dialogs.ConfirmAnswers.Enqueue(true);
            _context.Dialogs.UnsavedChangesAnswers.Enqueue(UnsavedChangesChoice.Discard);

            SelectByDisplayName(viewModel, "Climbing (DualSense Wireless Controller)");

            Assert.Equal("Climbing", viewModel.SelectedConfiguration?.DisplayName);
            Assert.False(viewModel.IsDirty);
            Assert.Equal(2, _context.ConfigurationFiles().Length);
        }

        [Fact]
        public void CFG039_RenameAndSaveAs_OnlyCheckNamesOfThisController()
        {
            _context.Seed("Climbing", DualSense);
            var viewModel = CreateWithOthersShown();
            _context.Dialogs.NameAnswers.Enqueue("Climbing");

            viewModel.SaveAsCommand.Execute(null);

            Assert.Null(Assert.Single(_context.Dialogs.NamePrompts).Validate("Climbing"));
            Assert.Equal("Climbing", viewModel.SelectedConfiguration?.DisplayName);
        }
    }
}
