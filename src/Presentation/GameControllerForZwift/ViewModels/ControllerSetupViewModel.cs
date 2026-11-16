using GameControllerForZwift.Core.Controller;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.Core.Mapping;
using GameControllerForZwift.UI.WPF.Dialogs;
using System.IO;
using System.Windows;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    public partial class ControllerSetupViewModel : ObservableObject
    {
        #region Fields

        /// <summary>
        /// The notice shown after a successful import (CFG-031).
        /// </summary>
        public const string ImportNotice = "Saved a copy to your configurations.";

        [ObservableProperty]
        private string _title = "Controller Setup";

        [ObservableProperty]
        private string _description = "Select a game controller and configure its button mapping to Zwift functions.";

        //[ObservableProperty]
        //ZwiftFunctionSelectorViewModel _selectorViewModel;

        private readonly IDataIntegrator _dataIntegrator;

        private readonly IControllerProfileService _profileService;

        private readonly IConfigurationStore _configurationStore;
        private readonly IAppSettingsStore _appSettings;
        private readonly IDialogService _dialogs;
        private readonly ControllerModelMatcher _matcher;

        // The Default as shipped. The profile service's copy changes as the user edits, so this snapshot stays read-only (CFG-020).
        private readonly IReadOnlyDictionary<ControllerInput, ZwiftFunction> _defaultMappings;

        private IController? _selectedController;
        private ControllerModel? _currentModel;
        private List<ConfigurationItem> _configurations = [];
        private ConfigurationItem? _selectedConfiguration;
        private string? _listSignature;
        private bool _isRebuildingList;
        private bool _isApplyingMappings;
        private int _ownStoreChanges;

        [ObservableProperty]
        private List<IController> _controllers = new();
        [ObservableProperty]
        private ControllerData? _currentControllerValues;

        [ObservableProperty]
        private List<ZwiftFunctionSelectorViewModel> _buttonFunctionMappings;

        [ObservableProperty]
        private List<ZwiftFunctionSelectorViewModel> _dpadFunctionMappings;

        [ObservableProperty]
        private List<ZwiftFunctionSelectorViewModel> _leftStickFunctionMappings;

        [ObservableProperty]
        private List<ZwiftFunctionSelectorViewModel> _rightStickFunctionMappings;

        [ObservableProperty]
        private List<ZwiftFunctionSelectorViewModel> _shoulderFunctionMappings;

        /// <summary>
        /// Gets a value indicating whether the current mappings differ from the selected configuration as saved (CFG-021).
        /// </summary>
        [ObservableProperty]
        private bool _isDirty;

        /// <summary>
        /// Gets the non-blocking notice to show, or null when there is none (CFG-031).
        /// </summary>
        [ObservableProperty]
        private string? _noticeText;

        #endregion

        #region Constructor

        public ControllerSetupViewModel(
            IDataIntegrator dataIntegrator,
            IControllerProfileService profileManager,
            IConfigurationStore configurationStore,
            IAppSettingsStore appSettings,
            IDialogService dialogService,
            ControllerModelMatcher matcher)
        {
            ArgumentNullException.ThrowIfNull(dataIntegrator);
            ArgumentNullException.ThrowIfNull(profileManager);
            ArgumentNullException.ThrowIfNull(configurationStore);
            ArgumentNullException.ThrowIfNull(appSettings);
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(matcher);

            _dataIntegrator = dataIntegrator;
            _profileService = profileManager;
            _configurationStore = configurationStore;
            _appSettings = appSettings;
            _dialogs = dialogService;
            _matcher = matcher;

            // Create initial mappings
            _buttonFunctionMappings = CreateButtonMappings();
            _dpadFunctionMappings = CreateDPadMappings();
            _leftStickFunctionMappings = CreateLeftStickMappings();
            _rightStickFunctionMappings = CreateRightStickMappings();
            _shoulderFunctionMappings = CreateShoulderMappings();

            // Load default profile and apply it
            _defaultMappings = ConfigurationMappings.FromProfile(_profileService.GetDefaultProfile());
            SetConfigurationList(BuildConfigurationItems(), Guid.Empty, applyMappings: true);

            _configurationStore.Changed += ConfigurationStore_Changed;
            _dataIntegrator.InputPolled += _dataIntegrator_InputChanged;

            // Auto setup the first controller
            RefreshControllerList();
            if (Controllers.Count > 0)
            {
                SelectedController = Controllers.First();
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the selected controller. Switching to a different controller model with unsaved changes asks first (CFG-024).
        /// </summary>
        public IController? SelectedController
        {
            get => _selectedController;
            set
            {
                if (ReferenceEquals(_selectedController, value))
                    return;

                // A null comes from the list being refreshed; it does not change which configurations apply.
                var newModel = value is null ? null : ControllerModel.From(value);
                bool modelChanges = newModel is not null && (_currentModel is null || !_matcher.Matches(_currentModel, newModel));

                if (modelChanges && !ConfirmLeaveUnsavedChanges(_currentModel ?? newModel))
                {
                    RaisePropertyChangedAgain(nameof(SelectedController));
                    return;
                }

                var oldValue = _selectedController;
                _selectedController = value;
                OnPropertyChanged();
                OnSelectedControllerChanged(oldValue, value);

                if (modelChanges)
                {
                    _currentModel = newModel;
                    LoadConfigurationsForCurrentController();
                }
            }
        }

        /// <summary>
        /// Gets the configurations for the selected controller: Default first, then saved ones by name (CFG-015).
        /// </summary>
        public List<ConfigurationItem> Configurations
        {
            get => _configurations;
            private set => SetProperty(ref _configurations, value);
        }

        /// <summary>
        /// Gets or sets the selected configuration. Switching with unsaved changes asks first (CFG-024).
        /// </summary>
        public ConfigurationItem? SelectedConfiguration
        {
            get => _selectedConfiguration;
            set
            {
                // While the list is rebuilt the ComboBox pushes null and stale items back; the rebuild decides the selection.
                if (_isRebuildingList || value is null || ReferenceEquals(value, _selectedConfiguration))
                    return;

                var id = value.Id;
                if (!ConfirmLeaveUnsavedChanges(_currentModel))
                {
                    RaisePropertyChangedAgain(nameof(SelectedConfiguration));
                    return;
                }

                // Saving in the prompt rebuilds the list, so look the item up again by id.
                var target = _configurations.FirstOrDefault(i => i.Id == id);
                if (target is null)
                {
                    SetConfigurationList(BuildConfigurationItems(), id, applyMappings: true);
                }
                else
                {
                    _selectedConfiguration = target;
                    OnPropertyChanged();
                    ApplyMappings(target.Mappings);
                    UpdateDirtyState();
                }

                RaisePropertyChangedAgain(nameof(SelectedConfiguration));
                RememberLastUsed(_currentModel, _selectedConfiguration?.Id ?? Guid.Empty);
            }
        }

        /// <summary>
        /// Gets the model of the selected controller, or null if no controller has been selected yet.
        /// </summary>
        public ControllerModel? CurrentControllerModel => _currentModel;

        #endregion

        #region Commands
        [RelayCommand]
        public void RefreshControllerList()
        {
            Controllers = _dataIntegrator.GetControllers().ToList();
        }

        //[RelayCommand]
        //public void ResetMappingsToDefault()
        //{

        //}

        //[RelayCommand]
        //public void ClearAllMappings()
        //{

        //}

        /// <summary>
        /// Overwrites the selected configuration (CFG-022). Disabled on Default and when nothing changed (CFG-020, CFG-037).
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSave))]
        private void Save() => SaveSelected();

        private bool CanSave() => _selectedConfiguration is { IsDefault: false } && IsDirty;

        /// <summary>
        /// Saves the current mappings as a new configuration (CFG-023).
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSaveAs))]
        private void SaveAs() => SaveAsNew(_currentModel);

        private bool CanSaveAs() => _currentModel is not null;

        /// <summary>
        /// Deletes the selected configuration after confirmation, then selects Default (CFG-029).
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanModifySelected))]
        private void Delete()
        {
            var item = _selectedConfiguration;
            if (item?.Configuration is null)
                return;

            if (!_dialogs.Confirm("Delete configuration?", $"Delete '{item.Name}'? This can't be undone.", "Delete"))
                return;

            if (!TryChangeStore("Couldn't delete the configuration", () => _configurationStore.Delete(item.Configuration)))
                return;

            SetConfigurationList(BuildConfigurationItems(), Guid.Empty, applyMappings: true);
            RememberLastUsed(_currentModel, Guid.Empty);
        }

        /// <summary>
        /// Renames the selected configuration, keeping its id (CFG-025).
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanModifySelected))]
        private void Rename()
        {
            var item = _selectedConfiguration;
            if (item?.Configuration is null)
                return;

            var otherNames = _configurations.Where(i => !i.IsDefault && i.Id != item.Id).Select(i => i.Name).ToList();
            var name = _dialogs.PromptForName("Rename configuration", "Enter a new name for this configuration.", item.Name, "Rename",
                text => ConfigurationNames.Validate(text, otherNames));
            if (name is null || ConfigurationNames.Validate(name, otherNames) is not null)
                return;
            if (string.Equals(ConfigurationNames.Normalize(name), item.Name, StringComparison.Ordinal))
                return;

            SavedConfiguration? renamed = null;
            if (!TryChangeStore("Couldn't rename the configuration", () => renamed = _configurationStore.Rename(item.Configuration, name)))
                return;

            SetConfigurationList(BuildConfigurationItems(), renamed!.Id, applyMappings: false);
        }

        private bool CanModifySelected() => _selectedConfiguration is { IsDefault: false };

        /// <summary>
        /// Imports a configuration file into the config folder and selects it (CFG-030 to CFG-033).
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSaveAs))]
        private void Import()
        {
            var model = _currentModel;
            if (model is null)
                return;

            var path = _dialogs.PickConfigurationFile();
            if (path is null)
                return;

            SavedConfiguration imported;
            try
            {
                imported = _configurationStore.ReadFile(path);
            }
            catch (ConfigurationFormatException ex)
            {
                _dialogs.ShowError("Couldn't import the configuration", ex.Message);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _dialogs.ShowError("Couldn't import the configuration", ex.Message);
                return;
            }

            // CFG-033: a configuration made for another controller model is only used for this one if the user agrees.
            if (!_matcher.Matches(imported.Controller, model))
            {
                if (!_dialogs.Confirm("Use for this controller?",
                    $"'{imported.Name}' was made for {imported.Controller.DisplayName}. Use it for {model.DisplayName}?",
                    "Use it"))
                {
                    return;
                }
                imported = imported with { Id = Guid.NewGuid(), Controller = model };
            }

            // Importing a file that is already in the folder (or a copy of one) must not create a second file with the same id.
            if (LoadAllSafely().Any(c => c.Id == imported.Id))
                imported = imported with { Id = Guid.NewGuid() };

            // CFG-032: a name that collides gets a new one, pre-filled with a suggestion.
            var existingNames = _configurations.Where(i => !i.IsDefault).Select(i => i.Name).ToList();
            if (ConfigurationNames.Validate(imported.Name, existingNames) is not null)
            {
                var suggestion = ConfigurationNames.SuggestUnique(imported.Name, existingNames);
                var name = _dialogs.PromptForName("Name the imported configuration",
                    $"A configuration named '{imported.Name}' already exists for this controller. Choose another name.",
                    suggestion, "Import", text => ConfigurationNames.Validate(text, existingNames));
                if (name is null || ConfigurationNames.Validate(name, existingNames) is not null)
                    return;
                imported = imported with { Name = ConfigurationNames.Normalize(name) };
            }

            // Selecting the import switches configuration, so unsaved changes are handled first (CFG-024).
            if (!ConfirmLeaveUnsavedChanges(model))
                return;

            SavedConfiguration? saved = null;
            if (!TryChangeStore("Couldn't import the configuration", () => saved = _configurationStore.Save(imported with { FilePath = null })))
                return;

            SetConfigurationList(BuildConfigurationItems(), saved!.Id, applyMappings: true);
            RememberLastUsed(model, saved.Id);
            NoticeText = ImportNotice;
        }

        /// <summary>
        /// Opens the config folder in File Explorer, creating it first if needed (CFG-034).
        /// </summary>
        [RelayCommand]
        private void OpenFolder()
        {
            string folder;
            try
            {
                folder = _configurationStore.EnsureFolder();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _dialogs.ShowError("Couldn't open the configurations folder", ex.Message);
                return;
            }
            _dialogs.OpenFolder(folder);
        }

        /// <summary>
        /// Hides the notice.
        /// </summary>
        [RelayCommand]
        private void DismissNotice() => NoticeText = null;

        #endregion

        #region Methods

        private void OnSelectedControllerChanged(IController? oldValue, IController? newValue)
        {
            // Stop the previous processing, if any
            _dataIntegrator.StopProcessing();

            if (newValue != null)
            {
                _dataIntegrator.StartProcessing(newValue);
            }
        }

        #region Saved configurations

        /// <summary>
        /// Rebuilds the configuration list after a change outside this view model (CFG-006, CFG-019). The selection is kept by id,
        /// and falls back to Default if the configuration is gone. Unsaved edits are kept.
        /// </summary>
        public void RefreshConfigurations()
        {
            var items = BuildConfigurationItems();
            SetConfigurationList(items, _selectedConfiguration?.Id ?? Guid.Empty, applyMappings: false);
        }

        private void ConfigurationStore_Changed(object? sender, EventArgs e)
        {
            // This view model refreshes after its own changes; this handles the Settings page and the folder watcher.
            if (_ownStoreChanges > 0)
                return;

            var items = BuildConfigurationItems();

            // The app's own writes also reach the folder watcher; nothing changed, so leave the list alone.
            if (string.Equals(Signature(items), _listSignature, StringComparison.Ordinal))
                return;

            SetConfigurationList(items, _selectedConfiguration?.Id ?? Guid.Empty, applyMappings: false);
        }

        private void LoadConfigurationsForCurrentController()
        {
            var items = BuildConfigurationItems();

            // CFG-016 to CFG-018: Default when there is nothing saved or the last-used one is gone; otherwise the last-used one.
            var selectedId = Guid.Empty;
            if (items.Count > 1 && _currentModel is not null
                && _appSettings.GetLastUsed(_matcher.GetKey(_currentModel)) is Guid lastUsed
                && items.Any(i => i.Id == lastUsed))
            {
                selectedId = lastUsed;
            }

            SetConfigurationList(items, selectedId, applyMappings: true);
        }

        private List<ConfigurationItem> BuildConfigurationItems()
        {
            var items = new List<ConfigurationItem> { new(ConfigurationNames.DefaultName, _defaultMappings, null) };
            if (_currentModel is null)
                return items;

            var model = _currentModel;
            items.AddRange(LoadAllSafely()
                .Where(c => _matcher.Matches(c.Controller, model))
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.Id)
                .Select(c => new ConfigurationItem(c.Name, c.Mappings, c)));
            return items;
        }

        private IReadOnlyList<SavedConfiguration> LoadAllSafely()
        {
            try
            {
                return _configurationStore.LoadAll();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return [];
            }
        }

        private void SetConfigurationList(List<ConfigurationItem> items, Guid selectedId, bool applyMappings)
        {
            bool wasDirty = IsDirty;
            var target = items.FirstOrDefault(i => i.Id == selectedId) ?? items[0];

            _isRebuildingList = true;
            try
            {
                Configurations = items;
                _selectedConfiguration = target;
                OnPropertyChanged(nameof(SelectedConfiguration));
            }
            finally
            {
                _isRebuildingList = false;
            }

            _listSignature = Signature(items);

            // Unsaved edits survive a refresh; without edits the mappings follow the (possibly changed) file.
            if (applyMappings || !wasDirty)
                ApplyMappings(target.Mappings);

            UpdateDirtyState();
            RaisePropertyChangedAgain(nameof(SelectedConfiguration));
        }

        private static string Signature(List<ConfigurationItem> items) =>
            string.Join("|", items.Select(i => i.Configuration is null
                ? "default"
                : string.Join(";", i.Id, i.Name, i.Configuration.Modified.UtcTicks, i.Configuration.FilePath,
                    string.Join(",", i.Mappings.OrderBy(kv => kv.Key).Select(kv => (int)kv.Value)))));

        private bool SaveSelected()
        {
            var item = _selectedConfiguration;
            if (item is null)
                return false;
            if (item.Configuration is null)
                return SaveAsNew(_currentModel);

            SavedConfiguration? saved = null;
            var updated = item.Configuration with { Mappings = GetCurrentMappings() };
            if (!TryChangeStore("Couldn't save the configuration", () => saved = _configurationStore.Save(updated)))
                return false;

            SetConfigurationList(BuildConfigurationItems(), saved!.Id, applyMappings: false);
            RememberLastUsed(_currentModel, saved.Id);
            return true;
        }

        private bool SaveAsNew(ControllerModel? model)
        {
            if (model is null)
                return false;

            var existingNames = (ReferenceEquals(model, _currentModel)
                    ? _configurations.Where(i => !i.IsDefault).Select(i => i.Configuration!)
                    : LoadAllSafely().Where(c => _matcher.Matches(c.Controller, model)))
                .Select(c => c.Name)
                .ToList();
            var initialName = _selectedConfiguration is { IsDefault: false } selected
                ? ConfigurationNames.SuggestUnique(selected.Name, existingNames)
                : string.Empty;

            var name = _dialogs.PromptForName("Save configuration as", "Name this configuration.", initialName, "Save",
                text => ConfigurationNames.Validate(text, existingNames));
            if (name is null || ConfigurationNames.Validate(name, existingNames) is not null)
                return false;

            var configuration = new SavedConfiguration(Guid.NewGuid(), ConfigurationNames.Normalize(name), model,
                GetCurrentMappings(), DateTimeOffset.UtcNow);
            SavedConfiguration? saved = null;
            if (!TryChangeStore("Couldn't save the configuration", () => saved = _configurationStore.Save(configuration)))
                return false;

            SetConfigurationList(BuildConfigurationItems(), saved!.Id, applyMappings: false);
            RememberLastUsed(model, saved.Id);
            return true;
        }

        /// <summary>
        /// Asks what to do with unsaved changes before switching (CFG-024). Returns false if the user canceled or saving failed.
        /// </summary>
        /// <param name="modelForSave">The controller model to save under if the user saves edits made to Default.</param>
        private bool ConfirmLeaveUnsavedChanges(ControllerModel? modelForSave)
        {
            var item = _selectedConfiguration;
            if (!IsDirty || item is null)
                return true;

            return _dialogs.AskUnsavedChanges(item.Name) switch
            {
                UnsavedChangesChoice.Discard => true,
                UnsavedChangesChoice.Save => item.IsDefault ? SaveAsNew(modelForSave) : SaveSelected(),
                _ => false,
            };
        }

        private bool TryChangeStore(string errorTitle, Action change)
        {
            _ownStoreChanges++;
            try
            {
                change();
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ConfigurationFormatException)
            {
                _dialogs.ShowError(errorTitle, ex.Message);
                return false;
            }
            finally
            {
                _ownStoreChanges--;
            }
        }

        private void RememberLastUsed(ControllerModel? model, Guid id)
        {
            if (model is not null)
                _appSettings.SetLastUsed(_matcher.GetKey(model), id == Guid.Empty ? null : id);
        }

        private IEnumerable<ZwiftFunctionSelectorViewModel> AllSelectors =>
            ButtonFunctionMappings.Concat(DpadFunctionMappings).Concat(LeftStickFunctionMappings)
                .Concat(RightStickFunctionMappings).Concat(ShoulderFunctionMappings);

        /// <summary>
        /// Returns the mappings as currently shown, for every input.
        /// </summary>
        public IReadOnlyDictionary<ControllerInput, ZwiftFunction> GetCurrentMappings() =>
            ConfigurationMappings.Normalize(AllSelectors.Select(vm =>
                new KeyValuePair<ControllerInput, ZwiftFunction>(vm.SelectedInput, vm.SelectedZwiftFunction)));

        private void ApplyMappings(IReadOnlyDictionary<ControllerInput, ZwiftFunction> mappings)
        {
            _isApplyingMappings = true;
            try
            {
                // Inputs a configuration does not mention are set to None (CFG-010).
                foreach (var vm in AllSelectors)
                {
                    var function = mappings.TryGetValue(vm.SelectedInput, out var f) ? f : ZwiftFunction.None;
                    vm.SetInputMapping(new InputMapping(vm.SelectedInput, function));
                }
            }
            finally
            {
                _isApplyingMappings = false;
            }
        }

        private void UpdateDirtyState()
        {
            var selected = _selectedConfiguration;
            bool dirty = selected is not null && !ConfigurationMappings.AreEqual(GetCurrentMappings(), selected.Mappings);
            foreach (var item in _configurations)
                item.IsDirty = dirty && ReferenceEquals(item, selected);
            IsDirty = dirty;
            UpdateCommandStates();
        }

        private void UpdateCommandStates()
        {
            SaveCommand.NotifyCanExecuteChanged();
            SaveAsCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
            RenameCommand.NotifyCanExecuteChanged();
            ImportCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Tells bindings to read a property again, now and once the current input has been handled. A ComboBox ignores a
        /// change raised while it is still pushing its own selection, so the second notification puts it back in sync.
        /// </summary>
        private void RaisePropertyChangedAgain(string propertyName)
        {
            OnPropertyChanged(propertyName);
            SynchronizationContext.Current?.Post(_ => OnPropertyChanged(propertyName), null);
        }

        #endregion

        #region Support Child ViewModels

        partial void OnCurrentControllerValuesChanged(ControllerData? oldValue, ControllerData? newValue)
        {
            if (newValue is null)
                return;

            foreach (ZwiftFunctionSelectorViewModel vm in ButtonFunctionMappings)
                vm.ControllerData = newValue;

            foreach (ZwiftFunctionSelectorViewModel vm in DpadFunctionMappings)
                vm.ControllerData = newValue;

            foreach (ZwiftFunctionSelectorViewModel vm in LeftStickFunctionMappings)
                vm.ControllerData = newValue;

            foreach (ZwiftFunctionSelectorViewModel vm in RightStickFunctionMappings)
                vm.ControllerData = newValue;

            foreach (ZwiftFunctionSelectorViewModel vm in ShoulderFunctionMappings)
                vm.ControllerData = newValue;
        }

        public List<ZwiftFunctionSelectorViewModel> CreateButtonMappings()
        {
            var inputMappings = new List<ZwiftFunctionSelectorViewModel>();

            var inputMappingsData = new Dictionary<string, ControllerInput>
            {
                { "A", ControllerInput.A },
                { "B", ControllerInput.B },
                { "X", ControllerInput.X },
                { "Y", ControllerInput.Y },
                { "Menu", ControllerInput.Menu },
                { "View", ControllerInput.View }
            };

            foreach (var kvp in inputMappingsData)
            {
                inputMappings.Add(new ZwiftFunctionSelectorViewModel
                {
                    InputName = kvp.Key,
                    SelectedInput = kvp.Value
                });
            }

            foreach (var vm in inputMappings)
                vm.MappingChanged += InputMappingChanged;

            return inputMappings;
        }

        private void InputMappingChanged(object? sender, EventArgs e)
        {
            if (sender is ZwiftFunctionSelectorViewModel selector)
            {
                _dataIntegrator.UpdateMapping(selector.GetInputMapping());

                // Applying a configuration sets every input; the dirty state is updated once afterwards.
                if (!_isApplyingMappings)
                    UpdateDirtyState();
            }
        }

        public List<ZwiftFunctionSelectorViewModel> CreateDPadMappings()
        {
            var inputMappings = new List<ZwiftFunctionSelectorViewModel>();

            var inputMappingsData = new Dictionary<string, ControllerInput>
            {
                { "Up", ControllerInput.DPadUp },
                { "Left", ControllerInput.DPadLeft },
                { "Down", ControllerInput.DPadDown },
                { "Right", ControllerInput.DPadRight }
            };

            foreach (var kvp in inputMappingsData)
            {
                inputMappings.Add(new ZwiftFunctionSelectorViewModel
                {
                    InputName = kvp.Key,
                    SelectedInput = kvp.Value
                });
            }

            foreach (var vm in inputMappings)
                vm.MappingChanged += InputMappingChanged;

            return inputMappings;
        }

        public List<ZwiftFunctionSelectorViewModel> CreateLeftStickMappings()
        {
            var inputMappings = new List<ZwiftFunctionSelectorViewModel>();

            var inputMappingsData = new Dictionary<string, ControllerInput>
            {
                { "Left Stick (L3)", ControllerInput.LeftThumbstickClick },
                { "Up", ControllerInput.LeftThumbstickTiltUp},
                { "Left", ControllerInput.LeftThumbstickTiltLeft },
                { "Down", ControllerInput.LeftThumbstickTiltDown },
                { "Right", ControllerInput.LeftThumbstickTiltRight }
            };

            foreach (var kvp in inputMappingsData)
            {
                inputMappings.Add(new ZwiftFunctionSelectorViewModel
                {
                    InputName = kvp.Key,
                    SelectedInput = kvp.Value
                });
            }

            foreach (var vm in inputMappings)
                vm.MappingChanged += InputMappingChanged;

            return inputMappings;
        }

        public List<ZwiftFunctionSelectorViewModel> CreateRightStickMappings()
        {
            var inputMappings = new List<ZwiftFunctionSelectorViewModel>();

            var inputMappingsData = new Dictionary<string, ControllerInput>
            {
                { "Right Stick (R3)", ControllerInput.RightThumbstickClick },
                { "Up", ControllerInput.RightThumbstickTiltUp },
                { "Left", ControllerInput.RightThumbstickTiltLeft },
                { "Down", ControllerInput.RightThumbstickTiltDown },
                { "Right", ControllerInput.RightThumbstickTiltRight }
            };

            foreach (var kvp in inputMappingsData)
            {
                inputMappings.Add(new ZwiftFunctionSelectorViewModel
                {
                    InputName = kvp.Key,
                    SelectedInput = kvp.Value
                });
            }

            foreach (var vm in inputMappings)
                vm.MappingChanged += InputMappingChanged;

            return inputMappings;
        }

        public List<ZwiftFunctionSelectorViewModel> CreateShoulderMappings()
        {
            var inputMappings = new List<ZwiftFunctionSelectorViewModel>();

            var inputMappingsData = new Dictionary<string, ControllerInput>
            {
                { "Left Bumper (L1)", ControllerInput.LeftBumper },
                { "Right Bumper (R1)", ControllerInput.RightBumper },
                { "Left Trigger (L2)", ControllerInput.LeftTrigger },
                { "Right Trigger (R2)", ControllerInput.RightTrigger }
            };

            foreach (var kvp in inputMappingsData)
            {
                inputMappings.Add(new ZwiftFunctionSelectorViewModel
                {
                    InputName = kvp.Key,
                    SelectedInput = kvp.Value
                });
            }

            foreach (var vm in inputMappings)
                vm.MappingChanged += InputMappingChanged;

            return inputMappings;
        }

        #endregion


        private void _dataIntegrator_InputChanged(object? sender, InputStateChangedEventArgs e)
        {
            var app = Application.Current;

            // may be null if app is closing
            if (app != null)
            {
                // Ensure that UI updates happen on the UI thread
                app.Dispatcher.Invoke(() =>
                {
                    CurrentControllerValues = e.Data;
                });
            }
        }

        #endregion
    }
}
