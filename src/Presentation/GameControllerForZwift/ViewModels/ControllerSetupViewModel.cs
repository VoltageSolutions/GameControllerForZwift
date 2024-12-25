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
        private List<IController> _controllers;
        [ObservableProperty]
        private IController _selectedController;
        [ObservableProperty]
        private ControllerData _currentControllerValues;

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
            if (Controllers.Any())
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

        partial void OnSelectedControllerChanged(IController oldValue, IController newValue)
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

            ApplyProfileToMappings(profile, _buttonFunctionMappings);
            ApplyProfileToMappings(profile, _dpadFunctionMappings);
            ApplyProfileToMappings(profile, _leftStickFunctionMappings);
            ApplyProfileToMappings(profile, _rightStickFunctionMappings);
            ApplyProfileToMappings(profile, _shoulderFunctionMappings);
        }

        private void ApplyProfileToMappings(ControllerProfile profile, List<ZwiftFunctionSelectorViewModel> viewMappings)
        {
            foreach (var vm in viewMappings)
            {
                if(profile.Mappings.Where(m => m.Input == vm.SelectedInput).FirstOrDefault() is InputMapping inputMap)
                {
                    vm.SetInputMapping(inputMap);
                }
            }
        }

        partial void OnCurrentControllerValuesChanged(ControllerData oldValue, ControllerData newValue)
        {
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
            if ((null != sender) && (sender.GetType() == typeof(ZwiftFunctionSelectorViewModel)))
            {
                _dataIntegrator.UpdateMapping((sender as ZwiftFunctionSelectorViewModel).GetInputMapping());
            }
        }

        public List<ZwiftFunctionSelectorViewModel> CreateDPadMappings()
        {
            var inputMappings = new List<ZwiftFunctionSelectorViewModel>();

            var inputMappingsData = new Dictionary<string, ControllerInput>
            {
                { "Up", ControllerInput.DPad_Up },
                { "Left", ControllerInput.DPad_Left },
                { "Down", ControllerInput.DPad_Down },
                { "Right", ControllerInput.DPad_Right }
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
                { "Left Stick (L3)", ControllerInput.LeftThumbstick_Click },
                { "Up", ControllerInput.LeftThumbstick_TiltUp},
                { "Left", ControllerInput.LeftThumbstick_TiltLeft },
                { "Down", ControllerInput.LeftThumbstick_TiltDown },
                { "Right", ControllerInput.LeftThumbstick_TiltRight }
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
                { "Right Stick (R3)", ControllerInput.RightThumbstick_Click },
                { "Up", ControllerInput.RightThumbstick_TiltUp },
                { "Left", ControllerInput.RightThumbstick_TiltLeft },
                { "Down", ControllerInput.RightThumbstick_TiltDown },
                { "Right", ControllerInput.RightThumbstick_TiltRight }
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
                { "Right Bumper (R1)", ControllerInput.RightBumper }
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
