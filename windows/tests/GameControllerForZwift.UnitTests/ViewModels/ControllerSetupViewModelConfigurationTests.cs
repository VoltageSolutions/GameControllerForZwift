using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.Dialogs;
using GameControllerForZwift.UI.WPF.ViewModels;
using static GameControllerForZwift.UI.WPF.Tests.ViewModels.ConfigurationTestContext;

namespace GameControllerForZwift.UI.WPF.Tests.ViewModels
{
    /// <summary>
    /// Saved configurations on the Controller Setup page. See docs/spec/features/saved-configurations.md.
    /// </summary>
    public sealed class ControllerSetupViewModelConfigurationTests : IDisposable
    {
        private readonly ConfigurationTestContext _context = new();

        public void Dispose() => _context.Dispose();

        #region Selecting a configuration

        [Fact]
        public void CFG016_CFG020_NoSavedConfigurations_SelectsDefaultWithOnlySaveAsEnabled()
        {
            var viewModel = _context.CreateViewModel(Xbox);

            Assert.Equal(["Default"], viewModel.Names());
            Assert.True(viewModel.SelectedConfiguration!.IsDefault);
            Assert.False(viewModel.SaveCommand.CanExecute(null));
            Assert.False(viewModel.DeleteCommand.CanExecute(null));
            Assert.False(viewModel.RenameCommand.CanExecute(null));
            Assert.True(viewModel.SaveAsCommand.CanExecute(null));
            Assert.Equal(ZwiftFunction.Select, viewModel.Selector(ControllerInput.A).SelectedZwiftFunction);
            Assert.Equal(ZwiftFunction.None, viewModel.Selector(ControllerInput.X).SelectedZwiftFunction);
        }

        [Fact]
        public void CFG015_ListsDefaultFirstThenSavedByNameIgnoringCase()
        {
            _context.Seed("charlie", Xbox);
            _context.Seed("Alpha", Xbox);
            _context.Seed("beta", Xbox);

            var viewModel = _context.CreateViewModel(Xbox);

            Assert.Equal(["Default", "Alpha", "beta", "charlie"], viewModel.Names());
        }

        [Fact]
        public void CFG017_SelectsLastUsedConfiguration()
        {
            _context.Seed("Flat", Xbox, ZwiftFunction.Brake);
            var climbing = _context.Seed("Climbing", Xbox, ZwiftFunction.ShiftDown);
            _context.Settings.SetLastUsed(_context.KeyFor(Xbox), climbing.Id);

            var viewModel = _context.CreateViewModel(Xbox);

            Assert.Equal(climbing.Id, viewModel.SelectedConfiguration!.Id);
            Assert.Equal(ZwiftFunction.ShiftDown, viewModel.Selector(ControllerInput.X).SelectedZwiftFunction);
            Assert.False(viewModel.IsDirty);
        }

        [Fact]
        public void CFG017_LastUsedPersistsAcrossRestarts()
        {
            var first = _context.CreateViewModel(Xbox);
            _context.Dialogs.NameAnswers.Enqueue("Climbing");
            first.SaveAsCommand.Execute(null);

            var second = _context.CreateViewModel(Xbox);

            Assert.Equal("Climbing", second.SelectedConfiguration!.Name);
        }

        [Fact]
        public void CFG018_LastUsedThatNoLongerExists_SelectsDefault()
        {
            _context.Seed("Climbing", Xbox);
            _context.Settings.SetLastUsed(_context.KeyFor(Xbox), Guid.NewGuid());

            var viewModel = _context.CreateViewModel(Xbox);

            Assert.True(viewModel.SelectedConfiguration!.IsDefault);
        }

        [Fact]
        public void CFG016_SavedConfigurationsWithoutLastUsed_SelectsDefault()
        {
            _context.Seed("Climbing", Xbox);

            var viewModel = _context.CreateViewModel(Xbox);

            Assert.True(viewModel.SelectedConfiguration!.IsDefault);
        }

