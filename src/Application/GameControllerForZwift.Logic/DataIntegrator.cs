using GameControllerForZwift.Core;
using System.Collections.Concurrent;
using System.Reflection.Metadata;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GameControllerForZwift.Logic
{
    public class DataIntegrator
    {
        #region Fields
        private readonly IInputService _inputService;
        private readonly IOutputService _outputService;
        public ConcurrentQueue<ControllerData> DataQueue => _dataQueue;
        private readonly ConcurrentQueue<ControllerData> _dataQueue = new ConcurrentQueue<ControllerData>();
        private readonly int _maxQueueSize;
        private CancellationTokenSource _cts;// = new CancellationTokenSource();
        public event EventHandler<InputStateChangedEventArgs> InputPolled;
        private ControllerData? _lastDataState;
        #endregion

        #region Constructor
        // todo - pass in required interfaces for reader and writer
        public DataIntegrator(IInputService inputService, IOutputService outputService, int maxQueueSize = 10)
        {
            _inputService = inputService;
            _outputService = outputService;
            _maxQueueSize = maxQueueSize;
        }
        #endregion
        

        #region Methods

        public IEnumerable<IController> GetControllers() => _inputService.GetControllers();

        public void StartProcessing(IController controller)
        {
            if (null != _cts)
                _cts.Dispose();
            _cts = new CancellationTokenSource();

            _ = Task.Run(() => ReadDataAsync(controller, _cts.Token));
            _ = Task.Run(() => WriteDataAsync(_cts.Token));
        }

        public void StopProcessing()
        {
            if (null != _cts)
                _cts.Cancel();
        }

        private async Task ReadDataAsync(IController controller, CancellationToken cancellationToken)
        {
            if (controller != null)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var controllerData = controller.ReadData();
                    ProcessControllerData(controllerData);

                    await Task.Delay(25, cancellationToken);
                }
            }
        }

        public void ProcessControllerData(ControllerData controllerData)
        {
            // Add data to the queue, discarding the oldest if the queue is full
            if (_dataQueue.Count >= _maxQueueSize)
            {
                _dataQueue.TryDequeue(out _); // Remove the oldest data
            }

            _dataQueue.Enqueue(controllerData);
            //InputChanged?.Invoke(this, new InputStateChangedEventArgs(ControllerInput.LeftThumbstickX, controllerData));
        }

        protected virtual void OnInputPulled(InputStateChangedEventArgs e)
        {
            InputPolled?.Invoke(this, e);
        }

        private async Task WriteDataAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                while (_dataQueue.TryDequeue(out ControllerData controllerData))
                {
                    InputPolled?.Invoke(this, new InputStateChangedEventArgs(controllerData));

                    // call the interface
                    if (controllerData.A)
                        _outputService.PerformAction(ZwiftFunction.Select, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                    if (controllerData.B)
                        _outputService.PerformAction(ZwiftFunction.GoBack, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                    if (controllerData.Y)
                        _outputService.PerformAction(ZwiftFunction.HideHUD, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                    if (controllerData.DPadUp)
                        _outputService.PerformAction(ZwiftFunction.ShowMenu, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                    if (controllerData.DPadDown)
                        _outputService.PerformAction(ZwiftFunction.Uturn, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                    if (controllerData.DPadLeft)
                        _outputService.PerformAction(ZwiftFunction.NavigateLeft, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                    if (controllerData.DPadRight)
                        _outputService.PerformAction(ZwiftFunction.NavigateRight, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                    if (controllerData.LeftBumper)
                        _outputService.PerformAction(ZwiftFunction.FTPBiasDown, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                    if (controllerData.RightBumper)
                        _outputService.PerformAction(ZwiftFunction.FTPBiasUp, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                }
                await Task.Delay(10, cancellationToken); // Adjust delay based on needs
            }
        }
        #endregion
    }
}
