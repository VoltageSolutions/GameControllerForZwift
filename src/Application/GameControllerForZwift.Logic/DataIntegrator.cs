using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Mapping;
using System.Collections.Concurrent;

namespace GameControllerForZwift.Logic
{
    public class DataIntegrator : IDataIntegrator
    {
        #region Fields
        private readonly IInputService _inputService;
        private readonly IOutputService _outputService;
        private readonly IControllerProfileService _controllerProfileService;
        public ConcurrentQueue<ControllerData> DataQueue => _dataQueue;
        private readonly ConcurrentQueue<ControllerData> _dataQueue = new ConcurrentQueue<ControllerData>();
        private readonly int _maxQueueSize;
        private CancellationTokenSource _cts;// = new CancellationTokenSource();
        public event EventHandler<InputStateChangedEventArgs> InputPolled;
        private ControllerData? _lastDataState;
        #endregion

        #region Constructor
        public DataIntegrator(IInputService inputService, IOutputService outputService, IControllerProfileService controllerProfileService, int maxQueueSize = 10)
        {
            _inputService = inputService;
            _outputService = outputService;
            _controllerProfileService = controllerProfileService;
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
                originalMapping.PlayerView = mapping.PlayerView;
                originalMapping.RiderAction = mapping.RiderAction;
            }
            else
            {
                profile.Mappings.Add(mapping);
            }
        }

        public async Task PerformActionAsync(ControllerInput controllerInput)
        {
            // this is not a good long-term way to get a profile.
            var profile = _controllerProfileService.Profiles.Profiles.FirstOrDefault();
            var mapping = profile.Mappings.Where(m => m.Input == controllerInput).FirstOrDefault();
            if (null != mapping)
            {
                await _outputService.PerformActionAsync(mapping.Function, mapping.PlayerView ?? ZwiftPlayerView.Default, mapping.RiderAction ?? ZwiftRiderAction.RideOn);
            }
        }

        public async Task WriteDataAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                while (_dataQueue.TryDequeue(out ControllerData controllerData))
                {
                    InputPolled?.Invoke(this, new InputStateChangedEventArgs(controllerData));

                    // this is not a good long-term way to get a profile, but it's a start
                    //var profile = _controllerProfileService.Profiles.Profiles.FirstOrDefault();

                    if (controllerData.A)
                        await PerformActionAsync(ControllerInput.A);
                    if (controllerData.B)
                        await PerformActionAsync(ControllerInput.B);
                    if (controllerData.X)
                        await PerformActionAsync(ControllerInput.X);
                    if (controllerData.Y)
                        await PerformActionAsync(ControllerInput.Y);
                    if (controllerData.Menu)
                        await PerformActionAsync(ControllerInput.Menu);
                    if (controllerData.View)
                        await PerformActionAsync(ControllerInput.View);
                    if (controllerData.DPad_Up)
                        await PerformActionAsync(ControllerInput.DPad_Up);
                    if (controllerData.DPad_Down)
                        await PerformActionAsync(ControllerInput.DPad_Down);
                    if (controllerData.DPad_Left)
                        await PerformActionAsync(ControllerInput.DPad_Left);
                    if (controllerData.DPad_Right)
                        await PerformActionAsync(ControllerInput.DPad_Right);
                    if (controllerData.LeftThumbstick_Click)
                        await PerformActionAsync(ControllerInput.LeftThumbstick_Click);
                    if (controllerData.LeftStick_TiltUp)
                        await PerformActionAsync(ControllerInput.LeftThumbstick_TiltUp);
                    if (controllerData.LeftStick_TiltDown)
                        await PerformActionAsync(ControllerInput.LeftThumbstick_TiltDown);
                    if (controllerData.LeftStick_TiltLeft)
                        await PerformActionAsync(ControllerInput.LeftThumbstick_TiltLeft);
                    if (controllerData.RightThumbstick_Click)
                        await PerformActionAsync(ControllerInput.RightThumbstick_Click);
                    if (controllerData.LeftStick_TiltRight)
                        await PerformActionAsync(ControllerInput.LeftThumbstick_TiltRight);
                    if (controllerData.LeftBumper)
                        await PerformActionAsync(ControllerInput.LeftBumper);
                    if (controllerData.RightBumper)
                        await PerformActionAsync(ControllerInput.RightBumper);
                }
                await Task.Delay(10, cancellationToken); // Adjust delay based on needs
            }
        }
        #endregion
    }
}