        [Fact]
        public void CFG012_ListsConfigurationsSavedByAnotherUnitOfTheSameModel()
        {
            _context.Seed("Climbing", Xbox with { Name = "Xbox Wireless Controller" });
            _context.Seed("Other pad", DualSense);

            var viewModel = _context.CreateViewModel(Xbox);

            Assert.Equal(["Default", "Climbing"], viewModel.Names());
        }

        [Fact]
        public void CFG014_ControllerWithoutIds_MatchesByName()
        {
            var generic = new ControllerModel(0, 0, "Generic Pad");
            _context.Seed("Mine", generic);
            _context.Seed("Not mine", new ControllerModel(0, 0, "Another Pad"));

            var viewModel = _context.CreateViewModel(generic);

            Assert.Equal(["Default", "Mine"], viewModel.Names());
        }

        [Fact]
        public void CFG024_SwitchingControllerModelWithUnsavedChanges_Prompts()
        {
            var viewModel = _context.CreateViewModel(Controller(Xbox), Controller(DualSense));
            viewModel.Selector(ControllerInput.X).SelectedZwiftFunction = ZwiftFunction.Brake;
            var xbox = viewModel.SelectedController;

            _context.Dialogs.UnsavedChangesAnswers.Enqueue(UnsavedChangesChoice.Cancel);
            viewModel.SelectedController = viewModel.Controllers[1];

            Assert.Equal(1, _context.Dialogs.UnsavedChangesPrompts);
            Assert.Same(xbox, viewModel.SelectedController);
            Assert.True(viewModel.IsDirty);

            _context.Dialogs.UnsavedChangesAnswers.Enqueue(UnsavedChangesChoice.Discard);
            viewModel.SelectedController = viewModel.Controllers[1];

            Assert.Same(viewModel.Controllers[1], viewModel.SelectedController);
            Assert.Equal(DualSense, viewModel.CurrentControllerModel);
            Assert.False(viewModel.IsDirty);
            Assert.Equal(ZwiftFunction.None, viewModel.Selector(ControllerInput.X).SelectedZwiftFunction);
        }

        #endregion

        #region Editing and saving

        [Fact]
        public void CFG021_EditingShowsUnsavedIndicatorAndRevertingClearsIt()
        {
            var climbing = _context.Seed("Climbing", Xbox, ZwiftFunction.ShiftUp);
            _context.Settings.SetLastUsed(_context.KeyFor(Xbox), climbing.Id);
            var viewModel = _context.CreateViewModel(Xbox);
            Assert.False(viewModel.SaveCommand.CanExecute(null));

            viewModel.Selector(ControllerInput.X).SelectedZwiftFunction = ZwiftFunction.Brake;

            Assert.True(viewModel.IsDirty);
            Assert.Equal("Climbing •", viewModel.SelectedConfiguration!.DisplayName);
            Assert.True(viewModel.SaveCommand.CanExecute(null));

            viewModel.Selector(ControllerInput.X).SelectedZwiftFunction = ZwiftFunction.ShiftUp;

            Assert.False(viewModel.IsDirty);
            Assert.Equal("Climbing", viewModel.SelectedConfiguration!.DisplayName);
            Assert.False(viewModel.SaveCommand.CanExecute(null));
        }

        [Fact]
        public void CFG020_EditingDefault_KeepsSaveAndDeleteDisabled()
        {
            var viewModel = _context.CreateViewModel(Xbox);

            viewModel.Selector(ControllerInput.X).SelectedZwiftFunction = ZwiftFunction.Brake;

            Assert.True(viewModel.IsDirty);
            Assert.Equal("Default •", viewModel.SelectedConfiguration!.DisplayName);
            Assert.False(viewModel.SaveCommand.CanExecute(null));
            Assert.False(viewModel.DeleteCommand.CanExecute(null));
            Assert.False(viewModel.RenameCommand.CanExecute(null));
            Assert.True(viewModel.SaveAsCommand.CanExecute(null));
        }

