using GameControllerForZwift.Core;
using System.Collections.Concurrent;

namespace GameControllerForZwift.Logic
{
    public class DataIntegrator : IDataIntegrator
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
                        await _outputService.PerformActionAsync(ZwiftFunction.Select, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                    if (controllerData.B)
                        await _outputService.PerformActionAsync(ZwiftFunction.GoBack, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                    if (controllerData.Y)
                        await _outputService.PerformActionAsync(ZwiftFunction.HideHUD, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                    if (controllerData.DPadUp)
                        await _outputService.PerformActionAsync(ZwiftFunction.ShowMenu, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                    if (controllerData.DPadDown)
                        await _outputService.PerformActionAsync(ZwiftFunction.Uturn, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                    if (controllerData.DPadLeft)
                        await _outputService.PerformActionAsync(ZwiftFunction.NavigateLeft, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                    if (controllerData.DPadRight)
                        await _outputService.PerformActionAsync(ZwiftFunction.NavigateRight, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                    if (controllerData.LeftStickUp)
                        await _outputService.PerformActionAsync(ZwiftFunction.ShowMenu, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                    if (controllerData.LeftStickDown)
                        await _outputService.PerformActionAsync(ZwiftFunction.Uturn, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                    if (controllerData.LeftStickLeft)
                        await _outputService.PerformActionAsync(ZwiftFunction.NavigateLeft, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                    if (controllerData.LeftStickRight)
                        await _outputService.PerformActionAsync(ZwiftFunction.NavigateRight, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                    if (controllerData.LeftBumper)
                        await _outputService.PerformActionAsync(ZwiftFunction.FTPBiasDown, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                    if (controllerData.RightBumper)
                        await _outputService.PerformActionAsync(ZwiftFunction.FTPBiasUp, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
                }
                await Task.Delay(10, cancellationToken); // Adjust delay based on needs
            }
        }
        #endregion
    }
}
