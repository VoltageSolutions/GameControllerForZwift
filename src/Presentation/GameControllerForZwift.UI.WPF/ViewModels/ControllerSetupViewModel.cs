using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.Core;
using GameControllerForZwift.Logic;
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

        [ObservableProperty]
        ZwiftFunctionSelectorViewModel _selectorViewModel;

        private DataIntegrator _dataIntegrator;
        private IInputService _inputService;
        [ObservableProperty]
        private List<IController> _controllers;
        [ObservableProperty]
        private IController _selectedController;
        [ObservableProperty]
        private ControllerData _currentControllerValues;

        #endregion

        #region Constructor

        // todo - this should take an interface for the dataintegrator and should need need the input service
        public ControllerSetupViewModel(DataIntegrator dataIntegrator, IInputService inputService)
        {
            _dataIntegrator = dataIntegrator;
            _inputService = inputService;
            _selectorViewModel = new ZwiftFunctionSelectorViewModel();

            //InitializeTimer();
            //_dataIntegrator.InputChanged += _dataIntegrator_InputChanged;
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
                    SelectorViewModel.ControllerData = CurrentControllerValues;
                });
            }
        }

        #endregion

        #region Commands
        [RelayCommand]
        public void RefreshControllerList()
        {
            Controllers = _dataIntegrator.GetControllers().ToList();
        }

        [RelayCommand]
        public void ResetMappingsToDefault()
        {

        }

        [RelayCommand]
        public void ClearAllMappings()
        {
            
        }

        
        #endregion

        #region Methods

        partial void OnSelectedControllerChanged(IController oldValue, IController newValue)
        {
            // Stop the previous processing, if any
            _dataIntegrator.StopProcessing();
            //_stopProcessingTask = Task.Run(() => StopProcessingAsync());
            StopProcessingAsync();

            // Start processing the new controller if it's not null
            if (newValue != null)
            {
                _dataIntegrator.StartProcessing(newValue);
                // local
                StartProcessing();
            }
        }

        #endregion

        private ControllerData _latestData;
        private DispatcherTimer _updateTimer;

        public void InitializeTimer()
        {
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };
            _updateTimer.Tick += (s, e) => UpdateUI();
            _updateTimer.Start();
        }

        private void UpdateUI()
        {
            if (_dataIntegrator.DataQueue.TryDequeue(out ControllerData controllerData))
            {
                CurrentControllerValues = controllerData;
                SelectorViewModel.ControllerData = CurrentControllerValues;
            }
        }


        private Task _processingTask;
        private Task _stopProcessingTask;
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
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CurrentControllerValues = controllerData;
                        SelectorViewModel.ControllerData = CurrentControllerValues;
                    }, System.Windows.Threading.DispatcherPriority.Render);
                    //CurrentControllerValues = controllerData;
                    //SelectorViewModel.ControllerData = CurrentControllerValues;
                    //System.Diagnostics.Debug.WriteLine($"Dequeued: {controllerData}");
                    //System.Diagnostics.Debug.WriteLine($"Current Timestamp: " + DateTime.Now.ToString("HH:mm:ss.fff"));
                    //System.Diagnostics.Debug.WriteLine($"Controller Timestamp: " + controllerData.Timestamp.ToString("HH:mm:ss.fff"));
                    //System.Diagnostics.Debug.WriteLine($"Left Stick X: {controllerData.LeftThumbstickX.ToString()}");
                    //System.Diagnostics.Debug.WriteLine($"Left Stick Y: {controllerData.LeftThumbstickY.ToString()}");
                }

                // Avoid busy-waiting by delaying briefly
                await Task.Delay(10, cancellationToken);
            }
        }
    }
}
