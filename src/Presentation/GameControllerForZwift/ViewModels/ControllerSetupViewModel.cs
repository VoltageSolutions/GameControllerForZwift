using GameControllerForZwift.Core.Controller;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Mapping;
using System.Windows;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    public partial class ControllerSetupViewModel : ObservableObject
    {
        #region Fields

        [ObservableProperty]
        private string _title = "Controller Setup";

        [ObservableProperty]
        private string _description = "Select a game controller and configure its button mapping to Zwift functions.";

        //[ObservableProperty]
        //ZwiftFunctionSelectorViewModel _selectorViewModel;

        private readonly IDataIntegrator _dataIntegrator;

        private readonly IControllerProfileService _profileService;

        [ObservableProperty]
        private List<IController> _controllers = new();
        [ObservableProperty]
        private IController? _selectedController;
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

        #endregion

        #region Constructor

        public ControllerSetupViewModel(IDataIntegrator dataIntegrator, IControllerProfileService profileManager)
        {
            _dataIntegrator = dataIntegrator;
            _profileService = profileManager;

            // Create initial mappings
            _buttonFunctionMappings = CreateButtonMappings();
            _dpadFunctionMappings = CreateDPadMappings();
            _leftStickFunctionMappings = CreateLeftStickMappings();
            _rightStickFunctionMappings = CreateRightStickMappings();
            _shoulderFunctionMappings = CreateShoulderMappings();

            // Load default profile and apply it
            LoadDefaultProfile();

            _dataIntegrator.InputPolled += _dataIntegrator_InputChanged;

            // Auto setup the first controller
            RefreshControllerList();
            if (Controllers.Count > 0)
            {
                SelectedController = Controllers.First();
            }
        }

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


        #endregion

        #region Methods

        partial void OnSelectedControllerChanged(IController? oldValue, IController? newValue)
        {
            // Stop the previous processing, if any
            _dataIntegrator.StopProcessing();

            if (newValue != null)
            {
                _dataIntegrator.StartProcessing(newValue);
            }
        }

        #region Support Child ViewModels

        private void LoadDefaultProfile()
        {
            var profile = _profileService.GetDefaultProfile();
            if (profile is null)
                return;

            ApplyProfileToMappings(profile, ButtonFunctionMappings);
            ApplyProfileToMappings(profile, DpadFunctionMappings);
            ApplyProfileToMappings(profile, LeftStickFunctionMappings);
            ApplyProfileToMappings(profile, RightStickFunctionMappings);
            ApplyProfileToMappings(profile, ShoulderFunctionMappings);
        }

        private static void ApplyProfileToMappings(ControllerProfile profile, List<ZwiftFunctionSelectorViewModel> viewMappings)
        {
            foreach (var vm in viewMappings)
            {
                if (profile.Mappings.Where(m => m.Input == vm.SelectedInput).FirstOrDefault() is InputMapping inputMap)
                {
                    vm.SetInputMapping(inputMap);
                }
            }
        }

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