        [Fact]
        public void CFG020_DefaultStaysAsShippedAfterEditingAndSavingAs()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            viewModel.Selector(ControllerInput.A).SelectedZwiftFunction = ZwiftFunction.Brake;
            _context.Dialogs.NameAnswers.Enqueue("Braking");
            viewModel.SaveAsCommand.Execute(null);

            viewModel.Select("Default");

            Assert.Equal(ZwiftFunction.Select, viewModel.Selector(ControllerInput.A).SelectedZwiftFunction);
        }

        [Fact]
        public void CFG022_CFG019_SaveOverwritesFileAndKeepsSelection()
        {
            var climbing = _context.Seed("Climbing", Xbox, ZwiftFunction.ShiftUp);
            _context.Seed("Zebra", Xbox);
            _context.Settings.SetLastUsed(_context.KeyFor(Xbox), climbing.Id);
            var viewModel = _context.CreateViewModel(Xbox);
            viewModel.Selector(ControllerInput.X).SelectedZwiftFunction = ZwiftFunction.Brake;

            viewModel.SaveCommand.Execute(null);

            Assert.Equal(2, _context.ConfigurationFiles().Length);
            var saved = _context.Store.ReadFile(climbing.FilePath!);
            Assert.Equal(climbing.Id, saved.Id);
            Assert.Equal(ZwiftFunction.Brake, saved.Mappings[ControllerInput.X]);
            Assert.Equal(climbing.Id, viewModel.SelectedConfiguration!.Id);
            Assert.False(viewModel.IsDirty);
            Assert.False(viewModel.SaveCommand.CanExecute(null));
        }

        [Fact]
        public void CFG023_CFG019_SaveAsCreatesNewConfigurationAndSelectsIt()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            viewModel.Selector(ControllerInput.X).SelectedZwiftFunction = ZwiftFunction.ShiftUp;
            _context.Dialogs.NameAnswers.Enqueue("  Climbing  ");

            viewModel.SaveAsCommand.Execute(null);

            Assert.Equal(["Default", "Climbing"], viewModel.Names());
            var selected = viewModel.SelectedConfiguration!;
            Assert.Equal("Climbing", selected.Name);
            Assert.NotEqual(Guid.Empty, selected.Id);
            Assert.False(viewModel.IsDirty);
            Assert.Single(_context.ConfigurationFiles());
            Assert.Equal(selected.Id, _context.Settings.GetLastUsed(_context.KeyFor(Xbox)));

            // A second Save as makes another configuration with its own id.
            _context.Dialogs.NameAnswers.Enqueue("Climbing copy");
            viewModel.SaveAsCommand.Execute(null);

