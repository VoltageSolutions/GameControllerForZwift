using GameControllerForZwift.Core.Mapping;
using GameControllerForZwift.Core.Controller;
using System.Collections.Concurrent;

namespace GameControllerForZwift.Core
{
    public class DataIntegrator : IDataIntegrator
    {
        #region Fields
        private readonly IInputService _inputService;
        private readonly IControllerProfileService _controllerProfileService;
        private readonly IOutputService _outputService;
        public ConcurrentQueue<ControllerData> DataQueue => _dataQueue;
        private readonly ConcurrentQueue<ControllerData> _dataQueue = new ConcurrentQueue<ControllerData>();
        private readonly int _maxQueueSize;
        private CancellationTokenSource _cts;// = new CancellationTokenSource();
        public event EventHandler<InputStateChangedEventArgs> InputPolled;
        private ControllerData? _lastDataState;
        #endregion

        #region Constructor
        public DataIntegrator(IInputService inputService, IControllerProfileService controllerProfileService, IOutputService outputService, int maxQueueSize = 10)
        {
            _inputService = inputService;
            _controllerProfileService = controllerProfileService;
            _maxQueueSize = maxQueueSize;
            _outputService = outputService;
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

            try { _ = _outputService.UpdateAsync(OutputState.Neutral); }
            catch { /* best effort */ }
        }

        public async Task ReadDataAsync(IController controller, CancellationToken cancellationToken)
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
            if (controllerData == null)
                return; // disposed controllers return null

            // Add data to the queue, discarding the oldest if the queue is full
            if (_dataQueue.Count >= _maxQueueSize)
            {
                _dataQueue.TryDequeue(out _); // Remove the oldest data
            }

            _dataQueue.Enqueue(controllerData);
        }

        protected virtual void OnInputPolled(InputStateChangedEventArgs e)
        {
            InputPolled?.Invoke(this, e);
        }

        public void UpdateMapping(InputMapping mapping)
        {
            var profile = _controllerProfileService.Profiles.Profiles.FirstOrDefault();
            var originalMapping = profile.Mappings.Where(m => m.Input == mapping.Input).FirstOrDefault();
            if (originalMapping != null)
            {
                originalMapping.Function = mapping.Function;
            }
            else
            {
                profile.Mappings.Add(mapping);
            }
        }

        public async Task WriteDataAsync(CancellationToken cancellationToken)
        {
            var inputs = Enum.GetValues<ControllerInput>();

            while (!cancellationToken.IsCancellationRequested)
            {
                while (_dataQueue.TryDequeue(out ControllerData? controllerData))
                {
                    if (controllerData == null)
                        continue;

                    InputPolled?.Invoke(this, new InputStateChangedEventArgs(controllerData));

                    // this is not a good long-term way to get a profile, but it's a start
                    var profile = _controllerProfileService.Profiles?.Profiles?.FirstOrDefault();

                    var pressed = new HashSet<ZwiftFunction>();
                    double left = 0, right = 0, brake = 0;

                    if (profile != null)
                    {
                        foreach (var input in inputs)
                        {
                            var value = controllerData.GetAnalogValue(input);
                            if (value <= 0)
                                continue;

                            var mapping = profile.Mappings.FirstOrDefault(m => m.Input == input);
                            if (mapping == null || mapping.Function == ZwiftFunction.None)
                                continue;

                            switch (mapping.Function)
                            {
                                case ZwiftFunction.SteerLeft: left = Math.Max(left, value); break;
                                case ZwiftFunction.SteerRight: right = Math.Max(right, value); break;
                                case ZwiftFunction.Brake: brake = Math.Max(brake, value); break;
                                default: pressed.Add(mapping.Function); break;
                            }
                        }
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        var state = new OutputState(pressed, Math.Clamp(right - left, -1.0, 1.0), Math.Clamp(brake, 0.0, 1.0), controllerData.BatteryPercent);
                        await _outputService.UpdateAsync(state);
                    }
                }
                await Task.Delay(10, cancellationToken); // Adjust delay based on needs
            }
        }
        #endregion
    }
}
