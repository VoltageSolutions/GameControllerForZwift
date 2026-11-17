using GameControllerForZwift.Core.Controller;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.Core.Mapping;
using GameControllerForZwift.UI.WPF.Dialogs;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    public partial class ControllerSetupViewModel : ObservableObject
    {
        #region Fields

        /// <summary>
        /// The notice shown after a successful import (CFG-031).
        /// </summary>
        public const string ImportNotice = "Saved a copy to your configurations.";

        /// <summary>
        /// How often the app checks for controllers that connected or disconnected (CTRL-002).
        /// </summary>
        public static readonly TimeSpan ControllerDetectionInterval = TimeSpan.FromMilliseconds(500);

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
        private DispatcherTimer? _detectionTimer;
        private bool _isDetectingControllers;
        private bool _showOtherControllersConfigurations;

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

            _showOtherControllersConfigurations = _appSettings.GetShowOtherControllersConfigurations();

            // Load default profile and apply it
            _defaultMappings = ConfigurationMappings.FromProfile(_profileService.GetDefaultProfile());
            SetConfigurationList(BuildConfigurationItems(), Guid.Empty, applyMappings: true);

            _configurationStore.Changed += ConfigurationStore_Changed;
            _dataIntegrator.InputPolled += _dataIntegrator_InputChanged;

            // Lists the connected controllers and selects the first one (CTRL-007).
            DetectControllers();
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

                // A null (no controller selected) does not change which configurations apply.
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
        /// Gets the connected controllers. The list is updated in place, so a controller that stays connected keeps its
        /// entry and stays selected while others come and go.
        /// </summary>
        public ObservableCollection<IController> Controllers { get; } = [];

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

                // CFG-040: a configuration of another controller is used through a copy for this one.
                if (value.IsFromOtherController)
                {
                    UseCopyOfOtherControllersConfiguration(value.Configuration!);
                    RaisePropertyChangedAgain(nameof(SelectedConfiguration));
                    return;
                }

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
        /// Gets or sets a value indicating whether the configuration list also shows the saved configurations of other
        /// controller models (CFG-039). The choice is kept in settings.json.
        /// </summary>
        public bool ShowOtherControllersConfigurations
        {
            get => _showOtherControllersConfigurations;
            set
            {
                if (!SetProperty(ref _showOtherControllersConfigurations, value))
                    return;

                _appSettings.SetShowOtherControllersConfigurations(value);
                SetConfigurationList(BuildConfigurationItems(), _selectedConfiguration?.Id ?? Guid.Empty, applyMappings: false);
            }
        }

        /// <summary>
        /// Gets the model of the selected controller, or null if no controller has been selected yet.
        /// </summary>
        public ControllerModel? CurrentControllerModel => _currentModel;

        #endregion

        #region Commands
        /// <summary>
        /// Updates the controller list from the controllers connected now (CTRL-001 to CTRL-008). Controllers that stay
        /// connected keep their entry. The selected controller stays selected while it is connected. Nothing on screen
        /// changes when no controller connected or disconnected.
        /// </summary>
        public void DetectControllers()
        {
            // A prompt opened by an automatic switch (CFG-024) runs a nested message loop, so the timer can tick meanwhile.
            if (_isDetectingControllers)
                return;

            _isDetectingControllers = true;
            try
            {
                var change = ControllerListChange.Between(Controllers, _dataIntegrator.GetControllers());
                if (!change.HasChanges)
                    return;

                // New entries go in first, so the selection can move to one before the old selected entry is removed.
                foreach (var controller in change.Added)
                    Controllers.Add(controller);

                var previous = _selectedController;
                var next = ControllerSelection.Choose(previous, change);
                if (next is not null && !ReferenceEquals(next, previous))
                {
                    SelectedController = next;

                    // CTRL-008: the user canceled the switch; the disconnected controller is no longer selectable.
                    if (ReferenceEquals(_selectedController, previous) && previous is not null)
                        SelectedController = null;
                }
                else if (next is null && change.Result.Count == 0)
                {
                    ClearControllerSelection();
                }

                foreach (var controller in change.Removed)
                    RemoveController(controller);
            }
            finally
            {
                _isDetectingControllers = false;
            }
        }

        /// <summary>
        /// Starts checking for controllers that connect or disconnect, every <see cref="ControllerDetectionInterval"/>, on the
        /// calling thread (CTRL-002). Call it on the UI thread: that thread owns SDL and the controller list.
        /// </summary>
        public void StartControllerDetection()
        {
            _detectionTimer ??= new DispatcherTimer(ControllerDetectionInterval, DispatcherPriority.Background,
                (_, _) => DetectControllers(), Dispatcher.CurrentDispatcher);
            _detectionTimer.Start();
        }

        /// <summary>
        /// Stops checking for controllers.
        /// </summary>
        public void StopControllerDetection() => _detectionTimer?.Stop();

        /// <summary>
        /// Gets a value indicating whether the app checks for controllers that connect or disconnect.
        /// </summary>
        public bool IsDetectingControllers => _detectionTimer?.IsEnabled == true;

        /// <summary>
        /// Shows no controller after the last one disconnected (CTRL-006): only Default is listed, and Save as and Import are
        /// disabled. Unsaved changes are handled first (CFG-024). If the user cancels, the edits and their configuration list stay.
        /// </summary>
        private void ClearControllerSelection()
        {
            if (_selectedController is null && _currentModel is null)
                return;

            bool leave = ConfirmLeaveUnsavedChanges(_currentModel);
            SelectedController = null;
            if (!leave)
                return;

            _currentModel = null;
            LoadConfigurationsForCurrentController();
        }

        private void RemoveController(IController controller)
        {
            for (int i = Controllers.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(Controllers[i], controller))
                    Controllers.RemoveAt(i);
            }
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

            var otherNames = OwnSavedItems().Where(i => i.Id != item.Id).Select(i => i.Name).ToList();
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
            SaveCopyAndSelect(imported, model,
                "Use for this controller?",
                $"'{imported.Name}' was made for {imported.Controller.DisplayName}. Use it for {model.DisplayName}?",
                "Name the imported configuration", "Import", "Couldn't import the configuration");
        }

        /// <summary>
        /// Saves a copy of another controller model's configuration for the selected controller and selects it, if the user
        /// agrees (CFG-040). The original stays with its own controller.
        /// </summary>
        private void UseCopyOfOtherControllersConfiguration(SavedConfiguration source)
        {
            var model = _currentModel;
            if (model is null)
                return;

            SaveCopyAndSelect(source, model,
                $"Use {source.Name} with this controller?",
                $"'{source.Name}' was made for {source.Controller.DisplayName}. Save a copy for {model.DisplayName} and use it?",
                "Name the copy", "Save copy", "Couldn't copy the configuration");
        }

        /// <summary>
        /// Saves a configuration into the config folder for the given controller model and selects it (CFG-030 to CFG-033,
        /// CFG-040). A configuration of another model is only used if the user agrees. A name that is taken gets a new one.
        /// Unsaved changes are handled before the switch (CFG-024). Returns false if the user stopped or saving failed.
        /// </summary>
        private bool SaveCopyAndSelect(SavedConfiguration source, ControllerModel model, string confirmTitle, string confirmMessage,
            string nameTitle, string nameConfirmText, string errorTitle)
        {
            var copy = source;
            if (!_matcher.Matches(copy.Controller, model))
            {
                if (!_dialogs.Confirm(confirmTitle, confirmMessage, "Use it"))
                    return false;
                copy = copy with { Id = Guid.NewGuid(), Controller = model };
            }

            // A file that is already in the folder (or a copy of one) must not create a second file with the same id.
            if (LoadAllSafely().Any(c => c.Id == copy.Id))
                copy = copy with { Id = Guid.NewGuid() };

            // CFG-032: a name that collides gets a new one, pre-filled with a suggestion.
            var existingNames = OwnSavedItems().Select(i => i.Name).ToList();
            if (ConfigurationNames.Validate(copy.Name, existingNames) is not null)
            {
                var suggestion = ConfigurationNames.SuggestUnique(copy.Name, existingNames);
                var name = _dialogs.PromptForName(nameTitle,
                    $"A configuration named '{copy.Name}' already exists for this controller. Choose another name.",
                    suggestion, nameConfirmText, text => ConfigurationNames.Validate(text, existingNames));
                if (name is null || ConfigurationNames.Validate(name, existingNames) is not null)
                    return false;
                copy = copy with { Name = ConfigurationNames.Normalize(name) };
            }

            // Selecting the copy switches configuration, so unsaved changes are handled first (CFG-024).
            if (!ConfirmLeaveUnsavedChanges(model))
                return false;

            SavedConfiguration? saved = null;
            if (!TryChangeStore(errorTitle, () => saved = _configurationStore.Save(copy with { FilePath = null })))
                return false;

            SetConfigurationList(BuildConfigurationItems(), saved!.Id, applyMappings: true);
            RememberLastUsed(model, saved.Id);
            NoticeText = ImportNotice;
            return true;
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
                && items.Any(i => i.Id == lastUsed && !i.IsFromOtherController))
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
            var all = LoadAllSafely();
            items.AddRange(all
                .Where(c => _matcher.Matches(c.Controller, model))
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.Id)
                .Select(c => new ConfigurationItem(c.Name, c.Mappings, c)));

            // CFG-039: after this controller's entries, the other models' configurations by name, labeled with their controller.
            if (_showOtherControllersConfigurations)
            {
                items.AddRange(all
                    .Where(c => !_matcher.Matches(c.Controller, model))
                    .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(c => c.Controller.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(c => c.Id)
                    .Select(c => new ConfigurationItem(c.Name, c.Mappings, c, isFromOtherController: true)));
            }

            return items;
        }

        /// <summary>
        /// Returns the saved configurations of the selected controller model, without Default and other models' entries.
        /// </summary>
        private IEnumerable<ConfigurationItem> OwnSavedItems() => _configurations.Where(i => !i.IsDefault && !i.IsFromOtherController);

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
                    ? OwnSavedItems().Select(i => i.Configuration!)
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