            Assert.Equal("Climbing copy", viewModel.SelectedConfiguration!.Name);
            Assert.NotEqual(selected.Id, viewModel.SelectedConfiguration!.Id);
            Assert.Equal(2, _context.ConfigurationFiles().Length);
        }

        [Fact]
        public void CFG027_CFG028_SaveAsValidationBlocksDuplicatesAndDefault()
        {
            _context.Seed("Climbing", Xbox);
            var viewModel = _context.CreateViewModel(Xbox);

            viewModel.SaveAsCommand.Execute(null);

            var validate = Assert.Single(_context.Dialogs.NamePrompts).Validate;
            Assert.Equal("A configuration named 'Climbing' already exists for this controller", validate("climbing "));
            Assert.NotNull(validate("default"));
            Assert.NotNull(validate("   "));
            Assert.Null(validate("Flat"));
        }

        [Fact]
        public void CFG028_SaveAsWithDuplicateName_IsNeverSaved()
        {
            _context.Seed("Climbing", Xbox);
            var viewModel = _context.CreateViewModel(Xbox);
            _context.Dialogs.NameAnswers.Enqueue("CLIMBING");

            viewModel.SaveAsCommand.Execute(null);

            Assert.Single(_context.ConfigurationFiles());
            Assert.True(viewModel.SelectedConfiguration!.IsDefault);
        }

        [Fact]
        public void CFG023_SaveAsCanceled_ChangesNothing()
        {
            var viewModel = _context.CreateViewModel(Xbox);

            viewModel.SaveAsCommand.Execute(null);

            Assert.Empty(_context.ConfigurationFiles());
            Assert.Equal(["Default"], viewModel.Names());
        }

        [Fact]
        public void CFG024_UnsavedChangesCancel_KeepsSelectionAndEdit()
        {
            var climbing = _context.Seed("Climbing", Xbox, ZwiftFunction.ShiftUp);
            _context.Seed("Flat", Xbox, ZwiftFunction.SteerLeft);
            _context.Settings.SetLastUsed(_context.KeyFor(Xbox), climbing.Id);
            var viewModel = _context.CreateViewModel(Xbox);
            viewModel.Selector(ControllerInput.X).SelectedZwiftFunction = ZwiftFunction.Brake;
            _context.Dialogs.UnsavedChangesAnswers.Enqueue(UnsavedChangesChoice.Cancel);

            viewModel.Select("Flat");

            Assert.Equal(1, _context.Dialogs.UnsavedChangesPrompts);
            Assert.Equal(climbing.Id, viewModel.SelectedConfiguration!.Id);
            Assert.Equal(ZwiftFunction.Brake, viewModel.Selector(ControllerInput.X).SelectedZwiftFunction);
            Assert.True(viewModel.IsDirty);
        }

        [Fact]
        public void CFG024_UnsavedChangesDiscard_SwitchesWithoutSaving()
        {
            var climbing = _context.Seed("Climbing", Xbox, ZwiftFunction.ShiftUp);
            _context.Seed("Flat", Xbox, ZwiftFunction.SteerLeft);
            _context.Settings.SetLastUsed(_context.KeyFor(Xbox), climbing.Id);
            var viewModel = _context.CreateViewModel(Xbox);
            viewModel.Selector(ControllerInput.X).SelectedZwiftFunction = ZwiftFunction.Brake;
            _context.Dialogs.UnsavedChangesAnswers.Enqueue(UnsavedChangesChoice.Discard);

            viewModel.Select("Flat");

            Assert.Equal("Flat", viewModel.SelectedConfiguration!.Name);
            Assert.Equal(ZwiftFunction.SteerLeft, viewModel.Selector(ControllerInput.X).SelectedZwiftFunction);
            Assert.False(viewModel.IsDirty);
            Assert.Equal(ZwiftFunction.ShiftUp, _context.Store.ReadFile(climbing.FilePath!).Mappings[ControllerInput.X]);
            Assert.Equal(viewModel.SelectedConfiguration!.Id, _context.Settings.GetLastUsed(_context.KeyFor(Xbox)));
        }

        [Fact]
        public void CFG024_UnsavedChangesSave_SavesThenSwitches()
        {
            var climbing = _context.Seed("Climbing", Xbox, ZwiftFunction.ShiftUp);
            _context.Seed("Flat", Xbox, ZwiftFunction.SteerLeft);
            _context.Settings.SetLastUsed(_context.KeyFor(Xbox), climbing.Id);
            var viewModel = _context.CreateViewModel(Xbox);
            viewModel.Selector(ControllerInput.X).SelectedZwiftFunction = ZwiftFunction.Brake;
            _context.Dialogs.UnsavedChangesAnswers.Enqueue(UnsavedChangesChoice.Save);

            viewModel.Select("Flat");

            Assert.Equal("Flat", viewModel.SelectedConfiguration!.Name);
            Assert.Equal(ZwiftFunction.SteerLeft, viewModel.Selector(ControllerInput.X).SelectedZwiftFunction);
            Assert.Equal(ZwiftFunction.Brake, _context.Store.ReadFile(climbing.FilePath!).Mappings[ControllerInput.X]);
        }

        [Fact]
        public void CFG024_UnsavedChangesToDefaultSave_AsksForNameThenSwitches()
        {
            _context.Seed("Flat", Xbox, ZwiftFunction.SteerLeft);
            var viewModel = _context.CreateViewModel(Xbox);
            viewModel.Selector(ControllerInput.X).SelectedZwiftFunction = ZwiftFunction.Brake;
            _context.Dialogs.UnsavedChangesAnswers.Enqueue(UnsavedChangesChoice.Save);
            _context.Dialogs.NameAnswers.Enqueue("Braking");

            viewModel.Select("Flat");

            Assert.Equal(["Default", "Braking", "Flat"], viewModel.Names());
            Assert.Equal("Flat", viewModel.SelectedConfiguration!.Name);
        }

        [Fact]
        public void CFG024_SwitchingWithoutChanges_DoesNotPrompt()
        {
            _context.Seed("Flat", Xbox);
            var viewModel = _context.CreateViewModel(Xbox);

            viewModel.Select("Flat");

            Assert.Equal(0, _context.Dialogs.UnsavedChangesPrompts);
            Assert.Equal("Flat", viewModel.SelectedConfiguration!.Name);
        }

        [Fact]
        public void CFG025_CFG019_RenameKeepsIdAndSelection()
        {
            var climbing = _context.Seed("Climbing", Xbox);
            _context.Settings.SetLastUsed(_context.KeyFor(Xbox), climbing.Id);
            var viewModel = _context.CreateViewModel(Xbox);
            _context.Dialogs.NameAnswers.Enqueue("Alpine");

            viewModel.RenameCommand.Execute(null);

            Assert.Equal(["Default", "Alpine"], viewModel.Names());
            Assert.Equal(climbing.Id, viewModel.SelectedConfiguration!.Id);
            Assert.Equal("Alpine", viewModel.SelectedConfiguration!.Name);
            var file = Assert.Single(_context.ConfigurationFiles());
            Assert.EndsWith("_Alpine.json", file, StringComparison.Ordinal);
        }

        [Fact]
        public void CFG025_RenameValidation_AllowsOwnNameButNotOthers()
        {
            var climbing = _context.Seed("Climbing", Xbox);
            _context.Seed("Flat", Xbox);
            _context.Settings.SetLastUsed(_context.KeyFor(Xbox), climbing.Id);
            var viewModel = _context.CreateViewModel(Xbox);

            viewModel.RenameCommand.Execute(null);

            var prompt = Assert.Single(_context.Dialogs.NamePrompts);
            Assert.Equal("Climbing", prompt.InitialName);
            Assert.Null(prompt.Validate("CLIMBING"));
            Assert.NotNull(prompt.Validate("flat"));
        }

        [Fact]
        public void CFG025_RenameWithUnsavedChanges_KeepsEdits()
        {
            var climbing = _context.Seed("Climbing", Xbox, ZwiftFunction.ShiftUp);
            _context.Settings.SetLastUsed(_context.KeyFor(Xbox), climbing.Id);
            var viewModel = _context.CreateViewModel(Xbox);
            viewModel.Selector(ControllerInput.X).SelectedZwiftFunction = ZwiftFunction.Brake;
            _context.Dialogs.NameAnswers.Enqueue("Alpine");

            viewModel.RenameCommand.Execute(null);

            Assert.Equal(ZwiftFunction.Brake, viewModel.Selector(ControllerInput.X).SelectedZwiftFunction);
            Assert.True(viewModel.IsDirty);
            Assert.Equal("Alpine •", viewModel.SelectedConfiguration!.DisplayName);
        }

        #endregion

        #region Delete

        [Fact]
        public void CFG029_CFG019_DeleteAsksThenDeletesAndSelectsDefault()
        {
            var climbing = _context.Seed("Climbing", Xbox);
            _context.Settings.SetLastUsed(_context.KeyFor(Xbox), climbing.Id);
            var viewModel = _context.CreateViewModel(Xbox);
            _context.Dialogs.ConfirmAnswers.Enqueue(true);

            viewModel.DeleteCommand.Execute(null);

            Assert.Single(_context.Dialogs.Confirmations);
            Assert.Empty(_context.ConfigurationFiles());
            Assert.Equal(["Default"], viewModel.Names());
            Assert.True(viewModel.SelectedConfiguration!.IsDefault);
            Assert.Equal(ZwiftFunction.Select, viewModel.Selector(ControllerInput.A).SelectedZwiftFunction);
            Assert.Null(_context.Settings.GetLastUsed(_context.KeyFor(Xbox)));
        }

        [Fact]
        public void CFG029_DeleteNotConfirmed_KeepsConfiguration()
        {
            var climbing = _context.Seed("Climbing", Xbox);
            _context.Settings.SetLastUsed(_context.KeyFor(Xbox), climbing.Id);
            var viewModel = _context.CreateViewModel(Xbox);
            _context.Dialogs.ConfirmAnswers.Enqueue(false);

            viewModel.DeleteCommand.Execute(null);

            Assert.Single(_context.ConfigurationFiles());
            Assert.Equal(climbing.Id, viewModel.SelectedConfiguration!.Id);
        }

        #endregion

        #region Import

        [Fact]
        public void CFG030_CFG031_CFG019_ImportCopiesFileSelectsItAndShowsNotice()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            var source = new SavedConfiguration(Guid.NewGuid(), "Shared", Xbox,
                ConfigurationMappings.Normalize([new(ControllerInput.Y, ZwiftFunction.RideOn)]), DateTimeOffset.UtcNow);
            var path = _context.WriteExternalFile(source);
            _context.Dialogs.PickedFiles.Enqueue(path);

            viewModel.ImportCommand.Execute(null);

            Assert.True(File.Exists(path));
            var copy = Assert.Single(_context.ConfigurationFiles());
            Assert.Equal(source.Id, _context.Store.ReadFile(copy).Id);
            Assert.Equal(source.Id, viewModel.SelectedConfiguration!.Id);
            Assert.Equal(ZwiftFunction.RideOn, viewModel.Selector(ControllerInput.Y).SelectedZwiftFunction);
            Assert.Equal("Saved a copy to your configurations.", viewModel.NoticeText);
            Assert.Empty(_context.Dialogs.Errors);

            viewModel.DismissNoticeCommand.Execute(null);
            Assert.Null(viewModel.NoticeText);
        }

        [Fact]
        public void CFG032_ImportWithCollidingName_AsksForNewNameWithSuggestion()
        {
            _context.Seed("Climbing", Xbox);
            var viewModel = _context.CreateViewModel(Xbox);
            var source = new SavedConfiguration(Guid.NewGuid(), "climbing", Xbox, ConfigurationMappings.Normalize([]), DateTimeOffset.UtcNow);
            _context.Dialogs.PickedFiles.Enqueue(_context.WriteExternalFile(source));
            _context.Dialogs.NameAnswers.Enqueue("climbing (2)");

            viewModel.ImportCommand.Execute(null);

            var prompt = Assert.Single(_context.Dialogs.NamePrompts);
            Assert.Equal("climbing (2)", prompt.InitialName);
            Assert.NotNull(prompt.Validate("CLIMBING"));
            Assert.Equal(["Default", "Climbing", "climbing (2)"], viewModel.Names());
            Assert.Equal("climbing (2)", viewModel.SelectedConfiguration!.Name);
        }

        [Fact]
        public void CFG032_ImportCollisionCanceled_ImportsNothing()
        {
            _context.Seed("Climbing", Xbox);
            var viewModel = _context.CreateViewModel(Xbox);
            var source = new SavedConfiguration(Guid.NewGuid(), "Climbing", Xbox, ConfigurationMappings.Normalize([]), DateTimeOffset.UtcNow);
            _context.Dialogs.PickedFiles.Enqueue(_context.WriteExternalFile(source));

            viewModel.ImportCommand.Execute(null);

            Assert.Single(_context.ConfigurationFiles());
            Assert.Null(viewModel.NoticeText);
        }

        [Fact]
        public void CFG033_ImportForAnotherModel_RewritesControllerAndIdWhenConfirmed()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            var source = new SavedConfiguration(Guid.NewGuid(), "From a friend", DualSense, ConfigurationMappings.Normalize([]), DateTimeOffset.UtcNow);
            _context.Dialogs.PickedFiles.Enqueue(_context.WriteExternalFile(source));
            _context.Dialogs.ConfirmAnswers.Enqueue(true);

            viewModel.ImportCommand.Execute(null);

            Assert.Single(_context.Dialogs.Confirmations);
            var imported = _context.Store.ReadFile(Assert.Single(_context.ConfigurationFiles()));
            Assert.NotEqual(source.Id, imported.Id);
            Assert.Equal(Xbox.VendorId, imported.Controller.VendorId);
            Assert.Equal(Xbox.ProductId, imported.Controller.ProductId);
            Assert.Equal(imported.Id, viewModel.SelectedConfiguration!.Id);
        }

        [Fact]
        public void CFG033_ImportForAnotherModel_DeclinedImportsNothing()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            var source = new SavedConfiguration(Guid.NewGuid(), "From a friend", DualSense, ConfigurationMappings.Normalize([]), DateTimeOffset.UtcNow);
            _context.Dialogs.PickedFiles.Enqueue(_context.WriteExternalFile(source));
            _context.Dialogs.ConfirmAnswers.Enqueue(false);

            viewModel.ImportCommand.Execute(null);

            Assert.Empty(_context.ConfigurationFiles());
            Assert.True(viewModel.SelectedConfiguration!.IsDefault);
        }

        [Fact]
        public void CFG007_ImportNewerFormat_IsRefusedWithMessage()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            var json = """
                {
                  "formatVersion": 2,
                  "id": "6f1c2b9e-1d2a-4f2e-9a51-3c0d7c1b2a10",
                  "name": "Future",
                  "controller": { "vendorId": "045E", "productId": "0B13", "name": "Xbox" },
                  "mappings": []
                }
                """;
            _context.Dialogs.PickedFiles.Enqueue(_context.WriteExternalFile(json));

            viewModel.ImportCommand.Execute(null);

            var error = Assert.Single(_context.Dialogs.Errors);
            Assert.Contains("newer version", error, StringComparison.Ordinal);
            Assert.Empty(_context.ConfigurationFiles());
        }

        [Fact]
        public void CFG005_ImportBrokenFile_ShowsErrorAndImportsNothing()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            _context.Dialogs.PickedFiles.Enqueue(_context.WriteExternalFile("{ not json"));

            viewModel.ImportCommand.Execute(null);

            Assert.Single(_context.Dialogs.Errors);
            Assert.Empty(_context.ConfigurationFiles());
        }

        [Fact]
        public void CFG030_ImportSameFileTwice_GetsNewIdAndName()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            var source = new SavedConfiguration(Guid.NewGuid(), "Shared", Xbox, ConfigurationMappings.Normalize([]), DateTimeOffset.UtcNow);
            var path = _context.WriteExternalFile(source);
            _context.Dialogs.PickedFiles.Enqueue(path);
            viewModel.ImportCommand.Execute(null);
            _context.Dialogs.PickedFiles.Enqueue(path);
            _context.Dialogs.NameAnswers.Enqueue("Shared (2)");

            viewModel.ImportCommand.Execute(null);

            Assert.Equal(2, _context.Store.LoadAll().Select(c => c.Id).Distinct().Count());
            Assert.Equal("Shared (2)", viewModel.SelectedConfiguration!.Name);
        }

        [Fact]
        public void CFG024_ImportWithUnsavedChanges_Prompts()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            viewModel.Selector(ControllerInput.X).SelectedZwiftFunction = ZwiftFunction.Brake;
            var source = new SavedConfiguration(Guid.NewGuid(), "Shared", Xbox, ConfigurationMappings.Normalize([]), DateTimeOffset.UtcNow);
            _context.Dialogs.PickedFiles.Enqueue(_context.WriteExternalFile(source));
            _context.Dialogs.UnsavedChangesAnswers.Enqueue(UnsavedChangesChoice.Cancel);

            viewModel.ImportCommand.Execute(null);

            Assert.Equal(1, _context.Dialogs.UnsavedChangesPrompts);
            Assert.Empty(_context.ConfigurationFiles());
            Assert.True(viewModel.IsDirty);
        }

        #endregion

        #region Refresh and folder

        [Fact]
        public void CFG006_CFG019_ExternalChange_RebuildsListAndKeepsSelectionById()
        {
            var climbing = _context.Seed("Climbing", Xbox);
            _context.Settings.SetLastUsed(_context.KeyFor(Xbox), climbing.Id);
            var viewModel = _context.CreateViewModel(Xbox);
            viewModel.Selector(ControllerInput.X).SelectedZwiftFunction = ZwiftFunction.Brake;

            // Another configuration appears in the folder, for example copied in with File Explorer.
            var other = new SavedConfiguration(Guid.NewGuid(), "Added", Xbox, ConfigurationMappings.Normalize([]), DateTimeOffset.UtcNow);
            File.WriteAllText(Path.Join(_context.ConfigurationsFolder, "added.json"), ConfigurationSerializer.Serialize(other));
            _context.Store.NotifyExternalChange();

            Assert.Equal(["Default", "Added", "Climbing"], viewModel.Names());
            Assert.Equal(climbing.Id, viewModel.SelectedConfiguration!.Id);
            Assert.Equal(ZwiftFunction.Brake, viewModel.Selector(ControllerInput.X).SelectedZwiftFunction);
            Assert.True(viewModel.IsDirty);
        }

        [Fact]
        public void CFG019_SelectedFileDeletedOutsideApp_SelectsDefault()
        {
            var climbing = _context.Seed("Climbing", Xbox);
            _context.Settings.SetLastUsed(_context.KeyFor(Xbox), climbing.Id);
            var viewModel = _context.CreateViewModel(Xbox);

            File.Delete(climbing.FilePath!);
            _context.Store.NotifyExternalChange();

            Assert.Equal(["Default"], viewModel.Names());
            Assert.True(viewModel.SelectedConfiguration!.IsDefault);
            Assert.Equal(ZwiftFunction.None, viewModel.Selector(ControllerInput.X).SelectedZwiftFunction);
        }

        [Fact]
        public void CFG005_BrokenFileInFolder_IsSkipped()
        {
            _context.Seed("Climbing", Xbox);
            File.WriteAllText(Path.Join(_context.ConfigurationsFolder, "broken.json"), "{ nope");

            var viewModel = _context.CreateViewModel(Xbox);

            Assert.Equal(["Default", "Climbing"], viewModel.Names());
            Assert.True(File.Exists(Path.Join(_context.ConfigurationsFolder, "broken.json")));
        }

        [Fact]
        public void CFG034_OpenFolder_CreatesFolderAndOpensIt()
        {
            var viewModel = _context.CreateViewModel(Xbox);
            Assert.False(Directory.Exists(_context.ConfigurationsFolder));

            viewModel.OpenFolderCommand.Execute(null);

            Assert.True(Directory.Exists(_context.ConfigurationsFolder));
            Assert.Equal([_context.ConfigurationsFolder], _context.Dialogs.OpenedFolders);
        }

        #endregion
    }
}
