using GameControllerForZwift.Core;
using System.Collections.Concurrent;
using System.Reflection.Metadata;

namespace GameControllerForZwift.Logic
{
    public class DataIntegrator
    {
        private IInputService _inputService;
        private readonly ConcurrentQueue<ControllerData> _dataQueue = new ConcurrentQueue<ControllerData>();
        private readonly int _maxQueueSize;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        // todo - pass in required interfaces for reader and writer
        public DataIntegrator(IInputService inputService, int maxQueueSize = 100)
        {
            _inputService = inputService;
            _maxQueueSize = maxQueueSize;
        }

        public ConcurrentQueue<ControllerData> DataQueue => _dataQueue;

        public void StartProcessing()
        {
            //ReadDataAsync(_cts.Token);
            _ = Task.Run(() => ReadDataAsync(_cts.Token));
            //_ = Task.Run(() => WriteDataAsync(_cts.Token));
        }

        public void StopProcessing() => _cts.Cancel();

        // this is a test method to help get things going
        public ControllerData ReadData(IController controller)
        {
            //// for testing only
            //var controllers = _inputService.GetControllers();
            //IController controller = controllers.First();

            return controller.ReadData();
        }

        private async Task ReadDataAsync(CancellationToken cancellationToken)
        {
            // for testing only
            var controllers = _inputService.GetControllers();

            if (controllers.Any())
            {
                IController controller = controllers.First();

                if (controller != null)
                {

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var data = controller.ReadData();

                        // Add data to the queue, discarding the oldest if the queue is full
                        if (_dataQueue.Count >= _maxQueueSize)
                        {
                            _dataQueue.TryDequeue(out _); // Remove the oldest data
                        }
                        _dataQueue.Enqueue(data);

                        await Task.Delay(500, cancellationToken);
                    }
                }
            }
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
    }
}
