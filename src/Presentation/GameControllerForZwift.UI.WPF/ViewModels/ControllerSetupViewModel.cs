using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.Core;
using GameControllerForZwift.Logic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

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

        // todo - this should take an interface for the dataintegrator and should need need the input service
        public ControllerSetupViewModel(IDataIntegrator dataIntegrator)
        {
            _dataIntegrator = dataIntegrator;
            _buttonFunctionMappings = CreateButtonMappings();
            _dpadFunctionMappings = CreateDPadMappings();
            _leftStickFunctionMappings = CreateLeftStickMappings();
            _rightStickFunctionMappings = CreateRightStickMappings();
            _shoulderFunctionMappings = CreateShoulderMappings();

            _dataIntegrator.InputPolled += _dataIntegrator_InputChanged;
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
            //_stopProcessingTask = Task.Run(() => StopProcessingAsync());
            //StopProcessingAsync();

            // Start processing the new controller if it's not null
            if (newValue != null)
            {
                _dataIntegrator.StartProcessing(newValue);
                // local
                //StartProcessing();
            }
        }

        #endregion

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

            return inputMappings;
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

            return inputMappings;
        }

        public List<ZwiftFunctionSelectorViewModel> CreateLeftStickMappings()
        {
            var inputMappings = new List<ZwiftFunctionSelectorViewModel>();

            var inputMappingsData = new Dictionary<string, ControllerInput>
            {
                { "Left Stick (L3)", ControllerInput.LeftThumbstick },
                { "Up", ControllerInput.LeftThumbstickUp},
                { "Left", ControllerInput.LeftThumbstickLeft },
                { "Down", ControllerInput.LeftThumbstickDown },
                { "Right", ControllerInput.LeftThumbstickRight }
            };

            foreach (var kvp in inputMappingsData)
            {
                inputMappings.Add(new ZwiftFunctionSelectorViewModel
                {
                    InputName = kvp.Key,
                    SelectedInput = kvp.Value
                });
            }

            return inputMappings;
        }

        public List<ZwiftFunctionSelectorViewModel> CreateRightStickMappings()
        {
            var inputMappings = new List<ZwiftFunctionSelectorViewModel>();

            var inputMappingsData = new Dictionary<string, ControllerInput>
            {
                { "Right Stick (R3)", ControllerInput.RightThumbstick },
                { "Up", ControllerInput.RightThumbstickUp },
                { "Left", ControllerInput.RightThumbstickLeft },
                { "Down", ControllerInput.RightThumbstickDown },
                { "Right", ControllerInput.RightThumbstickRight }
            };

            foreach (var kvp in inputMappingsData)
            {
                inputMappings.Add(new ZwiftFunctionSelectorViewModel
                {
                    InputName = kvp.Key,
                    SelectedInput = kvp.Value
                });
            }

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

            return inputMappings;
        }


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
                    //SelectorViewModel.ControllerData = CurrentControllerValues;
                });
            }
        }

        private Task _processingTask;
        //private Task _stopProcessingTask;
        private CancellationTokenSource _cancellationTokenSource;
        public void StartProcessing()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _processingTask = Task.Run(() => ProcessQueueAsync(_cancellationTokenSource.Token));
        }

        // Function to stop the processing task
        public async Task StopProcessingAsync()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                try
                {
                    await _processingTask;
                }
                catch (OperationCanceledException)
                {
                    // Task was canceled, safe to ignore
                }
                finally
                {
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }
            }
        }

        // The async function to process the queue in a continuous loop
        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_dataIntegrator.DataQueue.TryDequeue(out ControllerData controllerData))
                {
                    var app = Application.Current;

                    // may be null if app is closing
                    if (app != null)
                    {
                        // Ensure that UI updates happen on the UI thread
                        app.Dispatcher.Invoke(() =>
                        {
                            CurrentControllerValues = controllerData;
                            //SelectorViewModel.ControllerData = CurrentControllerValues;
                        });
                    }
                }

                // Wait, but don't wait longer than the dataIntegrator
                await Task.Delay(10, cancellationToken);
            }
        }
    }
}
