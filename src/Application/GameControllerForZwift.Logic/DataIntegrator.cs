using GameControllerForZwift.Core;
using System.Collections.Concurrent;
using System.Reflection.Metadata;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GameControllerForZwift.Logic
{
    public class DataIntegrator
    {
        #region Fields
        private IInputService _inputService;
        public ConcurrentQueue<ControllerData> DataQueue => _dataQueue;
        private readonly ConcurrentQueue<ControllerData> _dataQueue = new ConcurrentQueue<ControllerData>();
        private readonly int _maxQueueSize;
        private CancellationTokenSource _cts;// = new CancellationTokenSource();
        public event EventHandler<InputStateChangedEventArgs> InputChanged;
        private ControllerData? _lastDataState;
        #endregion

        #region Constructor
        // todo - pass in required interfaces for reader and writer
        public DataIntegrator(IInputService inputService, int maxQueueSize = 10000)
        {
            _inputService = inputService;
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
            //StartPolling(controller, _cts.Token);
            //_ = Task.Run(() => WriteDataAsync(_cts.Token));
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

                    await Task.Delay(10, cancellationToken);
                }
            }
        }

        public void ProcessControllerData(ControllerData controllerData)
        {
            // Add data to the queue, discarding the oldest if the queue is full
            //if (_dataQueue.Count >= _maxQueueSize)
            //{
            //    _dataQueue.TryDequeue(out _); // Remove the oldest data
            //}

            //if (controllerData.TryGetSingleChange(_lastDataState, out var singleChange))
            //{
            //    InputChanged?.Invoke(this, new InputStateChangedEventArgs(singleChange, controllerData));
            //    //_dataQueue.Enqueue(controllerData);
            //}
            //else
            //{
            //    // Multiple changes: Fire the ButtonPressed event for all changes
            //    foreach (var input in Enum.GetValues<ControllerInput>())
            //    {
            //        bool currentStatePressed = controllerData.IsPressed(input);
            //        bool previousStatePressed = _lastDataState?.IsPressed(input) ?? false;

            //        if (currentStatePressed != previousStatePressed)
            //        {
            //            InputChanged?.Invoke(this, new InputStateChangedEventArgs(input, controllerData));
            //            //_dataQueue.Enqueue(controllerData);
            //        }
            //    }
            //}

            //_lastDataState = controllerData;


            InputChanged?.Invoke(this, new InputStateChangedEventArgs(ControllerInput.LeftThumbstickX, controllerData));
        }

        protected virtual void OnButtonChanged(InputStateChangedEventArgs e)
        {
            InputChanged?.Invoke(this, e);
        }

        private async Task WriteDataAsync(CancellationToken cancellationToken)
        {
            //while (!cancellationToken.IsCancellationRequested)
            //{
            //    while (_dataQueue.TryDequeue(out var data))
            //    {
            //        // call the interface
            //    }
            //    await Task.Delay(500, cancellationToken); // Adjust delay based on needs
            //}
        }
        #endregion
    }
}
