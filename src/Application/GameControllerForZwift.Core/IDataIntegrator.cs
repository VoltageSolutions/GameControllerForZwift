using GameControllerForZwift.Core.Mapping;
using GameControllerForZwift.Core.Controller;
using System.Collections.Concurrent;

namespace GameControllerForZwift.Core
{
    public interface IDataIntegrator
    {
        /// <summary>
        /// Event that is raised when input is polled.
        /// </summary>
        event EventHandler<InputStateChangedEventArgs> InputPolled;

        /// <summary>
        /// Gets the concurrent queue of controller data.
        /// </summary>
        ConcurrentQueue<ControllerData> DataQueue { get; }

        /// <summary>
        /// Retrieves the available controllers.
        /// </summary>
        IEnumerable<IController> GetControllers();

        /// <summary>
        /// Starts processing input and output for the specified controller.
        /// </summary>
        /// <param name="controller">The controller to process.</param>
        void StartProcessing(IController controller);

        /// <summary>
        /// Stops processing input and output.
        /// </summary>
        void StopProcessing();

        /// <summary>
        /// Processes the provided controller data.
        /// </summary>
        /// <param name="controllerData">The controller data to process.</param>
        void ProcessControllerData(ControllerData controllerData);

        public void UpdateMapping(InputMapping mapping);
    }
}
